﻿using Aurora.Profiles.Discord.GSI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

using System.IO;

namespace Aurora.Profiles.Discord
{
    /// <summary>
    /// Interaction logic for Control_Minecraft.xaml
    /// </summary>
    public partial class Control_Discord : UserControl {

        private Application profile;

        public Control_Discord(Application profile) {
            this.profile = profile;

            InitializeComponent();
            SetSettings();
            

            profile.ProfileChanged += (sender, e) => SetSettings();
        }

        private void SetSettings() {
            GameEnabled.IsChecked = profile.Settings.IsEnabled;
        }

        private void GameEnabled_Checked(object sender, RoutedEventArgs e) {
            if (IsLoaded) {
                profile.Settings.IsEnabled = GameEnabled.IsChecked ?? false;
                profile.SaveProfiles();
            }
        }

        private void PatchButton_Click(object sender, RoutedEventArgs e)
        {
            InstallPlugin();
        }

        private void UnpatchButton_Click(object sender, RoutedEventArgs e)
        {
            UninstallPlugin();
        }

        private void InstallPlugin()
        {
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string bd = Path.Combine(appdata, "BetterDiscord", "plugins");

            if (!Directory.Exists(bd))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(bd));
            }

            using (FileStream pluginStream = File.Create(Path.Combine(bd, "AuroraGSI.plugin.js")))
            {
                pluginStream.Write(Properties.Resources.DiscordGSIPlugin, 0, Properties.Resources.DiscordGSIPlugin.Length);
            }
        }

        private void UninstallPlugin()
        {
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string bd = Path.Combine(appdata, "BetterDiscord", "plugins");

            if (!Directory.Exists(bd))
            {
                MessageBox.Show("Can't uninstall plugin, directory not found.");
                return;
            }
                
            if (File.Exists(Path.Combine(bd, "AuroraGSI.plugin.js")))
            {
                File.Delete(Path.Combine(bd, "AuroraGSI.plugin.js"));
                MessageBox.Show("Plugin uninstalled successfully");
                return;
            }
            else
            {
                MessageBox.Show("Plugin not found.");
                return;
            }
        }


    }
}