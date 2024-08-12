using System;
using System.Threading.Tasks;
using System.Windows;
using WordFinderLib;

namespace WordFinderApp
{
    public partial class MainWindow : Window
    {
        private WordFinder wordFinder;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(WordLengthTextBox.Text, out int wordLength) || wordLength <= 0)
            {
                MessageBox.Show("Please enter a valid word length.");
                return;
            }

            if (!int.TryParse(NumberOfWordsTextBox.Text, out int numberOfWords) || numberOfWords <= 0)
            {
                MessageBox.Show("Please enter a valid number of words.");
                return;
            }

            string filePath = FileNameTextBox.Text;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                MessageBox.Show("Please enter a valid file name.");
                return;
            }

            StartButton.IsEnabled = false;
            ProgressBar.Value = 0;

            wordFinder = new WordFinder(wordLength, numberOfWords);

            wordFinder.ProgressChanged += UpdateProgress;
            wordFinder.TotalFoundChanged += ShowTotalFound;

            try
            {
                await Task.Run(() => wordFinder.Execute(filePath + ".txt"));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }

            StartButton.IsEnabled = true;
        }

        private void UpdateProgress(int progress, int total)
        {
            Dispatcher.Invoke(() => ProgressBar.Value = (double)progress / total * 100);
        }

        private void ShowTotalFound(int totalFound)
        {
            Dispatcher.Invoke(() => MessageBox.Show($"Total found: {totalFound}"));
        }
    }
}
