using GTAIVDowngradeUtilityWPF.Common;
using GTAIVDowngradeUtilityWPF.Functions;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using NLog;
using RedistributableChecker;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

// hi here, i'm an awful coder, so please clean up for me if it really bothers you

namespace GTAIVDowngradeUtilityWPF
{
    public partial class MainWindow : Window
    {
        bool backupexists = false;
        bool sp = true;
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

            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = configFile.AppSettings.Settings;
            directory = settings["directory"].Value;

            if (directory != "")
            {
                if (AppVersionGrabber.GetFileVersion($"{directory}\\GTAIV.exe").StartsWith("1, 0"))
                {
                    Logger.Debug(" Folder contains a retail exe already.");
                    MessageBox.Show("Your game exe is already downgraded, proceeding to use the tool may produce unexpected results and corrupt the game.");
                }
                else { Logger.Debug(" Folder contains an exe of Steam Version."); }

                if (Directory.Exists($"{directory}\\backup")) { backupexists = true; }

                if (directory.Contains("steamapps")) { achievementscheckbox.IsEnabled = true; }
                else if (directory.Contains("Program Files"))
                {
                    if (IsRunningAsAdministrator() == false)
                    {
                        MessageBox.Show("Your game is located in Program Files, which requires elevated permissions to be modified.\n\nPressing 'Ok' will restart the app with elevated permissions.");
                        try
                        {
                            var proc = new ProcessStartInfo();
                            proc.UseShellExecute = true;
                            proc.WorkingDirectory = Environment.CurrentDirectory;
                            proc.FileName = Path.Combine(proc.WorkingDirectory, "GTAIVDowngradeUtilityWPF.exe");
                            proc.Verb = "runas";
                            Process.Start(proc);
                        }
                        catch (Exception error)
                        {
                            Logger.Error(error, "Failed to elevate.");
                            throw;
                        }

                        Application.Current.Shutdown();
                    }
                }

                directorytxt.Text = "Game Directory:";
                directorytxt.FontWeight = FontWeights.Normal;
                directorytxt.TextDecorations = null;
                tipsnote.TextDecorations = TextDecorations.Underline;
                gamedirectory.Text = directory;
                options.IsEnabled = true;
                version.IsEnabled = true;
                buttons.IsEnabled = true;
            }
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

