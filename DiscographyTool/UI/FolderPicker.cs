using Microsoft.Win32;
using System.IO;

namespace DiscographyTool.UI
{
    public static class FolderPicker
    {
        public static string? PickFolder()
        {
            var dlg = new OpenFileDialog
            {
                CheckFileExists = false,
                FileName = "Выбрать папку"
            };

            if (dlg.ShowDialog() != true)
                return null;

            return Path.GetDirectoryName(dlg.FileName);
        }
    }
}
