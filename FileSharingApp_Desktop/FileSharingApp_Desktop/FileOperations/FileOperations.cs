using System;
using System.Diagnostics;
using System.IO;
public class FileOperations
{
    #region Public Variables

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
    /// Size of the file as int64. this is the length of file as bytes.
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
    public SizeUnit FileSizeUnit
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

    #endregion

    #region Enums

    /// <summary>
    /// Determines if this device will send a file or receive.
    /// </summary>
    public enum TransferMode
    {
        Send,
        Receive
    }
    /// <summary>
    /// Size unit of the file
    /// </summary>
    public enum SizeUnit
    {
        Byte = 0,
        KB,
        MB,
        GB,
        TB,
        none
    }

    #endregion

    #region Private Variables

    private static object lock_FileName = new object();
    private string _FilePath;
    private string _FileName;
    private double _FileSize;
    private long _FileSizeAsBytes;
    private Stream Fs;
    private SizeUnit _sizeTypes;

    #endregion

    #region Public Functions

    /// <summary>
    /// Create File Operation
    /// </summary>
    /// <param name="FilePath">Reading address of the file to be sent or the address to save the received file</param>
    public FileOperations()
    {
    }
    public void Init(string filePath, TransferMode transferMode)
    {
        this.FilePath = filePath;                                       /// Assign path to FilePath variable
        if (string.IsNullOrEmpty(FilePath))
        {
            Debug.WriteLine("Init File Ops: FilePath is null!");
            return;
        }
        else
        {
            char[] splitterUsta = { '\\', '/' };                            /// Define splitter array that will be used to find file name
            string[] nameArray = FilePath.Split(splitterUsta);              /// Split path string to array by '/' sign
            this.FileName = nameArray[nameArray.Length - 1];                /// Get the last string which will be the file name as "filename.extension"
        }
        if (transferMode == TransferMode.Receive)
        {
            Debug.WriteLine("File is Created: " + FilePath);
            Fs = File.OpenWrite(FilePath);
            return;
        }
        else
            Fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);                             /// Open File
        long fileSizeAsByte = Fs.Length;                                /// Get the Total length of the file as bytes
        FileSizeAsBytes = fileSizeAsByte;                              /// Store File Length as bytes in a global variable
        CalculateFileSize(FileSizeAsBytes);
    }
    public void CalculateFileSize(long fileSizeAsByte)
    {
        int pow = (int)Math.Log(fileSizeAsByte, 1024);                  /// calculate the greatest type ( byte megabyte gigabyte etc...) the filesize can be respresent as integer variable
        FileSize = fileSizeAsByte / Math.Pow(1024, pow);                /// Convert file size from bytes to the greatest type
        FileSizeUnit = (SizeUnit)pow;                                   /// Assign the unit
    }
    public void FileReadAtByteIndex(long BufferIndx, out int BytesRead, out byte[] Buffer, int chunkSize = 1024, byte functionByte = 0)
    {
        byte[] chunk = new byte[chunkSize];
        Fs.Position = BufferIndx;
        BytesRead = Fs.Read(chunk, 0, chunkSize);
        Buffer = new byte[BytesRead + 1];
        Buffer[0] = functionByte;
        Array.Copy(chunk, 0, Buffer, 1, BytesRead);
    }
    public void FileWriteAtByteIndex(long BufferIndx, byte[] Buffer)
    {
        Fs.Position = BufferIndx;
        Fs.Write(Buffer, 1, Buffer.Length - 1);
    }
    public void CloseFile()
    {
        if (Fs != null)
            Fs.Close();
    }

    #endregion
}
