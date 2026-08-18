using System.IO;

namespace Ixen.Core
{
    public interface IImageSource
    {
        Stream Open(string name);
    }
}
