using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace wpf_image_filter;

public class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private List<string> _imagePaths = new();
    private CancellationTokenSource? _cancellationTokenSource;

    public ObservableCollection<string> AvailableFilters { get; }
    public string SelectedFilter { get; set; } = "Grayscale";
    public ObservableCollection<string> LogMessages { get; }
    public ObservableCollection<ProcessedImage> ProcessedImages { get; }

    private string _selectedFilesText = string.Empty;
    public string SelectedFilesText
    {
        get => _selectedFilesText;
        set { _selectedFilesText = value; OnPropertyChanged(nameof(SelectedFilesText)); }
    }

    private int _progressPercentage;
    public int ProgressPercentage
    {
        get => _progressPercentage;
        set { _progressPercentage = value; OnPropertyChanged(nameof(ProgressPercentage)); }
    }

    private bool _isProcessing;
    public bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            _isProcessing = value;
            OnPropertyChanged(nameof(IsProcessing));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public ICommand LoadImagesCommand { get; }
    public ICommand ProcessImagesCommand { get; }
    public ICommand CancelProcessingCommand { get; }

    public MainViewModel()
    {
        AvailableFilters = ["Grayscale", "Sepia", "Warm", "Cold", "Blur"];
        LogMessages = [];
        ProcessedImages = [];

        LoadImagesCommand = new RelayCommand(_ => LoadImages());
        ProcessImagesCommand = new RelayCommand(async _ => await ProcessImagesAsync(), _ => _imagePaths.Any() && !IsProcessing);
        CancelProcessingCommand = new RelayCommand(_ => CancelProcessing(), _ => IsProcessing);
    }

    private void LoadImages()
    {
        OpenFileDialog openFileDialog = new()
        {
            Multiselect = true,
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            _imagePaths = openFileDialog.FileNames.ToList();
            SelectedFilesText = string.Join("; ", _imagePaths.Select(Path.GetFileName));
            LogMessages.Clear();
            LogMessages.Add($"{_imagePaths.Count} images loaded.");
        }
    }

    private async Task ProcessImagesAsync()
{
    IsProcessing = true;
    ProgressPercentage = 0;
    ProcessedImages.Clear();
    LogMessages.Clear();
    _cancellationTokenSource = new CancellationTokenSource();

    int total = _imagePaths.Count;
    int completed = 0;
    bool wasCancelled = false;

    try
    {
        var tasks = _imagePaths.Select(path => Task.Run(() =>
        {
            string fileName = Path.GetFileName(path);
            try
            {
                _cancellationTokenSource!.Token.ThrowIfCancellationRequested();
                LogSafe($"Processing {fileName}...");

                BitmapImage original = LoadBitmap(path);
                BitmapImage filtered = ApplyFilter(original, SelectedFilter, _cancellationTokenSource.Token);

                string outputPath = Path.Combine(Path.GetDirectoryName(path)!, $"filtered_{fileName}");
                SaveBitmapToFile(filtered, outputPath);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ProcessedImages.Add(new ProcessedImage
                    {
                        FileName = fileName,
                        Thumbnail = filtered,
                        Status = "Done"
                    });
                    LogMessages.Add($"✔️ {fileName} processed.");
                });
            }
            catch (OperationCanceledException)
            {
                wasCancelled = true;
                LogSafe($"⚠️ {fileName} — canceled.");
            }
            catch (Exception ex)
            {
                LogSafe($"❌ Error: {fileName}: {ex.Message}");
            }
            finally
            {
                int progress = (int)(Interlocked.Increment(ref completed) * 100.0 / total);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ProgressPercentage = progress;
                });
            }

        }, _cancellationTokenSource.Token)).ToList();

        await Task.WhenAll(tasks);
    }
    catch (OperationCanceledException)
    {
        wasCancelled = true;
        LogSafe("⛔️ Processing was canceled by the user.");
    }
    finally
    {
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        IsProcessing = false;

        LogMessages.Add(wasCancelled ? "⛔️ Processing was canceled." : "✅ All images processed successfully.");

        if (ProgressPercentage < 100)
            ProgressPercentage = 100;
    }
}
    
    private void CancelProcessing()
    {
        if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested) return;
        _cancellationTokenSource.Cancel();
        LogSafe("⛔️ Cancel requested.");
    }

    private void LogSafe(string message)
    {
        Application.Current.Dispatcher.Invoke(() => LogMessages.Add(message));
    }

    private BitmapImage LoadBitmap(string path)
    {
        using FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        BitmapDecoder decoder = BitmapDecoder.Create(fs, BitmapCreateOptions.IgnoreColorProfile | BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.OnLoad);
        BitmapSource source = decoder.Frames[0];
        source.Freeze();

        using MemoryStream ms = new();
        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(source));
        encoder.Save(ms);
        ms.Position = 0;

        BitmapImage bmp = new();
        bmp.BeginInit();
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.StreamSource = ms;
        bmp.EndInit();
        bmp.Freeze();
        return bmp;
    }


    private BitmapImage ApplyFilter(BitmapImage original, string filter, CancellationToken cancellationToken)

    {
        WriteableBitmap writable = new(original);
        int width = writable.PixelWidth;
        int height = writable.PixelHeight;
        int stride = width * 4;
        byte[] pixels = new byte[height * stride];
        writable.CopyPixels(pixels, stride, 0);

        for (int i = 0; i < pixels.Length; i += 4)
        {
            cancellationToken.ThrowIfCancellationRequested(); // <-- Проверка отмены

            byte b = pixels[i];
            byte g = pixels[i + 1];
            byte r = pixels[i + 2];

            switch (filter)
            {
                case "Grayscale":
                    byte gray = (byte)((r + g + b) / 3);
                    pixels[i] = pixels[i + 1] = pixels[i + 2] = gray;
                    break;
                case "Sepia":
                    pixels[i]     = (byte)Math.Min(255, 0.272 * r + 0.534 * g + 0.131 * b);
                    pixels[i + 1] = (byte)Math.Min(255, 0.349 * r + 0.686 * g + 0.168 * b);
                    pixels[i + 2] = (byte)Math.Min(255, 0.393 * r + 0.769 * g + 0.189 * b);
                    break;
                case "Warm":
                    pixels[i]     = (byte)Math.Min(255, b * 0.9);
                    pixels[i + 2] = (byte)Math.Min(255, r * 1.1);
                    break;
                case "Cold":
                    pixels[i]     = (byte)Math.Min(255, b * 1.1);
                    pixels[i + 2] = (byte)Math.Min(255, r * 0.9);
                    break;
                case "Blur":
                    pixels[i]     = (byte)(b * 0.95);
                    pixels[i + 1] = (byte)(g * 0.95);
                    pixels[i + 2] = (byte)(r * 0.95);
                    break;
            }
        }


        WriteableBitmap resultBitmap = new(width, height, 96, 96, PixelFormats.Bgra32, null);
        resultBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);

        using MemoryStream ms = new();
        BitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(resultBitmap));
        encoder.Save(ms);
        ms.Position = 0;

        BitmapImage final = new();
        final.BeginInit();
        final.CacheOption = BitmapCacheOption.OnLoad;
        final.StreamSource = ms;
        final.EndInit();
        final.Freeze();
        return final;
    }

    private void SaveBitmapToFile(BitmapImage bitmap, string path)
    {
        BitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream fs = new(path, FileMode.Create);
        encoder.Save(fs);
    }

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
