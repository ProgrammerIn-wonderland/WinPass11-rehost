using Microsoft.Win32;
using System.Diagnostics;
using File = WinPass11.Helpers.File;

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

            Thread thread = new Thread(() => File.WaitForExist(downloadDir, replacementUri));
            thread.Start();
            thread.Join();

            MessageBox.Show("Tasks completed!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Application.Exit();
        }
    }
}