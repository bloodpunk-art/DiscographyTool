using System.Collections.Generic;

namespace DiscographyTool.Core
{
    public static class DiscographyService
    {
        public static string Generate(
            string rootFolder,
            string country,
            string artistImageUrl,
            string artistLogoUrl,
            Dictionary<string, string> albumImages,
            AudioFormat format,
            ScanStructure scanResult,
            TracklistLayout layout)
        {
            return DiscographyGenerator.Generate(
                rootFolder,
                scanResult.DetectedArtist,
                scanResult.DetectedGenre,
                country,
                artistImageUrl,
                artistLogoUrl,
                albumImages,
                format,
                scanResult,
                layout
            );
        }
    }
}
