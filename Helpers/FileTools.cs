using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace WinPass11.Helpers
{
    class FileTools
    {
        public static bool IsLocked(FileInfo file)
        {
            FileStream stream = null;

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
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                WebClient client = new WebClient();
                client.DownloadFile(replacement, path);
            }
            catch
            {
                Debug.WriteLine($"Failed to replace file: {new FileInfo(path).Name}");
            }
        }

        public static void WaitForExist(string path, string replacement)
        {
            FileInfo file = new FileInfo(path);

            int i = 0;
            while (!System.IO.File.Exists(path) && i < 30)
            {
                if (i == 30)
                {
                    MessageBox.Show($"The application has timed out.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Application.Exit();
                }

                Debug.WriteLine($"Sleeping, file not yet found: {path}");
                Thread.Sleep(500);
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

            if (!string.IsNullOrEmpty(replacement))
            {
                Replace(replacement, path);
            }
        }
    }
}