        public bool IsGFWLInstalled()
        {
            const string keyPath = @"SOFTWARE\Classes\Installer\Products\";
            const string gfwlKeyName = "Microsoft Games for Windows - LIVE Redistributable";

            using (RegistryKey dependencies = Registry.LocalMachine.OpenSubKey(keyPath))
            {
                if (dependencies != null)
                {
                    foreach (string subkeyName in dependencies.GetSubKeyNames())
                    {
                        using (RegistryKey subkey = dependencies.OpenSubKey(subkeyName))
                        {
                            string displayName = subkey.GetValue("ProductName") as String;
                            if (displayName != null && displayName.Contains(gfwlKeyName))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public static bool IsRunningAsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private void mpsp_Click(object sender, RoutedEventArgs e)
        {
            if (sp == false)
            {
                mpspbtn.Content = "Singleplayer";
                sp = true;
                gfwlcheckbox.Visibility = Visibility.Visible;
                gfwlmpcheckbox.Visibility = Visibility.Collapsed;
                gtaccheckbox.Visibility = Visibility.Collapsed;
                gtacgfwlcheckbox.Visibility = Visibility.Collapsed;
                ffixmincheckbox.Visibility = Visibility.Collapsed;
                if (advancedcheck.IsChecked == true)
                {
                    xlivelesscheckbox.Visibility = Visibility.Visible;
                    version.Visibility = Visibility.Visible;
                    zpatchcheckbox.Visibility = Visibility.Visible;
                }
                gfwlmpcheckbox.IsChecked = false;
                gtaccheckbox.IsChecked = false;
                gtacgfwlcheckbox.IsChecked = false;
                ffixmincheckbox.IsChecked = false;
            }
            else
            {
                mpspbtn.Content = "Multiplayer";
                sp = false;
                gfwlcheckbox.Visibility = Visibility.Collapsed;
                gfwlmpcheckbox.Visibility = Visibility.Visible;
                gtaccheckbox.Visibility = Visibility.Visible;
                gtacgfwlcheckbox.Visibility = Visibility.Visible;
                xlivelesscheckbox.Visibility = Visibility.Collapsed;
                version.Visibility = Visibility.Collapsed;
                zpatchcheckbox.Visibility = Visibility.Collapsed;
                ffixmincheckbox.Visibility = Visibility.Visible;
                gfwlmpcheckbox.IsChecked = true;
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
                xlivelesscheckbox.IsEnabled = true;
                if (ffixcheckbox.IsChecked == true && gfwlcheckbox.IsChecked == false && xlivelesscheckbox.IsChecked == false)
                {
                    ffixcheckbox.IsEnabled = false;
                    ffixcheckbox.IsChecked = false;
                }
            }
            else
            {
                xlivelesscheckbox.IsChecked = false;
                xlivelesscheckbox.IsEnabled = false;
                ffixcheckbox.IsEnabled = true;
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
                MessageBox.Show("It's required to at least install XLiveless Addon when using FusionFix in compatibility mode.");
            }
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");
                MessageBox.Show("This option installs FusionFix (and GFWL patch if GFWL is enabled).\n\nGenerally not necessary, but it provides a lot of improvements to the shaders and adds in a way to load mods without replacing original files. Also helpful for downgrading radio.");
            }
        }

        private void ffixmin_Click(object sender, RoutedEventArgs e)
        {
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");
                MessageBox.Show("This option installs the FusionFix-GFWLMin patch, which overall increases the stability in Multiplayer by disabling anything that would only apply to Singleplayer but never Multiplayer.");
            }
        }

        private void gfwl_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled GFWL.");
            if (gfwlcheckbox.IsChecked == false)
            {
                xlivelesscheckbox.IsEnabled = true;
                if (achievementscheckbox.IsEnabled == true)
                {
                    achievementscheckbox.IsChecked = true;
                }
                if (ffixcheckbox.IsChecked == true && xlivelesscheckbox.IsChecked == false && zpatchcheckbox.IsChecked == false)
                {
                    ffixcheckbox.IsEnabled = false;
                    ffixcheckbox.IsChecked = false;
                }
                else
                {
                    xlivelesscheckbox.IsEnabled = false;
                    achievementscheckbox.IsChecked = false;
                    ffixcheckbox.IsEnabled = true;
                }
                if (tipscheck.IsChecked == true)
                {
                    Logger.Debug(" Displaying a tip...");
                    MessageBox.Show("This option will attempt to ensure GFWL compatibility by installing the GFWL redists and ensuring there is no xlive.dll in the folder.\n\nThis tool does not guarantee 100% compatibility, however. Also it's highly recommended to install ZolikaPatch if enabling this.");
                }
            }
        }

            private void gfwlmp_Click(object sender, RoutedEventArgs e)
            {
                Logger.Debug(" User toggled GFWL for Multiplayer.");
                if (gfwlcheckbox.IsChecked == false)
                {
                    if (achievementscheckbox.IsEnabled == true)
                    {
                        achievementscheckbox.IsChecked = true;
                    }
                }
                else
                {
                    achievementscheckbox.IsChecked = false;
                }
                if (tipscheck.IsChecked == true)
                {
                    Logger.Debug(" Displaying a tip...");
                    MessageBox.Show("This option will attempt to ensure GFWL compatibility by installing the GFWL redists and ensuring there is no xlive.dll in the folder.\n\nThis tool does not guarantee 100% compatibility, however.");
                }
            }

            private void gtac_Click(object sender, RoutedEventArgs e)
            {
                Logger.Debug(" User toggled GTAC.");
                if (tipscheck.IsChecked == true)
                {
                    Logger.Debug(" Displaying a tip...");
                    MessageBox.Show("This option will attempt to ensure GTAC compatibility, but not GFWL.");
                }
            }

            private void gtacgfwl_Click(object sender, RoutedEventArgs e)
            {
                Logger.Debug(" User toggled Both.");
                if (gtacgfwlcheckbox.IsChecked == false)
                {
                    if (achievementscheckbox.IsEnabled == true)
                    {
                        achievementscheckbox.IsChecked = true;
                    }
                }
                else
                {
                    achievementscheckbox.IsChecked = false;
                }
                if (tipscheck.IsChecked == true)
                {
                    Logger.Debug(" Displaying a tip...");
                    MessageBox.Show("This option will attempt to ensure simultaneous GFWL and GTAC compatibility.");
                }
            }

            private void advanced_Click(object sender, RoutedEventArgs e)
            {
                Logger.Debug(" User toggled advanced mode.");
                if (advancedcheck.IsChecked == true)
                {
                    if (sp == true)
                    {
                        fullcheckbox.Visibility = Visibility.Visible;
                        version.Visibility = Visibility.Visible;
                        xlivelesscheckbox.Visibility = Visibility.Visible;
                        zpatchcheckbox.Visibility = Visibility.Visible;
                    }
                    radiocheckbox.Visibility = Visibility.Visible;
                    achievementscheckbox.Visibility = Visibility.Visible;
                    tipsnote.Visibility = Visibility.Visible;
                }
                else
                {
                    version.Visibility = Visibility.Collapsed;
                    fullcheckbox.Visibility = Visibility.Collapsed;
                    radiocheckbox.Visibility = Visibility.Collapsed;
                    achievementscheckbox.Visibility = Visibility.Collapsed;
                    xlivelesscheckbox.Visibility = Visibility.Collapsed;
                    zpatchcheckbox.Visibility = Visibility.Collapsed;
                    tipsnote.Visibility = Visibility.Collapsed;
                }
                if (tipscheck.IsChecked == true && advancedcheck.IsChecked == true)
                {
                    Logger.Debug(" Displaying a tip...");
                    MessageBox.Show("Advanced Mode enables all toggles. They're hidden by default as the defaults for these options are fine for majority.\n\nDon't touch these toggles if you have no idea what you're doing and read the tips.");
                }
            }
            private void steamchieves_Click(object sender, RoutedEventArgs e)
            {
                Logger.Debug(" User toggled Steam Achievements.");
                if (tipscheck.IsChecked == true)
                {
                    Logger.Debug(" Displaying a tip...");
                    MessageBox.Show("This option installs Zolika's Steam Achievements mod to be able to get Steam achievements on downgraded copies.\n\nWill not work with GFWL. Option is disabled if you don't have your game installed in `steamapps`.");
                }
            }

            private void xliveless_Click(object sender, RoutedEventArgs e)
            {
                Logger.Debug(" User toggled XLiveless Addon.");
                if (xlivelesscheckbox.IsChecked == true)
                {
                    gfwlcheckbox.IsChecked = false;
                    gfwlcheckbox.IsEnabled = false;
                    ffixcheckbox.IsEnabled = true;
                }
                else
                {
                    gfwlcheckbox.IsEnabled = true;
                    if (ffixcheckbox.IsChecked == true && gfwlcheckbox.IsChecked == false && zpatchcheckbox.IsChecked == false)
                    {
                        ffixcheckbox.IsEnabled = false;
                        ffixcheckbox.IsChecked = false;
                    }
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

                            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                            var settings = configFile.AppSettings.Settings;

                            settings["directory"].Value = dialog.FileName;
                            configFile.Save(ConfigurationSaveMode.Modified);
                            ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);

                            if (Directory.Exists($"{dialog.FileName}\\backup")) { backupexists = true; }

                            if (dialog.FileName.Contains("steamapps")) { achievementscheckbox.IsEnabled = true; }
                            else if (dialog.FileName.Contains("Program Files"))
                            {
                                if (IsRunningAsAdministrator() == false)
                                {
                                    MessageBox.Show("Your game is located in Program Files, which requires elevated permissions to be modified.\n\nPressing 'Ok' will restart the app with elevated permissions.");
                                    try
                                    {
                                        var proc = new ProcessStartInfo();
                                        proc.UseShellExecute = true;
                                        proc.WorkingDirectory = Environment.CurrentDirectory;
                                        proc.FileName = Path.Combine(proc.WorkingDirectory, "GTAIVDowngradeUtilityWPF.exe");
                                        proc.Verb = "runas";
                                        Process.Start(proc);
                                    }
                                    catch (Exception error)
                                    {
                                        Logger.Error(error, "Failed to elevate.");
                                        throw;
                                    }

                                    Application.Current.Shutdown();
                                }
                            }

                            directorytxt.Text = "Game Directory:";
                            directorytxt.FontWeight = FontWeights.Normal;
                            directorytxt.TextDecorations = null;
                            tipsnote.TextDecorations = TextDecorations.Underline;
                            directory = dialog.FileName;
                            gamedirectory.Text = directory;
                            options.IsEnabled = true;
                            version.IsEnabled = true;
                            buttons.IsEnabled = true;
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

            string downloadingWhat;
            bool downloadfinished = false;
            private async Task Download(string downloadUrl, string destination, string downloadedName, string downloadingWhatFun)
            {
                downloadingWhat = downloadingWhatFun;
                try
                {
                    Thread thread = new Thread(() => {
                        Logger.Debug(" Downloading the selected release...");
                        WebClient client = new WebClient();
                        client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                        client.DownloadFileCompleted += new AsyncCompletedEventHandler(client_DownloadFileCompleted);
                        client.DownloadFileAsync(new Uri(downloadUrl), Path.Combine(destination, downloadedName));
                    });
                    thread.Start();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error downloading");
                    throw;
                }
            }

            void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
            {
                Dispatcher.BeginInvoke((Action)delegate
                {
                    double bytesIn = double.Parse(e.BytesReceived.ToString());
                    double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
                    double percentage = bytesIn / totalBytes * 100;
                    int percentageInt = Convert.ToInt16(percentage);
                    downgradebtn.Content = $"Downloading {downloadingWhat}... ({percentageInt}%)";
                });
            }

            void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
            {
                Dispatcher.BeginInvoke((Action)delegate
                {
                    Logger.Debug(" Successfully downloaded.");
                    downloadfinished = true;
                    downgradebtn.Content = "Downgrading...";
                });
            }
            private async void downgrade_Click(object sender, RoutedEventArgs e)
            {
                options.IsEnabled = false;
                version.IsEnabled = false;
                buttons.IsEnabled = false;
                downgradebtn.Content = "Downgrading...";
                Logger.Info(" Starting the downgrade...");
                if (backupexists == false)
                {
                    Logger.Debug(" Backup not found, prompting to backup.");
                    MessageBoxResult result = MessageBox.Show("Backup not found. Do you wish to backup now?\n\nAny plugin mods found will be removed, but they can be found again in the backup.", "No backup found", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        BackupGame.Backup(directory, backupexists);
                        backupexists = true;
                    }
                }

                if (radiocheckbox.IsChecked == true)
                {
                    Logger.Info(" Downgrading radio...");
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
                                    downgradebtn.Content = "Downgrade";
                                    options.IsEnabled = true;
                                    version.IsEnabled = true;
                                    buttons.IsEnabled = true;
                                    return;
                                }
                            }
                        }
                        else
                        {
                            downgradebtn.Content = "Downgrade";
                            options.IsEnabled = true;
                            version.IsEnabled = true;
                            buttons.IsEnabled = true;
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
                                    break;
                                }
                            }
                            else
                            {
                                downgradebtn.Content = "Downgrade";
                                options.IsEnabled = true;
                                version.IsEnabled = true;
                                buttons.IsEnabled = true;
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

                Logger.Info(" Checking redistributables...");
                bool isvc = RedistributablePackage.IsInstalled(RedistributablePackageVersion.VC2005x86);
                bool isgfwl = IsGFWLInstalled();

                if (!isvc || (!isgfwl && gfwlcheckbox.IsChecked == true))
                {
                    Logger.Info(" Some redistributable not found...");
                    if (!Directory.Exists("Files\\Redist"))
                    {
                        Logger.Info(" Downloading redistributables...");
                        HttpResponseMessage firstResponseredist;
                        try
                        {
                            firstResponseredist = await httpClient.GetAsync("https://api.github.com/repos/gillian-guide/GTAIVFullDowngradeAssets/releases/latest");
                            firstResponseredist.EnsureSuccessStatusCode();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, "Error getting latest release, probably ratelimited");
                            throw;
                        }
                        var firstResponseBodyredist = await firstResponseredist.Content.ReadAsStringAsync();
                        Directory.CreateDirectory("Files\\Redist");
                        var downloadUrlredist = JsonDocument.Parse(firstResponseBodyredist).RootElement.GetProperty("assets")[1].GetProperty("browser_download_url").GetString();
                        Download(downloadUrlredist!, "Files", "Redist.zip", "redistributables");
                        while (!downloadfinished)
                        {
                            await Task.Delay(500);
                        }
                        downloadfinished = false;
                        ZipFile.ExtractToDirectory("Files\\Redist.zip", "Files\\Redist", true);
                        File.Delete("Files\\Redist.zip");
                    }
                    if (!isvc)
                    {
                        Logger.Info("Installing Visual C++ redistributable...");
                        var vcredist = new Process
                        {
                            StartInfo =
                        {
                            FileName = $"Files\\Redist\\vcredist_x86.exe",
                            Arguments = "/Q"
                        }
                        };
                        vcredist.Start();
                        await vcredist.WaitForExitAsync();
                    }
                    if (!isgfwl && gfwlcheckbox.IsChecked == true) { Logger.Info(" Installing GFWL redistributables..."); await Process.Start($"Files\\Redist\\gfwlivesetup.exe").WaitForExitAsync(); }
                }

                // removing any scripts because they can break shit and we don't want that
                Logger.Info(" Removing existing plugins to avoid incompatibility (can be found in the backup again).");
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
                foreach (var file in from dll in new string[4] { "launc.dll", "orig_socialclub.dll", "socialclub.dll", "1911.dll" } where File.Exists($"{directory}\\{dll}") select dll)
                {
                    File.Delete($"{directory}\\{file}");
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
                    Logger.Info(" Installing Ultimate ASI Loader...");
                    string downloadedual = settings["ultimate-asi-loader"].Value;
                    if (!File.Exists("Files\\Shared\\dinput8.dll") || !File.Exists("Files\\Shared\\xlive.dll"))
                    {
                        settings["ultimate-asi-loader"].Value = "";
                        configFile.Save(ConfigurationSaveMode.Modified);
                        ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
                        Logger.Debug(" Ultimate ASI Loader not downloaded - changed the value of downloaded ual to none.");
                    }
                    HttpResponseMessage firstResponseual;
                    try
                    {
                        firstResponseual = await httpClient.GetAsync("https://api.github.com/repos/ThirteenAG/Ultimate-ASI-Loader/releases/latest");
                        firstResponseual.EnsureSuccessStatusCode();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Error getting latest release, probably ratelimited");
                        throw;
                    }
                    var firstResponseBodyual = await firstResponseual.Content.ReadAsStringAsync();
                    var latestual = JsonDocument.Parse(firstResponseBodyual).RootElement.GetProperty("tag_name").GetString();
                    if (latestual != downloadedual)
                    {
                        Logger.Debug(" Latest UAL not matching to downloaded, downloading...");
                        var downloadUrlual = JsonDocument.Parse(firstResponseBodyual).RootElement.GetProperty("assets")[0].GetProperty("browser_download_url").GetString();
                        Download(downloadUrlual!, "Files\\Shared", "Ultimate-ASI-Loader.zip", $"UAL {latestual}");
                        while (!downloadfinished)
                        {
                            await Task.Delay(500);
                        }
                        downloadfinished = false;
                        ZipFile.ExtractToDirectory("Files\\Shared\\Ultimate-ASI-Loader.zip", "Files\\Shared\\", true);
                        File.Delete("Files\\Shared\\Ultimate-ASI-Loader.zip");
                        settings["ultimate-asi-loader"].Value = latestual;
                        configFile.Save(ConfigurationSaveMode.Modified);
                        ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
                        Logger.Debug(" Edited the value in the config.");

                    }
                    Logger.Debug(" Renaming unmatching UAL names if exist...");
                    if (gfwlcheckbox.IsChecked == false || (sp == false && gfwlmpcheckbox.IsChecked == false || gtacgfwlcheckbox.IsChecked == false))
                    {
                        if (File.Exists("Files\\Shared\\dinput8.dll"))
                        {
                            File.Move("Files\\Shared\\dinput8.dll", "Files\\Shared\\xlive.dll", true);
                        }
                        if (File.Exists($"{directory}\\dinput8.dll"))
                        {
                            File.Move($"{directory}\\dinput8.dll", $"{directory}\\xlive.dll", true);
                        }
                        if (xlivelesscheckbox.IsChecked == true && sp)
                        {
                            CopyFolder("Files\\XLivelessAddon", directory);
                        }
                    }
                    else if (gfwlcheckbox.IsChecked == true || (sp == false && gfwlmpcheckbox.IsChecked == true || gtacgfwlcheckbox.IsChecked == true))
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
                if (!File.Exists("Files\\Shared\\PlayGTAIV.exe"))
                {
                    Logger.Info(" Downloading shared files...");
                    HttpResponseMessage firstResponseshared;
                    try
                    {
                        firstResponseshared = await httpClient.GetAsync("https://api.github.com/repos/gillian-guide/GTAIVFullDowngradeAssets/releases/135442404");
                        firstResponseshared.EnsureSuccessStatusCode();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Error getting latest release, probably ratelimited");
                        throw;
                    }
                    var firstResponseBodyshared = await firstResponseshared.Content.ReadAsStringAsync();
                    var downloadUrlshared = JsonDocument.Parse(firstResponseBodyshared).RootElement.GetProperty("assets")[0].GetProperty("browser_download_url").GetString();
                    Download(downloadUrlshared!, "Files", "BaseAssets.zip", "Base assets");
                    while (!downloadfinished)
                    {
                        await Task.Delay(500);
                    }
                    downloadfinished = false;
                    ZipFile.ExtractToDirectory("Files\\BaseAssets.zip", "Files", true);
                    File.Delete("Files\\BaseAssets.zip");
                }

                Logger.Info(" Copying shared files...");
                CopyFolder("Files\\Shared", directory);

                // GFWL files
                if (gfwlcheckbox.IsChecked == true)
                {
                    Logger.Info(" Copying GFWL files...");
                    CopyFolder("Files\\GFWL\\Shared", directory);
                    if (patch8click.IsChecked == true)
                    {
                        CopyFolder("Files\\GFWL\\1080", directory);
                    }
                    else
                    {
                        CopyFolder("Files\\GFWL\\1070", directory);
                    }
                }

                // full files
                if (fullcheckbox.IsChecked == true)
                {
                    Logger.Info(" Downloading and copying full files...");
                    if (patch8click.IsChecked == true)
                    {
                        if (!Directory.Exists("Files\\1080FullFiles"))
                        {
                            Directory.CreateDirectory("Files\\1080FullFiles");
                            HttpResponseMessage firstResponse1080;
                            try
                            {
                                firstResponse1080 = await httpClient.GetAsync("https://api.github.com/repos/gillian-guide/GTAIVFullDowngradeAssets/releases/135442415");
                                firstResponse1080.EnsureSuccessStatusCode();
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex, "Error getting latest release, probably ratelimited");
                                throw;
                            }
                            var firstResponseBody1080 = await firstResponse1080.Content.ReadAsStringAsync();
                            var downloadUrl1080 = JsonDocument.Parse(firstResponseBody1080).RootElement.GetProperty("assets")[0].GetProperty("browser_download_url").GetString();
                            Download(downloadUrl1080!, "Files\\1080FullFiles", "1080FullFiles.zip", "Full files");
                            while (!downloadfinished)
                            {
                                await Task.Delay(500);
                            }
                            downloadfinished = false;
                            ZipFile.ExtractToDirectory("Files\\1080FullFiles\\1080FullFiles.zip", "Files\\1080FullFiles", true);
                            File.Delete("Files\\1080FullFiles\\1080FullFiles.zip");
                        }
                        CopyFolder("Files\\1080FullFiles", directory);
                    }
                    else
                    {
                        if (!Directory.Exists("Files\\1070FullFiles"))
                        {
                            Directory.CreateDirectory("Files\\1070FullFiles");
                            HttpResponseMessage firstResponse1070;
                            try
                            {
                                firstResponse1070 = await httpClient.GetAsync("https://api.github.com/repos/gillian-guide/GTAIVFullDowngradeAssets/releases/135442418");
                                firstResponse1070.EnsureSuccessStatusCode();
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex, "Error getting latest release, probably ratelimited");
                                throw;
                            }
                            var firstResponseBody1070 = await firstResponse1070.Content.ReadAsStringAsync();
                            var downloadUrl1070 = JsonDocument.Parse(firstResponseBody1070).RootElement.GetProperty("assets")[0].GetProperty("browser_download_url").GetString();
                            Download(downloadUrl1070!, "Files\\1070FullFiles", "1070FullFiles.zip", "Full files");
                            while (!downloadfinished)
                            {
                                await Task.Delay(500);
                            }
                            downloadfinished = false;
                            ZipFile.ExtractToDirectory("Files\\1070FullFiles\\1070FullFiles.zip", "Files\\1070FullFiles", true);
                            File.Delete("Files\\1070FullFiles\\1070FullFiles.zip");
                        }
                        CopyFolder("Files\\1070FullFiles", directory);
                    }
                }

