using System.Diagnostics;
using System.Net;

namespace WinPass11.Helpers
{
    class FileTools
    {
        public static bool IsLocked(FileInfo file)
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
                {
                    stream.Close();
                }
            }

            return false;
        }

        public static void Replace(string replacement, string path)
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

        public static void WaitForExist(string path, string? replacement)
        {
            FileInfo file = new(path);

            int i = 0;
            while (!System.IO.File.Exists(path) && i < 60)
            {
                if (i == 60)
                {
                    MessageBox.Show($"The application has timed out.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Application.Exit();
                }

                Debug.WriteLine($"Sleeping, file not yet found: {path}");
                Thread.Sleep(1000);
                i++;
            }
            Thread.Sleep(5000);

            while (IsLocked(file))
            {
                Process[] processes = Process.GetProcessesByName("SetupHost.exe");
                foreach (Process process in processes)
                {
                    process.Kill();
                }
            }

            if (System.IO.File.Exists(path))
            {
                try
                {
                    System.IO.File.Delete(path);
                }
                catch
                {
                    Debug.WriteLine($"Failed to delete file: {path}");
                }
            }

            if (!string.IsNullOrEmpty(replacement))
            {
                Replace(replacement, path);
            }
        }
    }
}
