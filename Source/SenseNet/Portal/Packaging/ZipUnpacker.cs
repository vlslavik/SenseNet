﻿using System;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace SenseNet.Packaging
{
    internal static class ZipUnpacker
    {
        //public static void Unpack(string fsPath)
        //{
        //    var extractDir = Path.Combine(Path.GetDirectoryName(fsPath), Path.GetFileNameWithoutExtension(fsPath) + PackageManager.ExtractDirextorySuffix);
        //    if (!Directory.Exists(extractDir))
        //    {
        //        Logger.LogMessage("Extract to " + extractDir);
        //        Extract(fsPath, extractDir);
        //    }
        //}

        private static void Extract(string zipFilePath, string extractDir)
        {
            var targetDirectory = extractDir;

            using (ZipInputStream s = new ZipInputStream(File.OpenRead(zipFilePath)))
            {
                ZipEntry theEntry;
                while ((theEntry = s.GetNextEntry()) != null)
                {
                    //Console.WriteLine(theEntry.Name);
                    var targetPath = Path.Combine(targetDirectory, theEntry.Name);

                    string directoryName = Path.GetDirectoryName(targetPath);
                    string fileName = Path.GetFileName(targetPath);

                    if (directoryName.Length > 0)
                        Directory.CreateDirectory(directoryName);

                    if (fileName != String.Empty)
                    {
                        using (FileStream streamWriter = File.Create(targetPath))
                        {
                            int size = 2048;
                            byte[] data = new byte[2048];
                            while (true)
                            {
                                size = s.Read(data, 0, data.Length);
                                if (size > 0)
                                    streamWriter.Write(data, 0, size);
                                else
                                    break;
                            }
                        }
                    }
                }
            }
        }
    }
}
