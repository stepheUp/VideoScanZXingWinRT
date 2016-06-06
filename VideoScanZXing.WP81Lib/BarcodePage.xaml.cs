
using Lumia.Imaging;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration;
using System.Linq;
using Windows.Media.Devices;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace VideoScanZXing.WP81Lib
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BarcodePage : Page
    {
        CameraPreviewImageSource _cameraPreviewImageSource;
        WriteableBitmap _writeableBitmap;
        WriteableBitmapRenderer _writeableBitmapRenderer;

        SemaphoreSlim _semRender = new SemaphoreSlim(1);
        SemaphoreSlim _semScan = new SemaphoreSlim(1);

        TimeSpan _timeout;
        Stopwatch _sw = new Stopwatch();

        TimeSpan focus_period = TimeSpan.FromSeconds(5);
        VideoDeviceController vdc = null;

        Task _renderTask;
        bool _capturing;
        double _width;
        double _height;
        bool _cleanedUp;

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
        }

        private void Cleanup()
        {
            if (!_cleanedUp)
            {
                // Free all - NECESSARY TO CLEANUP PROPERLY !
                _cameraPreviewImageSource.PreviewFrameAvailable -= OnPreviewFrameAvailable;
                _capturing = false;
                _cameraPreviewImageSource.Dispose();
                _writeableBitmapRenderer.Dispose();
                _cleanedUp = true;
            }
        }


        public async Task InitializeAsync()
        {
            _timeout = BarCodeManager.MaxTry;
            _sw.Restart();
            _capturing = true;
            _cleanedUp = false;
            // Create a camera preview image source (from Imaging SDK)
            _cameraPreviewImageSource = new CameraPreviewImageSource();

            // La sélection de la caméra arrière plante sur mon device :/
            //var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            //var backCamera = devices.FirstOrDefault(x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Back);
            //await _cameraPreviewImageSource.InitializeAsync(backCamera.Id);
            await _cameraPreviewImageSource.InitializeAsync(string.Empty);

            var properties = await _cameraPreviewImageSource.StartPreviewAsync();

            vdc = (VideoDeviceController)_cameraPreviewImageSource.VideoDeviceController;
            if (vdc.FocusControl.Supported)
            {
                vdc.FocusControl.Configure(new FocusSettings { Mode = FocusMode.Auto });
            }

            // Create a preview bitmap with the correct aspect ratio
            _width = 640.0;
            _height = (_width / properties.Width) * properties.Height;
            _writeableBitmap = new WriteableBitmap((int)_width, (int)_height);

            captureElement.Source = _writeableBitmap;

            _writeableBitmapRenderer = new WriteableBitmapRenderer();
            _writeableBitmapRenderer.Source = _cameraPreviewImageSource;

            // Attach preview frame delegate
            _cameraPreviewImageSource.PreviewFrameAvailable += OnPreviewFrameAvailable;
        }
  
        private async void OnBarCodeFound(string barcode)
        {
            Cleanup();
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
            CoreDispatcherPriority.Normal, () =>
            {
                BarCodeManager.OnBarCodeFound(barcode);
                this.Frame.GoBack();
            });

        }


        private async void OnError(Exception e)
        {
            Cleanup();
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
            CoreDispatcherPriority.Normal, () =>
            {
                BarCodeManager.OnError(e);
                this.Frame.GoBack();
            });
        }


        private async void OnPreviewFrameAvailable(IImageSize args)
        {
            if (_sw.Elapsed > focus_period)
            {
                try
                {
                    focus_period = focus_period + TimeSpan.FromSeconds(5);
                    await vdc.FocusControl.FocusAsync();
                }
                catch { }
            }


            if (_sw.Elapsed > _timeout)
            {
                OnError(new TimeoutException("Could not find any barcode"));
            }
            else
            {
                _renderTask = Render();
            }
        }

        private async Task Render()
        {
            // On AFFICHE 1 image après l'autre, on ignore celles qui arrivent entre temps
            if(await _semRender.WaitAsync(0) == true)
            { 
                try
                {
                    byte[] pixelsArray = null;
                        // Render camera preview frame to screen
                        _writeableBitmapRenderer.WriteableBitmap = _writeableBitmap;
                        await _writeableBitmapRenderer.RenderAsync();                                               
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                            CoreDispatcherPriority.High, () =>
                            {
                                // On clone pour ne pas être lié au thread de l'UI dans le scan de code barre
                                pixelsArray = _writeableBitmap.PixelBuffer.ToArray();
                                _writeableBitmap.Invalidate();
                            });


                        if (_capturing)
                        {
                            // On SCANNE 1 image à la fois on ignore celles qui arrivent entre temps
                            if (await _semScan.WaitAsync(0) == true)
                            {
                                // On n'attend pas la fin...
                                await Task.Run(() => ScanImage(pixelsArray));
                            }                               
                        }
                }
                catch(Exception ex)
                {
                    OnError(ex);
                }
                finally
                {
                    _semRender.Release();
                }
            }
        }

        private void ScanImage(byte[] pixelsArray)
        {
            try
            {
                var result = BarCodeManager.ScanBitmap(pixelsArray, (int)_width, (int)_height);
                if(result != null)
                {
                    OnBarCodeFound(result.Text);            
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
