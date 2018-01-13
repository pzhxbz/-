using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    class Shoter
    {
        public string ProcName;

        private class User32
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct Rect
            {
                public int left;
                public int top;
                public int right;
                public int bottom;
            }

            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);
        }

        [DllImport("user32.dll", EntryPoint = "FindWindowEx")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1); //窗体置顶
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2); //取消窗体置顶
        public const uint SWP_NOMOVE = 0x0002; //不调整窗体位置
        public const uint SWP_NOSIZE = 0x0001; //不调整窗体大小


        private IntPtr targetHandle;
        private List<IntPtr> getList;

        public Shoter(string procName)
        {
            this.ProcName = procName;
            getList = new List<IntPtr>();
        }

        public Shoter()
        {
            getList = new List<IntPtr>();
        }

        public Bitmap GetProcPhoto()
        {
            if (this.ProcName.Length == 0)
            {
                return null;
            }

            Process proc;
            try
            {
                proc = Process.GetProcessesByName(this.ProcName)[0];
            }
            catch (IndexOutOfRangeException e)
            {
                return null;
            }

            this.targetHandle = proc.MainWindowHandle;

            WindowHandleInfo.EnumWindows(this.Report, 0);

            var guessHandle = this.getList[0];

            var allChildWindows = new WindowHandleInfo(guessHandle);

            var childList = allChildWindows.GetAllChildHandles();

            var rect = new User32.Rect();
            User32.GetWindowRect(childList[0], ref rect);

            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;



            Point center = new Point(rect.left + width / 2, rect.top + height / 2);

            int padding = 26;

            double scale = width * 1.0 / height;

            int realTop, realHeight, realWidth, realLeft;


            if (scale > 1.5)
            {
                realTop = rect.top + padding;
                realHeight = height - padding * 2;
                realWidth = (int)(realHeight * 1.5);
                realLeft = center.X - realWidth / 2;
            }
            else
            {
                realLeft = rect.left + padding;
                realWidth = width - padding * 2;
                realHeight = (int)(realWidth / 1.5);
                realTop = center.Y - realHeight / 2;
            }
            Bitmap bmp = new Bitmap(realWidth, realHeight, PixelFormat.Format32bppArgb);
            Graphics graphics = Graphics.FromImage(bmp);
            // graphics.CopyFromScreen(rect.left, rect.top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
            // SetWindowPos(guessHandle, HWND_TOPMOST, 1, 1, 1, 1, SWP_NOMOVE | SWP_NOSIZE);
            graphics.CopyFromScreen(realLeft, realTop, 0, 0, new Size(realWidth, realHeight), CopyPixelOperation.SourceCopy);
            return bmp;

        }

        public bool Report(int hwnd, int lParam)
        {
            IntPtr ownerHandle = WinAPI.GetWindow((IntPtr)hwnd, WinAPI.GW_OWNER);
            if (ownerHandle == this.targetHandle)
            {
                StringBuilder sb = new StringBuilder(256);
                WindowHandleInfo.GetWindowText((IntPtr)hwnd, sb, sb.Capacity);
                Console.WriteLine(sb.ToString());
                if (sb.ToString().IndexOf("- Expresii -") != -1)
                {
                    getList.Add((IntPtr)hwnd);
                }
            }

            return true;
        }

    }


    public class WindowHandleInfo
    {
        [DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll")]
        private static extern int GetWindowTextW(IntPtr hWnd, [MarshalAs(UnmanagedType.LPWStr)]StringBuilder lpString, int nMaxCount);


        private delegate bool EnumWindowProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr lParam);

        public delegate bool CallBack(int hwnd, int lParam);

        [DllImport("user32")]
        public static extern int EnumWindows(CallBack x, int y);



        private IntPtr _MainHandle;

        public WindowHandleInfo(IntPtr handle)
        {
            this._MainHandle = handle;
        }

        public List<IntPtr> GetAllChildHandles()
        {
            List<IntPtr> childHandles = new List<IntPtr>();

            GCHandle gcChildhandlesList = GCHandle.Alloc(childHandles);
            IntPtr pointerChildHandlesList = GCHandle.ToIntPtr(gcChildhandlesList);

            try
            {
                EnumWindowProc childProc = new EnumWindowProc(EnumWindow);
                EnumChildWindows(this._MainHandle, childProc, pointerChildHandlesList);
            }
            finally
            {
                gcChildhandlesList.Free();
            }

            return childHandles;
        }

        private bool EnumWindow(IntPtr hWnd, IntPtr lParam)
        {
            GCHandle gcChildhandlesList = GCHandle.FromIntPtr(lParam);

            if (gcChildhandlesList == null || gcChildhandlesList.Target == null)
            {
                return false;
            }

            List<IntPtr> childHandles = gcChildhandlesList.Target as List<IntPtr>;
            childHandles.Add(hWnd);

            return true;
        }
    }


    public static class WinAPI
    {
        public const uint GW_OWNER = 4;
        [System.Runtime.InteropServices.DllImport("User32.dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
    }
}
