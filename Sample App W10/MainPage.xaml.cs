using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Sample_App_W10
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        IList<string> _barcodesFound = new ObservableCollection<string>();

        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;

            lvBarcodesFound.ItemsSource = _barcodesFound;

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            VideoScanZXing.W10Lib.BarCodeManager.StartScan(BarcodeFound, OnError, TimeSpan.FromSeconds(25));
        }

        void BarcodeFound(string barcode)
        {
            _barcodesFound.Add(barcode);
        }

        void OnError(Exception e)
        {
            // Show the error in the barcode list
            _barcodesFound.Add(String.Format("OnError: {0}", e.Message));
        }
    }
}
