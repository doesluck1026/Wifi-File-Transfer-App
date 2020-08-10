using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Communication pack structure should be like below:
///     [Header Pack= 7 bytes long] + [Data Bytes = (2^40)-1 bytes long]
/// Header Pack Data Structure:
///     [Start Byte] + [Function Byte] + [Spare Byte] + [Length Bytes=4 bytes Length]
///         -> Start byte is ascii 74 character and same in every packs
///         -> Function Byte specifies what the whole pack is about and can be one of the Functions enum which defined below.
///         -> Length bytes specifies the length of the bytes that come after header bytes. these bytes are encoded as little endien.
/// Data Bytes Data Structure:
///     [region1 length= 2 bytes] + [region1 bytes] + [region2 length= 2 bytes] + [region2 bytes]....
/// </summary>


class Communication
{
    private static Server server;
    private static Client client;
    private static readonly int HeaderLen = 7;
    private static readonly byte StartByte = (byte)'J';
    private static int Port = 41000;


    public static bool isClientConnected = false;
    public static bool isConnectedToServer = false;
    public static uint LastPackNumberReceived {get; private set;}
    public static uint LastPackNumberSent {get; private set; }
    public static int NumberOfPacks { get; private set; }
    public static bool isFileReceived { get; private set; }

    public enum SizeTypes
    {
        Byte=0,
        KB=1,
        MB,
        GB,
        TB
    }
    private enum  Functions
    {
        QueryTransfer=2,
        SendingFile=3,
        FileSent=4,
        TransferStatus=5,
    }

