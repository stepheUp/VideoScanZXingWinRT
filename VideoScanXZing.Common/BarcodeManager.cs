using System;

using ZXing;
using Windows.UI.Xaml.Media.Imaging;
using System.Diagnostics;

namespace VideoScanZXing.Common
{
    /// <summary>
    /// Scan a barcode for a live video stream
    /// </summary>
    public static class BarCodeManager
    {
        internal static Action<string> _onBarCodeFound;
        internal static Action<Exception> _onError;

        internal static BarcodeReader _ZXingReader;

        static BarCodeManager()
        {
            MaxTry = 15;
        }

        /// <summary>
        /// Starts the scan : navigates to the scan page and starts reading video stream
        /// Note : Scan will auto-stop if navigation occurs
        /// </summary>
        /// <param name="onBarCodeFound">Delegate Action on a barcode found</param>
        /// <param name="onError">Delegate Action on error</param>
        /// <param name="zxingReader">(optional) A specific reader format, Default will be EAN13Reader </param>
        public static void StartScan(Action<string> onBarCodeFound, Action<Exception> onError, BarcodeFormat barcodeFormat = BarcodeFormat.EAN_13)
        {

                _onBarCodeFound = onBarCodeFound;
                _onError = onError;

                _ZXingReader = GetReader(barcodeFormat);
        }

        public static Result ScanBitmap(byte[] pixelsArray, int width, int height)
        {
            var result = _ZXingReader.Decode(pixelsArray, width, height, BitmapFormat.Unknown);

            if (result != null)
            {
                Debug.WriteLine(result.Text);
                if (BarCodeManager._onBarCodeFound != null)
                {
                    //_stop = true;
                    BarCodeManager._onBarCodeFound(result.Text);
                }
            }

            return result;
        }

        //public static Result ScanBitmap(WriteableBitmap writeableBmp)
        //{
        //    var barcodeReader = new BarcodeReader()
        //    {
        //        AutoRotate = true,
        //        Options = new ZXing.Common.DecodingOptions() { TryHarder = false, PossibleFormats = new BarcodeFormat []{ BarcodeFormat.EAN_13 } }
        //    };
        //    var result = barcodeReader.Decode(writeableBmp);

        //    //if (result != null)
        //    //{
        //    //    CaptureImage.Source = writeableBmp;
        //    //}

        //    return result;
        //}

        /// <summary>
        /// Try 20 times to focus and scan for 1,5 sec (default)
        /// </summary>
        public static int MaxTry
        {
            get;
            set;
        }

        //private static Reader _ZXingReader;
        //internal static Reader ZXingReader
        //{
        //    get
        //    {
        //        if (_ZXingReader == null)
        //            return new EAN13Reader();
        //        return _ZXingReader;
        //    }

        //    set
        //    {
        //        _ZXingReader = value;
        //    }
        //}

        /// <summary>
        /// Returns the zxing reader class for the current specified ScanMode.
        /// </summary>
        /// <returns></returns>
        //internal static Reader GetReader(BarcodeFormat format)
        //{
        //    var zxingHints
        //        = new Dictionary<DecodeHintType, object>() { { DecodeHintType.TRY_HARDER, true } };

        //    Reader r;

        //    switch (format.ToString())
        //    {
        //        case "CODE_128":
        //            r = new Code128Reader();
        //            break;
        //        case "CODE_39":
        //            r = new Code39Reader();
        //            break;
        //        case "EAN_13":
        //            r = new EAN13Reader();
        //            break;
        //        case "EAN_8":
        //            r = new EAN8Reader();
        //            break;
        //        case "ITF":
        //            r = new ITFReader();
        //            break;
        //        case "UPC_A":
        //            r = new UPCAReader();
        //            break;
        //        case "UPC_E":
        //            r = new UPCEReader();
        //            break;
        //        case "QR_CODE":
        //            r = new QRCodeReader();
        //            break;
        //        case "DATAMATRIX":
        //            r = new DataMatrixReader();
        //            break;

        //        case "ALL_1D":
        //            r = new MultiFormatOneDReader(zxingHints);
        //            break;

        //        //Auto-Detect:
        //        case "UPC_EAN":
        //        default:
        //            r = new MultiFormatUPCEANReader(zxingHints);
        //            break;
        //    }
        //    return r;
        //}

        /// <summary>
        /// Returns the zxing reader class for the current specified ScanMode.
        /// </summary>
        /// <returns></returns>
        internal static BarcodeReader GetReader(BarcodeFormat format = BarcodeFormat.All_1D)
        {
            return new BarcodeReader()
            {
                AutoRotate = true,
                Options = new ZXing.Common.DecodingOptions() { TryHarder = false, PossibleFormats = new BarcodeFormat[] { format } }
            };
        }

    }
}

