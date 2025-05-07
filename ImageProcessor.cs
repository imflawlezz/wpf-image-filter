using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace wpf_image_filter;

public class ImageProcessor
{
    public static BitmapImage ApplyFilter(BitmapImage original, string filter, CancellationToken cancellationToken)
    {
        WriteableBitmap writable = new(original);
        int width = writable.PixelWidth;
        int height = writable.PixelHeight;
        int stride = width * 4;
        byte[] pixels = new byte[height * stride];
        writable.CopyPixels(pixels, stride, 0);

        for (int i = 0; i < pixels.Length; i += 4)
        {
            cancellationToken.ThrowIfCancellationRequested();

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
                    pixels[i] = (byte)Math.Min(255, 0.272 * r + 0.534 * g + 0.131 * b);
                    pixels[i + 1] = (byte)Math.Min(255, 0.349 * r + 0.686 * g + 0.168 * b);
                    pixels[i + 2] = (byte)Math.Min(255, 0.393 * r + 0.769 * g + 0.189 * b);
                    break;
                case "Warm":
                    pixels[i] = (byte)Math.Min(255, b * 0.9);
                    pixels[i + 2] = (byte)Math.Min(255, r * 1.1);
                    break;
                case "Cold":
                    pixels[i] = (byte)Math.Min(255, b * 1.1);
                    pixels[i + 2] = (byte)Math.Min(255, r * 0.9);
                    break;
                case "Blur":
                    pixels[i] = (byte)(b * 0.75);
                    pixels[i + 1] = (byte)(g * 0.75);
                    pixels[i + 2] = (byte)(r * 0.75);
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

    public static BitmapImage LoadBitmap(string path)
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

    public static void SaveBitmapToFile(BitmapImage bitmap, string path)
    {
        BitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream fs = new(path, FileMode.Create);
        encoder.Save(fs);
    }
}