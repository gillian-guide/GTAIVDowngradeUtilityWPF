using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GTAIVDowngradeUtilityWPF.Functions
{
    public static class BackupGame
    {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

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
        public static void Backup(string directory, bool backupexists)
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
            foreach (string file in Directory.GetFiles(directory))
            {
                string fileName = Path.GetFileName(file);
                string destinationFilePath = Path.Combine(backupfolder, fileName);
                File.Copy(file, destinationFilePath, true);
            }
            if (Directory.Exists($"{directory}\\plugins"))
            {
                FileSystem.CopyDirectory($"{directory}\\plugins", $"{backupfolder}\\plugins");
            }
            var shaderslist = new List<string> {
                "gpuptfx_simplerender.fxc",
                "gpuptfx_update.fxc",
                "gta_rmptfx_litsprite.fxc",
                "gta_trees.fxc",
                "rage_atmoscatt_clouds.fxc",
                "rage_billboard_nobump.fxc",
                "rage_bink.fxc",
                "rage_perlinnoise.fxc",
                "rmptfx_collision.fxc",
                "rmptfx_default.fxc",
                "rmptfx_litsprite.fxc"
            };
            var shadersfolderslist = new List<string> {
                "win32_30",
                "win32_30_atidx10",
                "win32_30_low_ati",
                "win32_30_nv6",
                "win32_30_nv7",
                "win32_30_nv8"
            };
            var radiosfxlist = new List<string> {
                "LOADING.rpf",
                "radio_afro_beat.rpf",
                "radio_beat_95.rpf",
                "radio_classical_ambient.rpf",
                "radio_dance_rock.rpf",
                "radio_extratracks.rpf",
                "radio_hardcore.rpf",
                "radio_k109_the_studio.rpf",
                "radio_liberty_rock.rpf",
                "radio_ny_classics.rpf",
                "radio_san_juan_sounds.rpf",
                "radio_the_vibe.rpf",
                "radio_vladivostok.rpf"
            };
            var radioconfiglist = new List<string> {
                "CATEGORIES.DAT15",
                "CURVES.DAT12",
                "GAME.DAT16",
                "rpf.xml",
                "SOUNDS.DAT15",
                "speech.dat"
            };
            foreach (string foldername in shadersfolderslist)
            {
                if (!Directory.Exists($"{backupfolder}\\common\\shaders\\{foldername}"))
                {
                    Directory.CreateDirectory($"{backupfolder}\\common\\shaders\\{foldername}");
                }
                foreach (string filename in shaderslist)
                {
                    File.Copy($"{directory}\\common\\shaders\\{foldername}\\{filename}", $"{backupfolder}\\common\\shaders\\{foldername}\\{filename}", true);
                }
            }

            if (!Directory.Exists($"{backupfolder}\\common\\data\\cdimages\\"))
            {
                Directory.CreateDirectory($"{backupfolder}\\common\\data\\cdimages");
            }
            if (!Directory.Exists($"{backupfolder}\\common\\data\\text\\"))
            {
                Directory.CreateDirectory($"{backupfolder}\\common\\data\\text");
            }
            File.Copy($"{directory}\\common\\data\\cdimages\\script.img", $"{backupfolder}\\common\\data\\cdimages\\script.img", true);
            File.Copy($"{directory}\\common\\data\\cdimages\\script_network.img", $"{backupfolder}\\common\\data\\cdimages\\script_network.img", true);
            File.Copy($"{directory}\\common\\data\\frontend_menus.xml", $"{backupfolder}\\common\\data\\frontend_menus.xml", true);
            CopyFolder($"{directory}\\common\\text", $"{backupfolder}\\common\\text");

            MessageBoxResult result2 = MessageBox.Show("Do you also wish to backup the audio?\n\nThis may hang the app for a minute if using an HDD.\n\nThese files will not be replaced if you choose to downgrade radio with FusionFix, however.", "Backup radio?", MessageBoxButton.YesNo);
            if (result2 == MessageBoxResult.Yes)
            {
                if (!Directory.Exists($"{backupfolder}\\pc\\audio\\sfx"))
                {
                    Directory.CreateDirectory($"{backupfolder}\\pc\\audio\\sfx");
                }
                if (!Directory.Exists($"{backupfolder}\\pc\\audio\\config"))
                {
                    Directory.CreateDirectory($"{backupfolder}\\pc\\audio\\config");
                }
                foreach (string filename in radiosfxlist)
                {
                    File.Copy($"{directory}\\pc\\audio\\sfx\\{filename}", $"{backupfolder}\\pc\\audio\\sfx\\{filename}", true);
                }
                foreach (string filename in radioconfiglist)
                {
                    File.Copy($"{directory}\\pc\\audio\\config\\{filename}", $"{backupfolder}\\pc\\audio\\config\\{filename}", true);
                }
            }

            backupexists = true;
            MessageBox.Show("Successfully backed up!");
            Logger.Debug(" Backup complete, disabled the backup button to prevent accidental backups after downgrading.");
        }
    }
}
