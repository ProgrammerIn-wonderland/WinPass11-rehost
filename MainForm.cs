using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinPass11.Helpers;

namespace WinPass11
{
    public partial class MainForm : Form
    {
        private readonly string appraiserRes = "https://github.com/ArkaneDev/files/raw/main/appraiserres.dll";
        private readonly string tempDir = Path.Combine(Path.GetTempPath(), "WinPass11");

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        { // Create a temp directory for later use
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
            Directory.CreateDirectory(tempDir);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        { // Remove the temp directory on exit
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }

        private void cmbChannel_SelectionChangeComitted(object sender, EventArgs e)
        { // Enable the install button once a channel is selected
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

            string channel = cmbChannel.SelectedItem.ToString();

            if (channel == "Release")
                HandleRelease();
            else // Beta and Dev Channels
            {
                RegTools.CreateKey(@"SOFTWARE\Microsoft\WindowsSelfHost\Applicability", new object[,]
                {
                    { "BranchName", channel },
                    { "ContentType", "Mainline" },
                    { "Ring", "External" }
                });

                RegTools.CreateKey(@"SOFTWARE\Microsoft\WindowsSelfHost\UI\Selection", new object[,]
                {
                    { "UIBranch", channel },
                    { "UIContentType", "Mainline" },
                    { "UIRing", "External" }
                });

                RegTools.CreateKey(@"SYSTEM\Setup\LabConfig", new object[,]
                {
                    { "BypassTPMCheck", 1, RegistryValueKind.DWord },
                    { "BypassSecureBootCheck", 1, RegistryValueKind.DWord }
                });
                progressBar.Value = 30;

                Process process = Process.Start("UsoClient.exe", "StartInteractiveScan");
                process.WaitForExit();
                Debug.WriteLine($"'{process.StartInfo.FileName}' has exited with code {process.ExitCode}.");
                progressBar.Value = 60;
                progressBar.Style = ProgressBarStyle.Marquee;

                string downloadDir = $"{Path.GetPathRoot(Environment.SystemDirectory)}\\$WINDOWS.~BT\\Sources\\AppraiserRes.dll";

                Thread thread = new Thread(() => FileTools.WaitForExist(downloadDir, appraiserRes));
                thread.Start();
                progressBar.Value = 90;
                thread.Join();
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = 100;

                MessageBox.Show("Tasks completed!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Exit();

            }
        }
        private async void HandleRelease()
        { // Release handles upgrading differently from the other Insider branches

            string filename = "";
            
            if (MessageBox.Show("Do you have an ISO image of Windows 11?\r\n(Selecting no will download a 5.2GB file)", "WinPass11", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No) {
                progressBar.Value = 30;
                //await Task.Run(() => FileTools.Download("https://tb.rg-adguard.net/dl.php?go=b1fb62af", $"{tempDir}\\Windows11.iso")); // Downloads Windows ISO

                WebClient webClient = new WebClient(); // though less than ideal, we have to reprogram the download portion, as the current one doesn't provide progress
                webClient.DownloadProgressChanged += (s, e) =>
                {
                    float scaled = (e.ProgressPercentage * (0.333f)) + 30;
                    progressBar.Value = (int) scaled;
                };
                await webClient.DownloadFileTaskAsync(new Uri("https://tb.rg-adguard.net/dl.php?go=b1fb62af"), // Thank you adguard :)
                    $"{tempDir}\\Windows11.iso");
                filename = $"{tempDir}\\Windows11.iso";

            } else
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                filename = fileDialog.FileName;

            } else
            {
                MessageBox.Show("No file provided, exiting WinPass11");
                Application.Exit();
            }
            progressBar.Value = 65;
            await Task.Run(() => DiscTools.ExtractISO(filename, $@"{tempDir}\ISO\")); // ISO extractor

            string[] pathparser1 = filename.Split('\\');
            string extractdir = tempDir + "\\ISO\\" + pathparser1[pathparser1.Length - 1].Remove(pathparser1[pathparser1.Length - 1].Length - 4, 4) + "\\"; // hellish filepath parsing

            progressBar.Value = 70;
            FileTools.Download(appraiserRes, $"{extractdir}\\Sources\\appraiserres.dll");

            progressBar.Style = ProgressBarStyle.Marquee;
            MessageBox.Show("Continue Setup in Windows 11 Installer.\r\n\r\nIMPORTANT!\r\n\r\nPlease Click \"Change how Setup downloads updates\", then select \"Not Now\".", "Important", MessageBoxButtons.OK, MessageBoxIcon.Information);

            Process process = Process.Start($"{extractdir}\\Setup.exe", "");
            process.WaitForExit();
            Debug.WriteLine($"'{process.StartInfo.FileName}' has exited with code {process.ExitCode}.");

            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = 100;
        }
    }
}