    #region Server Functions
    /// <summary>
    /// Creates a server and starts listening to port. This is used to send file to another device.
    /// </summary>
    /// <returns></returns>
    public static bool CreateServer()
    {
        server = new Server(Port);                      /// Create server instance
        string serverIP=server.SetupServer();           /// Setup Server on default port. this Function will return device ip as string.
       
        string code = GenerateTransferCode(serverIP);   /// Generate a code to secure transfer
        // display the code in ui here
        
        isClientConnected =server.StartListener();      /// Start Listener for possible clients.
        return isClientConnected;                       /// return connection status
    }
    /// <summary>
    /// Sends the file specs to the receiver. the receiver will send a response after receiving tihs query.
    /// </summary>
    /// <param name="fileName">Name of File to be transfered</param>
    /// <param name="fileSize">Size of the File</param>
    /// <param name="sizeType">type of size like MB, GB etc..</param>
    public static bool QueryForTransfer(string fileName, double fileSize, SizeTypes sizeType)
    {
        if (fileName == null || fileName.Length == 0)                                       /// Check if file is selected correctly
            return false;
        byte[] nameBytes = Encoding.ASCII.GetBytes(fileName);                               /// Get the byte array of file name
        int lenName = nameBytes.Length;                                                     /// Calculate the length of name bytes 
        byte[] sizeBytes = BitConverter.GetBytes(fileSize);                                 /// Get the byte array of file
        int lenSize = sizeBytes.Length;                                                     /// Calculate the length of file size
        int DataLen = lenName + lenSize + 1 + 4 + 2;                                        /// Get the total length of bytes that come after header bytes.
        byte[] HeaderBytes = PrepareDataHeader(Functions.QueryTransfer, (uint)DataLen);     /// Prepare Data Header.
        byte[] DataToSend = new byte[DataLen + HeaderLen];                                  /// Create Data Packet to carry all the data.
        Array.Copy(HeaderBytes, 0, DataToSend, 0, HeaderBytes.Length);                      /// Copy header bytes to Data Pack
        DataToSend[HeaderLen] = (byte)(lenName & 0xff);                                     /// Write the length of name bytes 
        DataToSend[HeaderLen+1] = (byte)((lenName>>8) & 0xff);
        Array.Copy(nameBytes, 0, DataToSend, HeaderLen + 2, lenName);                       /// Copy name bytes to Data Pack
        int IndexSize = HeaderLen + 2 + lenName;                                            /// Calculate the Index of sizeBytes
        Array.Copy(sizeBytes, 0, DataToSend, IndexSize, lenSize);                           /// Copy Size Bytes to DData Pack
        int IndexType = IndexSize + lenSize;                                                /// Calculate the Index of sizeType
        DataToSend[IndexType] = (byte)sizeType;                                             /// Write Size Type to Data pack as enum
        int packageCount= CalculatePackageCount(fileSize, sizeType);                        /// Calculaate the number of Data Packs rewuired to send all of the file to the client.
        NumberOfPacks = packageCount;                                                       /// Store this value for later use
        byte[] packageCountBytes = BitConverter.GetBytes(packageCount);                     /// Write the number of packs to dataa pack
        Array.Copy(packageCountBytes, 0, DataToSend, IndexType + 1, packageCountBytes.Length);  
        return server.SendDataToClient(DataToSend);                                         /// Send Data pack to the client.
    }
    /// <summary>
    /// This function is used to Get and analyze the response that client sent.
    /// </summary>
    /// <returns>Returns the state of acceptance</returns>
    public static bool GetResponse()
    {
        bool isAccepted = false;                                                                /// define return value
        byte[] ReceivedData=server.GetData();                                                   /// Get Received Data from buffer
        if(ReceivedData[1]==(byte)Functions.QueryTransfer)                                      /// Check if Function byte is about query for Transfer
        {
            if(ReceivedData[HeaderLen]==1)                                                      /// Get the response byte. 1 = True ; 0=false
            {
                Debug.WriteLine("Starting transfer");
                isAccepted = true;
            }
            else
            {
                Debug.WriteLine("File Declined!");
            }
        }
        else if (ReceivedData[1] == (byte)Functions.TransferStatus)                         /// Check if the function is about transfer status.( This is used to ack each package)
        {
            if (ReceivedData[HeaderLen] == 1)                                               /// 1=True
            {
                uint LastPackReceived = BitConverter.ToUInt32(ReceivedData,HeaderLen+2);    /// Get Get the index of the last package that client has received
                LastPackNumberReceived = LastPackReceived;                                  /// Store it in a global value.
                isAccepted = true;
            }
        }
            return isAccepted;
    }
    /// <summary>
    /// Sends File packs to the client and gets the acknowledge
    /// </summary>
    /// <param name="data">File Pack bytes (Max 32 Kb)</param>
    /// <param name="numPackage">Index of the data pack to be sent</param>
    /// <returns>Returns Acknowledge</returns>
    public static bool SendFilePacks(byte[] data,uint numPackage)
    {
        byte[] HeaderBytes = PrepareDataHeader(Functions.SendingFile, (uint)(data.Length + 4));     /// Prepare Data Header for given length. +4 is for to specify current package index.
        byte[] DataToSend = new byte[data.Length + 4 + HeaderLen];                                  /// Create carrier data pack
        Array.Copy(HeaderBytes, 0, DataToSend, 0, HeaderLen);                                       /// Copy Header bytes to the carrier
        Array.Copy(BitConverter.GetBytes(numPackage), 0, DataToSend, HeaderLen, sizeof(int));       /// Copy Index bytes to carrier pack
        Array.Copy(data, 0, DataToSend, HeaderLen+4, data.Length);                                  /// Copy given data bytes to carrier pack
        server.SendDataToClient(DataToSend);                                                        /// Send data to client.
        LastPackNumberSent = numPackage;                                                            /// Update Index
        return GetResponse();                                                                       /// Get the Ack from client.
    }
    /// <summary>
    /// Let the receiver know that all of the file bytes has been sent
    /// </summary>
    /// <returns>Returns the state of Transfer.</returns>
    public static bool CompleteTransfer()
    {
        byte[] HeaderBytes = PrepareDataHeader(Functions.FileSent, 1);  /// Prepare Header bytes.
        byte[] DataToSend = new byte[HeaderLen+1];                      /// Create Carrier Pack
        Array.Copy(HeaderBytes, 0, DataToSend, 0, HeaderLen);           /// Copy Header bytes to the carrier.
        server.SendDataToClient(DataToSend);                            /// Send Carrier pack to the client.
        return GetResponse();                                           /// Get the Response. This will return true if client confirms the transfer.
    }   
    /// <summary>
    ///  Generates a code to secure Transfer. This Code Contains the last threenumbers of servers ip and 3 random characters.
    ///  This code should be entered to client device to start transfer.
    /// </summary>
    /// <param name="ip">ip address of server device</param>
    /// <returns>Returns the code as string</returns>
    private static string GenerateTransferCode(string ip)
    {
        char[] splitterUsta = { '.' };              /// Define the seperator for ip address.
        string ipEnd = ip.Split(splitterUsta)[3];   /// split the ip string. this will return four different strings. for Example: if ip is "192.168.1.100", this code will return "100"
        if (ipEnd.Length==1)                        /// if the length of string is 1
                ipEnd = "00" + ipEnd;               /// add 2 zeros to string 
        else if (ipEnd.Length == 2)                 /// so on
            ipEnd = "0" + ipEnd;
        Debug.WriteLine("ipend=" + ipEnd);
        Random random = new Random();
        int head = random.Next(100, 1000);          /// Get random  3 digits integer
        string code = head.ToString() + ipEnd;      /// put the ip part and the random number together.    
        return code;                                /// return the code
    }
    #endregion

