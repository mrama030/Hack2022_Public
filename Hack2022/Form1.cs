using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Hack2022.AzureConnect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Hack2022
{
    public partial class Main : Form
    {
        private ShareClient _shareClient;

        #region Application Load/Setup

        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            // Create default downloads folder for the tool.
            if (!Directory.Exists(Constants.EcpQuickToolDownloadsPath)) Directory.CreateDirectory(Constants.EcpQuickToolDownloadsPath);

            // Populate Settings Tab
            textBoxFileShareName.Text = Properties.Settings.Default.AzureFileShareName;
            textBoxConnectionString.Text = Properties.Settings.Default.AzureFileShareConnectionString;
            textBoxDownloadsPath.Text = Properties.Settings.Default.DownloadsPath;
            textBoxEGInstallWSExePath.Text = Properties.Settings.Default.EGInstallWSExePath;

            // Populate Riposte Core Tab
            if (File.Exists(Constants.DefaultInstallLocations.RiposteCore))
            {
                var riposteCoreVersionInfo = FileVersionInfo.GetVersionInfo(Constants.DefaultInstallLocations.RiposteCore);
                string riposteCoreVersion = riposteCoreVersionInfo.ProductVersion;

                textBoxRiposteCore_CurrentVersion_Location.Text = Constants.DefaultInstallLocations.RiposteCore;
                labelRiposteCore_CurrentVersion_Version.Text = string.IsNullOrWhiteSpace(riposteCoreVersion) ? "Unknown" : riposteCoreVersion;
            }
            else
            {
                textBoxRiposteCore_CurrentVersion_Location.Text = "Not In Default Location";
                labelRiposteCore_CurrentVersion_Version.Text = "Unknown";
            }

            // Populate EssentialCP Tab
            if (File.Exists(Constants.DefaultInstallLocations.EssentialCP))
            {
                var ecpVersionInfo = FileVersionInfo.GetVersionInfo(Constants.DefaultInstallLocations.EssentialCP);
                string ecpVersion = ecpVersionInfo.ProductVersion;

                textBoxECP_CurrentVersion_Location.Text = Constants.DefaultInstallLocations.EssentialCP;
                labelECP_CurrentVersion_Version.Text = string.IsNullOrWhiteSpace(ecpVersion) ? "Unknown" : ecpVersion;
            }
            else
            {
                textBoxECP_CurrentVersion_Location.Text = "Not In Default Location";
                labelECP_CurrentVersion_Version.Text = "Unknown";
            }
        }

        #endregion

        #region Shared Logic Accross Tool

        private bool TestAzureConnection(bool saveCreatedService = false)
        {
            string fileShareName = textBoxFileShareName.Text.Trim();
            string connectionString = textBoxConnectionString.Text.Trim();

            if (string.IsNullOrWhiteSpace(fileShareName) || string.IsNullOrWhiteSpace(connectionString)) return false;

            var connectionSettings = new AzureFileShareSettings
            {
                FileShareName = fileShareName,
                ConnectionString = connectionString,
            };

            var client = AzureConnectMethods.ConnectToFileShare(connectionSettings);

            if (client == null) return false;
            ;
            Response<bool> exists = client.Exists();
            if (exists.Value == false) return false;

            List<ShareFileItem> requiredDirs = client.GetDirectories(Helper.GetRequiredDirectoryNames(), Constants.AzurePaths.TestBuildsAzurePath);

            bool isValidService = exists.Value && requiredDirs.Count() == 2;

            if (isValidService && saveCreatedService) _shareClient = client;

            return isValidService;
        }

        private void PopulateDropDownLists(string softwarePath, ComboBox versionDropDown, ComboBox subVersionDropDown)
        {
            versionDropDown.Items.Clear();
            subVersionDropDown.Items.Clear();

            versionDropDown.DisplayMember = nameof(VersionInfo.Version);
            subVersionDropDown.DisplayMember = nameof(VersionInfo.Version);

            var softwareVersions = _shareClient.GetDirectories(startingDirectoryPath: softwarePath);

            if (softwareVersions.Count > 0)
            {
                // Populate Version Drop Down list.
                PopulateDropDownFromDirectoriesList(softwareVersions, versionDropDown, softwarePath);
                versionDropDown.SelectedIndex = 0;
            }
            else
            {
                MessageBox.Show(this, Constants.Messages.NoVersionsFound);
            }
        }

        private void PopulateDropDownFromDirectoriesList(List<ShareFileItem> softwareVersionFolders, ComboBox dropDown, string softwarePath)
        {
            foreach (var versionFolder in softwareVersionFolders)
            {
                if (versionFolder.IsDirectory)
                {
                    dropDown.Items.Add(new VersionInfo
                    {
                        VersionFileShareFolder = versionFolder,
                        Version = versionFolder.Name,
                        VersionFullPath = Helper.CombinePath(softwarePath, versionFolder.Name),
                    });
                }
            }
        }

        private void UpdateSubVersionDropDownList(ComboBox versionDropDown, ComboBox subVersionDropDown)
        {
            subVersionDropDown.Items.Clear();

            if (versionDropDown.Items.Count > 0)
            {
                var selectedVersion = GetSelectedVersionInfo(versionDropDown);

                // Populate Sub Versions.
                if (!string.IsNullOrWhiteSpace(selectedVersion.VersionFullPath))
                {
                    var subVersions = _shareClient.GetDirectories(startingDirectoryPath: selectedVersion.VersionFullPath);

                    if (subVersions.Count > 0)
                    {
                        PopulateDropDownFromDirectoriesList(subVersions, subVersionDropDown, selectedVersion.VersionFullPath);
                        subVersionDropDown.SelectedIndex = 0;
                    }
                    else
                    {
                        MessageBox.Show(this, Constants.Messages.NoSubVersionsFound + selectedVersion.Version);
                    }
                }
            }
        }

        private bool IsSubVersionValid(ComboBox subVersionDropDown)
        {
            if (subVersionDropDown.Items.Count > 0)
            {
                var selected = subVersionDropDown.Items[subVersionDropDown.SelectedIndex] as VersionInfo;
                return !string.IsNullOrWhiteSpace(selected.VersionFullPath);
            }

            return false;
        }

        private VersionInfo GetSelectedVersionInfo(ComboBox dropDown)
        {
            int index = dropDown.SelectedIndex;
            if (index < 0) return null;
            return dropDown.Items[index] as VersionInfo;
        }

        //public async void DownloadFile(VersionInfo subVersionInfo, string filePathPrefix, string fileName, IProgress<int> progress)
        //{
        //    string localDownloadPath = textBoxDownloadsPath.Text.Trim();
        //    if (!Directory.Exists(localDownloadPath)) Directory.CreateDirectory(localDownloadPath);

        //    string fullDirectoryPathToFile = Helper.CombinePath(subVersionInfo.VersionFullPath, filePathPrefix);

        //    await Task.Run(() => _shareClient.DownloadFileAsync(fullDirectoryPathToFile, fileName, localDownloadPath, progress));
        //}

        private bool IsFileAlreadyDownloaded(string fileName, string downloadsPath, out string expecteFileFullPath)
        {
            expecteFileFullPath = Helper.CombinePath(downloadsPath, fileName);
            return File.Exists(expecteFileFullPath);
        }

        private void HandleGetCurrentVersionsClick(IWin32Window owner, string softwareAzurePath, ComboBox versionDropDown, ComboBox subVersionDropDown)
        {
            if (TestAzureConnection(true))
            {
                PopulateDropDownLists(softwareAzurePath, versionDropDown, subVersionDropDown);
            }
            else
            {
                MessageBox.Show(owner, Constants.Messages.AzureConnectionError, "Unable To Connect");
            }
        }

        private void HandleDownloadOrDownloadInstallClick(IWin32Window owner, ProgressBar progressBar, ComboBox subVersionDropDown, string fileNamePrefix, string fileNameSuffix, string filePathPrefix, bool isInstall)
        {
            progressBar.Value = 0;
            VersionInfo subVersion = GetSelectedVersionInfo(subVersionDropDown);

            if (subVersion == null)
            {
                MessageBox.Show(owner, "Cannot Download/Install as no Sub-Version folder exists.", "No Sub-Version Exists for download.");
                return;
            }

            string fileNameToDownload = $"{fileNamePrefix}{subVersion.Version}{fileNameSuffix}";

            progressBar.Value = 0;
            DownloadAndInstallFile(subVersion, filePathPrefix, fileNameToDownload, isInstall);
            progressBar.Value = 100;
            //var downloadProgress = new Progress<int>(p => { progressBarRiposteCore_Download.Value = p; });
        }

        public void DownloadAndInstallFile(VersionInfo subVersionInfo, string filePathPrefix, string fileName, bool installAfter = true)
        {
            string localDownloadPath = textBoxDownloadsPath.Text.Trim();
            if (!Directory.Exists(localDownloadPath)) Directory.CreateDirectory(localDownloadPath);

            string expectedFileFullPath;
            bool fileExists = IsFileAlreadyDownloaded(fileName, localDownloadPath, out expectedFileFullPath);
            long? downloadedSize = null;

            if (fileExists)
            {
                if (!installAfter)
                {
                    MessageBox.Show(this, $"\n{fileName} has already been downloaded to {localDownloadPath}", "No Download Required");
                }
            }
            else
            {
                // Download File.
                string fullDirectoryPathToFile = Helper.CombinePath(subVersionInfo.VersionFullPath, filePathPrefix);

                downloadedSize = _shareClient.DownloadFile(fullDirectoryPathToFile, fileName, localDownloadPath);

                if (!installAfter)
                {
                    MessageBox.Show(this, $"{downloadedSize.Value / Constants.NumberOfBytesInMegabyte} Megabytes Downloaded.\n" +
                          $"\n{fileName} has been downloaded to {localDownloadPath}",
                          "Download Complete");
                }
            }

            if (installAfter) 
            {
                InstallZipFile(expectedFileFullPath, fileName, downloadedSize);
            }
        }

        public void InstallZipFile(string expectedFileFullPath, string fileName, long? downloadedSize)
        {
            string egInstallerFileFullPath = textBoxEGInstallWSExePath.Text.Trim();

            if (!File.Exists(egInstallerFileFullPath))
            {
                MessageBox.Show(this, $"Please update the settings Tab and ensure that the EGInstallWSPackage.exe file is located in the specified path: {egInstallerFileFullPath} or update the specified path.", "EG Install WS executable could not be located");
                return;
            }

            string cmdCommand =
                     $"\"{egInstallerFileFullPath}\" " +
                     $"\"{expectedFileFullPath}\"";

            Process cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = false;
            cmd.StartInfo.CreateNoWindow = false;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();

            cmd.StandardInput.WriteLine("riposteuser logoff");
            Thread.Sleep(2000);
            cmd.StandardInput.WriteLine(cmdCommand);
            //cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit();
            //Console.WriteLine(cmd.StandardOutput.ReadToEnd());

            string summaryMessage = string.Empty;
            if (downloadedSize.HasValue)
            {
                summaryMessage += $"{downloadedSize.Value / Constants.NumberOfBytesInMegabyte} Megabytes Downloaded.\n" + $"{fileName} has been downloaded to {expectedFileFullPath}";
            }
            summaryMessage += $"{fileName} was succesfully installed.";
            summaryMessage += $"\nPC must be rebooted in order for any changes to take effect. Please reboot to complete installation.";

            MessageBox.Show(this, summaryMessage, $"Installation Complete for {fileName}");
        }

        #endregion

        #region Riposte Core Tab

        // Get Versions List Button.
        private void buttonRiposteCore_GetVersions_Click(object sender, EventArgs e)
        {
            HandleGetCurrentVersionsClick(this.tabRiposteCore, Constants.AzurePaths.RiposteCoreAzurePath, comboBoxRiposteCore_Version, comboBoxRiposteCode_SubVersion);
        }

        // Version Drop Down - On Selection.
        private void comboBoxRiposteCore_Version_SelectedIndexChanged(object sender, EventArgs e)
        {
            progressBarRiposteCore_Download.Value = 0;
            UpdateSubVersionDropDownList(comboBoxRiposteCore_Version, comboBoxRiposteCode_SubVersion);
        }

        // Sub-Version Drop Down - On Selection.
        private void comboBoxRiposteCode_SubVersion_SelectedIndexChanged(object sender, EventArgs e)
        {
            progressBarRiposteCore_Download.Value = 0;

            if (IsSubVersionValid(comboBoxRiposteCode_SubVersion))
            {
                buttonRiposteCore_Download.Enabled = true;
                buttonRiposteCore_DownloadInstall.Enabled = true;
            }
            else
            {
                buttonRiposteCore_Download.Enabled = false;
                buttonRiposteCore_DownloadInstall.Enabled = false;
            }
        }

        // Download Button
        private void buttonRiposteCore_Download_Click(object sender, EventArgs e) => RiposteCoreDownloadOrInstall(false);

        // Download & Install Button
        private void buttonRiposteCore_DownloadInstall_Click(object sender, EventArgs e) => RiposteCoreDownloadOrInstall(true);

        private void RiposteCoreDownloadOrInstall(bool install)
        {
            HandleDownloadOrDownloadInstallClick(this.tabRiposteCore, 
                                                 progressBarRiposteCore_Download,
                                                 comboBoxRiposteCode_SubVersion,
                                                 Constants.RiposteCoreAzureFile.FileNamePrefix,
                                                 Constants.RiposteCoreAzureFile.FileNameSuffix, 
                                                 Constants.RiposteCoreAzureFile.FilePathPrefix, 
                                                 install);
        }

        #endregion

        #region EssentialCP Tab

        // Get Versions List Button.
        private void button_ECP_GetVersionsList_Click(object sender, EventArgs e)
        {
            HandleGetCurrentVersionsClick(this.tabRiposteCore, Constants.AzurePaths.EssentialCPAzurePath, comboBox_ECP_Version, comboBox_ECP_SubVersion);
        }

        // Version Drop Down - On Selection.
        private void comboBox_ECP_Version_SelectedIndexChanged(object sender, EventArgs e)
        {
            progressBar_ECP_ProgressBar.Value = 0;
            UpdateSubVersionDropDownList(comboBox_ECP_Version, comboBox_ECP_SubVersion);
        }

        // Sub-Version Drop Down - On Selection.
        private void comboBox_ECP_SubVersion_SelectedIndexChanged(object sender, EventArgs e)
        {
            progressBar_ECP_ProgressBar.Value = 0;

            if (IsSubVersionValid(comboBox_ECP_SubVersion))
            {
                button_ECP_Download.Enabled = true;
                button_ECP_DownloadAndInstall.Enabled = true;
            }
            else
            {
                button_ECP_Download.Enabled = false;
                button_ECP_DownloadAndInstall.Enabled = false;
            }
        }

        // Download Button
        private void button_ECP_Download_Click(object sender, EventArgs e) => EssentialCPDownloadOrInstall(false);

        // Download & Install Button
        private void button_ECP_DownloadAndInstall_Click(object sender, EventArgs e) => EssentialCPDownloadOrInstall(true);

        private void EssentialCPDownloadOrInstall(bool install)
        {
            HandleDownloadOrDownloadInstallClick(this.tabECP,
                                                 progressBar_ECP_ProgressBar,
                                                 comboBox_ECP_SubVersion,
                                                 Constants.EssentialCPAzureFile.FileNamePrefix,
                                                 Constants.EssentialCPAzureFile.FileNameSuffix,
                                                 Constants.EssentialCPAzureFile.FilePathPrefix,
                                                 install);
        }

        #endregion

        #region Settings Tab

        // Settings Tab - Test Button
        private void buttonTestConnection_Click(object sender, EventArgs e)
        {
            bool isValidConnection = TestAzureConnection();
            string resultText = isValidConnection ? Constants.Messages.AzureConnectionSuccess : Constants.Messages.AzureConnectionError;
            MessageBox.Show(this.tabSettings, resultText, "Connection Test");
        }

        private void SaveUpdatedSettings() => Properties.Settings.Default.Save();

        private void UpdateAzureConnectionSettings(bool saveSettings = true)
        {
            Properties.Settings.Default.AzureFileShareName = textBoxFileShareName.Text.Trim();
            Properties.Settings.Default.AzureFileShareConnectionString = textBoxConnectionString.Text.Trim();
            if (saveSettings) SaveUpdatedSettings();
        }

        private void UpdateLocalPathSettings(bool saveSettings = true)
        {
            Properties.Settings.Default.DownloadsPath = textBoxDownloadsPath.Text.Trim();
            Properties.Settings.Default.EGInstallWSExePath = textBoxEGInstallWSExePath.Text.Trim();
            if (saveSettings) SaveUpdatedSettings();
        }

        // Settings Tab - Save Connection Settings Button
        private void buttonSaveAzureConnectionSettings_Click(object sender, EventArgs e) => UpdateAzureConnectionSettings();
        
        // Settings Tab - Save Local Path Settings Button
        private void buttonSavePathSettings_Click(object sender, EventArgs e) => UpdateLocalPathSettings();

        // Settings Tab - Save All Settings Button
        private void buttonSaveAllSettings_Click(object sender, EventArgs e)
        {
            UpdateAzureConnectionSettings(false);
            UpdateLocalPathSettings(false);
            SaveUpdatedSettings();
        }

        // Settings Tab - Select Downloads Folder Button
        private void buttonDownloadsPathBrowse_Click(object sender, EventArgs e)
        {
            var folderBrowserDialog = new FolderBrowserDialog();

            DialogResult result = folderBrowserDialog.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test user input.
            {
                textBoxDownloadsPath.Text = folderBrowserDialog.SelectedPath; // Update Text Field.
            }
        }

        // Settings Tab - Select EGInstallWSPackage Executable File Button
        private void buttonEGInstallWSPathBrowse_Click(object sender, EventArgs e)
        {
            var fileBrowserDialog = new OpenFileDialog();
            fileBrowserDialog.Title = "Identify EGInstallWSPackage.exe Path";

            string expectedFolderPath = fileBrowserDialog.InitialDirectory = @"C:\Counters\bin";

            if (Directory.Exists(expectedFolderPath)) fileBrowserDialog.InitialDirectory = expectedFolderPath;
            else fileBrowserDialog.InitialDirectory = @"C:\";

            fileBrowserDialog.Filter = "Exe Files (.exe)|*.exe|All Files (*.*)|*.*";
            fileBrowserDialog.FilterIndex = 1;
            fileBrowserDialog.RestoreDirectory = true;

            DialogResult result = fileBrowserDialog.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test user input.
            {
                textBoxEGInstallWSExePath.Text = fileBrowserDialog.FileName; // Update Text Field.
            }
        }


        #endregion

        #region Menu Items

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this.menuStrip1, "Created by Mohamed Ali Ramadan for the 2022 Hack-a-thon @ Escher Group.", "About");
        }

        private void guideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this.menuStrip1, 
                            "This tool allows to quickly get a list of all the latest versions for RiposteCore and EssentialCP POS workstation software. " +
                            "Once a desired version is selected, user has the ability to download the files and also trigger the install commands. " +
                            "It is recommended to only use this tool if the Install commands work on your local system as error handling/logging " +
                            "is not implemented in this version. If there are errors, investiate them using the standard command line and come back to this " +
                            "tool once errors are resolved. Make sure to reboot after every succesful installation.", "Guide");
        }

        #endregion
    }
}
