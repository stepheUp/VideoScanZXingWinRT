
using Lumia.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Windows.UI.Core;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Graphics.Display;
using Windows.Media.Devices;
using Windows.Media;
using System.Runtime.InteropServices;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace VideoScanZXing.W10Lib
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BarcodePage : Page
    {
        MediaCapture m_mediaCapture;

        SemaphoreSlim _semRender = new SemaphoreSlim(1);
        SemaphoreSlim _semScan = new SemaphoreSlim(1);

        TimeSpan _timeout;
        Stopwatch _sw = new Stopwatch();

        double _width = 640;
        double _height = 480;
        bool _cleanedUp = true; //Resources are still unallocated
        bool _processScan = true;

        DisplayOrientations _autoRotation;

        ObservableCollection<string> _barcodes = new ObservableCollection<string>();

        public BarcodePage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            _autoRotation = DisplayInformation.AutoRotationPreferences;
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;

            // TODO: Prepare page for display here.
            await InitializeAsync();

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.

   
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // Free all - NECESSARY TO CLEANUP PROPERLY !
            Cleanup();

            DisplayInformation.AutoRotationPreferences = _autoRotation;
        }

        private async void Cleanup()
        {
            if (!_cleanedUp)
            {
                // Free all - NECESSARY TO CLEANUP PROPERLY !
                m_mediaCapture.FocusChanged -= M_mediaCapture_FocusChanged;

                var focusControl = m_mediaCapture.VideoDeviceController.FocusControl;
                await focusControl.UnlockAsync();

                await m_mediaCapture.StopPreviewAsync();
                m_mediaCapture.Dispose();
                m_mediaCapture = null;
                _cleanedUp = true;
            }
        }



        public async Task InitializeAsync()
        {
            _timeout = BarCodeManager.MaxTry;
            _sw.Restart();
   

            var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            var backCamera = devices.FirstOrDefault(x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Back);

            m_mediaCapture = new MediaCapture();
            await m_mediaCapture.InitializeAsync();

            
            var focusControl = m_mediaCapture.VideoDeviceController.FocusControl;
            if (!focusControl.FocusChangedSupported)
            {
                OnErrorAsync(new Exception("AutoFocus control is not supported on this device"));
            }
            else
            {
                _cleanedUp = false;
                _processScan = true;

                m_mediaCapture.FocusChanged += M_mediaCapture_FocusChanged;
                captureElement.Source = m_mediaCapture;
                await m_mediaCapture.StartPreviewAsync();
                await focusControl.UnlockAsync();
                var settings = new FocusSettings { Mode = FocusMode.Continuous, AutoFocusRange = AutoFocusRange.FullRange };
                focusControl.Configure(settings);
                await focusControl.FocusAsync();
            }
        }

        private void M_mediaCapture_FocusChanged(MediaCapture sender, MediaCaptureFocusChangedEventArgs args)
        {
            if (_processScan)
            {
                CapturePhotoFromCameraAsync();
            }
        }

        async void CapturePhotoFromCameraAsync()
        {
            // On en traite 1 à la fois et on poubellise celles ci arrivent entre temps
            if (await _semRender.WaitAsync(0) == true)
            {
                try
                {
                    VideoFrame videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, (int)_width, (int)_height);
                    await m_mediaCapture.GetPreviewFrameAsync(videoFrame);

                    var bytes = await SaveSoftwareBitmapToBufferAsync(videoFrame.SoftwareBitmap);
                    ScanImageAsync(bytes);
                }
                finally
                {
                    _semRender.Release();
                }
            }
        }

        private async Task<byte[]> SaveSoftwareBitmapToBufferAsync(SoftwareBitmap softwareBitmap)
        {
            byte[] bytes = null;

            try
            {
                IRandomAccessStream stream = new InMemoryRandomAccessStream();
                {
                    // Create an encoder with the desired format
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, stream);

                    // Set the software bitmap
                    encoder.SetSoftwareBitmap(softwareBitmap);

                    // Set additional encoding parameters, if needed
                    //encoder.BitmapTransform.ScaledWidth = (uint)_width;
                    //encoder.BitmapTransform.ScaledHeight = (uint)_height;
                    //encoder.BitmapTransform.Rotation = Windows.Graphics.Imaging.BitmapRotation.Clockwise90Degrees;
                    //encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
                    encoder.IsThumbnailGenerated = false;

                    await encoder.FlushAsync();

                    bytes = new byte[stream.Size];

                    // This returns IAsyncOperationWithProgess, so you can add additional progress handling
                    await stream.ReadAsync(bytes.AsBuffer(), (uint)stream.Size, Windows.Storage.Streams.InputStreamOptions.None);
                }
            }

            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return bytes;
        }



        private async void OnBarCodeFoundAsync(string barcode)
        {
            _processScan = false;

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
            CoreDispatcherPriority.Normal, () =>
            {
                if (BarCodeManager.OnBarCodeFound != null)
                {
                    Debug.WriteLine(barcode);
                    BarCodeManager.OnBarCodeFound(barcode);
                }

                this.Frame.GoBack();
            });

        }



        private async void OnErrorAsync(Exception e)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
            CoreDispatcherPriority.Normal, () =>
            {
                BarCodeManager.OnError(e);
                this.Frame.GoBack();
            });
        }

 

        private async void ScanImageAsync(byte[] pixelsArray)
        {
            await _semScan.WaitAsync();
            try
            {
                if (_processScan)
                {
                    var result = BarCodeManager.ScanBitmap(pixelsArray, (int)_width, (int)_height);
                    if (result != null)
                    {
                        OnBarCodeFoundAsync(result.Text);
                    }
                }                
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                // Wasn't able to find a barcode
            }
            finally
            {
                _semScan.Release();
            }
        }

    }
}
