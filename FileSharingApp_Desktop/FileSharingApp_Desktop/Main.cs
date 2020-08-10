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
        #region Parameters

        private static int PackSize = 1024 * 32;            /// this represents the maximum length of bytes to be transfered to client in one package. default is 32 kB and should be smaller than 64 kB

        #endregion

        #region Variables

        private static string URL;                          /// File Path
        private static string FileName;                     /// Name of the file including extention
        private static double FileSize;                     /// Size of the file which can be bytes, kilobytes megabytes etc...
        private static Communication.SizeTypes SizeType;    /// unit of filesize parameter which can be bytes, kilobytes megabytes etc...
        private static FileStream fileStream;               /// File Stream to be used to read file 
        private static Thread sendingThread;

        #endregion

        #region Server Functions

        /// <summary>
        /// Gets the Selected URL and setups server
        /// </summary>
        /// <param name="url">Path of the file to be transfered</param>
        /// <returns></returns>
        public static bool SetFileURL(string url)           
        {
            URL = url;                                  /// assign URL
           bool isConnected= WaitForConnection();       /// Setup the server and accept connection
            if(isConnected)                             /// if connection succeed
                ReadFile();                             /// Read file specs
            return isConnected;                         /// return connection status
        }
        /// <summary>
        /// This function is used to send information to client about the file to be transfered and queries if client still wants to receive the file
        /// </summary>
        /// <returns> returns acception status according to answer of the client (true or false)</returns>
        public static bool QueryForTransfer()
        {
           bool isAccepted= Communication.QueryForTransfer(FileName, FileSize, SizeType);
            return isAccepted;                          /// return acception status
        }
        /// <summary>
        /// Starts sending slected file to client in another thread.
        /// </summary>
        /// <returns>returns true if transfer is started</returns>
        public static bool StartFileTransfer()
        {
            try
            {
                sendingThread = new Thread(SendingCoreFcn);
                sendingThread.Start();
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
        private static bool WaitForConnection()
        {
            bool Success=Communication.CreateServer();  /// setup the server and start listening to port
            return Success; 
        }
        /// <summary>
        /// assigns to file stream object and reads file specs..
        /// </summary>
        private static void ReadFile()
        {
            fileStream = new FileStream(URL, FileMode.Open);        /// create a file stream object to be used to read file
            long fileSizeAsByte= fileStream.Length;                 /// get the length of the file as bytes length
            int pow = (int)Math.Log(fileSizeAsByte, 1024);          /// calculate the greatest type ( byte megabyte gigabyte etc...) the filesize can be respresent as integer variable
            FileSize = fileSizeAsByte/Math.Pow(1024, pow);          /// Convert file size from bytes to the greatest type
            switch (pow)                                            /// to assign type:
            {                                                       
                case 0:                                             /// if pow equals to 0
                    SizeType = Communication.SizeTypes.Byte;        /// then the type is bytes
                    break;
                case 1:                                             /// if pow equals to 1 
                    SizeType = Communication.SizeTypes.KB;          /// then the type is kilobytes and so on
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
        /// <summary>
        /// This function is used in a thread to send all file bytes to client.
        /// </summary>
        private static void SendingCoreFcn()
        {
            if(fileStream!=null)
            {
                /// define variables
                long bytesSent = 0;
                int BytesRead = 0;
                uint numPack = 0;
                bool isSent = false;
                byte[] BytesToSend = new byte[PackSize];                                /// Define byte array to carry file bytes
                while (bytesSent<fileStream.Length)                                     /// while the number of bytes sent to client is smaller than the total file length
                {
                    BytesRead = fileStream.Read(BytesToSend, 0, BytesToSend.Length);    /// read file and copy to carrier array.
                    isSent = Communication.SendFilePacks(BytesToSend, numPack);         /// send the bytes
                    if (!isSent)                                                        /// if fails
                        break;                                                          /// stop file transfer
                    numPack++;                                                          /// increase the number of package sent variable
                    bytesSent += BytesRead;                                             /// update the number of bytes sent to client.
                }
                if (isSent)                                                             /// if all file is sent
                {
                    Communication.CompleteTransfer();                                   /// stop data transfer and let client know that the transfer is successfully done.
                    Debug.WriteLine("File is Succesfully Sent");
                }
                else
                    Debug.WriteLine("File Transfer Failed!");
            }
        }

        #endregion

        #region Client Functions



        #endregion

    }
}
