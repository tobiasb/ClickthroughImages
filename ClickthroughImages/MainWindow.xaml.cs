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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Threading;
using System.ComponentModel;
using System.Windows.Threading;

namespace ClickthroughImages
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private string destinationDirectory;
        private string sourceDirectory;
        private List<string> imageFilePaths = new List<string>();
        private List<string> imageFilePathsAlreadyExistingInDestination = new List<string>();
        private List<string> imageFilePathsAlreadySavedInThisSession = new List<string>();
        private int userRotationOfCurrentImage = 0;
        private bool isContinueLastSession;
        private BackgroundWorker backgroundWorkerImageReader = new BackgroundWorker();

        private string ImageFileSearchPattern
        {
            get
            {
                try
                { return Properties.Settings.Default.ImagesSearchPattern; }
                catch
                { return "*.*"; }
            }
        }

        private int currentListIndex;
        private int CurrentListIndex
        {
            get { return currentListIndex; }
            set
            {
                currentListIndex = value;

                userRotationOfCurrentImage = 0;
                Dispatcher.Invoke(new Action(ShowImage));
            }
        }
        
        /// <summary>
        /// Returns a rotated TransformedBitmap from an image file
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="degrees"></param>
        /// <returns></returns>
        private TransformedBitmap GetRotatedImage(string fileName, int degrees)
        {
            try
            {
                BitmapImage myBitmapImage = new BitmapImage();
                myBitmapImage.BeginInit();
                myBitmapImage.UriSource = new Uri(fileName, UriKind.RelativeOrAbsolute);
                //myBitmapImage.DecodePixelWidth = (int)imgCenterImage.ActualWidth;
                myBitmapImage.EndInit();

                TransformedBitmap tb = new TransformedBitmap();
                tb.BeginInit();
                tb.Source = myBitmapImage;
                // Set image rotation.
                RotateTransform transform = new RotateTransform(degrees);
                tb.Transform = transform;
                tb.EndInit();

                return tb;
            }
            catch
            { return null; }
        }

        private void ShowImage()
        {
            try
            {
                if (CurrentListIndex >= 0 && CurrentListIndex < imageFilePaths.Count)
                {
                    TransformedBitmap tb = GetRotatedImage(imageFilePaths[CurrentListIndex], userRotationOfCurrentImage);

                    if (tb != null)
                    {
                        double ratio = tb.Width / tb.Height;

                        //Adjust the width of the displayed image to the ratio of the original one.
                        imgCenterImage.Width = imgCenterImage.ActualHeight * ratio;
                        imgCenterImage.Source = tb;
                    }
                    else
                    { imgCenterImage.Source = null; }
                }
            }
            catch (Exception ex)
            { MessageBox.Show(ex.StackTrace, ex.Message, MessageBoxButton.OK, MessageBoxImage.Information); }
        }

        private void InitializeImagesList(object sender, DoWorkEventArgs e)
        {
            //If the last image is continued, get all the filenames in the destination directory so they can be filtered
            //out and not shown in this session
            if (isContinueLastSession)
            {
                imageFilePathsAlreadyExistingInDestination.AddRange(Directory.GetFiles(destinationDirectory, ImageFileSearchPattern));
            }

            imageFilePaths.AddRange(GetImageFilePathsInFolder(sourceDirectory));
            Dispatcher.Invoke(new Action(ShowImage));

            foreach (string folder in Directory.GetDirectories(sourceDirectory).OrderBy(f => f))
            {
                imageFilePaths.AddRange(GetImageFilePathsInFolder(folder));
                
                Dispatcher.Invoke(new Action(ShowImage));
                Dispatcher.Invoke(new Action(UpdateStatusLabel));
            }

            Dispatcher.Invoke(new Action(UpdateStatusLabel));
        }

        void UpdateStatusLabel()
        {
            labelStatus.Content = (CurrentListIndex + 1) + "/" + imageFilePaths.Count;

            if (imageFilePaths.Count > currentListIndex)
            {
                bool currentImageAlreadySaved = imageFilePathsAlreadySavedInThisSession.Contains(imageFilePaths[CurrentListIndex]);
                this.Title = imageFilePaths[CurrentListIndex] + (currentImageAlreadySaved ? " ( Saved )" : string.Empty);
            }
        }

        public List<string> GetImageFilePathsInFolder(string folder)
        {
            List<string> images = new List<string>();

            //Recursively get images from subfolders of current folder
            foreach (string subFolder in Directory.GetDirectories(folder).OrderBy(f => f))
            {
                images.AddRange(GetImageFilePathsInFolder(System.IO.Path.Combine(folder, subFolder)));
            }
            
            if (isContinueLastSession)
            {
                //Only consider the image if it hasn't been processed in the previous session
                foreach (string fileName in Directory.GetFiles(folder, ImageFileSearchPattern))
                {
                    if (!File.Exists(System.IO.Path.Combine(destinationDirectory, System.IO.Path.GetFileName(fileName))))
                    { images.Add(System.IO.Path.Combine(folder, fileName)); }
                }
            }
            else
            {
                images.AddRange(Directory.GetFiles(folder, ImageFileSearchPattern));
            }

            return images;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UserSelectionDetails detailsWindow = new UserSelectionDetails();

            if (detailsWindow.ShowDialog() == true)
            {
                this.sourceDirectory = detailsWindow.SourceDirectory;
                this.destinationDirectory = detailsWindow.DestinationDirectory;
                this.isContinueLastSession = detailsWindow.IsContinue;

                backgroundWorkerImageReader.DoWork += InitializeImagesList;
                backgroundWorkerImageReader.RunWorkerAsync();
            }
            else
            { this.Close(); }
        }

        private void SaveImage(object sender, DoWorkEventArgs e)
        {
            int currentIndex = (int)e.Argument;

            string destinationFileName =
                System.IO.Path.Combine(destinationDirectory, System.IO.Path.GetFileName(imageFilePaths[currentIndex]));

            FileStream stream = null;
            try
            {
                stream = new FileStream(destinationFileName, FileMode.Create);

                JpegBitmapEncoder encoder = new JpegBitmapEncoder();

                TransformedBitmap tb = GetRotatedImage(imageFilePaths[currentIndex], userRotationOfCurrentImage);

                encoder.Frames.Add(BitmapFrame.Create(tb));

                encoder.Save(stream);
                stream.Close();

                imageFilePathsAlreadySavedInThisSession.Add(imageFilePaths[currentIndex]);

                Dispatcher.Invoke(new Action(UpdateStatusLabel));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace, ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
                if (stream != null) { stream.Close(); }
                File.Delete(destinationFileName);
            }
        }

        #region Control Events
        private void buttonVorherigesBild_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CurrentListIndex > 0)
                { CurrentListIndex--; }

                Dispatcher.Invoke(new Action(UpdateStatusLabel));
            }
            catch (Exception ex)
            { MessageBox.Show(ex.StackTrace, ex.Message, MessageBoxButton.OK, MessageBoxImage.Information); }
        }

        private void buttonNaechstesBild_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CurrentListIndex < imageFilePaths.Count - 1)
                { CurrentListIndex++; }

                Dispatcher.Invoke(new Action(UpdateStatusLabel));
            }
            catch (Exception ex)
            { MessageBox.Show(ex.StackTrace, ex.Message, MessageBoxButton.OK, MessageBoxImage.Information); }
        }

        private void buttonSpeichern_Click(object sender, RoutedEventArgs e)
        {
            //Thread t = new Thread(
            //    new ThreadStart(
            //        delegate()
            //        {

            //        }
            //));

            //t.Start();

            BackgroundWorker backgroundWorkerImageSaver = new BackgroundWorker();
            backgroundWorkerImageSaver.DoWork += SaveImage;
            backgroundWorkerImageSaver.RunWorkerAsync(CurrentListIndex);
        }

        private void buttonRotate90_Click(object sender, RoutedEventArgs e)
        {
            userRotationOfCurrentImage = (userRotationOfCurrentImage + 90) % 360;
            Dispatcher.Invoke(new Action(ShowImage));
        } 
        #endregion

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right)
            { buttonNaechstesBild_Click(null, null); }
            else if (e.Key == Key.Left)
            { buttonVorherigesBild_Click(null, null); }
            else if (e.Key == Key.R)
            { buttonRotate90_Click(null, null); }
            else if (e.Key == Key.S)
            { buttonSpeichern_Click(null, null); }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Dispatcher.Invoke(new Action(ShowImage));
        }
    }
}
