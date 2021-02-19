using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class Parameters
{
    private static readonly string parametersPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BuggyCompany\\BuggyFileTransfer\\Parameters.dat";

    public static string SavingPath;
    public static string DeviceName;
    private static bool didInit = false;
    public static void Init()
    {
        var param = new ParametersBag();
        param.Load(parametersPath);
        SavingPath = param.SavingPath;
        DeviceName = param.DeviceName;
        didInit = true;
    }
    public static void Save()
    {
        if (didInit)
            return;
        var param = new ParametersBag();
        param.SavingPath = SavingPath;
        param.DeviceName = DeviceName;
        param.Save(parametersPath);
    }
}
