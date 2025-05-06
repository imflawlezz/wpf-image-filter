using System.Windows.Media.Imaging;

namespace wpf_image_filter;

public class ProcessedImage
{
    public string FileName { get; set; } = "";
    public BitmapImage Thumbnail { get; set; } = new();
    public string Status { get; set; } = "Pending";
}
