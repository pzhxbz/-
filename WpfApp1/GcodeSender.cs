using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WpfApp1
{
    class GcodeSender
    {

        private List<string> portList;
        private List<string> fileLines;
        private bool connected;
        private SerialPort serialPort1;
        private bool dataProcessing;
        public bool IsFinish = true;

        private bool isStop = false;

        public GcodeSender()
        {
            portList = new List<string>();
            foreach (string s in SerialPort.GetPortNames())
            {
                if (s != "COM1")
                    portList.Add(s);

            }
            if (portList.Count < 1)
            {
                logInfo("no device found!");
                return;
            }

            var components = new System.ComponentModel.Container();
            serialPort1 = new SerialPort(components);
            this.serialPort1.BaudRate = 115200;
            this.serialPort1.ReadBufferSize = 2048;
            this.serialPort1.ReadTimeout = 500;
            this.serialPort1.WriteTimeout = 3000;
            this.serialPort1.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(this.SerialPort1DataReceived);
        }
        public List<string> GetPortList()
        {
            return portList;
        }
        public void OpenGcodeFile(string filename)
        {
            fileLines = new List<string>();
            StreamReader file = new StreamReader(filename);
            string line = "";
            line = file.ReadLine();
            while (line != null)
            {
                line = line.Replace(" ", "");//remove spaces
                line = line.Replace("\r", " ");//remove CR
                line = line.Replace("\n", " ");//remove LF
                line = line.ToUpper();//all uppercase
                line = line.Trim();
                if ((!string.IsNullOrEmpty(line)) && (line[0] != '('))//trim lines and remove all empty lines and comment lines
                {
                    fileLines.Add(line);//add line to list to send
                    //fileLinesCount++;//Count total lines
                }
                line = file.ReadLine();
            }
            file.Close();
        }

        public void StopSend()
        {
            isStop = true;
        }

        private void sendFileThread()
        {
            try
            {
                foreach (var line in fileLines)
                {
                    serialPort1.Write(line + "\r");
                    logInfo(line);
                    dataProcessing = false;
                    while (!dataProcessing)
                    {
                        Thread.Sleep(1);
                    }
                    Thread.Sleep(10);
                    if (isStop == true)
                    {
                        string[] homeGcode = { "G00Z0.0000", "G00X0Y0" };
                        foreach (var h in homeGcode)
                        {
                            serialPort1.Write(h + "\r");
                            dataProcessing = false;
                            while (!dataProcessing)
                            {
                                Thread.Sleep(1);
                            }
                        }
                        break;
                    }
                }
            }
            catch (Exception er)
            {
                logInfo("Error sending next line");
                ClosePort();
                IsFinish = true;
                //return false;
            }
            IsFinish = true;
            //return true;
        }
        public void SendFile()
        {
            IsFinish = false;
            isStop = false;
            ThreadStart childref = new ThreadStart(sendFileThread);

            Thread childThread = new Thread(childref);

            childThread.Start();

        }

        private void logInfo(String info)
        {
            // Console.Out.WriteLine(info);
        }

        public bool OpenPort()
        {
            try
            {
                connected = false;
                serialPort1.PortName = portList[0];

                serialPort1.Open();
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();

                //dataProcessing = true;
                //GRBL_errCount = 0;
                return (true);
            }
            catch (Exception err)
            {
                logInfo("Opening port error");
                ClosePort();
                return (false);
            }

        }



        public bool ClosePort()
        {
            try
            {
                //setTransferFalse();
                if (serialPort1.IsOpen)
                {
                    serialPort1.DiscardInBuffer();
                    serialPort1.DiscardOutBuffer();
                    serialPort1.Close();
                }

                return (true);
            }
            catch (Exception err)
            {
                logInfo("Closing port error");
                return (false);
            }
        }

        private void SerialPort1DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            while ((serialPort1.IsOpen) && (serialPort1.BytesToRead > 0))
            {
                var rxString = string.Empty;
                try
                {
                    rxString = serialPort1.ReadTo("\r\n");//read line from grbl, discard CR LF
                    //logInfo("recv  :" + rxString);

                    dataProcessing = true;
                    //this.Invoke(new EventHandler(dataRx));//tigger rx process 
                    //while ((serialPort1.IsOpen) && (dataProcessing)) ;//wait previous data line processed done
                }
                catch (Exception errort)
                {
                    var mens = "Error reading line from serial port";
                    //logInfo(mens);
                    ClosePort();
                    isStop = true;

                    //err = errort;
                    //this.Invoke(new EventHandler(logErrorThr));
                }
            }
        }
    }
}
