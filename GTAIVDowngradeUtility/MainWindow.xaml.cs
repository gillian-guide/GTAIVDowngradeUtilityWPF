using GTAIVDowngradeUtilityWPF.Common;
using GTAIVDowngradeUtilityWPF.Functions;
using GTAIVSetupUtilityWPF.Functions;
using Microsoft.WindowsAPICodePack.Dialogs;
using NLog;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Windows;

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
            foreach (System.IO.FileInfo file in directory.GetFiles()) { file.Delete(); }
            foreach (System.IO.DirectoryInfo subDirectory in directory.GetDirectories()) { subDirectory.Delete(true); }
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
        private void version_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled game version.");
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");
                MessageBox.Show("1.0.8.0 is generally a better patch, as it fixes a few bugs, including VRAM detection and a few 60 FPS issues.\n\nYou may want to prefer 1.0.7.0 if your specific mods (LCPDFR, ScriptHookDotNet mods) don't support 1.0.8.0, but generally it's recommended to keep it at 1.0.8.0.");
            }

        }

        private void full_Click(object sender, RoutedEventArgs e)
        {
            if (fullcheckbox.IsChecked == true)
            {
                zpatchcheckbox.IsEnabled = true;
            }
            else
            {
                zpatchcheckbox.IsEnabled = false;
                zpatchcheckbox.IsChecked = true;
            }
            Logger.Debug(" User toggled full downgrading.");
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");

                MessageBox.Show($"This option will download and unpack extra files to match the old version files (almost) entirely.\n\nThe reason I don't do that from the start is to save space, and because it's really unnecessary to have the whole thing.");
            }
        }

        private void zpatch_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled ZolikaPatch.");
            if (zpatchcheckbox.IsChecked == false)
            {
                radiocheckbox.IsChecked = true;
            }
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");

                MessageBox.Show($"This option allows to disable installing ZolikaPatch. Only needed when performing a full downgrade.\n\nKeep in mind that the DLC's (The Lost and Damned & The Ballad of Gay Tony) are not accessible without ZolikaPatch. You will also be missing a lot of quality of life improvements.\n\nDue to issues of running a downgraded copy without ZolikaPatch, radio downgrade is enforced with ZPatch off.");
            }
        }
        private void radio_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled radio downgrading.");
            if (radiocheckbox.IsChecked == false)
            {
                zpatchcheckbox.IsChecked = true;
            }
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");

                MessageBox.Show($"This option will prompt you to downgrade your radio later depending on your current options. It is not necessary to do, however.");
            }
        }
        private void ffix_Click(object sender, RoutedEventArgs e)
        {
            if (ffixcheckbox.IsChecked == true && gfwlcheckbox.IsChecked == false && xlivelesscheckbox.IsChecked == false && zpatchcheckbox.IsChecked == false)
            {
                MessageBox.Show("It's recommended to at least enable XLiveless Addon when using FusionFix in compatibility mode.");
            }
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");
                MessageBox.Show("This option installs FusionFix.\n\nGenerally not necessary, but it provides a lot of improvements to the shaders and adds in a way to load mods without replacing original files. Also helpful for downgrading radio.");
            }
        }
        private void gfwl_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled GFWL.");
            if (gfwlcheckbox.IsChecked == false)
            {
                xlivelesscheckbox.IsEnabled = true;
                gfwlportablecheckbox.IsChecked = false;
                gfwlportablecheckbox.IsEnabled = false;
            }
            else
            {
                xlivelesscheckbox.IsEnabled = false;
                gfwlportablecheckbox.IsEnabled = true;
                gfwlportablecheckbox.IsChecked = true;
            }
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");
                MessageBox.Show("This option will attempt to ensure GFWL compatibility by installing the GFWL redists and ensuring there is no xlive.dll in the folder.\n\nThis tool does not guarantee 100% compatibility, however. Also it's highly recommended to install ZolikaPatch if enabling this.");
            }
        }

        private void gfwlportable_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled Portable GFWL");
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");
                MessageBox.Show("This option will add portable GFWL files, removing the necessity to install GFWL Redistributables.");
            }
        }
        private void steamchieves_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled Steam Achievements.");
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");
                MessageBox.Show("This option installs Zolika's Steam Achievements mod to be able to get Steam achievements on downgraded copies.\n\nMay not work with GFWL.");
            }
        }

        private void xliveless_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled XLiveless Addon.");
            if (xlivelesscheckbox.IsChecked == true)
            {
                gfwlcheckbox.IsChecked = false;
                gfwlcheckbox.IsEnabled = false;
                gfwlportablecheckbox.IsChecked = false;
                gfwlportablecheckbox.IsEnabled = false;
            }
            else
            {
                gfwlportablecheckbox.IsEnabled = true;
                gfwlcheckbox.IsEnabled = true;
            }
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");
                MessageBox.Show("This option adds a few additions to xliveless.\n\nOnly available if not setting up for GFWL.");
            }
        }


        private void aboutButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User opened the About window.");
            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = configFile.AppSettings.Settings;
            string fusionfix = settings["fusionfix"].Value;
            string ual = settings["ultimate-asi-loader"].Value;
            MessageBox.Show(
                "This software is made by Gillian for the RevIVal Community and the Modding Guide.\n\n" +
                $"Downloaded Ultimate ASI Loader: {ual}\n" +
                $"Downloaded FusionFix: {fusionfix}\n" +
                $"Version: {GetAssemblyVersion()}",
                "Information");
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User is selecting the game folder...");
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

                        if (Directory.Exists($"{dialog.FileName}\\backup")) { backupexists = true; }

                        if (dialog.FileName.Contains("Steam")) { achievementscheckbox.IsEnabled = true; achievementscheckbox.IsChecked = true; }

                        directorytxt.Text = "Game Directory:";
                        directorytxt.FontWeight = FontWeights.Normal;
                        directorytxt.TextDecorations = null;
                        tipsnote.TextDecorations = TextDecorations.Underline;
                        gamedirectory.Text = dialog.FileName;
                        directory = dialog.FileName;
                        options.IsEnabled = true;
                        version.IsEnabled = true;
                        break;
                    }

                    else
                    {
                        Logger.Debug(" User selected the wrong folder. Displaying a MessageBox.");
                        MessageBox.Show("The selected folder does not contain GTA IV. Try again.");
                    }
                }
                else
                {
                    break;
                }

            }
        }

        private void backup_Click(object sender, RoutedEventArgs e)
        {
            backupbtn.IsEnabled = false;
            backupbtn.Content = "Backing up...";
            BackupGame.Backup(directory, backupexists);
            backupbtn.Content = "Backed up!";
        }
        private async void downgrade_Click(object sender, RoutedEventArgs e)
        {
            options.IsEnabled = false;
            version.IsEnabled = false;
            downgradebtn.Content = "Downgrading...";
            Logger.Info(" Starting the downgrade...");
            if (backupexists == false)
            {
                Logger.Debug(" Backup not found, prompting to backup.");
                MessageBoxResult result = MessageBox.Show("Backup not found. Do you wish to backup now?", "No backup found", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    BackupGame.Backup(directory, backupexists);
                }
            }

            if (radiocheckbox.IsChecked == true)
            {
                if (ffixcheckbox.IsChecked == true && !File.Exists($"{directory}\\update\\pc\\audio\\sfx\\radio_ny_classics.rpf"))
                {
                    MessageBoxResult result = MessageBox.Show("You chose to downgrade radio, but you don't have any downgrader downloaded.\n\nDo you wish to download one now? (you will be sent to download a downgrader that matches your options; selecting no will cancel downgrading)", "No radio downgrader found", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = "cmd",
                            Arguments = $"/c start {"http://downgraders.rockstarvision.com/"}",
                            CreateNoWindow = true,
                            UseShellExecute = false,
                        };
                        Process.Start(psi);
                        while (true)
                        {
                            MessageBoxResult result2 = MessageBox.Show("Press 'Yes' after installing the downgrader (at the bottom of the page).\n\nPress 'No' to cancel downgrading.", "No radio downgrader found", MessageBoxButton.YesNo);
                            if (result2 == MessageBoxResult.Yes)
                            {
                                if (!File.Exists($"{directory}\\update\\pc\\audio\\sfx\\radio_ny_classics.rpf"))
                                {
                                    MessageBox.Show("Radio downgrader not detected in the game folder. Try again.");
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                return;
                            }
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                else if (ffixcheckbox.IsChecked == false && !File.Exists("Files\\RadioDowngrade\\install.bat"))
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "cmd",
                        Arguments = $"/c start {"https://mega.nz/folder/Fn0Q3LhY#_0t1VZQFuQX22lMxRZNB1A/file/cnsk2bgb"}",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                    };
                    Process.Start(psi);
                    MessageBoxResult resultv = MessageBox.Show("Press 'Yes' if you want the new Vladivostok songs mixed in with the old ones. Press 'No' to only have the old Vladivostok songs.", "No radio downgrader found", MessageBoxButton.YesNo);
                    if (resultv == MessageBoxResult.Yes)
                    {
                        ProcessStartInfo psi2 = new ProcessStartInfo
                        {
                            FileName = "cmd",
                            Arguments = $"/c start {"https://mega.nz/folder/Fn0Q3LhY#_0t1VZQFuQX22lMxRZNB1A/file/kvkmlRRY"}",
                            CreateNoWindow = true,
                            UseShellExecute = false,
                        };
                        Process.Start(psi2);
                    }
                    else
                    {
                        ProcessStartInfo psi2 = new ProcessStartInfo
                        {
                            FileName = "cmd",
                            Arguments = $"/c start {"https://mega.nz/folder/Fn0Q3LhY#_0t1VZQFuQX22lMxRZNB1A/file/hj8WGZIT"}",
                            CreateNoWindow = true,
                            UseShellExecute = false,
                        };
                        Process.Start(psi2);
                    }
                    while (true)
                    {
                        MessageBoxResult result2 = MessageBox.Show("Press 'Yes' after downloading both archives (the ones you need are highlighted) and extracting them to 'Files\\RadioDowngrade'.\n\nThe folder should have 'common', 'pc', 'tlad', 'tbogt' folders and an 'install.bat'.\n\nPress 'No' to cancel downgrading.", "No radio downgrader found", MessageBoxButton.YesNo);
                        if (result2 == MessageBoxResult.Yes)
                        {
                            if (!File.Exists("Files\\RadioDowngrade\\install.bat") || !File.Exists("Files\\RadioDowngrade\\jptch.exe") || !Directory.Exists("Files\\RadioDowngrade\\pc") || (!Directory.Exists("Files\\RadioDowngrade\\common")) || !Directory.Exists("Files\\RadioDowngrade\\tlad") || !Directory.Exists("Files\\RadioDowngrade\\tbogt"))
                            {
                                MessageBox.Show("Radio downgrader not detected in the downgrader's Files folder. Try again.");
                            }
                            else
                            {
                                return;
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                }
                else if (ffixcheckbox.IsChecked == false && File.Exists("Files\\RadioDowngrade\\install.bat"))
                {
                    CopyFolder("Files\\RadioDowngrade", directory);
                    ProcessStartInfo psirad = new ProcessStartInfo
                    {
                        FileName = $"{directory}\\install.bat",
                        WorkingDirectory = directory
                    };
                    Process.Start(psirad);
                }
            }
            // http client for downloading various mods
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Other");

            // removing any scripts because they can break shit and we don't want that
            Logger.Info(" Removing existing plugins to avoid incompatibility.");
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
            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = configFile.AppSettings.Settings;
            if (!Directory.Exists("Files\\Shared"))
            {
                Directory.CreateDirectory("Files\\Shared");
            }

            // ultimate asi loader
            if (zpatchcheckbox.IsChecked == true || ffixcheckbox.IsChecked == true || achievementscheckbox.IsChecked == true || gfwlcheckbox.IsChecked == true || xlivelesscheckbox.IsChecked == true)
            {
                Logger.Info(" Installing Ultimate ASI Loader (won't be installed if no asi's are selected)...");
                string downloadedual = settings["ultimate-asi-loader"].Value;
                if (!File.Exists("Files\\Shared\\dinput8.dll") || !File.Exists("Files\\Shared\\dinput8.dll"))
                {
                    settings["ultimate-asi-loader"].Value = "";
                    configFile.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
                    Logger.Debug(" Ultimate ASI Loader not downloaded - changed the value of downloaded ual to null.");
                }
                var firstResponseual = await httpClient.GetAsync("https://api.github.com/repos/ThirteenAG/Ultimate-ASI-Loader/releases/latest");
                firstResponseual.EnsureSuccessStatusCode();
                var firstResponseBodyual = await firstResponseual.Content.ReadAsStringAsync();
                var latestual = JsonDocument.Parse(firstResponseBodyual).RootElement.GetProperty("tag_name").GetString();
                if (latestual != downloadedual)
                {
                    Logger.Debug(" Latest UAL not matching to downloaded, downloading...");
                    var downloadUrlual = JsonDocument.Parse(firstResponseBodyual).RootElement.GetProperty("assets")[0].GetProperty("browser_download_url").GetString();
                    GithubDownloader.Download(downloadUrlual!, "Files\\Shared", "Ultimate-ASI-Loader.zip");
                    ZipFile.ExtractToDirectory("Files\\Shared\\Ultimate-ASI-Loader.zip", "Files\\Shared\\", true);
                    File.Delete("Files\\Shared\\Ultimate-ASI-Loader.zip");
                    settings["ultimate-asi-loader"].Value = latestual;
                    configFile.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
                    Logger.Debug(" Edited the value in the config.");

                }
                Logger.Debug(" Renaming unmatching UAL names if exist...");
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
                    if (xlivelesscheckbox.IsChecked == true)
                    {
                        CopyFolder("Files\\XLivelessAddon", directory);
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
                if (File.Exists($"{directory}\\dsound.dll"))
                {
                    Logger.Debug(" Removing dsound.dll from the game folder to avoid incompatibility.");
                    File.Delete($"{directory}\\dsound.dll");
                }
            }

            // shared files
            if (!File.Exists("Files\\Shared\\play.dll"))
            {
                Logger.Info(" Downloading shared files...");
                var firstResponseshared = await httpClient.GetAsync("https://api.github.com/repos/gillian-guide/GTAIVFullDowngradeAssets/releases/135442404");
                firstResponseshared.EnsureSuccessStatusCode();
                var firstResponseBodyshared = await firstResponseshared.Content.ReadAsStringAsync();
                var downloadUrlshared = JsonDocument.Parse(firstResponseBodyshared).RootElement.GetProperty("assets")[0].GetProperty("browser_download_url").GetString();
                GithubDownloader.Download(downloadUrlshared!, "Files", "BaseAssets.zip");
                ZipFile.ExtractToDirectory("Files\\BaseAssets.zip", "Files", true);
                File.Delete("Files\\BaseAssets.zip");
            }

            Logger.Info(" Copying shared files...");
            CopyFolder("Files\\Shared", directory);

            // GFWL files
            if (gfwlcheckbox.IsChecked == true)
            {
                CopyFolder("Files\\GFWL", directory);
                if (gfwlportablecheckbox.IsChecked == true)
                {
                    CopyFolder("Files\\PortableGFWL", directory);
                }
            }

            // full files
            if (fullcheckbox.IsChecked == true)
            {
                if (patch8click.IsChecked == true)
                {
                    if (!Directory.Exists("Files\\1080FullFiles"))
                    {
                        Directory.CreateDirectory("Files\\1080FullFiles");
                        var firstResponse1080 = await httpClient.GetAsync("https://api.github.com/repos/gillian-guide/GTAIVFullDowngradeAssets/releases/135442415");
                        firstResponse1080.EnsureSuccessStatusCode();
                        var firstResponseBody1080 = await firstResponse1080.Content.ReadAsStringAsync();
                        var downloadUrl1080 = JsonDocument.Parse(firstResponseBody1080).RootElement.GetProperty("assets")[0].GetProperty("browser_download_url").GetString();
                        GithubDownloader.Download(downloadUrl1080!, "Files\\1080FullFiles", "1080FullFiles.zip");
                        ZipFile.ExtractToDirectory("Files\\1080FullFiles\\1080FullFiles.zip", "Files", true);
                        File.Delete("Files\\1080FullFiles\\1080FullFiles.zip");
                    }
                    CopyFolder("Files\\1080FullFiles", directory);
                }
                else
                {
                    if (!Directory.Exists("Files\\1070FullFiles"))
                    {
                        Directory.CreateDirectory("Files\\1070FullFiles");
                        var firstResponse1070 = await httpClient.GetAsync("https://api.github.com/repos/gillian-guide/GTAIVFullDowngradeAssets/releases/135442418");
                        firstResponse1070.EnsureSuccessStatusCode();
                        var firstResponseBody1070 = await firstResponse1070.Content.ReadAsStringAsync();
                        var downloadUrl1070 = JsonDocument.Parse(firstResponseBody1070).RootElement.GetProperty("assets")[0].GetProperty("browser_download_url").GetString();
                        GithubDownloader.Download(downloadUrl1070!, "Files\\1070FullFiles", "1070FullFiles.zip");
                        ZipFile.ExtractToDirectory("Files\\1070FullFiles\\1070FullFiles.zip", "Files", true);
                        File.Delete("Files\\1070FullFiles\\1070FullFiles.zip");
                    }
                    CopyFolder("Files\\1070FullFiles", directory);
                }
            }

            // steam achievements
            if (achievementscheckbox.IsChecked == true)
            {
                File.Copy("Files\\ZolikaPatch\\SteamAchievements.asi", $"{directory}\\SteamAchievements.asi", true);
            }

            // zolikapatch setup
            if (zpatchcheckbox.IsChecked == true)
            {
                Logger.Info(" Installing ZolikaPatch and it's matching ini...");
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
            }
            Logger.Info(" Moving over the specific .exe...");
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
                Logger.Info(" Installing FusionFix...");
                string downloadedff = settings["fusionfix"].Value;
                if (!File.Exists("Files\\FusionFix\\GTAIV.EFLC.FusionFix.asi"))
                {
                    settings["fusionfix"].Value = "";
                    configFile.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
                    Logger.Debug(" FusionFix not downloaded - changed the value of downloaded ffix to null.");
                }
                var firstResponseff = await httpClient.GetAsync("https://api.github.com/repos/ThirteenAG/GTAIV.EFLC.FusionFix/releases/latest");
                firstResponseff.EnsureSuccessStatusCode();
                var firstResponseBodyff = await firstResponseff.Content.ReadAsStringAsync();
                var latestff = JsonDocument.Parse(firstResponseBodyff).RootElement.GetProperty("tag_name").GetString();
                if (latestff != downloadedff)
                {
                    if (!Directory.Exists("Files\\FusionFix"))
                    {
                        Directory.CreateDirectory("Files\\FusionFix");
                    }
                    Logger.Debug(" Downloaded version of FusionFix doesn't match the latest version, downloading...");
                    var downloadUrlff = JsonDocument.Parse(firstResponseBodyff).RootElement.GetProperty("assets")[0].GetProperty("browser_download_url").GetString();
                    GithubDownloader.Download(downloadUrlff!, "Files\\FusionFix","FusionFix.zip");
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
                    Logger.Info(" Downloading the GFWL Patch for FusionFix...");
                    var firstResponse2 = await httpClient.GetAsync("https://api.github.com/repos/gillian-guide/GTAIV.EFLC.FusionFix-GFWL/releases/latest");
                    firstResponse2.EnsureSuccessStatusCode();
                    var firstResponseBody2 = await firstResponse2.Content.ReadAsStringAsync();
                    var downloadUrl2 = JsonDocument.Parse(firstResponseBody2).RootElement.GetProperty("assets")[0].GetProperty("browser_download_url").GetString();
                    GithubDownloader.Download(downloadUrl2!, "Files\\FusionFix", "FusionFix-GFWL.zip");
                    ZipFile.ExtractToDirectory("Files\\FusionFix\\FusionFix-GFWL.zip", "Files\\FusionFix", true);
                    File.Delete("Files\\FusionFix\\FusionFix-GFWL.zip");
                    CopyFolder("Files\\FusionFix\\", $"{directory}");
                }


            }
            Logger.Info(" Successfully downgraded!");
            MessageBox.Show("Your game has been downgraded in accordance with selected options!");
            downgradebtn.Content = "Downgrade";
            options.IsEnabled = true;
            version.IsEnabled = true;
        }

        private async void redist_Click(object sender, RoutedEventArgs e)
        {
            backupbtn.IsEnabled = false;
            redistbtn.Content = "Installing...";

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Other");

            var firstResponseredist = await httpClient.GetAsync("https://api.github.com/repos/gillian-guide/GTAIVFullDowngradeAssets/releases/latest");
            firstResponseredist.EnsureSuccessStatusCode();
            var firstResponseBodyredist = await firstResponseredist.Content.ReadAsStringAsync();
            Logger.Debug(" Installing redistributables...");
            if (gfwlportablecheckbox.IsChecked == false)
            {
                if (!Directory.Exists("Files\\Redist"))
                {
                    Directory.CreateDirectory("Files\\Redist");
                    Logger.Info(" Downloading redistributables...");
                    var downloadUrlredist = JsonDocument.Parse(firstResponseBodyredist).RootElement.GetProperty("assets")[1].GetProperty("browser_download_url").GetString();
                    GithubDownloader.Download(downloadUrlredist!, "Files", "Redist.zip");
                    ZipFile.ExtractToDirectory("Files\\Redist\\Redist.zip", "Files", true);
                    File.Delete("Files\\Redist\\Redist.zip");
                }

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
            }
            else
            {
                if (File.Exists($"{directory}\\Redistributables\\VCRed\\vcredist_x86.exe"))
                {
                    var vcredist = new Process
                    {
                        StartInfo =
                    {
                      FileName = $"{directory}\\Redistributables\\VCRed\\vcredist_x86.exe",
                      Arguments = "/Q"
                    }
                    };
                    vcredist.Start();
                }
                else
                {
                    if (!Directory.Exists("Files\\Redist"))
                    {
                        Directory.CreateDirectory("Files\\Redist");
                        Logger.Info(" Downloading redistributables...");
                        var downloadUrlredist = JsonDocument.Parse(firstResponseBodyredist).RootElement.GetProperty("assets")[1].GetProperty("browser_download_url").GetString();
                        GithubDownloader.Download(downloadUrlredist!, "Files", "Redist.zip");
                        ZipFile.ExtractToDirectory("Files\\Redist\\Redist.zip", "Files", true);
                        File.Delete("Files\\Redist\\Redist.zip");
                    }

                    var vcredist = new Process
                    {
                        StartInfo =
                    {
                      FileName = $"Files\\Redist\\vcredist_x86.exe",
                      Arguments = "/Q"
                    }
                    };
                    vcredist.Start();
                }
            }
            redistbtn.IsEnabled = true;
            redistbtn.Content = "Reinstall redistributables";
        }
    }
}
