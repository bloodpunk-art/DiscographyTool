using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DiscographyTool.Core;
using DiscographyTool.Covers;
using DiscographyTool.Networking;
using DiscographyTool.UI;
using Microsoft.Win32;

namespace DiscographyTool
{
    public partial class MainWindow : Window
    {
        private string _selectedFolder;
        private ScanStructure _scanResult;

        private readonly Dictionary<string, List<Tuple<string, TextBox>>> _categoryEntries =
            new Dictionary<string, List<Tuple<string, TextBox>>>();

        private readonly MainWindowState _state;

        public MainWindow()
        {
            InitializeComponent();
            _state = new MainWindowState();
            DataContext = _state;
        }

        private async void ChooseFolder_Click(object sender, RoutedEventArgs e)
        {
            GenerateButton.IsEnabled = false;

            var dlg = new OpenFileDialog
            {
                CheckFileExists = false,
                FileName = "Выбрать папку"
            };

            if (dlg.ShowDialog() != true)
                return;

            _selectedFolder = Path.GetDirectoryName(dlg.FileName);
            if (string.IsNullOrEmpty(_selectedFolder))
                return;

            FolderPathText.Text = _selectedFolder;

            bool skipImages = _state.SkipImages;

            if (!skipImages)
                CoverProcessor.Process(_selectedFolder);

            AlbumsPanel.Children.Clear();
            _categoryEntries.Clear();

            ArtistImageTextBox.Text = "";
            ArtistLogoTextBox.Text = "";

            _scanResult = RecursiveScanner.Scan(_selectedFolder);

            if (!string.IsNullOrWhiteSpace(_scanResult.DetectedArtist))
                Title = _scanResult.DetectedArtist + " — Генератор дискографии";

            if (!skipImages && !string.IsNullOrWhiteSpace(ImageBanSession.Token))
            {
                var result = await ArtistImageService.TryUploadAsync(_selectedFolder);
                if (!string.IsNullOrWhiteSpace(result.Item1))
                    ArtistImageTextBox.Text = result.Item1;
                if (!string.IsNullOrWhiteSpace(result.Item2))
                    ArtistLogoTextBox.Text = result.Item2;
            }

            bool isFlatStructure = !_scanResult.Categories.Keys.Any(k =>
                k.Equals("Albums", StringComparison.OrdinalIgnoreCase) ||
                k.Equals("EP's", StringComparison.OrdinalIgnoreCase) ||
                k.Equals("Singles", StringComparison.OrdinalIgnoreCase) ||
                k.Equals("Compilation", StringComparison.OrdinalIgnoreCase) ||
                k.Equals("Other", StringComparison.OrdinalIgnoreCase));

            Dictionary<string, List<ScannedAlbum>> displayCategories =
                isFlatStructure
                    ? new Dictionary<string, List<ScannedAlbum>>
                    {
                        { "Albums", _scanResult.Categories.Values.SelectMany(v => v).ToList() }
                    }
                    : _scanResult.Categories;

            foreach (var category in displayCategories)
            {
                AddCategoryHeader(category.Key);

                foreach (var album in category.Value.OrderBy(a => a.Year))
                {
                    string albumKey = album.AlbumPath;
                    string displayName = album.Year > 0
                        ? album.Year + " - " + album.AlbumName
                        : album.AlbumName;

                    AlbumsPanel.Children.Add(new TextBlock
                    {
                        Text = displayName,
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(0, 5, 0, 2)
                    });

                    var entry = new TextBox
                    {
                        Text = "",
                        Margin = new Thickness(0, 0, 0, 5)
                    };

                    AlbumsPanel.Children.Add(entry);

                    if (!_categoryEntries.ContainsKey(category.Key))
                        _categoryEntries[category.Key] = new List<Tuple<string, TextBox>>();

                    _categoryEntries[category.Key].Add(
                        Tuple.Create(albumKey, entry)
                    );
                }

                AlbumsPanel.Children.Add(new Separator
                {
                    Margin = new Thickness(0, 10, 0, 10)
                });
            }

            if (!skipImages)
                await UploadCoversAsync();

            GenerateButton.IsEnabled = true;
        }

        private async Task UploadCoversAsync()
        {
            if (string.IsNullOrWhiteSpace(ImageBanSession.Token) || _scanResult == null)
                return;

            string coversDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DiscographyTool",
                "covers"
            );

            if (!Directory.Exists(coversDir))
                return;

            foreach (var cat in _categoryEntries)
            {
                foreach (var pair in cat.Value)
                {
                    string albumKey = pair.Item1;
                    TextBox entry = pair.Item2;

                    string folderName = Path.GetFileName(albumKey);

                    var cover = Directory.GetFiles(coversDir)
                        .FirstOrDefault(f =>
                            Path.GetFileNameWithoutExtension(f)
                                .Equals(folderName, StringComparison.OrdinalIgnoreCase));

                    if (cover == null)
                        continue;

                    entry.Text = "Загрузка...";
                    string uploaded = await ImageBanUploader.UploadAsync(cover);
                    entry.Text = uploaded ?? "";
                }
            }
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFolder) || _scanResult == null)
                return;

            var albumImages = new Dictionary<string, string>();

            foreach (var cat in _categoryEntries)
            {
                foreach (var pair in cat.Value)
                    albumImages[pair.Item1] = pair.Item2.Text.Trim();
            }

            TracklistLayout layout =
                LayoutClassicRadio.IsChecked == true
                    ? TracklistLayout.Classic
                    : TracklistLayout.Center;

            string text = DiscographyService.Generate(
                _selectedFolder,
                CountryTextBox.Text.Trim(),
                ArtistImageTextBox.Text.Trim(),
                ArtistLogoTextBox.Text.Trim(),
                albumImages,
                DetectFormat(_selectedFolder),
                _scanResult,
                layout
            );

            string tempFile = Path.Combine(
                Path.GetTempPath(),
                "discography_" + Guid.NewGuid().ToString("N") + ".txt"
            );

            File.WriteAllText(tempFile, text);

            Process.Start(new ProcessStartInfo
            {
                FileName = tempFile,
                UseShellExecute = true
            });

            // ===== УДАЛЕНИЕ папки covers ПОСЛЕ генерации =====
            DeleteCoversFolder();
        }

        private static void DeleteCoversFolder()
        {
            try
            {
                string coversDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "DiscographyTool",
                    "covers"
                );

                if (Directory.Exists(coversDir))
                    Directory.Delete(coversDir, true);
            }
            catch
            {
                // намеренно игнорируем ошибки
                // (файлы заняты, нет доступа и т.п.)
            }
        }

        private static AudioFormat DetectFormat(string root)
        {
            return Directory.GetFiles(root, "*.flac", SearchOption.AllDirectories).Any()
                ? AudioFormat.FLAC
                : AudioFormat.MP3;
        }

        private void AddCategoryHeader(string name)
        {
            AlbumsPanel.Children.Add(new TextBlock
            {
                Text = name,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 10)
            });
        }
    }
}
