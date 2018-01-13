using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        private bool isMove = false;
        private Point lastPoint;
        private bool isProcess = false;

        private const double MOUSE_MOVE_OPACITY = 0.6;
        private const double MOUSE_LEAVE_OPCAITY = 0.3;


        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x00080000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public MainWindow()
        {
            InitializeComponent();


            this.lastPoint = new Point();

            this.Topmost = true;

            // set button image
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri("button.png", UriKind.Relative);
            bi.EndInit();
            buttonImage.Source = bi;

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            // disable close button
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            buttonImage.Opacity = 1;
            //MessageBox.Show("click", "event", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (isProcess)
            {
                return;
            }

            Thread t1 = new Thread(new ThreadStart(this.startTask));
            t1.IsBackground = true;
            t1.Start();

            isProcess = true;


        }

        private void Button_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isMove)
            {
                return;
            }

            var nowPoint = PointToScreen(e.GetPosition(this));

            this.Left += nowPoint.X - lastPoint.X;
            this.Top += nowPoint.Y - lastPoint.Y;
            lastPoint = nowPoint;

        }

        private void Button_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isMove = true;
            var nowPosition = PointToScreen(e.GetPosition(this));
            lastPoint = nowPosition;
        }

        private void Button_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isMove = false;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // f5 结束程序
            if (e.Key == Key.F5)
            {
                Close();
            }
        }

        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            buttonImage.Opacity = MOUSE_MOVE_OPACITY;
        }

        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            buttonImage.Opacity = MOUSE_LEAVE_OPCAITY;
        }


        private void startTask()
        {
            var shot = new Shoter("X");
            var bmp = shot.GetProcPhoto();
            if (bmp == null)
            {
                MessageBox.Show("获取图片失败", "", MessageBoxButton.OK, MessageBoxImage.Error);
                isProcess = false;
                return;
            }
            bmp.Save("test.jpg", ImageFormat.Jpeg);

            MessageBox.Show("成功获取图片", "ok", MessageBoxButton.OK, MessageBoxImage.None);

            string filename = "";

            var sender = new GcodeSender();
            sender.OpenGcodeFile(filename);
            if (!sender.OpenPort())
            {
                MessageBox.Show("获取设备出错", "ok", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            sender.SendFile();
            sender.ClosePort();


            isProcess = false;
        }


    }
}
