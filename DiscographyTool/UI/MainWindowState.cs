using System.Collections.Generic;
using DiscographyTool.Core;

namespace DiscographyTool.UI
{
    public class MainWindowState
    {
        public string SelectedFolder { get; set; }

        public string Country { get; set; }

        public string ArtistImageUrl { get; set; }

        public Dictionary<string, string> AlbumImages { get; private set; }

        public Id3Discography Discography { get; set; }

        // Флаг: не загружать изображения
        public bool SkipImages { get; set; }

        public MainWindowState()
        {
            SelectedFolder = string.Empty;
            Country = string.Empty;
            ArtistImageUrl = string.Empty;
            AlbumImages = new Dictionary<string, string>();
            Discography = null;
            SkipImages = false;
        }
    }
}
