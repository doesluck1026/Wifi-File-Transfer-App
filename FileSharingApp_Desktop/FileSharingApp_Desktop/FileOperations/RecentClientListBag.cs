using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

[Serializable]
class RecentClientListBag
{
    public List<string> RecentClients;

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
        bagFile = (RecentClientListBag)formatter.Deserialize(readerFileStream);
        // Close the readerFileStream when we are done
        readerFileStream.Close();
        this.RecentClients = bagFile.RecentClients;

    }
}