                // steam achievements
                if (achievementscheckbox.IsChecked == true)
                {
                    Logger.Info(" Copying Steam Achievements mod...");
                    File.Copy("Files\\ZolikaPatch\\SteamAchievements.asi", $"{directory}\\SteamAchievements.asi", true);
                }

                // zolikapatch setup
                if (zpatchcheckbox.IsChecked == true || sp == false)
                {
                    Logger.Info(" Installing ZolikaPatch and it's matching ini...");
                    File.Copy("Files\\ZolikaPatch\\ZolikaPatch.asi", $"{directory}\\ZolikaPatch.asi", true);

                    bool gfwl = (gfwlcheckbox.IsChecked == true || gfwlmpcheckbox.IsChecked == true);
                switch (ffixcheckbox.IsChecked, gfwl)
                {
                    case (true, true):
                        {
                            if (sp)
                            {
                                File.Copy("Files\\ZolikaPatch\\ZolikaPatch-FFix-GFWL.ini", $"{directory}\\ZolikaPatch.ini", true);
                            }
                            else if (gtaccheckbox.IsChecked == true || gtacgfwlcheckbox.IsChecked == true)
                            {
                                File.Copy("Files\\ZolikaPatch\\ZolikaPatch-NoFFix-GFWL.ini", $"{directory}\\ZolikaPatch.ini", true);
                            }
                            break;
                        }
                    case (false, true):
                        {
                            File.Copy("Files\\ZolikaPatch\\ZolikaPatch-NoFFix-GFWL.ini", $"{directory}\\ZolikaPatch.ini", true);
                            break;
                        }
                    case (true, false):
                        {
                            if (sp)
                            {
                                File.Copy("Files\\ZolikaPatch\\ZolikaPatch-FFix.ini", $"{directory}\\ZolikaPatch.ini", true);
                            }
                            else if (gtaccheckbox.IsChecked == true || gtacgfwlcheckbox.IsChecked == true)
                            {
                                File.Copy("Files\\ZolikaPatch\\ZolikaPatch-NoFFix-GFWL.ini", $"{directory}\\ZolikaPatch.ini", true);
                            }
                            break;
                        }
                    case (false, false):
                        {
                            if (sp)
                            {
                                File.Copy("Files\\ZolikaPatch\\ZolikaPatch-NoFFix.ini", $"{directory}\\ZolikaPatch.ini", true);
                            }
                            else if (gtaccheckbox.IsChecked == true || gtacgfwlcheckbox.IsChecked == true)
                            {
                                File.Copy("Files\\ZolikaPatch\\ZolikaPatch-NoFFix-GFWL.ini", $"{directory}\\ZolikaPatch.ini", true);
                            }
                            break;
                        }
                }
                }
                Logger.Info(" Moving over GTAIV.exe...");
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
                if (gtaccheckbox.IsChecked == false || gtacgfwlcheckbox.IsChecked == false)
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
                    HttpResponseMessage firstResponseff;
                    try
                    {
                        firstResponseff = await httpClient.GetAsync("https://api.github.com/repos/ThirteenAG/GTAIV.EFLC.FusionFix/releases/latest");
                        firstResponseff.EnsureSuccessStatusCode();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Error getting latest release, probably ratelimited");
                        throw;
                    }
                    string firstResponseBodyff = await firstResponseff.Content.ReadAsStringAsync();
                    var latestff = JsonDocument.Parse(firstResponseBodyff).RootElement.GetProperty("tag_name").GetString();
                    if (latestff != downloadedff)
                    {
                        if (!Directory.Exists("Files\\FusionFix"))
                        {
                            Directory.CreateDirectory("Files\\FusionFix");
                        }
                        Logger.Debug(" Downloaded version of FusionFix doesn't match the latest version, downloading...");
                        var downloadUrlff = JsonDocument.Parse(firstResponseBodyff).RootElement.GetProperty("assets")[0].GetProperty("browser_download_url").GetString();
                        Download(downloadUrlff!, "Files\\FusionFix", "FusionFix.zip", $"FusionFix {latestff}");
                        while (!downloadfinished)
                        {
                            await Task.Delay(500);
                        }
                        downloadfinished = false;
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
                    if (gfwlcheckbox.IsChecked == true || (sp == false && gfwlmpcheckbox.IsChecked == true))
                    {
                        if (sp == false && ffixmincheckbox.IsChecked == true)
                        {
                            Logger.Info(" Downloading the GFWL-Min Patch for FusionFix...");
                            HttpResponseMessage firstResponse2;
                            try
                            {
                                firstResponse2 = await httpClient.GetAsync("https://api.github.com/repos/SandeMC/GTAIV.EFLC.FusionFix-GFWLMin/releases/latest");
                                firstResponse2.EnsureSuccessStatusCode();
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex, "Error getting latest release, probably ratelimited");
                                throw;
                            }
                            var firstResponseBody2 = await firstResponse2.Content.ReadAsStringAsync();
                            var downloadUrl2 = JsonDocument.Parse(firstResponseBody2).RootElement.GetProperty("assets")[0].GetProperty("browser_download_url").GetString();
                            Download(downloadUrl2!, "Files\\FusionFix", "FusionFix-GFWLMin.zip", "FF-GFWLMin");
                            while (!downloadfinished)
                            {
                                await Task.Delay(500);
                            }
                            downloadfinished = false;
                            ZipFile.ExtractToDirectory("Files\\FusionFix\\FusionFix-GFWLMin.zip", "Files\\FusionFix", true);
                            File.Delete("Files\\FusionFix\\FusionFix-GFWLMin.zip");
                        }
                        else
                        {
                            Logger.Info(" Downloading the GFWL Patch for FusionFix...");
                            HttpResponseMessage firstResponse2;
                            try
                            {
                                firstResponse2 = await httpClient.GetAsync("https://api.github.com/repos/gillian-guide/GTAIV.EFLC.FusionFix-GFWL/releases/latest");
                                firstResponse2.EnsureSuccessStatusCode();
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex, "Error getting latest release, probably ratelimited");
                                throw;
                            }
                            var firstResponseBody2 = await firstResponse2.Content.ReadAsStringAsync();
                            var downloadUrl2 = JsonDocument.Parse(firstResponseBody2).RootElement.GetProperty("assets")[0].GetProperty("browser_download_url").GetString();
                            Download(downloadUrl2!, "Files\\FusionFix", "FusionFix-GFWL.zip", "FF-GFWL");
                            while (!downloadfinished)
                            {
                                await Task.Delay(500);
                            }
                            downloadfinished = false;
                            ZipFile.ExtractToDirectory("Files\\FusionFix\\FusionFix-GFWL.zip", "Files\\FusionFix", true);
                            File.Delete("Files\\FusionFix\\FusionFix-GFWL.zip");
                        }
                    }
                    CopyFolder("Files\\FusionFix\\", $"{directory}");
                }
                else
                {
                    CopyFolder("Files\\ShaderFixes\\", $"{directory}");
                }
            }
                Logger.Info(" Successfully downgraded!");
                MessageBox.Show("Your game has been downgraded in accordance with selected options!");
                downgradebtn.Content = "Downgrade";
                options.IsEnabled = true;
                version.IsEnabled = true;
                buttons.IsEnabled = true;
            }
        }
    }
