using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ixen.Core
{
    public interface IAsyncImageSource : IImageSource
    {
        Task<Stream> OpenAsync(string name, CancellationToken cancellation);
    }
}
