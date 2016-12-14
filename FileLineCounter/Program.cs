using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace FileLineCounter
{
    class Program
    {
        static void Main (string[] args)
        {
            try
            {
                #region Input information
                if (args == null)
                {
                    WriteHelpManual ();
                    return;
                }

                // Read input parameters
                Dictionary<string, string> parameters = new Dictionary<string, string> ();
                foreach (string arg in args)
                {
                    int pos = arg.IndexOf ("=", StringComparison.OrdinalIgnoreCase);
                    if (pos <= 0)
                    {
                        Console.WriteLine ("Ignored parameter: [" + arg + "]");
                        continue;
                    }

                    string tag   = arg.Substring (0, pos).Trim ().ToLowerInvariant ();
                    string value = arg.Substring (pos + 1).Trim ();

                    if (!parameters.ContainsKey (tag))
                    {
                        parameters.Add (tag, value);
                    }
                }

                if (parameters.Count == 0 || parameters.ContainsKey ("help"))
                {
                    WriteHelpManual ();
                    return;
                }

                if (!parameters.ContainsKey ("input") || !Directory.Exists (parameters["input"]))
                {
                    Console.WriteLine ("Invalid input directory");
                    return;
                }

                if (!parameters.ContainsKey ("output"))
                {
                    parameters.Add ("output", "FileLineCounter_" + DateTime.UtcNow.ToString ("yyyyMMdd_hhmmss") + ".csv");
                }
                #endregion

                using (StreamWriter writer = new StreamWriter (parameters["output"], false, Encoding.UTF8))
                {
                    // Write file header
                    writer.WriteLine ("FILE\tLINES\tFOLDER");

                    string fileFormat = "*.txt";
                    if (parameters.ContainsKey ("fileformat"))
                    {
                        fileFormat = parameters["fileformat"];
                    }

                    SearchOption searchOp = SearchOption.TopDirectoryOnly;
                    if (parameters.ContainsKey ("searchoption") && parameters["searchoption"].Equals ("all", StringComparison.OrdinalIgnoreCase))
                    {
                        searchOp = SearchOption.AllDirectories;
                    }

                    foreach (string file in Directory.GetFiles (parameters["input"], fileFormat, searchOp))
                    {
                        // We have to decompress file
                        if (fileFormat.IndexOf ("zip", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            string decompressedFolder = file + "_tmp";
                            ZipFile.ExtractToDirectory (file, decompressedFolder);

                            foreach (string tmpFile in Directory.GetFiles (decompressedFolder))
                            {
                                long total = LinesCount (tmpFile);
                                writer.WriteLine (Path.GetFileName (file) + "\t" + total + "\t" + GetLastFolder (file));
                            }
                            RemoveTempFolder (decompressedFolder);
                        }
                        else
                        {
                            long total = LinesCount (file);
                            writer.WriteLine (Path.GetFileName (file) + "\t" + total + "\t" + GetLastFolder (file));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine (ex.Message);
            }
        }

        /// <summary>
        /// Get last folder name
        /// </summary>
        /// <param name="file">File name</param>
        /// <returns>Last folder</returns>
        private static string GetLastFolder (string file)
        {            
            string[] data = file.Split ('\\');
            if (data.Length > 1)
            {
                return data[data.Length - 2];
            }
            return file;
        }

        /// <summary>
        /// Remove temp folder
        /// </summary>
        /// <param name="folder">Folder to be removed</param>
        private static void RemoveTempFolder (string folder)
        {
            DirectoryInfo di = new DirectoryInfo (folder);
            foreach (FileInfo file in di.GetFiles ())
            {
                file.Delete ();
            }
            foreach (DirectoryInfo dir in di.GetDirectories ())
            {
                dir.Delete (true);
            }
            Directory.Delete (folder);
        }

        /// <summary>
        /// File line count
        /// </summary>
        /// <param name="file">File name</param>
        /// <returns>Lines count</returns>
        private static long LinesCount (string file)
        {
            long total = 0;
            using (StreamReader reader = new StreamReader (file, Encoding.UTF8))
            {
                string line = null;
                while ((line = reader.ReadLine ()) != null)
                {
                    total++;
                }
            }
            return total;
        }

        /// <summary>
        /// Write manual
        /// </summary>
        private static void WriteHelpManual ()
        {
            Console.WriteLine ("input=directory to search for files");
            Console.WriteLine ("output=output fullpath");
            Console.WriteLine ("fileFormat=file type (*.txt, *.csv or *.zip)");
            Console.WriteLine ("searchOption=<all: folders and subfolders> or <current: only the current folder>");
        }
    }
}
