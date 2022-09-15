using System;
using System.Globalization;

class Parameters
{
    private static readonly string parametersPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\FileTransferParameters.dat";

    public static string SavingPath { get; set; }
    public static string DeviceName { get; set; }
    public static bool IsUsingFirstTime { get; set; }
    public static string DeviceLanguage { get; set; }
    public static string DeviceIP { get; set; }

    public static bool AcceptAllRequests { get; set; }

    public static bool DidInitParameters = false;

    public static void Init()
    {
        System.Diagnostics.Debug.WriteLine(" path :::+ "+ parametersPath);
        var param = new ParametersBag();
        try
        {
            param.Load(parametersPath);
            SavingPath = param.SavingPath;
            DeviceName = param.DeviceName;
            DeviceLanguage = param.DeviceLanguage;
            AcceptAllRequests = param.AcceptAllRequests;
            DidInitParameters = true;
        }
        catch
        {
            IsUsingFirstTime = true;
            SavingPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)+"\\";
            DeviceName = Environment.MachineName;
            char[] splitter = { '-' };
            string language = CultureInfo.InstalledUICulture.ToString().Split(splitter)[0];
            DeviceLanguage = language;
            AcceptAllRequests = false;
            DidInitParameters = true;
            Save();
        }
    }
    public static void Save()
    {
        try
        {
            if (!DidInitParameters)
            {
                System.Diagnostics.Debug.WriteLine("Init parameters first!");
                return;
            }
            var param = new ParametersBag();
            param.SavingPath = SavingPath;
            param.DeviceName = DeviceName;
            param.DeviceLanguage = DeviceLanguage;
            param.Save(parametersPath);
        }
        catch
        {

        }
    }
}
