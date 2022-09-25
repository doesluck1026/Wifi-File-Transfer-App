using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;


    [Serializable]
    public class TextFileHelper
    {
        #region Public Variables

        #endregion

        #region Private Variables

        private string Path;
        private bool DoesFileExist;
        #endregion

        #region Public Functions

        public TextFileHelper(string path)
        {
            Path = path;
            DoesFileExist=File.Exists(Path);
        }

        /// <summary>
        /// Function that reads the requested line in the file located in the PathFile path.
        /// </summary>
        /// <param name="PathFile"></param>
        /// <param name="line"></param>
        /// <returns>Returns the text on the line read. </returns>
        public string ReadFromFile(int line)
        {
            if (!DoesFileExist)
                return "";
            string text="";

            using (StreamReader file = new StreamReader(Path))
            {
                for (int i = 0; i < line + 1; i++)
                    text = file.ReadLine();
                //Console.WriteLine("line: " + text);
                file.Close();
            }
            return text;
        }

        /// <summary>
        /// Function that writes the text to the desired line in the file located in the PathFile path.
        /// </summary>
        /// <param name="FilePath"></param>
        /// <param name="text"></param>
        /// <param name="lineNumber"></param>
        /// <returns>Returns false if there is no file in the given path, true if there is. </returns>
        public bool WriteToFile(string text, int lineNumber)
        {
            bool createFileNeeded = false;
            if (!DoesFileExist)
            {
                DoesFileExist = true;
                createFileNeeded = true;
            }
            if (createFileNeeded)
            {
                using (var fs = File.Create(Path))
                {
                    fs.Close();
                    fs.Dispose();
                }
            }

            string[] lines = File.ReadAllLines(Path);
            using (StreamWriter writer = new StreamWriter(Path))
            {
                if (lines.Length == 0)
                {
                    writer.WriteLine(text);
                }
                else if (lineNumber >= lines.Length) // If data is to be written to a line larger than the number of lines in the file.
                {
                    for (int i = 0; i < lineNumber; i++)
                    {
                        if (i < lines.Length)
                            writer.WriteLine(lines[i]); // Writes the old data in the file
                        else
                            writer.WriteLine("empty"); // Writes empy to rows with no data.
                    }
                    writer.WriteLine(text);
                }
                else
                {
                    lines[lineNumber] = text; // Appends the text to be appended to the file at the end of the Lines array
                    for (int i = 0; i < lines.Length; i++)
                    {
                        writer.WriteLine(lines[i]); // Writes the final version of the directory to the file
                    }
                }
                writer.Close();
            }
            return true;
        }
        #endregion

        #region Private Functions

        #endregion
    }
