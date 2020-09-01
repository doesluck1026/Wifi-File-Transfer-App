using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
class FileOperations
{
    private static object lock_FileName = new object();
    private string _FilePath;
    private string _FileName;
    private double _FileSize;
    private long _FileSizeAsBytes;
    private FileStream Fs;
    private Communication.SizeTypes _sizeTypes;

    /// <summary>
    /// Determines if this device will send a file or receive.
    /// </summary>
    public enum TransferMode
    {
        Send,
        Receive
    }

    /// <summary>
    /// Path and name of the file including extention
    /// </summary>
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
    /// name of the file including extention
    /// </summary>
    public string FileName
    {
        get
        {
            lock (lock_FileName)
            {
                return _FileName;
            }
        }
        set
        {
            lock (lock_FileName)
            {
                _FileName = value;
            }
        }
    }
    /// <summary>
    /// Size of the file as double. this can be in the unit of bytes, megabytes gigabytes etc. Check SizeType to find it out.
    /// </summary>
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
    /// <summary>
    /// Size of the file as double. this can be in the unit of bytes, megabytes gigabytes etc. Check SizeType to find it out.
    /// </summary>
    public long FileSizeAsBytes
    {
        get
        {
            return _FileSizeAsBytes;
        }
        private set
        {
            _FileSizeAsBytes = value;
        }
    }
    /// <summary>
    /// Type of the file Size which can be kb, mb, gb, tb...
    /// </summary>
    public Communication.SizeTypes FilesizeType
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
    public FileOperations()
    {
    }
    public void Init(string FilePath, TransferMode transferMode)
    {
        this.FilePath = FilePath;                                       /// Assign path to FilePath variable
        if (transferMode == TransferMode.Receive)
        {
            if(FileName== null || FileName=="")
            {
                Debug.WriteLine("Init File Ops: Filename is null!");
                return;
            }
            Debug.WriteLine("File is Created: " + (FilePath + "\\" + FileName));
            Fs = File.OpenWrite(FilePath +"\\"+ FileName);
            return;
        }
        Fs = File.OpenRead(FilePath);                                   /// Open File
        char[] splitterUsta = { '\\', '/' };                                     /// Define splitter array that will be used to find file name
        string[] nameArray = FilePath.Split(splitterUsta);              /// Split path string to array by '/' sign
        this.FileName = nameArray[nameArray.Length - 1];                /// Get the last string which will be the file name as "filename.extension"
        long fileSizeAsByte = Fs.Length;                                /// Get the Total length of the file as bytes
        _FileSizeAsBytes = fileSizeAsByte;                              /// Store File Length as bytes in a global variable
        int pow = (int)Math.Log(fileSizeAsByte, 1024);                  /// calculate the greatest type ( byte megabyte gigabyte etc...) the filesize can be respresent as integer variable
        FileSize = fileSizeAsByte / Math.Pow(1024, pow);                /// Convert file size from bytes to the greatest type
        switch (pow)                                                    /// to assign type:
        {
            case 0:                                                     /// if pow equals to 0
                FilesizeType = Communication.SizeTypes.Byte;            /// then the type is bytes
                break;
            case 1:                                                     /// if pow equals to 1 
                FilesizeType = Communication.SizeTypes.KB;              /// then the type is kilobytes and so on
                break;
            case 2:
                FilesizeType = Communication.SizeTypes.MB;
                break;
            case 3:
                FilesizeType = Communication.SizeTypes.GB;
                break;
            case 4:
                FilesizeType = Communication.SizeTypes.TB;
                break;
        }
    }
    public void FileReadAtByteIndex(long BufferIndx, out int BytesRead, out byte[] Buffer, int chunkSize = 1024)
    {
        byte[] chunk = new byte[chunkSize];
        Fs.Position = BufferIndx;
        BytesRead = Fs.Read(chunk, 0, chunkSize);
        Buffer = new byte[BytesRead];
        Array.Copy(chunk, 0, Buffer, 0, BytesRead);
    }
    public void FileWriteAtByteIndex(long BufferIndx, byte[] Buffer)
    {
        Debug.WriteLine("Buffer Len: " + Buffer.Length);
        Fs.Position = BufferIndx;
        Fs.Write(Buffer, 0, Buffer.Length);
    }
    public void CloseFile()
    {
        Fs.Close();
    }
}
