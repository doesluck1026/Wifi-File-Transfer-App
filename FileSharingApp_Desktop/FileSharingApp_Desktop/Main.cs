using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileSharingApp_Desktop
{
    class Main
    {
        public enum ProccessType
        {
            SendFile,
            ReceiveFile
        }

        #region Parameters

        private static int PackSize = 1024 * 32;            /// this represents the maximum length of bytes to be transfered to client in one package. default is 32 kB and should be smaller than 64 kB

        #endregion

        #region Variables

        private static string URL;                          /// File Path
        private static Thread sendingThread;
        private static FileOperations FileOps;
        private static Thread receivingThread;
        private static object speedLock = new object();
        private static object completedLock = new object();
        private static double _transferSpeed = 0;
        private static int _completedPercentage = 0;
        public static double TransferSpeed
        {

            get
            {
                lock (speedLock)
                {
                    return _transferSpeed;
                }
            }
            set
            {
                lock (speedLock)
                {
                    _transferSpeed = value;
                }
            }
        }
        public static int CompletedPercentage
        {

            get
            {
                lock (completedLock)
                {
                    return _completedPercentage;
                }
            }
            set
            {
                lock (completedLock)
                {
                    _completedPercentage = value;
                }
            }
        }

        #endregion



        #region Server Functions

        /// <summary>
        /// Gets the Selected URL and setups server
        /// </summary>
        /// <param name="url">Path of the file to be transfered</param>
        /// <returns></returns>
        public static void SetFileURL(string url)           
        {
            URL = url;                                                  /// assign URL
            FileOps = new FileOperations(url,FileOperations.TransferMode.Send);
            string  clientHostname= WaitForConnection();                /// Setup the server and accept connection
            if (clientHostname!=null || clientHostname!="")             /// if connection succeed
            {
                bool isVerified = Communication.VerifyCode();
                if (isVerified)
                {
                    // display hostname here
                    // Ask User if still wants to transfer file to connected client.
                }
            }
        }
        /// <summary>
        /// This function is used to send information to client about the file to be transfered and queries if client still wants to receive the file
        /// Call this Function after User confirms to send file.
        /// </summary>
        /// <returns> returns acception status according to answer of the client (true or false)</returns>
        public static bool QueryForTransfer()
        {
           bool isAccepted= Communication.QueryForTransfer(FileOps.FileName, FileOps.FileSize, FileOps.FilesizeType);
            if(isAccepted)
            {
                bool isTransferStarted=StartFileTransfer();                     /// Start File Transfer
                return isTransferStarted;
            }
            return false;                                                       /// return false
        }
        /// <summary>
        /// Starts sending slected file to client in another thread.
        /// </summary>
        /// <returns>returns true if transfer is started</returns>
        private static bool StartFileTransfer()
        {
            try
            {
                sendingThread = new Thread(SendingCoreFcn);                             /// Start Sending File
                sendingThread.Start();
                Debug.WriteLine("File Transfer is Started");
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to Start sending thread! \n " + e.ToString());
                return false;
            }
        }
        /// <summary>
        /// Sets up the server and starts listening to clients
        /// </summary>
        /// <returns>returns true if a client successfully connected</returns>
        private static string WaitForConnection()
        {
            string IpCode=Communication.CreateServer();                     /// setup the server and start listening to port
            // display the "IpCode" in ui here
            string clientHostname = Communication.startServer();            /// Wait for Client to connect and return the hostname of connected client.
            return clientHostname; 
        }
        /// <summary>
        /// This function is used in a thread to send all file bytes to client.
        /// </summary>
        private static void SendingCoreFcn()
        {
            if(FileOps!=null)
            {
                /// define variables
                long bytesSent = 0;
                int BytesRead = 0;
                long numPack = 0;
                bool isSent = false;
                byte[] BytesToSend;                                                                     /// Define byte array to carry file bytes
                long numberOfPacks = Communication.NumberOfPacks;
                long checkPoint = 0;
                Stopwatch stopwatch = Stopwatch.StartNew();
                while (bytesSent<FileOps.FileSizeAsBytes)                                               /// while the number of bytes sent to client is smaller than the total file length
                {
                    FileOps.FileReadAtByteIndex(bytesSent, out BytesRead, out BytesToSend, PackSize);     /// read file and copy to carrier array.
                    isSent = Communication.SendFilePacks(BytesToSend, numPack);                         /// send the bytes
                    if (isSent)
                    {
                        numPack++;                                                                      /// increase the number of package sent variable
                        bytesSent += BytesRead;                                                         /// update the number of bytes sent to client.
                    }
                    if(stopwatch.ElapsedMilliseconds>1000)
                    {
                        TransferSpeed = (bytesSent - checkPoint) / (1024.0 * 1024);                     ///
                        checkPoint = bytesSent;
                        stopwatch.Restart();
                    }
                    CompletedPercentage = (int)(numPack / numberOfPacks) *100;
                }
                if (isSent)                                                                             /// if all file is sent
                {
                    Communication.CompleteTransfer();                                                   /// stop data transfer and let client know that the transfer is successfully done.
                    Debug.WriteLine("File is Succesfully Sent");
                }
                else
                    Debug.WriteLine("File Transfer Failed!");
            }
        }

        #endregion

        #region Client Functions
        public static void SetFilePathToSave(string path)
        {
            FileOps = new FileOperations(path,FileOperations.TransferMode.Receive);
        }
        public static bool EnterTheCode(string code)
        {
            bool success = false;
            string serverIP = Communication.DecodeTransferCode(code);
            if (serverIP != null && serverIP != "")
            {
                bool isConnected = Communication.ConnectToServer(serverIP);
                if (isConnected)
                {
                    Communication.SendVerification(code);
                    string fileName;
                    double fileSize;
                    Communication.SizeTypes sizeType;
                    Communication.GetFileSpecs(out fileName, out fileSize, out sizeType);
                    // Show Specs to user and ask for permission
                }
            }
            return success;
        }
        public static bool RespondToTransferRequest(bool isAccepted)
        {
            Communication.RespondToTransferRequest(isAccepted);
            if (isAccepted)
                return StartReceiving();
            else
                return false;
        }
        private static bool StartReceiving()
        {
            try
            {
                receivingThread = new Thread(ReceivingCoreFcn);
                receivingThread.Start();
                Debug.WriteLine("File Transfer is Started");
                return true;
            }
            catch(Exception e)
            {
                Debug.WriteLine("Failed to Start sending thread! \n " + e.ToString());
                return false;
            }
        }
        private static void ReceivingCoreFcn()
        {
            if (FileOps != null)
            {
                /// define variables
                long bytesWritten = 0;
                long numPack = 0;
                byte[] BytesToWrite;                                                                     /// Define byte array to carry file bytes
                long numberOfPacks = Communication.NumberOfPacks;
                long checkPoint = 0;
                Stopwatch stopwatch = Stopwatch.StartNew();
                while (numPack < Communication.NumberOfPacks)                                           /// while the number of bytes sent to client is smaller than the total file length
                {
                    BytesToWrite = Communication.ReceiveFilePacks();
                    FileOps.FileWriteAtByteIndex(numPack, BytesToWrite);                                /// read file and copy to carrier array.

                    numPack++;                                                                      /// increase the number of package sent variable
                    bytesWritten += BytesToWrite.Length;                                                         /// update the number of bytes sent to client.
                    if (stopwatch.ElapsedMilliseconds > 1000)
                    {
                        TransferSpeed = (bytesWritten - checkPoint) / (1024.0 * 1024);                     ///
                        checkPoint = bytesWritten;
                        stopwatch.Restart();
                    }
                    CompletedPercentage = (int)(numPack / numberOfPacks) * 100;
                }
                Communication.CompleteTransfer();                                                   /// stop data transfer and let client know that the transfer is successfully done.
                Debug.WriteLine("File is Succesfully Received");

            }
        }
        #endregion

    }
}
