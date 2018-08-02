using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
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

        private Processor processor;

        private bool isMove = false;
        private Point lastPoint;
        private bool isProcess = false;
        private bool isStopSend = false;

        private MusicPlayer player;

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

            processor = new Processor();

            player = new MusicPlayer();

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (isProcess)
            {
                if (isStopSend == false)
                    isStopSend = true;
                return;
            }

            buttonImage.Opacity = 1;
            //MessageBox.Show("click", "event", MessageBoxButton.YesNo, MessageBoxImage.Question);

            //startButton.IsEnabled = false;

            Thread t1 = new Thread(new ThreadStart(this.startTask));
            t1.IsBackground = true;
            t1.Start();
            isProcess = true;
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri("loading.png", UriKind.Relative);
            bi.EndInit();
            buttonImage.Source = bi;
            //await Task.Run(new Action(this.startTask));

            //startButton.IsEnabled = true;

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
            processor.Clear();

            var shot = new Shoter("X");
            var bmp = shot.GetProcPhoto();
            if (bmp == null)
            {
                Thread.Sleep(3000);
                MessageBox.Show("获取图片失败", "", MessageBoxButton.OK, MessageBoxImage.Error);
                taskFinish();
                return;
            }
            bmp.Save("screenshot.jpg", ImageFormat.Jpeg);

            //MessageBox.Show("成功获取图片", "ok", MessageBoxButton.OK, MessageBoxImage.None);


            processor.sendLine("screenshot.jpg");


            string typeStr = this.processor.GetTypeStr();
            var diaRes = MessageBox.Show(typeStr, "result", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (diaRes == MessageBoxResult.No)
            {
                this.taskFinish();
                return;
            }

            string gcodePath = processor.popStr();
            if (String.IsNullOrEmpty(gcodePath))
            {
                MessageBox.Show("get path failed");
                taskFinish();
                return;
            }

            string musicPath = processor.GetMusicPath();

            //MessageBox.Show("get music path:" + musicPath);

            if (String.IsNullOrEmpty(musicPath))
            {
                MessageBox.Show("get music failed");
                taskFinish();
                return;
            }

            if (!File.Exists(gcodePath))
            {
                MessageBox.Show("gcode file is not exsit : " + gcodePath);
                taskFinish();
                return;
            }

            if (!File.Exists(musicPath))
            {
                MessageBox.Show("music file is not exsit : " + gcodePath);
                taskFinish();
                return;
            }

            var sender = new GcodeSender();
            sender.OpenGcodeFile(gcodePath);
            if (!sender.OpenPort())
            {
                //Thread.Sleep(3000);
                MessageBox.Show("获取设备出错", "ok", MessageBoxButton.OK, MessageBoxImage.Error);
                taskFinish();
                return;
            }
            player.PlayMusic(musicPath);

            sender.SendFile();
            sender.IsFinish = false;
            while ((!sender.IsFinish)||(!player.IsOver()))
            {
                Thread.Sleep(1);
                if (isStopSend == true)
                {
                    sender.StopSend();
                    player.Stop();

                }
            }
            sender.ClosePort();

            taskFinish();

        }

        private void taskFinish()
        {
            isProcess = false;
            Action updateAction = new Action(taskFinishUI);
            this.Dispatcher.BeginInvoke(updateAction);
        }

        private void taskFinishUI()
        {
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri("button.png", UriKind.Relative);
            bi.EndInit();
            buttonImage.Source = bi;
            isStopSend = false;
        }


    }
}