    #region Client Functions
    /// <summary>
    /// Connects to server with given ip at given port. This is used to receive file from another device.
    /// </summary>
    /// <param name="ip"></param>
    /// <param name="port"></param>
    /// <returns></returns>
    public static bool ConnectToServer(string ip)
    {
        client = new Client(ip, Port);
        isConnectedToServer = client.ConnectToServer();
        return isConnectedToServer;
    }
    /// <summary>
    /// Gets and retruns file specs to be received 
    /// </summary>
    /// <param name="fileName">name of the file icluding extension</param>
    /// <param name="fileSize">size of the file which is a double and can be in any type (mb,kb,gb...)</param>
    /// <param name="sizeType">type of the size</param>
    public static void GetFileSpecs(out string fileName, out double fileSize, out SizeTypes sizeType)
    {
        fileName = "";
        fileSize = 0;
        sizeType = SizeTypes.Byte;
        byte[] receivedData = client.GetData();
        if (receivedData[0] == StartByte)
        {
            if (receivedData[1] == (byte)Functions.QueryTransfer)
            {
                int dataLen = BitConverter.ToInt32(receivedData, 3);
                if (dataLen == receivedData.Length - HeaderLen)
                {
                    int nameLen = receivedData[HeaderLen] | (receivedData[HeaderLen + 1] << 8);
                    fileName = Encoding.ASCII.GetString(receivedData, HeaderLen + 2, nameLen);
                    int IndexSize = HeaderLen + 2 + nameLen;
                    fileSize = BitConverter.ToDouble(receivedData, IndexSize);
                    sizeType = (SizeTypes)receivedData[IndexSize + sizeof(double)];
                    NumberOfPacks = BitConverter.ToInt32(receivedData,IndexSize+1);
                    Debug.WriteLine("Sending request received: Filename:" + fileName + " size:" + fileSize + sizeType.ToString());
                }
                else
                {
                    Debug.WriteLine("GetFileSpecs function: data length does not match! told:" + dataLen + " but received: " + (receivedData.Length - HeaderLen));
                }
            }
            else
            {
                Debug.WriteLine("GetFileSpecs function: function byte was Wrong:" + receivedData[1]);
            }
        }
        else
        {
            Debug.WriteLine("GetFileSpecs function: Start Byte was Wrong: " + receivedData[0]);
        }
    }
    /// <summary>
    /// This function is used to response the file transfer request.
    /// </summary>
    /// <param name="isAccepted">True to accept file, false to decline</param>
    public static void RespondToTransferRequest(bool isAccepted)
    {
        byte[] header = PrepareDataHeader(Functions.QueryTransfer, 1);      /// Prepare Data Header 
        byte[] dataToSend = new byte[HeaderLen + 1];                        /// Prepare Carrier Pack
        Array.Copy(header, 0, dataToSend, 0, HeaderLen);                    /// Copy header to Carrier Pack
        if (isAccepted)                                                     /// if transfer is accepted
        {
            dataToSend[HeaderLen] = 1;                                      /// write one to state byte
        }
        else                                                                /// if the transfer is rejected by user
        {
            dataToSend[HeaderLen] = 0;                                      /// write zero to state byte  
        }
        client.SendDataServer(dataToSend);                                  /// Send pack to the server
    }
    /// <summary>
    /// Gets data from buffer and checks. returns file bytes only. you can directly write them to fileStream
    /// </summary>
    /// <returns>File Bytes as Byte array</returns>
    public static byte[] ReceiveFilePacks()
    {
        byte[] receivedData = client.GetData();                                     /// Get Data From Buffer
        if(receivedData[0]==StartByte)                                              /// check if start byte is correct
        {
            if(receivedData[1]==(byte)Functions.SendingFile)                        /// Check the function byte
            {   
                uint dataLen = BitConverter.ToUInt32(receivedData, 3);              /// Get the length of the data bytes (index bytes are included to this number)
                uint packIndex = BitConverter.ToUInt32(receivedData, HeaderLen);    /// Get the index of data pack
                if (packIndex == LastPackNumberReceived - 1)                        /// Check if the index is correct
                {
                    LastPackNumberReceived = packIndex;                             /// update the index
                    SendAckToServer(true);                                          /// Send Ack to Server
                }
                else
                {
                    SendAckToServer(false);                                         /// Send Nack to Server
                    Debug.WriteLine("Index of the last package was incorrect. Do something about it!");
                }
                byte[] dataPack = new byte[dataLen-4];                              /// Create data pack variable to store file bytes 
                Array.Copy(receivedData, HeaderLen+4, dataPack, 0, dataLen-4);      /// Copy array to data packs byte
                return dataPack;                                                    /// return data pack
            }
            else if (receivedData[1]==(byte)Functions.FileSent)
            {
                ConfirmFileIsReceived();
                return null;
            }
            else
            {
                Debug.WriteLine("ReceiveFilePacks function: Function Byte is wrong!: " + (Functions)receivedData[1]);
                return null;
            }
        }
        else
        {
            Debug.WriteLine("ReceiveFilePacks function: Start Byte is wrong!: "+receivedData[0]);
            return null;
        }
    }
    private static void SendAckToServer(bool isreceived)
    {
        byte[] header = PrepareDataHeader(Functions.TransferStatus, 1);      /// Prepare Data Header 
        byte[] dataToSend = new byte[HeaderLen + 1];                        /// Prepare Carrier Pack
        Array.Copy(header, 0, dataToSend, 0, HeaderLen);                    /// Copy header to Carrier Pack
        if (isreceived)                                                     /// if transfer is accepted
        {
            dataToSend[HeaderLen] = 1;                                      /// write one to state byte
        }
        else                                                                /// if the transfer is rejected by user
        {
            dataToSend[HeaderLen] = 0;                                      /// write zero to state byte  
        }
        client.SendDataServer(dataToSend);                                  /// Send pack to the server
    }
    /// <summary>
    /// This function is used to tell server that the file is succesfully received.
    /// </summary>
    private static void ConfirmFileIsReceived()
    {
        byte[] header = PrepareDataHeader(Functions.FileSent, 1);
        byte[] dataToSend = new byte[HeaderLen + 1];
        header.CopyTo(dataToSend, 0);
        dataToSend[HeaderLen] = 1;
        client.SendDataServer(dataToSend);
        isFileReceived = true;
    }
    public static string DecodeTransferCode(string code)
    {
        if(code.Length==6)
        {
            string dummyCode = code.Substring(0, 3);
            string ipEndWithZeros = code.Substring(3, 3);
            string currentIP = client.GetDeviceIP();
            char[] splitterUsta = { '.' };                                          /// Define the seperator for ip address.
            string[] IpParts = currentIP.Split(splitterUsta);                       /// split the ip string. this will return four different strings. for Example: if ip is "192.168.1.100"
            char[] splitterZero = { '0' };                                          /// Define the seperator for ip address.
            string IpEnd=ipEndWithZeros.Trim(splitterZero);
            string ServersIP = IpParts[0] + IpParts[1] + IpParts[2] + IpEnd;
            return ServersIP;
        }
        else
        {
            Debug.WriteLine("Code Length was incorrect: " + code.Length);
            return "";
        }
    }
    #endregion

    #region Common Functions
    private static byte[] PrepareDataHeader(Functions func,uint len)
    {
        byte[] HeaderBytes = new byte[HeaderLen];
        HeaderBytes[0] = StartByte;
        HeaderBytes[1] = Convert.ToByte(func);
        byte[] lenBytes = BitConverter.GetBytes(len);
        Array.Copy(lenBytes, 0, HeaderBytes, 3, lenBytes.Length);
        return HeaderBytes;
    }
    private static int CalculatePackageCount(double fileSize, SizeTypes sizeType)
    {
        double PackageSize = 0;
        if (sizeType == SizeTypes.KB)
            PackageSize = (int)(fileSize * 1024);
        else if (sizeType == SizeTypes.MB)
            PackageSize = (int)(fileSize * 1024 * 1024);
        else if (sizeType == SizeTypes.GB)
            PackageSize = (int)(fileSize * 1024 * 1024 * 1024);
        else if (sizeType == SizeTypes.TB)
            PackageSize = (int)(fileSize * 1024 * 1024 * 1024 * 1024);
        int packageCount = (int)Math.Ceiling(PackageSize / (server.BufferSize - HeaderLen));
        return packageCount;
    }
    #endregion
}
