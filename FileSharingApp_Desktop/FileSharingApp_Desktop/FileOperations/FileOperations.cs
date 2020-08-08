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

        /// <summary>
        /// Create File Operation
        /// </summary>
        /// <param name="FilePath">Reading address of the file to be sent or the address to save the received file</param>
        public FileOperations(string FilePath)
        {
            this.FilePath = FilePath;
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
