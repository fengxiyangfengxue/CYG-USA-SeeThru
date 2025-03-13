using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ionic.Zip;

namespace Test._ScriptHelpers
{
    public class ZipHelper
    {

        public static void ZipFile(string filePath, string zipFilePath)
        {
            ZipFile(filePath, string.Empty, zipFilePath);
        }

        public static void ZipFile(string filePath, string directoryInZip, string zipFilePath)
        {
            ZipFiles(new List<string>() { filePath }, directoryInZip, zipFilePath);
        }

        public static void ZipFiles(List<string> filePaths, string zipFilePath)
        {
            ZipFiles(filePaths, string.Empty, zipFilePath);
        }

        public static void ZipFiles(List<string> filePaths, string directoryInZip, string zipFilePath)
        {
            using (ZipFile zip = new ZipFile())
            {
                if (string.IsNullOrEmpty(directoryInZip))
                    zip.AddFiles(filePaths);
                else
                    zip.AddFiles(filePaths, directoryInZip);

                FileInfo zipFile = new FileInfo(zipFilePath);
                if (!zipFile.Directory.Exists)
                    zipFile.Directory.Create();

                zip.Save(zipFile.FullName);
            }
        }

        public static void AppendFile(string zipFilePath, string filePath)
        {
            AppendFiles(zipFilePath, new List<string>() { filePath }, string.Empty);
        }

        public static void AppendFile(string zipFilePath, string filePath, string directoryInZip)
        {
            AppendFiles(zipFilePath, new List<string>() { filePath }, directoryInZip);
        }

        public static void AppendFiles(string zipFilePath, List<string> filePaths)
        {
            AppendFiles(zipFilePath, filePaths, string.Empty);
        }

        public static void AppendFiles(string zipFilePath, List<string> filePaths, string directoryInZip)
        {
            FileInfo zipFile = new FileInfo(zipFilePath);
            using (ZipFile zip = new ZipFile(zipFile.FullName))
            {
                if (string.IsNullOrEmpty(directoryInZip))
                    zip.AddFiles(filePaths);
                else
                    zip.AddFiles(filePaths, directoryInZip);

                zip.Save();
            }
        }

        public static void ZipDirectory(string directory, string zipFilePath)
        {
            ZipDirectories(new List<string>() { directory }, string.Empty, zipFilePath);
        }

        public static void ZipDirectory(string directory, string directoryInZip, string zipFilePath)
        {
            ZipDirectories(new List<string>() { directory }, directoryInZip, zipFilePath);
        }

        public static void ZipDirectories(List<string> directories, string zipFilePath)
        {
            ZipDirectories(directories, string.Empty, zipFilePath);
        }

        public static void ZipDirectories(List<string> directories, string directoryInZip, string zipFilePath)
        {
            using (ZipFile zip = new ZipFile())
            {
                directories.ForEach(d =>
                {
                    if (string.IsNullOrEmpty(directoryInZip))
                        zip.AddDirectory(d);
                    else
                        zip.AddDirectory(d, directoryInZip);
                });

                FileInfo zipFile = new FileInfo(zipFilePath);
                if (!zipFile.Directory.Exists)
                    zipFile.Directory.Create();

                zip.Save(zipFile.FullName);
            }
        }

        public static void AppendDirectory(string zipFilePath, string directory)
        {
            AppendDirectories(zipFilePath, new List<string>() { directory }, string.Empty);
        }

        public static void AppendDirectory(string zipFilePath, string directory, string directoryInZip)
        {
            AppendDirectories(zipFilePath, new List<string>() { directory }, directoryInZip);
        }

        public static void AppendDirectories(string zipFilePath, List<string> directories, string directoryInZip)
        {
            FileInfo zipFile = new FileInfo(zipFilePath);
            using (ZipFile zip = new ZipFile(zipFile.FullName))
            {
                directories.ForEach(d =>
                {
                    if (string.IsNullOrEmpty(directoryInZip))
                        zip.AddDirectory(d);
                    else
                        zip.AddDirectory(d, directoryInZip);
                });

                zip.Save(zipFile.FullName);
            }
        }


        public static void ExtractFile(string zipFilePath, string filePathInZip, string toDirectory)
        {
            ExtractFiles(zipFilePath, new List<string>() { filePathInZip }, toDirectory);
        }

        public static void ExtractFiles(string zipFilePath, List<string> filePathsInZip, string toDirectory)
        {
            FileInfo zipFile = new FileInfo(zipFilePath);
            using (ZipFile zip = new ZipFile(zipFile.FullName))
            {
                filePathsInZip.ForEach(d =>
                {
                    ZipEntry entry = zip[d];
                    if (entry != null)
                        entry.Extract(toDirectory, ExtractExistingFileAction.OverwriteSilently);
                });
            }
        }

        public static void ExtractDirectory(string zipFilePath, string directoriesInZip, string toDirectory)
        {
            ExtractDirectories(zipFilePath, new List<string>() { directoriesInZip }, toDirectory);
        }

        public static void ExtractDirectories(string zipFilePath, List<string> directoriesInZip, string toDirectory)
        {
            FileInfo zipFile = new FileInfo(zipFilePath);
            using (ZipFile zip = new ZipFile(zipFile.FullName))
            {
                directoriesInZip.ForEach(d =>
                {
                    var dirName = d.Replace(Path.DirectorySeparatorChar, '/');
                    if (!dirName.EndsWith("/"))
                        dirName = dirName + "/";

                    foreach (ZipEntry entry in zip)
                    {
                        if (entry.FileName.StartsWith(dirName, StringComparison.OrdinalIgnoreCase))
                        {
                            entry.Extract(toDirectory, ExtractExistingFileAction.OverwriteSilently);
                        }
                    }
                });
            }
        }

        public static void ExtractAll(string zipFilePath, string toDirectory)
        {

            FileInfo zipFile = new FileInfo(zipFilePath);
            using (ZipFile zip = new ZipFile(zipFile.FullName))
            {
                zip.ExtractAll(toDirectory, ExtractExistingFileAction.OverwriteSilently);
            }
        }


    }
}
