using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace wpf_image_filter;

public class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public string SelectedFilesText { get; set; }
    public ObservableCollection<string> AvailableFilters { get; }
    public string SelectedFilter { get; set; }
    public ObservableCollection<string> LogMessages { get; }
    public ObservableCollection<ProcessedImage> ProcessedImages { get; }
    public int ProgressPercentage { get; set; }
    public bool IsProcessing { get; set; }
    
    public ICommand LoadImagesCommand { get; }
    public ICommand ProcessImagesCommand { get; }
    public ICommand CancelProcessingCommand { get; }
    
}

public class ProcessedImage
{
    public BitmapImage Thumbnail { get; set; }
    public string FileName { get; set; }
    public string Status { get; set; }
}