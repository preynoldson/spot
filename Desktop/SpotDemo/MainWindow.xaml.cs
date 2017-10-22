using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

using Emgu.CV;
using Emgu.CV.Structure;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace SpotDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Brushes for the button fills
        SolidColorBrush eBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#750884C1"));
        SolidColorBrush lBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 0, 0, 0));

        private DirectoryInfo photoDirectory = null;
        List<string> photoSet = new List<string>();
        List<string> modelSet = new List<string>();
        private List<string> viewNames = null;

        private ImageSource selectedPhotoSource = null;
        private ImageSource selectedModelSource = null;
        private ImageSource selectedPhotoEdgeSource = null;
        private ImageSource selectedModelEdgeSource = null;
        private ImageSource selectedComparisonSource = null;

        // Color Matrix for inverting black/white
        System.Drawing.Imaging.ColorMatrix m_colorMatrix;

        public MainWindow()
        {
            // Get the folder containing the images
            GetDirectory();
            InitializeComponent();

            // Initialize the color matrix, used in inverting the colors of a bitmap
            InitColorMatrix();

            viewNames = new List<string>();
            foreach (string s in photoSet)
            {
                FileInfo file = new FileInfo(s);

                string prefix = file.Name.Split(new char[] { '-' })[0];
                viewNames.Add(prefix);
            }

            viewComboBox.ItemsSource = viewNames;
            viewComboBox.SelectedIndex = 0;

        }
    
        private void GetDirectory()
        {
           using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                //dlg.Description = "Select the photography path";
                dlg.Description = "Select the images path";

                System.Windows.Forms.DialogResult result = dlg.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    string dirName = dlg.SelectedPath;
                    if (Directory.Exists(dirName))
                        photoDirectory = new DirectoryInfo(dirName);
                }
            }

            FileInfo[] photoFiles = photoDirectory.GetFiles("*Photo*");
            FileInfo[] modelFiles = photoDirectory.GetFiles("*Model*");

            photoSet = new List<string>();
            modelSet = new List<string>();

            foreach (FileInfo pFile in photoFiles)
            {
                if (pFile.FullName.Contains("_P."))
                    continue;

                string prefix = pFile.FullName.Split(new char[] { '-' })[0];
                foreach (FileInfo mFile in modelFiles)
                {
                    if (mFile.FullName.Contains("_P."))
                        continue;

                    string mPrefix = mFile.FullName.Split(new char[] { '-' })[0];
                    if (prefix == mPrefix)
                    {
                        photoSet.Add(pFile.FullName);
                        modelSet.Add(mFile.FullName);
                        break;
                    }
                }
            }
        }

        // Create a matrix to invert the colors on the edge detected images
        private void InitColorMatrix()
        {
            // create the negative color matrix
            m_colorMatrix = new System.Drawing.Imaging.ColorMatrix(new float[][] {
                new float[] {-1, 0, 0, 0, 0},
                new float[] {0, -1, 0, 0, 0},
                new float[] {0, 0, -1, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {1, 1, 1, 0, 1}
            });
        }

        /// <summary>
        /// Inverts the colors of the provided bitamp.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private Bitmap ApplyInvert(System.Drawing.Bitmap source)
        {

            try
            {
                //create a blank bitmap the same size as original
                var bmpInvert = new System.Drawing.Bitmap(source.Width, source.Height);

                //get a graphics object from the new image
                using (var g = System.Drawing.Graphics.FromImage(bmpInvert))
                {
                    // create some image attributes
                    var attributes = new System.Drawing.Imaging.ImageAttributes();

                    attributes.SetColorMatrix(m_colorMatrix);

                    g.DrawImage(source, new System.Drawing.Rectangle(0, 0, source.Width, source.Height),
                        0, 0, source.Width, source.Height, System.Drawing.GraphicsUnit.Pixel, attributes);
                }
                return bmpInvert;
            }
            catch
            {
                return source;
            }
        }

        /// <summary>
        /// Takes a bitmap and converts it to an image that can be handled by WPF ImageBrush
        /// </summary>
        /// <param name="src">A bitmap image</param>
        /// <returns>The image as a BitmapImage for WPF</returns>
        public ImageSource BitmapToImageSource(Bitmap src)
        {
            MemoryStream ms = new System.IO.MemoryStream();
            ((Bitmap)src).Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }

        private void ProcessImages(int index)
        {
            string photo = photoSet[index];
            string model = modelSet[index];

            //Load the Image
            Image<Bgr, Byte> photoImage = new Image<Bgr, byte>(photo);
            selectedPhotoSource = BitmapToImageSource(photoImage.ToBitmap());

            // Convert to Grayscale and then process to find edges
            Image<Gray, byte> photoGray = new Image<Gray, byte>(photo);
            Image<Gray, float> photoSobel = photoGray.Sobel(0, 1, 3).Add(photoGray.Sobel(1, 0, 3)).AbsDiff(new Gray(0.0));
            photoSobel._ThresholdBinary(new Gray(75), new Gray(200));
            selectedPhotoEdgeSource = BitmapToImageSource(ApplyInvert(photoSobel.ToBitmap()));

            //Load the Model Image
            Image<Bgr, Byte> modelImage = new Image<Bgr, byte>(model);
            selectedModelSource = BitmapToImageSource(modelImage.ToBitmap());

            // Convert to Grayscale and then process to find edges
            Image<Gray, byte> modelGray = new Image<Gray, byte>(model);
            Image<Gray, float> modelSobel = modelGray.Sobel(0, 1, 3).Add(modelGray.Sobel(1, 0, 3)).AbsDiff(new Gray(0.0));
            modelSobel._ThresholdBinary(new Gray(75), new Gray(200));
            selectedModelEdgeSource = BitmapToImageSource(ApplyInvert(modelSobel.ToBitmap()));

            // TODO: Integrate Spot.Core to run the image comparison
            //selectedComparisonSource = BitmapToImageSource();
        }

        private void UIElement_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DragMove();
            }
            catch { }
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CloseButton_OnMouseEnter(object sender, MouseEventArgs e)
        {
            closeButtonRect.Fill = eBrush;
        }

        private void CloseButton_OnMouseLeave(object sender, MouseEventArgs e)
        {
            closeButtonRect.Fill = lBrush;
        }

        private void ExportButton_OnClick(object sender, RoutedEventArgs e)
        {
            // TODO: Implement image saving
        }

        private void ExportButton_OnMouseEnter(object sender, MouseEventArgs e)
        {
            exportRect.Fill = eBrush;
        }

        private void ExportButton_OnMouseLeave(object sender, MouseEventArgs e)
        {
            exportRect.Fill = lBrush;
        }

        private void photoRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (photoRadioButton.IsChecked.HasValue && photoRadioButton.IsChecked.Value && selectedPhotoSource != null)
                    imageBox.Source = selectedPhotoSource;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);

            }
        }

        private void modelRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (modelRadioButton.IsChecked.HasValue && modelRadioButton.IsChecked.Value && selectedModelSource != null)
                    imageBox.Source = selectedModelSource;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error\n" + ex.Message);
            }
        }

        private void photoEdgesRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (photoEdgesRadioButton.IsChecked.HasValue && photoEdgesRadioButton.IsChecked.Value && selectedPhotoEdgeSource != null)
                    imageBox.Source = selectedPhotoEdgeSource;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error\n" + ex.Message);
            }
        }

        private void modelEdgesRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (modelEdgesRadioButton.IsChecked.HasValue && modelEdgesRadioButton.IsChecked.Value && selectedModelEdgeSource != null)
                    imageBox.Source = selectedModelEdgeSource;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error\n" + ex.Message);
            }
        }

        private void comparisonRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (comparisonRadioButton.IsChecked.HasValue && comparisonRadioButton.IsChecked.Value && selectedComparisonSource != null)
                    imageBox.Source = selectedComparisonSource;
            }
            catch { }
        }

        private void ViewComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = viewComboBox.SelectedIndex;
            ProcessImages(index);

            if (photoRadioButton.IsChecked.HasValue && photoRadioButton.IsChecked.Value)
                imageBox.Source = selectedPhotoSource;
            else if (modelRadioButton.IsChecked.HasValue && modelRadioButton.IsChecked.Value)
                imageBox.Source = selectedModelSource;
            else if (photoEdgesRadioButton.IsChecked.HasValue && photoEdgesRadioButton.IsChecked.Value)
                imageBox.Source = selectedPhotoEdgeSource;
            else if (modelEdgesRadioButton.IsChecked.HasValue && modelEdgesRadioButton.IsChecked.Value)
                imageBox.Source = selectedModelEdgeSource;
        }
    }
}
