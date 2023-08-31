using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using Newtonsoft.Json;
using OpenAI_API;
using OpenAI_API.Completions;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using System.Threading.Tasks;
using PdfSharp.Drawing.Layout;

namespace ZenTranslatenator
{
    public partial class MainWindow : Window
    {
        private List<string> _textChunks;
        private int _currentChunkIndex;
        private const int MaxCharacterCount = 500;  // Set this according to the desired character limit


        public MainWindow()
        {
            InitializeComponent();
            _textChunks = new List<string>();
            _currentChunkIndex = 0;
        }

        private async void Translate(object sender, RoutedEventArgs e)
        {

            if (String.IsNullOrEmpty(APIKey.Text)){
                MessageBox.Show("Please provide your OpenAI API Key!");
                return;
            }
            // For the sake of simplicity, assuming the chunks are small enough to be translated within the API limit
            // You might want to further chunkify them based on token count if necessary

            List<string> originalChunks = _textChunks; // Assuming you have this method already or similar
            List<string> translatedChunks = new List<string>();

            foreach (string chunk in originalChunks)
            {
                string translatedText = await TranslateWithOpenAI(chunk);
                translatedChunks.Add(translatedText);
            }

            CreatePdf(originalChunks, translatedChunks, OutputFolder.Text);
            StartEnsoAnimation(1);
            
        }

        private async Task<string> TranslateWithOpenAI(string inputText)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {APIKey.Text}");

                var messages = new List<object>
        {
            new { role = "system", content = $"{AIPrimer.Text} {AdditionalNotes.Text}" },
            new { role = "user", content = $"Translate: {inputText}" }
        };
                string selectedModel = ((ComboBoxItem)GptVersionComboBox.SelectedItem).Content.ToString();
                var payload = new
                {
                    model = selectedModel,
                    messages = messages
                };
                StartEnsoAnimation(60000);

