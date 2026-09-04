using System.IO;
using System.Threading.Tasks;

namespace Ixen.Core
{
    public interface IAsyncImageSource : IImageSource
    {
        Task<Stream> OpenAsync(string name);
    }
}
