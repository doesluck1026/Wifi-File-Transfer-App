using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

[Serializable]
class ParametersBag
{
    public string SavingPath;
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
        // Close the readerFileStream when we are done
        readerFileStream.Close();
    }
}