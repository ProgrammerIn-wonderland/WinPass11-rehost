using Microsoft.Win32;
using System.Diagnostics;
using WinPass11.Helpers;
using static WinPass11.Helpers.Utils;
using FileTools = WinPass11.Helpers.FileTools;

namespace WinPass11
{
    public partial class MainForm : Form
    {
        private readonly string tempDir = Path.Combine(Path.GetTempPath(), "WinPass11");

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
            Directory.CreateDirectory(tempDir);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }

        private void cmbChannel_SelectionChangeComitted(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(cmbChannel.SelectedItem.ToString()))
            {
                btnInstall.Enabled = true;
            }
        }

        private void btnInstall_Click(object sender, EventArgs e)
        {
            btnInstall.Enabled = false;
            cmbChannel.Enabled = false;

            DialogResult result = MessageBox.Show("Are you sure you want to continue? This action cannot be undone.", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.No)
            {
                Application.Exit();
            };

            string channel = cmbChannel.SelectedItem.ToString()!;

            if (channel == "Release")
                release();
            else // Beta and Dev Channels
            {
                Helpers.Registry.CreateKey(@"SOFTWARE\Microsoft\WindowsSelfHost\Applicability", new object[,]
                {
                { "BranchName", channel },
                { "ContentType", "Mainline" },
                { "Ring", "External" }
                });

                Helpers.Registry.CreateKey(@"SOFTWARE\Microsoft\WindowsSelfHost\UI\Selection", new object[,]
                {
                { "UIBranch", channel },
                { "UIContentType", "Mainline" },
                { "UIRing", "External" }
                });

                Helpers.Registry.CreateKey(@"SYSTEM\Setup\LabConfig", new object[,]
                {
                { "BypassTPMCheck", 1, RegistryValueKind.DWord },
                { "BypassSecureBootCheck", 1, RegistryValueKind.DWord }
                });

                Process process = Process.Start("UsoClient.exe", "StartInteractiveScan");
                process.WaitForExit();
                Debug.WriteLine($"'{process.StartInfo.FileName}' has exited with code {process.ExitCode}.");

                string downloadDir = @$"{Path.GetPathRoot(Environment.SystemDirectory)}\$WINDOWS.~BT\Sources\AppraiserRes.dll";
                string replacementUri = "https://github.com/ArkaneDev/files/raw/main/appraiserres.dll";

                Thread thread = new Thread(() => FileTools.WaitForExist(downloadDir, replacementUri));
                thread.Start();
                thread.Join();

                MessageBox.Show("Tasks completed!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Exit();

            }
        }
        private async void release() // release has a whole different mechanism than the insider builds
        {
            Utils.ShowMessageBox("This option will require you to download an ISO file from https://www.microsoft.com/en-us/software-download/windows11", MessageBoxType.Information);
            openFileDialog1.Filter = "Windows ISO (*.iso)|*.iso|All files (*.*)|*.*"; // get file

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                progressBar1.Value = 30;
                await Task.Run(() => DiscTools.ExtractISO(openFileDialog1.FileName, $@"{tempDir}\ISO\")); // ISO extractor

                string[] pathparser1 = openFileDialog1.FileName.Split('\\');
                string extractdir = tempDir + "\\ISO\\" + pathparser1[pathparser1.Length - 1].Remove(pathparser1[pathparser1.Length - 1].Length - 4, 4) + "\\"; // hellish filepath parsing
                progressBar1.Value = 50;
                File.Delete($"{extractdir}\\Sources\\appraiserres.dll"); // File Replacing
                Utils.DownloadFile("https://github.com/ArkaneDev/files/raw/main/appraiserres.dll", $"{extractdir}\\Sources\\appraiserres.dll");
                progressBar1.Style = ProgressBarStyle.Marquee;
                Utils.ShowMessageBox("Continue Setup in Windows 11 Installer \r\n\r\nVERY IMPORTANT!\r\n\r\nPlease Click \"Change how setup downloads updates\" and click \"Not Now\"", MessageBoxType.Information);
                
                Utils.StartProcess($"{extractdir}\\Setup.exe", "");

                progressBar1.Style = ProgressBarStyle.Continuous;
                progressBar1.Value = 100;

            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}