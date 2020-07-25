using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Communication pack structure should be like below:
///     [Header Pack= 8 byte long] + [Data Bytes = (2^40)-1 bytes long]
/// Header Pack Data Structure:
///     [Start Byte] + [Function Byte] + [Spare Byte] + [Length Bytes=5 bytes Length]
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
    private enum Functions
    {
        QueryTransfer=2,
        SendingFile=3,
        FileReceived=4,
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
    public static void QueryForTransfer(string fileName,double fileSize,string sizeType)
    {
        byte[] nameBytes = Encoding.ASCII.GetBytes(fileName);
        int lenName = nameBytes.Length;
        byte[] sizeBytes = BitConverter.GetBytes(fileSize);
        byte[] typeBytes = Encoding.ASCII.GetBytes(sizeType);
        int lenSize = typeBytes.Length;
        int DataLen = lenName + lenSize + 8;
        byte[] HeaderBytes = PrepareDataHeader(Functions.QueryTransfer,(uint)DataLen);

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
