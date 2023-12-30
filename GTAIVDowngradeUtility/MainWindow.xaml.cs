using GTAIVDowngradeUtilityWPF.Common;
using GTAIVDowngradeUtilityWPF.GTAIVDowngradeUtility;
using GTAIVSetupUtilityWPF.Functions;
using Microsoft.VisualBasic.FileIO;
using Microsoft.WindowsAPICodePack.Dialogs;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

// hi here, i'm an awful coder, so please clean up for me if it really bothers you

namespace GTAIVDowngradeUtilityWPF
{
    public partial class MainWindow : Window
    {
        bool backupexists = false;
        string directory;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        [STAThread]
        public static void Main()
        {
            Application app = new Application();
            MainWindow mainWindow = new MainWindow();
            app.Run(mainWindow);
        }
        public MainWindow()
        {
            if (File.Exists("GTAIVDowngradeUtilityLog.txt")) { File.Delete("GTAIVDowngradeUtilityLog.txt"); }
            NLog.LogManager.Setup().LoadConfiguration(builder =>
            {
                builder.ForLogger().FilterMinLevel(LogLevel.Debug).WriteToFile(fileName: "GTAIVDowngradeUtilityLog.txt");
            });
            Logger.Info(" Initializing the main window...");
            InitializeComponent();
            Logger.Info(" Main window initialized!");
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Logger.Debug(" User clicked on a hyperlink from the main window.");
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmd",
                Arguments = $"/c start {e.Uri.AbsoluteUri}",
                CreateNoWindow = true,
                UseShellExecute = false,
            };
            Process.Start(psi);
        }
        private static void Empty(System.IO.DirectoryInfo directory)
        {
            foreach (System.IO.FileInfo file in directory.GetFiles()) file.Delete();
            foreach (System.IO.DirectoryInfo subDirectory in directory.GetDirectories()) subDirectory.Delete(true);
        }

        private static void CopyFolder(string sourceFolder, string destinationFolder)
        {
            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            string[] files = Directory.GetFiles(sourceFolder);
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                string destinationFilePath = Path.Combine(destinationFolder, fileName);
                File.Copy(file, destinationFilePath, true);
            }

