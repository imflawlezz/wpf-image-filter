using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace wpf_image_filter;

public class ImageProcessingService
{
    public event Action<string>? LogMessage;
    public event Action<int>? ProgressChanged;
    public event Action<bool>? ProcessingStateChanged;
    public event Action<ProcessedImage>? ImageProcessed;

    private CancellationTokenSource? _cts;
    private readonly List<string> _imagePaths = new();
    private string _selectedFilter = "Grayscale";

    public IEnumerable<string> AvailableFilters { get; } = new[] { "Grayscale", "Sepia", "Warm", "Cold", "Blur" };

    public string SelectedFilter
    {
        get => _selectedFilter;
        set => _selectedFilter = value ?? "Grayscale";
    }

    public void SetImagePaths(IEnumerable<string> paths)
    {
        _imagePaths.Clear();
        _imagePaths.AddRange(paths);
    }
    
    private bool _isProcessing;
    public bool IsProcessing
    {
        get => _isProcessing;
        private set
        {
            if (_isProcessing != value)
            {
                _isProcessing = value;
                ProcessingStateChanged?.Invoke(value);
            }
        }
    }

    public async Task ProcessImagesAsync()
    {
        if (_imagePaths.Count == 0) return;

        _cts = new CancellationTokenSource();
        IsProcessing = true;
        ProgressChanged?.Invoke(0);

        var tasks = new List<Task>();
        int total = _imagePaths.Count;
        int completed = 0;
        bool wasCancelled = false;

        foreach (var path in _imagePaths)
        {
            tasks.Add(ProcessSingleImageAsync(path, _cts.Token)
                .ContinueWith(t =>
                {
                    int progress = (int)(Interlocked.Increment(ref completed) * 100.0 / total);
                    ProgressChanged?.Invoke(progress);
                }));
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            wasCancelled = true;
            LogMessage?.Invoke("⛔️ Processing was canceled by the user.");
        }
        catch (Exception ex)
        {
            LogMessage?.Invoke($"❌ Error: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
            _cts?.Dispose();
            _cts = null;

            LogMessage?.Invoke(wasCancelled 
                ? "⛔️ Processing was canceled." 
                : "✅ All images processed successfully.");
            
            ProgressChanged?.Invoke(100);
        }
    }

    public void CancelProcessing()
    {
        if (_cts == null || _cts.IsCancellationRequested) return;
        _cts.Cancel();
        LogMessage?.Invoke("⛔️ Cancel requested.");
    }
    
    private async Task ProcessSingleImageAsync(string path, CancellationToken cancellationToken)
    {
        string fileName = Path.GetFileName(path);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            LogMessage?.Invoke($"Processing {fileName}...");

            var original = await Task.Run(() => ImageProcessor.LoadBitmap(path), cancellationToken);
            var filtered = await Task.Run(() => ImageProcessor.ApplyFilter(original, _selectedFilter, cancellationToken), cancellationToken);
            
            string filterPrefix = _selectedFilter.ToLower();
            string outputPath = Path.Combine(
                Path.GetDirectoryName(path)!, 
                $"{filterPrefix}_{fileName}");

            await Task.Run(() => ImageProcessor.SaveBitmapToFile(filtered, outputPath), cancellationToken);

            ImageProcessed?.Invoke(new ProcessedImage
            {
                FileName = fileName,
                Thumbnail = filtered,
                Status = "Done"
            });

            LogMessage?.Invoke($"✔️ {fileName} processed with {_selectedFilter} filter.");
        }
        catch (OperationCanceledException)
        {
            LogMessage?.Invoke($"⚠️ {fileName} — canceled.");
            throw;
        }
        catch (Exception ex)
        {
            LogMessage?.Invoke($"❌ Error: {fileName}: {ex.Message}");
        }
    }
}