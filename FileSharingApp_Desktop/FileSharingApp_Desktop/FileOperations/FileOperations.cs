using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSharingApp_Desktop.FileOperations
{

    class FileOperations
    {
        
        private static string _FilePath;
        private FileStream Fs;
        private double _FileSize;
        private Communication.SizeTypes _sizeTypes;

        public string FilePath
        {
            get
            {
                return _FilePath;
            }
            set
            {
                _FilePath = value;
            }
        }

        public double FileSize
        {
            get
            {
                return _FileSize;
            }
            private set
            {
                _FileSize = value;
            }
        }

        public Communication.SizeTypes FilesizeTypes
        {
            get
            {
                return _sizeTypes;
            }
            private set
            {
                _sizeTypes = value;
            }
        }

        /// <summary>
        /// Create File Operation
        /// </summary>
        /// <param name="FilePath">Reading address of the file to be sent or the address to save the received file</param>
        public FileOperations(string FilePath)
        {
            this.FilePath = FilePath;
            Fs = File.OpenRead(FilePath);

            long fileSizeAsByte = Fs.Length;
            int pow = (int)Math.Log(fileSizeAsByte, 1024);               /// calculate the greatest type ( byte megabyte gigabyte etc...) the filesize can be respresent as integer variable
            FileSize = fileSizeAsByte / Math.Pow(1024, pow);              /// Convert file size from bytes to the greatest type
            switch (pow)                                            /// to assign type:
            {
                case 0:                                                   /// if pow equals to 0
                    FilesizeTypes = Communication.SizeTypes.Byte;        /// then the type is bytes
                    break;
                case 1:                                                  /// if pow equals to 1 
                    FilesizeTypes = Communication.SizeTypes.KB;          /// then the type is kilobytes and so on
                    break;
                case 2:
                    FilesizeTypes = Communication.SizeTypes.MB;
                    break;
                case 3:
                    FilesizeTypes = Communication.SizeTypes.GB;
                    break;
                case 4:
                    FilesizeTypes = Communication.SizeTypes.TB;
                    break;
            }

        }

        public void FileReadAtByteIndex(long BufferIndx, out int BytesRead, out byte[] Buffer, int chunkSize = 1024)
        {
            Buffer = new byte[chunkSize];
            Fs.Position = BufferIndx;
            BytesRead = Fs.Read(Buffer, 0, chunkSize);
        }

        /// <summary>
        /// The file at the given address is taken to the Ram piece by piece and sent by the server
        /// </summary>
        /// <param name="server"> read the file by # chunks In KB multiples </param>
        public void File_PullAndPush(Server server, int chunkSize = 1024)
        {
            using (var Readfile = File.OpenRead(FilePath))
            {
                
                ///int cnt = 0;
                ///long totalBytes = 0;
                int bytesRead;
                var buffer = new byte[chunkSize];
                while ((bytesRead = Readfile.Read(buffer, 0, buffer.Length)) > 0)
                {
                    /// TODO: Process bytesRead number of bytes from the buffer
                    /// not the entire buffer as the size of the buffer is 1KB
                    /// whereas the actual number of bytes that are read are 
                    /// stored in the bytesRead integer.

                    /// to check accuracy
                    //totalBytes += bytesRead;
                    //Console.WriteLine(" cnt = " + cnt++ + " bytesRead = " + bytesRead + " totalBytes  = " + totalBytes);
                   
                    server.SendDataToClient(buffer); /// send data to Client 

                }
            }
        }

        /// <summary>
        /// Reconstruction of the file read piece by piece via client
        /// </summary>
        /// <param name="client"></param>
        public void File_PullAndSave(Client client)
        {
            using (var Writefile = File.OpenWrite(FilePath))
            {
                byte[] buffer;
                buffer = client.GetData();
                Writefile.Write(buffer, 0, buffer.Length); // to use "Writefile.Write(buffer)", the version of the IO class should be updated to 4.2.2.0 current version 4.0.0.0
            }
        }
    }
}
