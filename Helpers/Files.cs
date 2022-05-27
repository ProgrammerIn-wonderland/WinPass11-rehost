using System.Diagnostics;
using System.Net;

namespace WinPass11.Helpers
{
    class Files
    {
        public static bool IsFileLocked(FileInfo file)
        {
            FileStream? stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                Debug.WriteLine($"Requested file is locked: {file.Name}");
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            return false;
        }

        public static void ReplaceFile(string replacement, string path)
        {
            try
            {
                WebClient client = new();
                client.DownloadFile(replacement, path);
            }
            catch
            {
                Debug.WriteLine($"Failed to replace file: {new FileInfo(path).Name}");
            }
        }

        public static void WaitForFileExist(string path, string? replacement)
        {
            FileInfo file = new(path);

            int i = 0;
            while (!File.Exists(path) && i < 60)
            {
                Debug.WriteLine($"Sleeping, file not yet found: {path}");
                Thread.Sleep(1000);
                i++;
            }
            Thread.Sleep(5000);

            while (IsFileLocked(file))
            {
                Process[] processes = Process.GetProcessesByName("SetupHost.exe");
                foreach (Process process in processes)
                {
                    process.Kill();
                }
            }

            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                }
                catch
                {
                    Debug.WriteLine($"Failed to delete file: {path}");
                }
            }

            if (!string.IsNullOrEmpty(replacement))
            {
                ReplaceFile(replacement, path);
            }
        }
    }
}
