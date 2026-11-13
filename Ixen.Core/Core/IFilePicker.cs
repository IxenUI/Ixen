using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ixen.Core
{
    public interface IFilePicker
    {
        Task<IReadOnlyList<string>> OpenAsync(FilePickerOptions options);

        Task<string> OpenFolderAsync(FilePickerOptions options);

        Task<string> SaveAsync(FilePickerOptions options);
    }
}
