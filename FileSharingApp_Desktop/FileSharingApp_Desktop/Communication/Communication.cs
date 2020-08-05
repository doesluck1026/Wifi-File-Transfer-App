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

    public static bool isClientConnected = false;
    public static bool isConnectedToServer = false;
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
        server = new Server();
        isClientConnected=server.SetupServer();
        return isClientConnected;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileName">Name of File to be transfered</param>
    /// <param name="fileSize">Size of the File</param>
    /// <param name="sizeType">type of size like MB, GB etc..</param>
    public static bool QueryForTransfer(string fileName, double fileSize, SizeTypes sizeType)
    {
        byte[] nameBytes = Encoding.ASCII.GetBytes(fileName);
        int lenName = nameBytes.Length;
        byte[] sizeBytes = BitConverter.GetBytes(fileSize);
        int lenSize = sizeBytes.Length;
        int DataLen = lenName + lenSize + 1 + 4 + 4;
        byte[] HeaderBytes = PrepareDataHeader(Functions.QueryTransfer, (uint)DataLen);
        byte[] DataToSend = new byte[DataLen + HeaderLen];
        Array.Copy(HeaderBytes, 0, DataToSend, 0, HeaderBytes.Length);
        DataToSend[HeaderLen] = (byte)(lenName & 0xff);
        DataToSend[HeaderLen+1] = (byte)((lenName>>8) & 0xff);
        Array.Copy(nameBytes, 0, DataToSend, HeaderLen + 2, lenName);
        int IndexSize = HeaderLen + 2 + lenName;
        DataToSend[IndexSize] = (byte)(lenSize & 0xff);
        DataToSend[IndexSize + 1] = (byte)((lenSize >> 8) & 0xff);
        Array.Copy(sizeBytes, 0, DataToSend, IndexSize + 2, lenSize);
        int IndexType = IndexSize + 2 + lenSize;
        DataToSend[IndexType] = (byte)sizeType;
        int packageCount= CalculatePackageCount(fileSize, sizeType);
        byte[] packageCountBytes = BitConverter.GetBytes(packageCount);
        Array.Copy(packageCountBytes, 0, DataToSend, IndexType + 1, packageCountBytes.Length);
        return server.SendDataToClient(DataToSend);
    }
    public static bool GetResponse()
    {
        bool isAccepted = false;
        byte[] ReceivedData=server.GetData();
        if(ReceivedData[1]==(byte)Functions.QueryTransfer)
        {
            if(ReceivedData[HeaderLen]==1)
            {
                Debug.WriteLine("Starting transfer");
                isAccepted = true;
            }
            else
            {
                Debug.WriteLine("File Declined!");
            }
        }
        else if (ReceivedData[1] == (byte)Functions.TransferStatus)
        {
            if (ReceivedData[HeaderLen] == 1)
            {
                uint LastPackReceived = BitConverter.ToUInt32(ReceivedData,HeaderLen+2);
                isAccepted = true;
            }
        }
            return isAccepted;
    }
    public static bool SendFilePacks(byte[] data,int numPackage)
    {
        byte[] HeaderBytes = PrepareDataHeader(Functions.SendingFile, (uint)(data.Length + 4));
        byte[] DataToSend = new byte[data.Length + 4 + HeaderLen];
        Array.Copy(HeaderBytes, 0, DataToSend, 0, HeaderLen);
        Array.Copy(BitConverter.GetBytes(numPackage), 0, DataToSend, HeaderLen, sizeof(int));
        Array.Copy(data, 0, DataToSend, HeaderLen+4, data.Length);
        server.SendDataToClient(DataToSend);
        return GetResponse();
    }
    public static bool CompleteTransfer()
    {
        byte[] HeaderBytes = PrepareDataHeader(Functions.FileSent, 1);
        byte[] DataToSend = new byte[HeaderLen+1];
        Array.Copy(HeaderBytes, 0, DataToSend, 0, HeaderLen);
        return server.SendDataToClient(DataToSend);
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
    #region Client Functions
    /// <summary>
    /// Connects to server with given ip at given port. This is used to receive file from another device.
    /// </summary>
    /// <param name="ip"></param>
    /// <param name="port"></param>
    /// <returns></returns>
    public static bool ConnectToServer(string ip,int port)
    {
        client = new Client(ip, port);
        isConnectedToServer = client.ConnectToServer();
        return isConnectedToServer;
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
    #endregion
}
