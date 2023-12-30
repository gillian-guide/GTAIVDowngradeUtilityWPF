using GTAIVDowngradeUtilityWPF.Common;
using GTAIVDowngradeUtilityWPF.GTAIVDowngradeUtility;
using Microsoft.WindowsAPICodePack.Dialogs;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace GTAIVDowngradeUtilityWPF.GTAIVDowngradeUtility
{
    /// <summary>
    /// Interaction logic for DepotDownloaderWindow.xaml
    /// </summary>
    public partial class DepotDownloaderWindow : Window
    {
        string directory;
        bool ffix;
        bool isretail;
        bool issteam;
        string iniModify;
        string ziniModify;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public DepotDownloaderWindow()
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
        private string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? String.Empty;
        }

        private void fastdowngrade_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User switched back to normal downgrading.");
            Close();
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
            if (radiocheckbox.IsChecked == true && ffixcheckbox.IsChecked == false)
            {
                MessageBox.Show("FusionFix will be enabled, as you can't downgrade radio without long downgrading and not use FusionFix.");
                ffixcheckbox.IsChecked = true;
            }
            if (radiocheckbox.IsChecked == false && zpatchcheckbox.IsChecked == false)
            {
                MessageBox.Show("ZolikaPatch will be enabled, as you can't perform a fast downgrade without radio downgrading and not use ZolikaPatch.");
                zpatchcheckbox.IsChecked = true;
            }
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");
                string CurrentRadioDowngradeMethod = "Radio cannot be downgraded.";
                switch (ffixcheckbox.IsChecked, patch7click.IsChecked)
                {
                    case (true, true):
                    case (true, false):
                        CurrentRadioDowngradeMethod = "Radio Downgrader for FusionFix will be installed.";
                        break;

                }

                MessageBox.Show($"This option downgrades your radio, and how that works out depends on your current options.\n\nCurrently, your radio downgrade method is:\n\n{CurrentRadioDowngradeMethod}");
            }
        }
        private void securom_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled the SecuROM bypass");
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");
                MessageBox.Show("This option installs a Razor1991 crack to bypass the SecuROM requirement.\n\nOnly disable this if you want a pure depot version - however, you'll need a legit activation key to even make it work.");
            }
        }
        private void zpatch_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled ZolikaPatch");
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");
                MessageBox.Show("This option installs ZolikaPatch.\n\nOnly disable this if you want a pure depot version - however, your game will likely have boot issues. You'll also be missing on a lot of things, like the DLC selector.");
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
        private void validationmethod_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled the validation method.");
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");
                MessageBox.Show("This option may save some space by using a validation method replacing the original files instead of downloading the downgraded files entirely separately.");
            }
        }

        private void downgrade_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" Starting the downgrade...");
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
                            isretail = true;
                            Logger.Debug(" Folder contains a retail exe already.");
                            MessageBox.Show("Your game exe is already downgraded, proceeding to use the tool may produce unexpected results and corrupt the game.");
                        }
                        else { isretail = false; Logger.Debug(" Folder contains an exe of Steam Version."); }

                        directorytxt.Text = "Game Directory:";
                        gamedirectory.Text = dialog.FileName;
                        downgradeOptionsPanel.IsEnabled = true;
                        break;
                    }

                    else
                    {
                        Logger.Debug(" User selected the wrong folder. Displaying a MessageBox.");
                        MessageBox.Show("The selected folder does not contain a supported version of GTA IV.");
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
            string backupfolder = $"{directory}\\backup";
            string gtaivexe = $"{directory}\\GTAIV.exe";
            if (Directory.Exists(backupfolder))
            {
                System.IO.DirectoryInfo directoryclear = new System.IO.DirectoryInfo(backupfolder);
                Empty(directoryclear);
            }
            else
            {
                Directory.CreateDirectory(backupfolder);
            }
            File.Copy(gtaivexe, backupfolder);
            backupbtn.IsEnabled = false;
        }
    }
}
