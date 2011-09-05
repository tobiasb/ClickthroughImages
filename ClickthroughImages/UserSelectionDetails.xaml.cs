using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.IO;

namespace ClickthroughImages
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class UserSelectionDetails : Window
    {
        private FolderBrowserDialog folderBrowserDialog;

        public string SourceDirectory
        { get { return labelSourceDirectory.Content as string; } }

        public string DestinationDirectory
        { get { return labelDestinationDirectory.Content as string; } }

        public bool IsContinue
        { get { return (bool)checkBoxContinue.IsChecked; } }

        public UserSelectionDetails()
        {
            InitializeComponent();
        }

        public bool IsOk()
        {
            string sourceDirectory = labelSourceDirectory.Content as string;
            string destinationDirectory = labelDestinationDirectory.Content as string;

            return Directory.Exists(sourceDirectory) && Directory.Exists(destinationDirectory);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            folderBrowserDialog = new FolderBrowserDialog();

            if (!string.IsNullOrEmpty(Properties.Settings.Default.LastSourceDirectory))
            { labelSourceDirectory.Content = Properties.Settings.Default.LastSourceDirectory; }

            if (!string.IsNullOrEmpty(Properties.Settings.Default.LastDestinationdirectory))
            { labelDestinationDirectory.Content = Properties.Settings.Default.LastDestinationdirectory; }

            buttonWeiter.IsEnabled = IsOk();
        }

        private void buttonSourceDirectory_Click(object sender, RoutedEventArgs e)
        {
            folderBrowserDialog.ShowNewFolderButton = false;

            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                labelSourceDirectory.Content = folderBrowserDialog.SelectedPath;
            }

            buttonWeiter.IsEnabled = IsOk();
        }

        private void buttonDestinationDirectory_Click(object sender, RoutedEventArgs e)
        {
            folderBrowserDialog.ShowNewFolderButton = true;

            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                labelDestinationDirectory.Content = folderBrowserDialog.SelectedPath;
            }

            buttonWeiter.IsEnabled = IsOk();
        }

        private void buttonWeiter_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();

            Properties.Settings.Default.LastSourceDirectory = labelSourceDirectory.Content as string;
            Properties.Settings.Default.LastDestinationdirectory = labelDestinationDirectory.Content as string;
            Properties.Settings.Default.Save();
        }
    }
}