                try
                {
                    var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions",
                        new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));

                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        dynamic parsedResponse = JsonConvert.DeserializeObject(jsonResponse);
                        return parsedResponse.choices[0].message.content;
                    }
                    else
                    {
                        dynamic parsedResponse = JsonConvert.DeserializeObject(jsonResponse);
                        string errorMsg = parsedResponse.error.message ?? "Unknown error";
                        return $"Error occurred during translation: {errorMsg} (Status code: {response.StatusCode})";
                    }
                }
                catch (HttpRequestException ex)
                {
                    // This will catch any errors related to network or connection
                    return $"Error occurred during API call: {ex.Message}";
                }
                catch (Exception ex)
                {
                    // General error catch to ensure application does not crash
                    return $"Unexpected error: {ex.Message}";
                }
            }
        }

        private void CreatePdf(IEnumerable<string> originalChunks, IEnumerable<string> translatedChunks, string outputPath)
        {
            PdfDocument document = new PdfDocument();

            // Check if the directory exists and create it if it doesn't
            var directory = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Append numbers to the filename if it already exists
            var fileBase = Path.GetFileNameWithoutExtension(outputPath);
            var directoryPath = Path.GetDirectoryName(outputPath);
            var extension = Path.GetExtension(outputPath);
            int counter = 1;

            while (File.Exists(outputPath))
            {
                outputPath = Path.Combine(directoryPath, $"{fileBase} ({counter++}){extension}");
            }

            IEnumerator<string> originalEnumerator = originalChunks.GetEnumerator();
            IEnumerator<string> translatedEnumerator = translatedChunks.GetEnumerator();


            while (originalEnumerator.MoveNext() && translatedEnumerator.MoveNext())
            {

                int iterator = 0;
                StringBuilder stringBuilder = new StringBuilder();

                foreach (char item in  originalEnumerator.Current)
                {
                    stringBuilder.Append(item);
                    iterator += 1;

                    if (iterator > 30)
                    {
                        if (item == '。')
                        {
                            stringBuilder.Append("\r\n");
                            iterator = 0;
                        }
                    }
                    else if (iterator > 40)
                    {
                        stringBuilder.Append("\r\n");
                        iterator = 0;
                    }
                }

                // Create a new page and draw the Chinese text
                PdfPage page1 = document.AddPage();
                XGraphics gfx1 = XGraphics.FromPdfPage(page1);
                XFont font1 = new XFont("Arial Unicode MS", 12, XFontStyle.Regular);
                XRect xRect1 = new XRect(50, 50, page1.Width - 100, page1.Height - 100);
                XTextFormatter tf1 = new XTextFormatter(gfx1);
                gfx1.DrawRectangle(XBrushes.SeaShell, xRect1);
                tf1.DrawString(stringBuilder.ToString(), font1, XBrushes.Black, xRect1, XStringFormats.TopLeft);

                // Create another new page and draw the translated text
                PdfPage page2 = document.AddPage();
                XGraphics gfx2 = XGraphics.FromPdfPage(page2);
                XFont font2 = new XFont("Arial Unicode MS", 12, XFontStyle.Regular);
                XRect xRect2 = new XRect(50, 50, page1.Width - 100, page1.Height - 100);
                XTextFormatter tf2 = new XTextFormatter(gfx2);
                gfx2.DrawRectangle(XBrushes.SeaShell, xRect2);
                tf2.DrawString(translatedEnumerator.Current, font2, XBrushes.Black, xRect2, XStringFormats.TopLeft);
            }

            document.Save(outputPath);
        }

        private async void CallOpenAIAPI(string text)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {APIKey.Text}");

                var payload = new
                {
                    prompt = $"{AIPrimer.Text}\n\n{AdditionalNotes.Text}\n\n{text}",
                    max_tokens = 4000
                };

                var response = await httpClient.PostAsync("https://api.openai.com/v1/engines/davinci/completions",
                    new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));

                var result = await response.Content.ReadAsStringAsync();

                // Process result here, and maybe add to the PDF (this would require additional libraries for PDF generation)
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // You can add logic here if you want real-time validation or some other action
        }

        private void PickFolder_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PDF files (*.pdf)|*.pdf";
            saveFileDialog.DefaultExt = ".pdf";
            saveFileDialog.AddExtension = true;

            if (saveFileDialog.ShowDialog() == true)
            {
                OutputFolder.Text = saveFileDialog.FileName;
            }
        }

        private void ChunkifyText_Click(object sender, RoutedEventArgs e)
        {

            //Initialize text chunk variable

            _textChunks.Clear();

            // This will break up the input text into manageable chunks while trying to preserve context.
            // The 4000 tokens limit is abstract. In this example, I'll use character count as an approximation.
            string text = TextToTranslate.Text;

            int approximateChunkSize = 500;  // You might need to adjust this.

            for (int i = 0; i < text.Length; i += approximateChunkSize)
            {
                _textChunks.Add(text.Substring(i, Math.Min(approximateChunkSize, text.Length - i)));
            }

            if (_textChunks.Count > 0)
            {
                ChunkPreview.Text = _textChunks[0];
            }
            Chunks.Content = "Chunk 1 of " + _textChunks.Count;
            StartEnsoAnimation(1);
        }


        private void NextChunk_Click(object sender, RoutedEventArgs e)
        {
            if (_currentChunkIndex < _textChunks.Count - 1)
            {
                _textChunks[_currentChunkIndex] = ChunkPreview.Text;
                _currentChunkIndex++;
                ChunkPreview.Text = _textChunks[_currentChunkIndex];
                Chunks.Content = "Chunk " + (_currentChunkIndex + 1) + " of " + _textChunks.Count;
            }
        }

        private void PreviousChunk_Click(object sender, RoutedEventArgs e)
        {
            if (_currentChunkIndex > 0)
            {
                _textChunks[_currentChunkIndex] = ChunkPreview.Text;
                _currentChunkIndex--;
                ChunkPreview.Text = _textChunks[_currentChunkIndex];
                Chunks.Content = "Chunk " + (_currentChunkIndex + 1) + " of " + _textChunks.Count;
            }
        }

        private void StartEnsoAnimation(int numberOfRotations)
        {
            RotateTransform rotateTransform = new RotateTransform();
            EnsoImage.RenderTransform = rotateTransform;
            EnsoImage.RenderTransformOrigin = new Point(0.5, 0.5);
            DoubleAnimation animation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = new Duration(TimeSpan.FromSeconds(1)),
                RepeatBehavior = new RepeatBehavior(numberOfRotations)
            };

            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, animation);
        }


    }
}