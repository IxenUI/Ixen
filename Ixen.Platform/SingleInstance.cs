using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ixen.Platform
{
    public sealed class SingleInstance : IDisposable
    {
        private const int CONNECT_TIMEOUT = 3000;
        private const int MAX_ARGUMENTS = 4096;
        private const int ASFW_ANY = -1;

        private readonly Mutex _mutex;
        private readonly string _pipe;
        private readonly Action<string[]> _received;
        private readonly CancellationTokenSource _stop = new CancellationTokenSource();
        private readonly Task _listening;

        private SingleInstance(Mutex mutex, string pipe, Action<string[]> received)
        {
            _mutex = mutex;
            _pipe = pipe;
            _received = received;
            _listening = Task.Run(Listen);
        }

        public static SingleInstance Claim(string name, string[] args, Action<string[]> received)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("an instance needs a name that is the same for every launch", nameof(name));
            }

            string key = KeyOf(name);
            var mutex = new Mutex(false, "Local\\" + key);
            bool owned;

            try
            {
                owned = mutex.WaitOne(0);
            }
            catch (AbandonedMutexException)
            {
                owned = true;
            }

            if (owned)
            {
                return new SingleInstance(mutex, key, received);
            }

            mutex.Dispose();
            Forward(key, args ?? Array.Empty<string>());

            return null;
        }

        internal static string KeyOf(string name)
        {
            var builder = new StringBuilder("ixen-");

            foreach (char c in name)
            {
                builder.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
            }

            uint hash = 2166136261;

            foreach (char c in Environment.UserName ?? string.Empty)
            {
                hash = (hash ^ c) * 16777619;
            }

            return builder.Append('-').Append(hash.ToString("x8")).ToString();
        }

        private static void Forward(string key, string[] args)
        {
            if (OperatingSystem.IsWindows())
            {
                AllowSetForegroundWindow(ASFW_ANY);
            }

            try
            {
                using (var client = new NamedPipeClientStream(".", key, PipeDirection.InOut, PipeOptions.CurrentUserOnly))
                {
                    client.Connect(CONNECT_TIMEOUT);

                    using (var writer = new BinaryWriter(client, Encoding.UTF8, true))
                    {
                        writer.Write(args.Length);

                        foreach (string argument in args)
                        {
                            writer.Write(argument ?? string.Empty);
                        }

                        writer.Flush();
                    }

                    client.ReadByte();
                }
            }
            catch (TimeoutException)
            {
            }
            catch (IOException)
            {
            }
        }

        private NamedPipeServerStream Open()
            => new NamedPipeServerStream(_pipe, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

        private async Task Listen()
        {
            NamedPipeServerStream waiting = null;

            try
            {
                waiting = Open();

                while (!_stop.IsCancellationRequested)
                {
                    await waiting.WaitForConnectionAsync(_stop.Token).ConfigureAwait(false);

                    NamedPipeServerStream connected = waiting;

                    waiting = Open();

                    using (connected)
                    {
                        Answer(connected);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (IOException)
            {
            }
            finally
            {
                waiting?.Dispose();
            }
        }

        private void Answer(Stream connected)
        {
            try
            {
                string[] args = Read(connected);

                if (args != null)
                {
                    Deliver(args);
                }

                connected.WriteByte(1);
                connected.Flush();
            }
            catch (IOException)
            {
            }
        }

        private static string[] Read(Stream stream)
        {
            try
            {
                using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
                {
                    int count = reader.ReadInt32();

                    if (count < 0 || count > MAX_ARGUMENTS)
                    {
                        return null;
                    }

                    var args = new string[count];

                    for (int i = 0; i < count; i++)
                    {
                        args[i] = reader.ReadString();
                    }

                    return args;
                }
            }
            catch (EndOfStreamException)
            {
                return null;
            }
        }

        private void Deliver(string[] args)
        {
            try
            {
                _received?.Invoke(args);
            }
            catch (Exception)
            {
            }
        }

        public void Dispose()
        {
            _stop.Cancel();

            try
            {
                _listening.Wait(CONNECT_TIMEOUT);
            }
            catch (AggregateException)
            {
            }

            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
            }

            _mutex.Dispose();
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllowSetForegroundWindow(int processId);
    }
}
