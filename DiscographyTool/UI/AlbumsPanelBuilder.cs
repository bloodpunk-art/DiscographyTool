using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace DiscographyTool.UI
{
    public class AlbumsPanelBuilder
    {
        private readonly StackPanel _panel;

        public Dictionary<string, List<(string Album, TextBox Entry)>> Entries { get; }
            = new Dictionary<string, List<(string Album, TextBox Entry)>>();

        public AlbumsPanelBuilder(StackPanel panel)
        {
            _panel = panel;
        }

        public void Clear()
        {
            _panel.Children.Clear();
            Entries.Clear();
        }

        public void AddArtistImageField(TextBox artistImageTextBox)
        {
            _panel.Children.Add(new TextBlock
            {
                Text = "Ссылка на фото исполнителя (опционально):",
                Margin = new Thickness(0, 0, 0, 5)
            });

            _panel.Children.Add(artistImageTextBox);
            _panel.Children.Add(new Separator { Margin = new Thickness(0, 10, 0, 10) });
        }

        public void AddCategory(string category, IEnumerable<string> albumDirs)
        {
            _panel.Children.Add(new TextBlock
            {
                Text = category,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 10)
            });

            foreach (var dir in albumDirs)
            {
                var albumName = Path.GetFileName(dir);

                _panel.Children.Add(new TextBlock { Text = albumName });

                var entry = new TextBox();
                _panel.Children.Add(entry);

                if (!Entries.ContainsKey(category))
                    Entries[category] = new List<(string Album, TextBox Entry)>();

                Entries[category].Add((albumName, entry));
            }

            _panel.Children.Add(new Separator { Margin = new Thickness(0, 10, 0, 10) });
        }
    }
}
