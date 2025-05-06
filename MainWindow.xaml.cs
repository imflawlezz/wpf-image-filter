using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;


namespace wpf_image_filter
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            // Проверяем, что перетаскиваются файлы
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                
                // Проверяем, что все файлы имеют допустимые расширения
                var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp" };
                if (files.All(f => validExtensions.Contains(Path.GetExtension(f).ToLower())))
                {
                    e.Effects = DragDropEffects.Copy;
                    return;
                }
            }
            
            e.Effects = DragDropEffects.None;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp" };
                var imageFiles = files.Where(f => validExtensions.Contains(Path.GetExtension(f).ToLower())).ToArray();

                if (imageFiles.Length > 0)
                {
                    var viewModel = (MainViewModel)DataContext;
                    viewModel.LoadImagesFromPaths(imageFiles);
                }
            }
        }
        
        private void TextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp" };
                if (files.All(f => validExtensions.Contains(Path.GetExtension(f).ToLower())))
                {
                    e.Effects = DragDropEffects.Copy;
                    return;
                }
            }
            e.Effects = DragDropEffects.None;
        }

        private void TextBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp" };
                var imageFiles = files.Where(f => validExtensions.Contains(Path.GetExtension(f).ToLower())).ToArray();

                if (imageFiles.Length > 0)
                {
                    var viewModel = (MainViewModel)DataContext;
                    viewModel.LoadImagesFromPaths(imageFiles);
                }
            }
        }
    }
}