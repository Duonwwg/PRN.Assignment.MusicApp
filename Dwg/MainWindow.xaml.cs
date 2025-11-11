using MaterialDesignThemes.Wpf;
using NAudio.Wave;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Dwg
{
    public partial class MainWindow : Window
    {

        private readonly ObservableCollection<string> playlist = new();
        private readonly ObservableCollection<string> filePaths = new();
        private readonly DispatcherTimer timer = new();
        private WaveOutEvent? outputDevice;
        private AudioFileReader? audioFile;
        private bool isPlaying = false;

        public MainWindow()
        {
            InitializeComponent();
            PlaylistBox.ItemsSource = playlist;

            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += Timer_Tick;

            AllowDrop = true;
            Drop += Window_Drop;
        }

        private void OpenBtn_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "Audio Files|*.mp3;*.wav;*.wma;*.m4a;*.aac;*.flac"
            };

            if (dlg.ShowDialog() == true)
            {
                playlist.Clear();
                filePaths.Clear();

                foreach (string file in dlg.FileNames)
                {
                    playlist.Add(Path.GetFileName(file));
                    filePaths.Add(file);
                }

                if (playlist.Count > 0)
                    PlaylistBox.SelectedIndex = 0;
            }
        }

        private void PlaylistBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlaylistBox.SelectedIndex >= 0)
            {
                PlaySong(filePaths[PlaylistBox.SelectedIndex]);
            }
        }

        private void PlaySong(string path)
        {
            Stop(); // Dừng bài cũ

            audioFile = new AudioFileReader(path);
            outputDevice = new WaveOutEvent();
            outputDevice.Init(audioFile);
            outputDevice.PlaybackStopped += (s, e) => Dispatcher.Invoke(OnSongEnded);

            SongTitle.Text = Path.GetFileNameWithoutExtension(path);
            outputDevice.Play();
            isPlaying = true;
            timer.Start();
            PlayIcon.Kind = PackIconKind.Pause;
        }

        private void PlayBtn_Click(object sender, RoutedEventArgs e)
        {
            if (audioFile == null || PlaylistBox.SelectedIndex == -1) return;

            if (isPlaying)
            {
                outputDevice?.Pause();
                PlayIcon.Kind = PackIconKind.Play;
            }
            else
            {
                outputDevice?.Play();
                PlayIcon.Kind = PackIconKind.Pause;
            }
            isPlaying = !isPlaying;
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            Stop();
            ProgressBar.Value = 0;
            TimeLabel.Text = "00:00 / 00:00";
            PlayIcon.Kind = PackIconKind.Play;
            isPlaying = false; 
        }

        private void Stop()
        {
            outputDevice?.Stop();
            audioFile?.Dispose();
            outputDevice?.Dispose();
            outputDevice = null;
            audioFile = null;
            timer.Stop();
            isPlaying = false;
        }

        private void PrevBtn_Click(object sender, RoutedEventArgs e)
        {
            if (PlaylistBox.SelectedIndex > 0)
                PlaylistBox.SelectedIndex--;
        }

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            if (PlaylistBox.SelectedIndex < playlist.Count - 1)
                PlaylistBox.SelectedIndex++;
            else
                StopBtn_Click(null, null);
        }

        private void OnSongEnded()
        {
            if (PlaylistBox.SelectedIndex < playlist.Count - 1)
                PlaylistBox.SelectedIndex++;
            else
                StopBtn_Click(null, null);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (audioFile != null && outputDevice != null)
            {
                var current = audioFile.CurrentTime.TotalSeconds;
                var total = audioFile.TotalTime.TotalSeconds;
                if (total > 0)
                {
                    ProgressBar.Value = (current / total) * 100;
                    TimeLabel.Text = $"{FormatTime(current)} / {FormatTime(total)}";
                }
            }
        }

        private static string FormatTime(double seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            return $"{ts.Minutes:D2}:{ts.Seconds:D2}";
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (audioFile != null)
            {
                audioFile.Volume = (float)(VolumeSlider.Value / 100.0);
            }
        }

        private void RemoveSong_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string fileName) return;

            int index = playlist.IndexOf(fileName);
            if (index < 0) return;

            bool wasPlaying = index == PlaylistBox.SelectedIndex;

            playlist.RemoveAt(index);
            filePaths.RemoveAt(index);

            if (wasPlaying)
            {
                Stop();
                PlaylistBox.SelectedIndex = -1;
            }
            else if (PlaylistBox.SelectedIndex > index)
            {
                PlaylistBox.SelectedIndex--;
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                playlist.Clear();
                filePaths.Clear();

                foreach (string file in files)
                {
                    if (IsAudioFile(file))
                    {
                        playlist.Add(Path.GetFileName(file));
                        filePaths.Add(file);
                    }
                }
                if (playlist.Count > 0) PlaylistBox.SelectedIndex = 0;
            }
        }

        private static bool IsAudioFile(string file)
        {
            var ext = Path.GetExtension(file).ToLower();
            return ext is ".mp3" or ".wav" or ".wma" or ".m4a" or ".aac" or ".flac";
        }

        protected override void OnClosed(EventArgs e)
        {
            Stop();
            base.OnClosed(e);
        }
    }
}