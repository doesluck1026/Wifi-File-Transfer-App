using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FileSharingApp_Desktop
{
    class Main
    {
        private static string URL;
        private static string FileName;
        private static double FileSize;
        private static Communication.SizeTypes SizeType;
        private static FileStream fileStream;
        private static int PackSize = 1024 * 32;            //bytes
        public static bool SetFileURL(string url)
        {
            URL = url;
           bool isConnected= WaitForConnection();
            if(isConnected)
                ReadFile();
            return isConnected;
        }
        public static bool QueryForTransfer()
        {
           bool isAccepted= Communication.QueryForTransfer(FileName, FileSize, SizeType);
            return isAccepted;
        }
        private static bool WaitForConnection()
        {
            bool Success=Communication.CreateServer();
            return Success;
        }
        private static void ReadFile()
        {
            fileStream = new FileStream(URL, FileMode.Open);
            long fileSizeAsByte= fileStream.Length;
            int pow = (int)Math.Log(fileSizeAsByte, 1024);
            FileSize = fileSizeAsByte/Math.Pow(1024, pow);
            switch (pow)
            {
                case 0:
                    SizeType = Communication.SizeTypes.Byte;
                    break;
                case 1:
                    SizeType = Communication.SizeTypes.KB;
                    break;
                case 2:
                    SizeType = Communication.SizeTypes.MB;
                    break;
                case 3:
                    SizeType = Communication.SizeTypes.GB;
                    break;
                case 4:
                    SizeType = Communication.SizeTypes.TB;
                    break;
            }
        }
        private static void SendingCoreFcn()
        {
            if(fileStream!=null)
            {
                long bytesSent = 0;
                int BytesRead = 0;
                int numPack = 0;
                bool isSent = false;
                byte[] BytesToSend = new byte[PackSize];
                while (bytesSent<fileStream.Length)
                {
                    BytesRead = fileStream.Read(BytesToSend, 0, BytesToSend.Length);
                    isSent = Communication.SendFilePacks(BytesToSend, numPack);
                    if (!isSent)
                        break;
                    numPack++;
                    bytesSent += BytesRead;
                }
                if (isSent)
                {
                    Communication.CompleteTransfer();
                    Debug.WriteLine("File is Succesfully Sent");
                }
                else
                    Debug.WriteLine("File Transfer Failed!");

            }
        }
    }
}
