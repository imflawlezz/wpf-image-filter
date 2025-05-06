using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace wpf_image_filter;

public class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly ImageProcessingService _processingService;
    private string _selectedFilesText = string.Empty;
    private int _progressPercentage;

    public ObservableCollection<string> LogMessages { get; } = new();
    public ObservableCollection<ProcessedImage> ProcessedImages { get; } = new();

    public string SelectedFilesText
    {
        get => _selectedFilesText;
        set { _selectedFilesText = value; OnPropertyChanged(nameof(SelectedFilesText)); }
    }

    public int ProgressPercentage
    {
        get => _progressPercentage;
        set { _progressPercentage = value; OnPropertyChanged(nameof(ProgressPercentage)); }
    }

    public bool IsProcessing => _processingService.IsProcessing;
    public IEnumerable<string> AvailableFilters => _processingService.AvailableFilters;

    public string SelectedFilter
    {
        get => _processingService.SelectedFilter;
        set => _processingService.SelectedFilter = value;
    }

    public ICommand LoadImagesCommand { get; }
    public ICommand ProcessImagesCommand { get; }
    public ICommand CancelProcessingCommand { get; }

    public MainViewModel()
    {
        _processingService = new ImageProcessingService();
        _processingService.LogMessage += OnLogMessage;
        _processingService.ProgressChanged += OnProgressChanged;
        _processingService.ProcessingStateChanged += OnProcessingStateChanged;
        _processingService.ImageProcessed += OnImageProcessed;

        LoadImagesCommand = new RelayCommand(_ => LoadImages());
        ProcessImagesCommand = new RelayCommand(async _ => await _processingService.ProcessImagesAsync(), 
            _ => !string.IsNullOrEmpty(SelectedFilesText) && !IsProcessing);
        CancelProcessingCommand = new RelayCommand(_ => _processingService.CancelProcessing(), 
            _ => IsProcessing);
    }

    private void LoadImages()
    {
        var openFileDialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            _processingService.SetImagePaths(openFileDialog.FileNames);
            SelectedFilesText = string.Join("; ", openFileDialog.FileNames.Select(Path.GetFileName));
            LogMessages.Clear();
            LogMessages.Add($"{openFileDialog.FileNames.Length} images loaded.");
        }
    }

    private void OnLogMessage(string message)
    {
        Application.Current.Dispatcher.Invoke(() => LogMessages.Add(message));
    }

    private void OnProgressChanged(int progress)
    {
        Application.Current.Dispatcher.Invoke(() => ProgressPercentage = progress);
    }

    private void OnProcessingStateChanged(bool isProcessing)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            OnPropertyChanged(nameof(IsProcessing));
            CommandManager.InvalidateRequerySuggested();
        });
    }

    private void OnImageProcessed(ProcessedImage image)
    {
        Application.Current.Dispatcher.Invoke(() => ProcessedImages.Add(image));
    }

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    
    public void LoadImagesFromPaths(string[] paths)
    {
        _processingService.SetImagePaths(paths);
        SelectedFilesText = string.Join("; ", paths.Select(Path.GetFileName));
        LogMessages.Clear();
        LogMessages.Add($"{paths.Length} images loaded via drag&drop.");
    }
}