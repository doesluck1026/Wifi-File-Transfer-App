using System;
using System.Linq;
using System.Text;

namespace FileSharingApp_Desktop.AutoUpdater.ApplicationUpdate
{
	public static class Versions
	{
		public static string RemoteVersion(string url)
		{
			string rv = "";

			try
			{
				System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)
				System.Net.WebRequest.Create(url);
				System.Net.HttpWebResponse response = (System.Net.HttpWebResponse)req.GetResponse();
				System.IO.Stream receiveStream = response.GetResponseStream();
				System.IO.StreamReader readStream = new System.IO.StreamReader(receiveStream, Encoding.UTF8);
				string s = readStream.ReadLine();
				response.Close();
				if (ValidateFile(s))
				{
					rv = s;
				}
			}
			catch (Exception)
			{
				// Anything could have happened here but 
				// we don't want to stop the user
				// from using the application.
				rv = null;
			}
			return rv;
		}

		public static string LocalVersion(string path)
		{
			string lv = "";

			if (string.IsNullOrEmpty(path)
				|| System.IO.Path.GetInvalidPathChars().Intersect(path.ToCharArray()).Count() != 0
				|| !new System.IO.FileInfo(path).Exists)
			{
				lv = null;
			}
			else if (new System.IO.FileInfo(path).Exists)
			{
				string s = System.IO.File.ReadAllText(path);
				if (ValidateFile(s))
					lv = s;
				else
					lv = null;
			}
			return lv;
		}

		public static bool ValidateFile(string contents)
		{
			bool val = false;
			if (!string.IsNullOrEmpty(contents))
			{
				string pattern = "^([0-9]*\\.){3}[0-9]*$";
				System.Text.RegularExpressions.Regex re = new System.Text.RegularExpressions.Regex(pattern);
				val = re.IsMatch(contents);
			}
			return val;
		}

		public static string CreateLocalVersionFile(string folderPath, string fileName, string version)
		{
			if (!new System.IO.DirectoryInfo(folderPath).Exists)
			{
				System.IO.Directory.CreateDirectory(folderPath);
			}

			string path = folderPath + "\\" + fileName;

			if (new System.IO.FileInfo(path).Exists)
			{
				new System.IO.FileInfo(path).Delete();
			}

			if (!new System.IO.FileInfo(path).Exists)
			{
				System.IO.File.WriteAllText(path, version);
			}
			return path;
		}

		public static string CreateTargetLocation(string downloadToPath, string version)
		{
			if (!downloadToPath.EndsWith("\\")) // Give a trailing \ if there isn't one
				downloadToPath += "\\";

			string filePath = downloadToPath + version;

			System.IO.DirectoryInfo newFolder = new System.IO.DirectoryInfo(filePath);
			if (!newFolder.Exists)
				newFolder.Create();
			return filePath;
		}
	}
}
