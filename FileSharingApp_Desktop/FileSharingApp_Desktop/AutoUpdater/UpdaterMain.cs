using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FileSharingApp_Desktop.AutoUpdater
{
    class UpdaterMain
    {
		private static string remoteFile;
		private static string downloadToPath;
		private static string remoteVersion;
		private static string executeTarget = "updateVersion.txt";
		public static bool CompareVersions()
		{
			downloadToPath = Environment.CurrentDirectory;
			System.Diagnostics.Debug.WriteLine("Directory: " + downloadToPath);
			string localVersion = ApplicationUpdate.Versions.LocalVersion(downloadToPath + "\\version.txt");
			string remoteURL = "https://www.dropbox.com/sh/0olsbcuy836j6g8/AAAQWzP6IHUXWzffLcih4Mzga?dl=0";
		    remoteVersion = ApplicationUpdate.Versions.RemoteVersion(remoteURL + "\\updateVersion.txt");
			remoteFile = remoteURL + remoteVersion + ".zip";

			if (localVersion != remoteVersion)
			{
				//BeginDownload(remoteFile, downloadToPath, remoteVersion, "update.txt");
				return false;
			}
			return true;
		}
		public static void BeginDownload()
		{
			string filePath = ApplicationUpdate.Versions.CreateTargetLocation(downloadToPath, remoteVersion);

			Uri remoteURI = new Uri(remoteFile);
			System.Net.WebClient downloader = new System.Net.WebClient();

			downloader.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(downloader_DownloadFileCompleted);

			downloader.DownloadFileAsync(remoteURI, filePath + ".zip",
				new string[] { remoteVersion, downloadToPath, executeTarget });
		}
		private static void downloader_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			string[] us = (string[])e.UserState;
			string currentVersion = us[0];
			string downloadToPath = us[1];
			string executeTarget = us[2];

			if (!downloadToPath.EndsWith("\\")) // Give a trailing \ if there isn't one
				downloadToPath += "\\";

			string zipName = downloadToPath + currentVersion + ".zip"; // Download folder + zip file
			string exePath = downloadToPath + currentVersion + "\\" + executeTarget; // Download folder\version\ + executable

			if (new System.IO.FileInfo(zipName).Exists)
			{
				using (Ionic.Zip.ZipFile zip = new Ionic.Zip.ZipFile(zipName))
				{
					zip.ExtractAll(downloadToPath + currentVersion,
						Ionic.Zip.ExtractExistingFileAction.OverwriteSilently);
				}
				if (new System.IO.FileInfo(exePath).Exists)
				{
					ApplicationUpdate.Versions.CreateLocalVersionFile(downloadToPath, "version.txt", currentVersion);
					System.Diagnostics.Process proc = System.Diagnostics.Process.Start(exePath);
				}
				else
				{
					MessageBox.Show("Problem with download. File does not exist.");
				}
			}
			else
			{
				MessageBox.Show("Problem with download. File does not exist.");
			}
		}
	}
}
