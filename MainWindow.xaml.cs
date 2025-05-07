using System.Windows;
using System.Windows.Input;
using System.IO;

namespace wpf_image_filter
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        
        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = (WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        
        private void Window_DragEnter(object sender, DragEventArgs e)
        {
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
        
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwndSource = PresentationSource.FromVisual(this) as System.Windows.Interop.HwndSource;
            hwndSource?.AddHook(WindowProc);
        }

        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const int WM_NCHITTEST = 0x0084;

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_NCHITTEST)
            {
                Point position = PointFromScreen(new Point((lParam.ToInt32() & 0xFFFF), (lParam.ToInt32() >> 16)));
                double gripSize = 8;

                if (position.Y < gripSize)
                {
                    if (position.X < gripSize)
                    {
                        handled = true;
                        return (IntPtr)HTTOPLEFT;
                    }
                    else if (position.X > ActualWidth - gripSize)
                    {
                        handled = true;
                        return (IntPtr)HTTOPRIGHT;
                    }
                    else
                    {
                        handled = true;
                        return (IntPtr)HTTOP;
                    }
                }
                else if (position.Y > ActualHeight - gripSize)
                {
                    if (position.X < gripSize)
                    {
                        handled = true;
                        return (IntPtr)HTBOTTOMLEFT;
                    }
                    else if (position.X > ActualWidth - gripSize)
                    {
                        handled = true;
                        return (IntPtr)HTBOTTOMRIGHT;
                    }
                    else
                    {
                        handled = true;
                        return (IntPtr)HTBOTTOM;
                    }
                }
                else if (position.X < gripSize)
                {
                    handled = true;
                    return (IntPtr)HTLEFT;
                }
                else if (position.X > ActualWidth - gripSize)
                {
                    handled = true;
                    return (IntPtr)HTRIGHT;
                }
            }

            return IntPtr.Zero;
        }

    }
}