            string[] subFolders = Directory.GetDirectories(sourceFolder);
            foreach (string subFolder in subFolders)
            {
                string folderName = Path.GetFileName(subFolder);
                string destinationSubFolder = Path.Combine(destinationFolder, folderName);
                CopyFolder(subFolder, destinationSubFolder);
            }
        }

        private string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? String.Empty;
        }

        private void steamdepot_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User switched to steam depot downgrading.");
            DepotDownloaderWindow window2 = new DepotDownloaderWindow();
            window2.Show();
        }
        private void version_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled game version.");
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");
                MessageBox.Show("1.0.8.0 is generally a better patch, as it fixes a few bugs, including VRAM detection and a few 60 FPS issues.\n\nYou may want to prefer 1.0.7.0 if your specific mods (LCPDFR, ScriptHookDotNet mods) don't support 1.0.8.0, but generally it's recommended to keep it at 1.0.8.0.");
            }

        }
        private void radio_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled radio downgrading.");
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");

                MessageBox.Show($"This option will prompt you to downgrade your radio later depending on your current options. It is not necessary to do, however.");
            }
        }
        private void ffix_Click(object sender, RoutedEventArgs e)
        {
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");
                MessageBox.Show("This option installs FusionFix.\n\nGenerally not necessary, but it provides a lot of improvements to the shaders and adds in a way to load mods without replacing original files. Also helpful for downgrading radio.");
            }
        }
        private void gfwl_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled GFWL.");
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");
                MessageBox.Show("This option will attempt to ensure GFWL compatibility by installing the GFWL redists and ensuring there is no xlive.dll in the folder.\n\nThis tool does not guarantee 100% compatibility, however.");
            }
        }

        private void aboutButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User opened the About window.");
            MessageBox.Show(
                "This software is made by Gillian for the RevIVal Community. Below is debug text, you don't need it normally.\n\n" +
                $"Version: {GetAssemblyVersion()}",
                "Information");
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User is selecting the game folder...");
            steamdepotbtn.IsEnabled = false;
            while (true)
            {
                CommonOpenFileDialog dialog = new CommonOpenFileDialog();
                dialog.InitialDirectory = "C:\\Program Files (x86)\\Steam\\steamapps\\Grand Theft Auto IV\\GTAIV";
                dialog.IsFolderPicker = true;
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    Logger.Debug(" User selected a folder, proceeding...");
                    if (AppVersionGrabber.GetFileVersion($"{dialog.FileName}\\GTAIV.exe").StartsWith("1, 0") || (AppVersionGrabber.GetFileVersion($"{dialog.FileName}\\GTAIV.exe").StartsWith("1.2")))
                    {
                        if (AppVersionGrabber.GetFileVersion($"{dialog.FileName}\\GTAIV.exe").StartsWith("1, 0"))
                        {
                            Logger.Debug(" Folder contains a retail exe already.");
                            MessageBox.Show("Your game exe is already downgraded, proceeding to use the tool may produce unexpected results and corrupt the game.");
                        }
                        else { Logger.Debug(" Folder contains an exe of Steam Version."); }

                        if (dialog.FileName.Contains("Steam")){ } // TODO: Add steamdepotbtn.IsEnabled = true; in here when the page is functional

                        if (Directory.Exists($"{dialog.FileName}\\backup")) { backupexists = true; }

                        directorytxt.Text = "Game Directory:";
                        gamedirectory.Text = dialog.FileName;
                        directory = dialog.FileName;
                        downgradeOptionsPanel.IsEnabled = true;
                        break;
                    }

                    else
                    {
                        Logger.Debug(" User selected the wrong folder. Displaying a MessageBox.");
                        MessageBox.Show("The selected folder does not contain GTA IV.");
                    }
                }
                else
                {
                    break;
                }

            }
        }
        private void Backup()
        {
            Logger.Debug(" Starting backup...");
            string backupfolder = $"{directory}\\backup";
            if (backupexists == true)
            {
                MessageBoxResult result = MessageBox.Show("You already have an existing backup. Do you wish to replace it?", "Existing backup", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    System.IO.DirectoryInfo directoryclear = new System.IO.DirectoryInfo(backupfolder);
                    Empty(directoryclear);
                }
                else
                {
                    Logger.Debug("User had an existing backup and chose not to replace it.");
                    return;
                }
            }
            else
            {
                Directory.CreateDirectory(backupfolder);
            }
            File.Copy($"{directory}\\GTAIV.exe", $"{backupfolder}\\GTAIV.exe");
            File.Copy($"{directory}\\PlayGTAIV.exe", $"{backupfolder}\\PlayGTAIV.exe");
            File.Copy($"{directory}\\steam_api.dll", $"{backupfolder}\\steam_api.dll");
            foreach (var file in Directory.GetFiles(directory, "*.asi"))
            {
                File.Copy(file, Path.Combine(backupfolder, Path.GetFileName(file)));
            }
            foreach (var file in Directory.GetFiles(directory, "*.ini"))
            {
                File.Copy(file, Path.Combine(backupfolder, Path.GetFileName(file)));
            }
            if (Directory.Exists($"{directory}\\plugins"))
            {
                FileSystem.CopyDirectory($"{directory}\\plugins", $"{backupfolder}\\plugins");
            }
            backupexists = true;
            Logger.Debug(" Backup complete, disabled the backup button to prevent accidental backups after downgrading.");
        }

        private void backup_Click(object sender, RoutedEventArgs e)
        {
            backupbtn.IsEnabled = false;
            backupbtn.Content = "Backing up...";
            Backup();
            backupbtn.Content = "Backed up!";
        }
        private async void downgrade_Click(object sender, RoutedEventArgs e)
        {
            downgradeOptionsPanel.IsEnabled = false;
            downgradebtn.Content = "Downgrading...";
            Logger.Debug(" Starting the downgrade...");
            if (backupexists == false)
            {
                MessageBoxResult result = MessageBox.Show("Backup not found. Do you wish to backup now?", "No backup found", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    Backup();
                }
            }
            // http client for downloading fusionfix and asi loader
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Other");

            // removing any scripts because they can break shit and we don't want that
            foreach (var file in Directory.GetFiles(directory, "*.asi"))
            {
                File.Delete(file);
            }
            foreach (var file in Directory.GetFiles(directory, "*.ini"))
            {
                File.Delete(file);
            }
            if (Directory.Exists($"{directory}\\plugins"))
            {
                Directory.Delete($"{directory}\\plugins", true);
            }

            // actually downgrading now

            // ultimate asi loader
            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = configFile.AppSettings.Settings;
            string downloadedual = settings["ultimate-asi-loader"].Value;
            var firstResponse3 = await httpClient.GetAsync("https://api.github.com/repos/ThirteenAG/Ultimate-ASI-Loader/releases/latest");
            firstResponse3.EnsureSuccessStatusCode();
            var firstResponseBody3 = await firstResponse3.Content.ReadAsStringAsync();
            var latestual = JsonDocument.Parse(firstResponseBody3).RootElement.GetProperty("tag_name").GetString();
            if (latestual != downloadedual)
            {
                Logger.Debug(" Latest UAL not matching to downloaded, downloading...");
                Logger.Debug(" Latest is " + latestual);
                var downloadUrl3 = JsonDocument.Parse(firstResponseBody3).RootElement.GetProperty("assets")[0].GetProperty("browser_download_url").GetString();
                GithubDownloader.Download(downloadUrl3!, "Files\\Shared", "Ultimate-ASI-Loader.zip");
                ZipFile.ExtractToDirectory("Files\\Shared\\Ultimate-ASI-Loader.zip", "Files\\Shared\\", true);
                File.Delete("Files\\Shared\\Ultimate-ASI-Loader.zip");
                settings["ultimate-asi-loader"].Value = latestual;
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
                Logger.Debug(" Edited the value in the config.");

            }
            if (gfwlcheckbox.IsChecked == false)
            {
                if (File.Exists("Files\\Shared\\dinput8.dll"))
                {
                    File.Move("Files\\Shared\\dinput8.dll", "Files\\Shared\\xlive.dll", true);
                }
                if (File.Exists($"{directory}\\dinput8.dll"))
                {
                    File.Move($"{directory}\\dinput8.dll", $"{directory}\\xlive.dll", true);
                }
            }
            else
            {
                if (File.Exists("Files\\Shared\\xlive.dll"))
                {
                    File.Move("Files\\Shared\\xlive.dll", "Files\\Shared\\dinput8.dll", true);
                }
                    if (File.Exists($"{directory}\\xlive.dll"))
                {
                    File.Move($"{directory}\\xlive.dll", $"{directory}\\dinput8.dll", true);
                }
            }

            // shared files
            foreach (var file in Directory.GetFiles("Files\\Shared"))
            {
                File.Copy(file, Path.Combine(directory, Path.GetFileName(file)), true);
            }

            // zolikapatch setup

            File.Copy("Files\\ZolikaPatch\\ZolikaPatch.asi", $"{directory}\\ZolikaPatch.asi", true);

            switch (ffixcheckbox.IsChecked, gfwlcheckbox.IsChecked)
            {
                case (true, true):
                    {
                        File.Copy("Files\\ZolikaPatch\\ZolikaPatch-FFix-GFWL.ini", $"{directory}\\ZolikaPatch.ini", true);
                        break;
                    }
                case (false, true):
                    {
                        File.Copy("Files\\ZolikaPatch\\ZolikaPatch-NoFFix-GFWL.ini", $"{directory}\\ZolikaPatch.ini", true);
                        break;
                    }
                case (true, false):
                    {
                        File.Copy("Files\\ZolikaPatch\\ZolikaPatch-FFix.ini", $"{directory}\\ZolikaPatch.ini", true);
                        break;
                    }
                case (false, false):
                    {
                        File.Copy("Files\\ZolikaPatch\\ZolikaPatch-NoFFix.ini", $"{directory}\\ZolikaPatch.ini", true);
                        break;
                    }
            }

            if (patch8click.IsChecked == true)
            {
                File.Copy("Files\\1080\\GTAIV.exe", $"{directory}\\GTAIV.exe", true);
            }
            else
            {
                File.Copy("Files\\1070\\GTAIV.exe", $"{directory}\\GTAIV.exe", true);
            }

            // fusionfix
            if (ffixcheckbox.IsChecked == true)
            {
                string downloadedff = settings["fusionfix"].Value;
                var firstResponse = await httpClient.GetAsync("https://api.github.com/repos/ThirteenAG/GTAIV.EFLC.FusionFix/releases/latest");
                firstResponse.EnsureSuccessStatusCode();
                var firstResponseBody = await firstResponse.Content.ReadAsStringAsync();
                var latestff = JsonDocument.Parse(firstResponseBody).RootElement.GetProperty("tag_name").GetString();
                if (latestff != downloadedff)
                {
                    Logger.Debug(" Downloaded version of FusionFix doesn't match the latest version, downloading...");
                    var downloadUrl = JsonDocument.Parse(firstResponseBody).RootElement.GetProperty("assets")[0].GetProperty("browser_download_url").GetString();
                    GithubDownloader.Download(downloadUrl!, "Files\\FusionFix","FusionFix.zip");
                    ZipFile.ExtractToDirectory("Files\\FusionFix\\FusionFix.zip", "Files\\FusionFix\\", true);
                    File.Delete("Files\\FusionFix\\dinput8.dll");
                    File.Delete("Files\\FusionFix\\FusionFix.zip");
                    File.Move("Files\\FusionFix\\plugins\\GTAIV.EFLC.FusionFix.asi", "Files\\FusionFix\\GTAIV.EFLC.FusionFix.asi", true);
                    File.Move("Files\\FusionFix\\plugins\\GTAIV.EFLC.FusionFix.ini", "Files\\FusionFix\\GTAIV.EFLC.FusionFix.ini", true);
                    Directory.Delete("Files\\FusionFix\\plugins");
                    settings["fusionfix"].Value = latestff;
                    configFile.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
                    Logger.Debug(" Edited the value in the config.");
                }
                if (gfwlcheckbox.IsChecked == true)
                {
                    var firstResponse2 = await httpClient.GetAsync("https://api.github.com/repos/gillian-guide/GTAIV.EFLC.FusionFix-GFWL/releases/latest");
                    firstResponse2.EnsureSuccessStatusCode();
                    var firstResponseBody2 = await firstResponse2.Content.ReadAsStringAsync();
                    var downloadUrl2 = JsonDocument.Parse(firstResponseBody2).RootElement.GetProperty("assets")[0].GetProperty("browser_download_url").GetString();
                    GithubDownloader.Download(downloadUrl2!, "Files\\FusionFix", "FusionFix-GFWL.zip");
                    ZipFile.ExtractToDirectory("Files\\FusionFix\\FusionFix-GFWL.zip", "Files\\FusionFix", true);
                    File.Delete("Files\\FusionFix\\FusionFix-GFWL.zip");
                }
                CopyFolder("Files\\FusionFix\\", $"{directory}");

            }
            downgradebtn.Content = "Downgrade";
            downgradeOptionsPanel.IsEnabled = true;
        }

        private void redist_Click(object sender, RoutedEventArgs e)
        {
            backupbtn.IsEnabled = false;
            redistbtn.Content = "Installing...";
            Logger.Debug(" Installing redistributables...");
            var vcredist = new Process
            {
                StartInfo =
                {
                  FileName = $"Files\\Redist\\vcredist_x86.exe",
                  Arguments = "/Q"
                }
            };
            vcredist.Start();
            Process.Start($"Files\\Redist\\gfwlivesetup.exe");
            backupbtn.IsEnabled = true;
            redistbtn.Content = "Reinstall redistributables";
        }
    }
}
