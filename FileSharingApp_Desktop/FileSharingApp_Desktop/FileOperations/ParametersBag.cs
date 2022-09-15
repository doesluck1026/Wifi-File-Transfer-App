using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

[Serializable]
class ParametersBag
{
    public string SavingPath;
    public string DeviceName;
    public string DeviceLanguage;
    public bool AcceptAllRequests;
    public void Save(string url)
    {
        FileStream writerFileStream = new FileStream(url, FileMode.Create, FileAccess.Write);
        BinaryFormatter formatter = new BinaryFormatter();
        formatter.Serialize(writerFileStream, this);
        writerFileStream.Close();
    }
    public void Load(string url)
    {
        var bagFile = this;
        FileStream readerFileStream = new FileStream(url, FileMode.Open, FileAccess.Read);
        // Reconstruct data
        BinaryFormatter formatter = new BinaryFormatter();
        bagFile = (ParametersBag)formatter.Deserialize(readerFileStream);
        this.SavingPath = bagFile.SavingPath;
        this.DeviceName = bagFile.DeviceName;
        this.DeviceLanguage = bagFile.DeviceLanguage;
        this.AcceptAllRequests = bagFile.AcceptAllRequests;
        // Close the readerFileStream when we are done
        readerFileStream.Close();
    }
}