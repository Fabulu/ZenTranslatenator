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

namespace ZenTranslatenator
{
    public partial class MainWindow : Window
    {
        private List<string> _textChunks;
        private int _currentChunkIndex;

        public MainWindow()
        {
            InitializeComponent();
            _textChunks = new List<string>();
            _currentChunkIndex = 0;
        }

        private void Translate(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(APIKey.Text))
            {
                MessageBox.Show("Please provide your OpenAI API Key!");
                return;
            }

            // You'll need to implement the OpenAI API call here.
            // Use the HttpClient or another similar library to POST to the API endpoint with the required data.
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
            StartEnsoAnimation();
        }

        private void NextChunk_Click(object sender, RoutedEventArgs e)
        {
            if (_currentChunkIndex < _textChunks.Count - 1)
            {
                _currentChunkIndex++;
                ChunkPreview.Text = _textChunks[_currentChunkIndex];
                Chunks.Content = "Chunk " + (_currentChunkIndex + 1) + " of " + _textChunks.Count;
            }
        }

        private void PreviousChunk_Click(object sender, RoutedEventArgs e)
        {
            if (_currentChunkIndex > 0)
            {
                _currentChunkIndex--;
                ChunkPreview.Text = _textChunks[_currentChunkIndex];
                Chunks.Content = "Chunk " + (_currentChunkIndex + 1) + " of " + _textChunks.Count;
            }
        }

        private void StartEnsoAnimation()
        {
            RotateTransform rotateTransform = new RotateTransform();
            EnsoImage.RenderTransform = rotateTransform;
            EnsoImage.RenderTransformOrigin = new Point(0.5, 0.5);
            DoubleAnimation animation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = new Duration(TimeSpan.FromSeconds(5)),
                RepeatBehavior = new RepeatBehavior(1)
            };

            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, animation);
        }

    }
}