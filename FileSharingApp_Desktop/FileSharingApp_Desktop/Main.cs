using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class Main
{
    #region Parameters

    private static readonly byte StartByte = (byte)'J';
    private static readonly int BufferSize = 64 * 1024 - 1;
    private static readonly int Port = 42009;

    #endregion

    #region Public Variables
    /// <summary>
    /// This Event is thrown when a client is connected and wants to send this device some files.
    /// </summary>
    /// <param name="files">files to be sent by client</param>
    /// <returns></returns>
    public delegate void ClientRequestDelegate(string totalTransferSize, string senderDevice,bool isAlreadyAccepted=false);
    public static event ClientRequestDelegate OnClientRequested;

    public delegate void TransferFinishedDelegate();
    public static event TransferFinishedDelegate OnTransferFinished;
    public static event TransferFinishedDelegate OnTransferAborted;

    public delegate void TransferRespondedDelegate(bool isAccepted);
    public static event TransferRespondedDelegate OnTransferResponded;

    public static string FileSaveURL = "/storage/emulated/0/Download/";
    public static string ServerIP;
    public static string ClientIP;
    public static Metrics TransferMetrics
    {
        get
        {
            lock (Lck_TransferMetrics)
                return _transferMetrics;
        }
        private set
        {
            lock (Lck_TransferMetrics)
                _transferMetrics = value;
        }
    }
    public static bool IsTransfering
    {
        get
        {
            lock (Lck_IsTransfering)
                return _isTransfering;
        }
        set
        {
            lock (Lck_IsTransfering)
                _isTransfering = value;
        }
    }
    public static bool IsSending
    {
        get
        {
            lock (Lck_IsSending)
                return _isSending;
        }
        set
        {
            lock (Lck_IsSending)
                _isSending = value;
        }
    }
    private static bool IsTransferEnabled
    {
        get
        {
            lock (Lck_IsTransferEnabled)
                return _isTransferEnabled;
        }
        set
        {
            lock (Lck_IsTransferEnabled)
                _isTransferEnabled = value;
        }
    }
    #endregion

    #region Private Variables

    private static Client client;
    private static Server server;
    private static FileOperations File;
    public static string[] FilePaths;
    public static string[] FileNames;                     /// Name of Files
    private static long[] FileSizeAsBytes;                 /// Size of files as bytes
    private static double[] FileSizes;                     /// File Sizes as a double 
    private static FileOperations.SizeUnit[] SizeUnits;    /// Unit of filesizes
    private static Thread sendingThread;
    private static bool _isTransferEnabled = false;
    private static bool _isTransfering = false;
    private static bool _isSending = false;
    private static FileStruct CurrentFile;
    private static Metrics _transferMetrics;
    private static int MB = 1024 * 1024;
    private static readonly int ChunkSize = (int)(0.1 * MB);
    #endregion

    #region Lock Objects
    private static object Lck_IsTransferEnabled = new object();
    private static object Lck_TransferMetrics = new object();
    private static object Lck_IsTransfering = new object();
    private static object Lck_IsSending = new object();
    #endregion

    #region Enums and structures definitions
    public enum Functions
    {
        QueryTransfer,              /// || Number of files || Size of All Data (8 bytes) || length of deviceName(1 byte) || deviceNameBytes || length of folder name (zero : if no folder given) || name of folder ==> AcceptFiles or RejectFiles as response
        StartofFileTransfer,        /// || length of file name || name || size of file (8 bytes) ==> true when receiver is ready as response
        EndofFileTransfer,          /// || 4 bytes (spare) ==> no response expected   ( receiver should save the file)
        TransferMode,               /// || fileBytes
        AllisSent,                  /// || no bytes needed ==> no response expected
        AcceptFiles,                /// Response to QueryTransfer function
        RejectFiles,                /// Response to QueryTransfer function
        Ready,                      /// Ready to receive file (response from receiver to StartofFileTransfer)
        Aborted                     /// When any of the device aborts the transfer
    }
    public struct FileStruct
    {
        public string FilePath;                     /// Path of File
        public string FileName;                     /// Name of File 
        public double FileSize;                     /// Size of file
        public FileOperations.SizeUnit SizeUnit;    /// unit of size
        public long FileSizeAsBytes;                /// Size of file as bytes
    }
    public struct Metrics
    {
        public FileStruct CurrentFile;
        public double TransferSpeed;        /// MB/s
        public int CountOfFiles;
        public int IndexOfCurrentFile;
        public long TotalDataSizeAsBytes;
        public long TotalBytesSent;
        public double TotalDataSize;        /// MB KB GB...
        public double TotalDataSent;        /// MB KB GB...
        public FileOperations.SizeUnit SizeUnit;
        public FileOperations.SizeUnit SentSizeUnit;
        public double Progress;             /// between 0 and 100
        public double TotalElapsedTime;     /// Seconds
        public double EstimatedTime;        /// Seconds
    }
    #endregion

    #region Common Functions

    /// <summary>
    /// Setups a server and listens the port asyncroniously.
    /// All devices should Start a server on start up.
    /// </summary>
    public static void StartServer()
    {
        if (server != null)
            return;
        server = new Server(port: Port,ip:NetworkScanner.MyIP, bufferSize: BufferSize, StartByte: StartByte);
        ServerIP = server.SetupServer();
        server.StartListener();
        server.OnClientConnected += Server_OnClientConnected;
        IsTransferEnabled = true;
    }
    private static byte[] PrepareAbortMessage()
    {
        byte[] data = new byte[2];
        data[0] = (byte)Functions.Aborted;
        return data;
    }
    #endregion

    #region Sender Functions

    #region Public Functions

    public static void SetFilePaths(string[] paths)
    {
        FilePaths = new string[paths.Length];
        FileNames = new string[paths.Length];
        paths.CopyTo(FilePaths, 0);
        var file = new FileOperations();
        for (int i = 0; i < FilePaths.Length; i++)
        {
            FileNames[i] = file.GetFileName(paths[i]);
            Debug.WriteLine(i + " : " + FilePaths[i]);
        }
    }
    public static void ConnectToTargetDevice(string IP)
    {
        IsTransferEnabled = true;
        client = new Client(port: Port, ip: IP, bufferSize: BufferSize, StartByte: StartByte);
        ServerIP = client.ConnectToServer();
        Debug.WriteLine("Server IP: " + ServerIP);
        _transferMetrics = new Metrics();
        SendFirstFrame();
        Task.Run(() =>
        {
            byte[] clientResponse = client.GetData();
            bool isAccepted = false;
            if (clientResponse != null)
            {
                if ((Functions)clientResponse[0] == Functions.AcceptFiles)
                    isAccepted = true;
            }
            Debug.WriteLine("Rising event");
            if (OnTransferResponded != null)
                OnTransferResponded(isAccepted);
            else
            {
                client.DisconnectFromServer();
                client = null;
            }
        });

    }
    public static void BeginSendingFiles()
    {
        IsTransferEnabled = true;
        sendingThread = new Thread(SendingCoreFcn);
        sendingThread.Start();
    }
    public static void AbortTransfer()
    {
        IsTransferEnabled = false;
    }

    #endregion

    #region Private Functions
    private static void SendingCoreFcn()
    {
        lock (Lck_TransferMetrics)
        {
            _transferMetrics.CountOfFiles = FilePaths.Length;
            _transferMetrics.TotalBytesSent = 0;
            _transferMetrics.TotalDataSent = 0;
        }
        Stopwatch watch = Stopwatch.StartNew();
        IsTransfering = true;
        for (int i = 0; i < FilePaths.Length; i++)
        {
            SendFileInformation(i);
            if (!WaitforReceiverToBeReady())
            {
                Debug.WriteLine("receiver sent not ready");
                continue;
            }
            File = new FileOperations();
            File.Init(FilePaths[i], FileOperations.TransferMode.Send);
            CurrentFile.FilePath = File.FilePath;
            CurrentFile.FileName = File.FileName;
            CurrentFile.FileSize = File.FileSize;
            CurrentFile.FileSizeAsBytes = File.FileSizeAsBytes;
            CurrentFile.SizeUnit = File.FileSizeUnit;
            lock (Lck_TransferMetrics)
            {
                _transferMetrics.CurrentFile = CurrentFile;
                _transferMetrics.IndexOfCurrentFile = i + 1;
            }
            long byteCounter = 0;
            long totalBytesRead = 0;
            int numberOfBytesRead = 0;
            byte[] buffer;

            watch.Restart();
            while (IsTransferEnabled)
            {
                File.FileReadAtByteIndex(totalBytesRead, out numberOfBytesRead, out buffer, chunkSize: (int)(ChunkSize + TransferMetrics.TransferSpeed * MB * 0.3), functionByte: (byte)Functions.TransferMode);
                if (numberOfBytesRead == 0)
                {
                    UpdateMetrics(watch, byteCounter);
                    watch.Restart();
                    break;
                }
                client.SendDataServer(buffer);
                totalBytesRead += numberOfBytesRead;
                byteCounter += numberOfBytesRead;
                if (!CheckAck(Functions.TransferMode))
                {
                    client.DisconnectFromServer();
                    client = null;
                    server.CloseServer();
                    server = null;
                    StartServer();
                    if (File != null)
                        File.CloseFile();
                    return;
                }

                if (watch.Elapsed.TotalSeconds >= 0.5)
                {
                    UpdateMetrics(watch, byteCounter);
                    byteCounter = 0;
                    watch.Restart();
                }
                if (totalBytesRead == File.FileSizeAsBytes)
                {
                    UpdateMetrics(watch, byteCounter);
                    watch.Restart();
                    break;
                }
            }
            File.CloseFile();
            if (!IsTransferEnabled)
            {
                byte[] abortData = PrepareAbortMessage();
                client.SendDataServer(abortData);
                break;
            }
            byte[] endBytes = new byte[5];
            endBytes[0] = (byte)Functions.EndofFileTransfer;
            client.SendDataServer(endBytes);
            CheckAck(Functions.EndofFileTransfer);

        }
        IsTransfering = false;
        SendLastFrame();
        if (OnTransferFinished != null)
            OnTransferFinished();
        client.DisconnectFromServer();
        client = null;
        if (server != null) ;
        server.CloseServer();
        server = null;
        StartServer();
    }
    private static bool CheckAck(Functions func)
    {
        byte[] data = client.GetData();
        bool ack = false;
        if (data != null)
        {
            if (data.Length > 0)
            {
                if (data[0] == (byte)func)
                {
                    ack = true;
                }
                else if (data[0] == (byte)Functions.Aborted)
                {
                    if (OnTransferAborted != null)
                        OnTransferAborted();
                    IsTransfering = false;
                }
            }
        }
        return ack;
    }
    private static void UpdateMetrics(Stopwatch watch, long byteCount)
    {
        double elapsedTime = watch.Elapsed.TotalSeconds;
        Task.Run(() => UpdateTransferMetrics(byteCount, elapsedTime));
    }
    private static void UpdateTransferMetrics(long byteCounter, double elapsedTime)
    {
        lock (Lck_TransferMetrics)
        {
            _transferMetrics.TotalBytesSent += byteCounter;
            _transferMetrics.TransferSpeed = (_transferMetrics.TransferSpeed * 0.9 + 0.1 * (byteCounter / (MB * elapsedTime)));
            _transferMetrics.Progress = ((double)_transferMetrics.TotalBytesSent / (double)_transferMetrics.TotalDataSizeAsBytes) * 100.0;
            _transferMetrics.TotalElapsedTime += elapsedTime;
            _transferMetrics.EstimatedTime = (_transferMetrics.TotalDataSizeAsBytes - _transferMetrics.TotalBytesSent) / (_transferMetrics.TotalBytesSent / _transferMetrics.TotalElapsedTime);
            var file = new FileOperations();
            file.CalculateFileSize(_transferMetrics.TotalDataSizeAsBytes);
            _transferMetrics.TotalDataSize = file.FileSize;
            _transferMetrics.SizeUnit = file.FileSizeUnit;
            file.CalculateFileSize(_transferMetrics.TotalBytesSent);
            _transferMetrics.TotalDataSent = file.FileSize;
            _transferMetrics.SentSizeUnit = file.FileSizeUnit;
        }
    }
    private static void SendFirstFrame()
    {
        long totalTransferSize = GetTransferSize();
        int lenName = Parameters.DeviceName.Length;
        byte[] data = new byte[15 + lenName];
        data[0] = (byte)Functions.QueryTransfer;
        byte[] numFilesBytes = BitConverter.GetBytes(FilePaths.Length);
        numFilesBytes.CopyTo(data, 1);
        byte[] sizeBytes = BitConverter.GetBytes(totalTransferSize);
        sizeBytes.CopyTo(data, 5);
        data[13] = (byte)lenName;
        Encoding.UTF8.GetBytes(Parameters.DeviceName).CopyTo(data, 14);
        client.SendDataServer(data);
        Debug.WriteLine("Sent First Frame:" + FilePaths.Length);
    }
    private static void SendLastFrame()
    {
        byte[] data = new byte[2];
        data[0] = (byte)Functions.AllisSent;
        client.SendDataServer(data);
    }
    private static void SendFileInformation(int indexOfFile)
    {
        byte[] nameBytes = Encoding.UTF8.GetBytes(FileNames[indexOfFile]);
        int nameLen = nameBytes.Length;
        byte[] data = new byte[nameLen + 10];
        data[0] = (byte)Functions.StartofFileTransfer;
        data[1] = (byte)nameLen;
        nameBytes.CopyTo(data, 2);
        byte[] lengthBytes = BitConverter.GetBytes(FileSizeAsBytes[indexOfFile]);
        lengthBytes.CopyTo(data, nameLen + 2);
        client.SendDataServer(data);
    }
    private static bool WaitforReceiverToBeReady()
    {
        byte[] receivedData = client.GetData();
        if (receivedData == null)
            return false;
        bool isReady = false;
        if (receivedData.Length > 0)
        {
            if ((Functions)receivedData[0] == Functions.Ready)
                isReady = true;
        }
        return isReady;
    }
    private static long GetTransferSize()
    {
        long totalTransferSize = 0;     // bytes
        File = new FileOperations();
        int len = FilePaths.Length;
        FileNames = new string[len];
        FileSizeAsBytes = new long[len];
        FileSizes = new double[len];
        SizeUnits = new FileOperations.SizeUnit[len];
        for (int i = 0; i < FilePaths.Length; i++)
        {
            File.Init(FilePaths[i], FileOperations.TransferMode.Send);
            totalTransferSize += File.FileSizeAsBytes;
            FileNames[i] = File.FileName;
            //FilePaths[i] = File.FilePath;
            FileSizeAsBytes[i] = File.FileSizeAsBytes;
            FileSizes[i] = File.FileSize;
            SizeUnits[i] = File.FileSizeUnit;
            File.CloseFile();
        }
        lock (Lck_TransferMetrics)
            _transferMetrics.TotalDataSizeAsBytes = totalTransferSize;
        return totalTransferSize;
    }

    #endregion

    #endregion

    #region Receiver Functions

    #region Public Functions

    public static void ResponseToTransferRequest(bool isAccepted)
    {
        byte[] data = new byte[1];
        if (isAccepted)
        {
            data[0] = (byte)Functions.AcceptFiles;
            BeginReceivingFiles();
        }
        else
            data[0] = (byte)Functions.RejectFiles;
        server.SendDataToClient(data);
        if (!isAccepted)
        {
            Task.Run(() =>
            {
                Thread.Sleep(500);
                try
                {
                    server.CloseServer();
                    server = null;
                    StartServer();
                }
                catch
                {

                }
            });
        }
    }

    #endregion

    #region Private Functions
    private static void Server_OnClientConnected(string clientIP)
    {
        _transferMetrics = new Metrics();
        ClientIP = clientIP;
        byte[] receivedData = server.GetData();
        if (receivedData == null)
            return;
        if (receivedData[0] == (byte)Functions.QueryTransfer)
        {
            int numberOfFiles = BitConverter.ToInt32(receivedData, 1);
            long transferSize = BitConverter.ToInt64(receivedData, 5);
            int nameLen = receivedData[13];
            string senderDevice = Encoding.UTF8.GetString(receivedData, 14, nameLen);
            FilePaths = new string[numberOfFiles];
            if (File == null)
                File = new FileOperations();
            File.CalculateFileSize(transferSize);
            string fileSizeString = File.FileSize.ToString("0.00") + " " + File.FileSizeUnit.ToString();
            Debug.WriteLine("numberOfFiles: " + numberOfFiles + " transfer size: " + fileSizeString + " device Name: " + senderDevice);
            IsSending = false;
            if (Parameters.AcceptAllRequests)
            {
                ResponseToTransferRequest(true);
                OnClientRequested?.Invoke(fileSizeString, senderDevice,true);

            }
            else
            {
                if(OnClientRequested!=null)
                    OnClientRequested?.Invoke(fileSizeString, senderDevice);
                else
                    ResponseToTransferRequest(false);

            }
            _transferMetrics.TotalDataSizeAsBytes = transferSize;
        }
    }

    private static void BeginReceivingFiles()
    {
        IsTransferEnabled = true;
        sendingThread = new Thread(ReceivingCoreFcn);
        sendingThread.Start();

    }
    private static void SendAck(Functions func)
    {
        byte[] data = new byte[1];
        data[0] = (byte)func;
        server.SendDataToClient(data);
    }
    private static void ReceivingCoreFcn()
    {
        lock (Lck_TransferMetrics)
        {
            _transferMetrics.TotalBytesSent = 0;
            _transferMetrics.TotalDataSent = 0;
            _transferMetrics.CountOfFiles = FilePaths.Length;
        }
        Stopwatch watch = Stopwatch.StartNew();
        IsTransfering = true;
        FileNames = new string[FilePaths.Length];
        for (int i = 0; i < FilePaths.Length; i++)
        {
            GetCurrentFileName();
            FileNames[i] = CurrentFile.FileName;
            FilePaths[i] = FileSaveURL + CurrentFile.FileName;
            SendReadySignal();
            File = new FileOperations();
            Debug.WriteLine("receiveing for: " + i + " FilePaths.Length: " + FilePaths.Length);
            File.Init(FileSaveURL + CurrentFile.FileName, FileOperations.TransferMode.Receive);
            Debug.WriteLine("saveURL:" + FileSaveURL + " name: " + CurrentFile.FileName);
            lock (Lck_TransferMetrics)
            {
                _transferMetrics.CurrentFile = CurrentFile;
                _transferMetrics.IndexOfCurrentFile = i + 1;
            }
            long byteCounter = 0;
            long totalBytesWritten = 0;
            int numberOfBytesRead = 0;
            watch.Restart();
            while (IsTransferEnabled)
            {
                byte[] receivedData = server.GetData();
                if (receivedData == null)
                {
                    UpdateMetrics(watch, byteCounter);
                    watch.Restart();
                    break;
                }
                if (receivedData[0] == (byte)Functions.TransferMode)
                {
                    SendAck(Functions.TransferMode);
                    File.FileWriteAtByteIndex(totalBytesWritten, receivedData);
                    numberOfBytesRead = receivedData.Length - 1;
                    totalBytesWritten += numberOfBytesRead;
                    byteCounter += numberOfBytesRead;

                    if (watch.Elapsed.TotalSeconds >= 0.5)
                    {
                        UpdateMetrics(watch, byteCounter);
                        byteCounter = 0;
                        watch.Restart();
                    }
                }
                else if (receivedData[0] == (byte)Functions.EndofFileTransfer)
                {
                    SendAck(Functions.EndofFileTransfer);
                    UpdateMetrics(watch, byteCounter);
                    watch.Restart();
                    break;
                }
                else if (receivedData[0] == (byte)Functions.AllisSent)
                {
                    UpdateMetrics(watch, byteCounter);
                    watch.Restart();
                    server.CloseServer();
                    server = null;
                    IsTransfering = false;
                    StartServer();
                    if (File != null)
                        File.CloseFile();
                    OnTransferFinished();
                    return;
                }
                else if (receivedData[0] == (byte)Functions.Aborted)
                {
                    server.CloseServer();
                    server = null;
                    IsTransfering = false;
                    StartServer();
                    if (File != null)
                        File.CloseFile();
                    if (OnTransferAborted != null)
                        OnTransferAborted();
                    return;
                }
                else
                {
                    Debug.WriteLine("ReceivingCoreFcn :function byte was wrong:");
                    break;
                }
            }
            File.CloseFile();
            if (!IsTransferEnabled)
            {
                byte[] abortData = PrepareAbortMessage();
                server.SendDataToClient(abortData);
                break;
            }
        }
        if (server != null)
        {
            server.GetData();
            if (OnTransferFinished != null)
                OnTransferFinished();
            server.CloseServer();
            server = null;
        }
        IsTransfering = false;
        StartServer();
    }

    private static void SendReadySignal()
    {
        byte[] data = new byte[1];
        data[0] = (byte)Functions.Ready;
        server.SendDataToClient(data);
    }
    private static void GetCurrentFileName()
    {
        byte[] receivedData = server.GetData();
        if (receivedData == null)
        {
            Debug.WriteLine("GetCurrentFileName: received dataa was null");
            return;
        }
        if (receivedData[0] == (byte)Functions.StartofFileTransfer)
        {
            byte nameLen = receivedData[1];
            string fileName = Encoding.UTF8.GetString(receivedData, 2, nameLen);
            long fileSizeAsBytes = BitConverter.ToInt64(receivedData, nameLen + 2);
            CurrentFile.FilePath = FileSaveURL;
            CurrentFile.FileName = fileName;
            CurrentFile.FileSizeAsBytes = fileSizeAsBytes;
            if (File == null)
                File = new FileOperations();
            File.CalculateFileSize(fileSizeAsBytes);
            CurrentFile.FileSize = File.FileSize;
            CurrentFile.SizeUnit = File.FileSizeUnit;
        }
        else
            Debug.WriteLine("GetCurrentFileName: function byte was wrong: " + receivedData[0]);
    }
    #endregion

    #endregion
}
