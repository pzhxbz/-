using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WpfApp1
{
    class Processor
    {
        private Process p;
        private Queue<string> recvStr;
        private string musicPath;
        private string typeStr;
        public Processor()
        {
            recvStr = new Queue<string>();

            p = new Process();
            p.StartInfo.FileName = "python.exe";
            //p.StartInfo.Arguments = "main.py screenshot.jpg";
            p.StartInfo.Arguments = "main.py";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardError = true;
            p.OutputDataReceived += new DataReceivedEventHandler((senders, e) =>
            {
                string getData = e.Data;
                if (String.IsNullOrEmpty(getData))
                {
                    return;
                }
                if (getData.Contains("[gcode]:"))
                {
                    // gcodePath = getData.Replace("[gcode]:", "");
                    recvStr.Enqueue(getData.Replace("[gcode]:", ""));
                }
                if(getData.Contains("[music]:"))
                {
                    this.musicPath = getData.Replace("[music]:", "");
                }
                if (getData.Contains("[result]:"))
                {
                    string s = getData.Replace("[result]:", "");
                    int resType = -1;
                    if (int.TryParse(s, out resType))
                    {
                        string resStr = "";
                        if (resType == 0)
                        { resStr = "荷"; }
                        else if (resType == 1)
                        { resStr = "竹"; }
                        else if (resType == 2)
                        { resStr = "月"; }
                        else if (resType == 3)
                        { resStr = "菊"; }
                        else if (resType == 4)
                        { resStr = "兰"; }
                        else if (resType == 5)
                        { resStr = "柳"; }
                        else if (resType == 6)
                        { resStr = "梅"; }
                        else if (resType == 7)
                        { resStr = "山"; }
                        else { resStr = "error"; }
                        this.typeStr = resStr;
                    }
                    else { this.typeStr = "error"; }
                }
            });
            p.Start();
            p.BeginOutputReadLine();
            p.PriorityClass = ProcessPriorityClass.High;
            //p.WaitForExit();
        }


        public void sendLine(string data)
        {
            p.StandardInput.WriteLine(data);
        }

        public string popStr()
        {
            int count = 0;
            while (true)
            {
                try
                {
                    return recvStr.Dequeue();
                }
                catch (Exception)
                {
                    count++;
                    Thread.Sleep(50);
                    if (count > 400)  // 20sec
                    {
                        break;
                    }
                    continue;
                }
            }
            return "";

        }

        public string GetMusicPath()
        {
            int count = 0;
            while(String.IsNullOrEmpty(this.musicPath))
            {
                Thread.Sleep(50);
                count++;
                if(count >= 400)
                {
                    break;
                }
            }

            return this.musicPath;
        }

        public string GetTypeStr()
        {
            int count = 0;
            while (String.IsNullOrEmpty(this.typeStr))
            {
                Thread.Sleep(50);
                count++;
                if (count >= 400)
                {
                    break;
                }
            }

            return this.typeStr;
        }

        public void Clear()
        {
            this.musicPath = "";
            this.recvStr.Clear();
            this.typeStr = "";
        }


        ~Processor()
        {
            p.Close();
        }

    }
}
