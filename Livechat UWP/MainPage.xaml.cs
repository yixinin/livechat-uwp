using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace Livechat_UWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        MediaCapture captureManager = null;
        public MainPage()
        {
            this.InitializeComponent();
        }

        async private void PhotographButton_Click(object sender, RoutedEventArgs e)
        {
            if (captureManager == null)
            {
                capturePreview.Visibility = Visibility.Visible;
                ProfilePic.Visibility = Visibility.Collapsed;
                captureManager = new MediaCapture();

                //选择后置摄像头
                DeviceInformation cameraDevice = await FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel.Back);
                if (cameraDevice == null)
                {
                    System.Diagnostics.Debug.WriteLine("No camera device found!");
                    return;
                }
                var settings = new MediaCaptureInitializationSettings
                {
                    StreamingCaptureMode = StreamingCaptureMode.Video,
                    //MediaCategory = MediaCategory.Other,
                    //AudioProcessing = AudioProcessing.Default,
                    PhotoCaptureSource = PhotoCaptureSource.Auto,
                    AudioDeviceId = string.Empty,
                    VideoDeviceId = cameraDevice.Id
                };
                await captureManager.InitializeAsync(settings);
                var webSocketUrl = "ws://172.16.67.134:9902/live/ws";
                using (var s = new WebsocketPushStream(webSocketUrl))
                {

                    //var stream = s.AsRandomAccessStream();
                    var encodingProfile = MediaEncodingProfile.CreateHevc(VideoEncodingQuality.HD1080p);
                    await captureManager.StartRecordToStreamAsync(encodingProfile, s);
                }

            }
        }


        private static async Task<DeviceInformation> FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel desired)
        {

            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            DeviceInformation desiredDevice = allVideoDevices.FirstOrDefault(x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == desired);

            return desiredDevice ?? allVideoDevices.FirstOrDefault();
        }
        //拍照
        async private void CapturePhoto_Click(object sender, RoutedEventArgs e)
        {

            if (captureManager != null)
            {
                capturePreview.Visibility = Visibility.Collapsed;
                ProfilePic.Visibility = Visibility.Visible;
                ProfilePic.Source = null;
                //declare string for filename
                string captureFileName = string.Empty;

                //图片格式
                ImageEncodingProperties format = ImageEncodingProperties.CreateJpeg();

                //创建本地存储文件夹
                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                    "Photo" + DateTime.Now.ToString("yyMMddHHmmss") + ".jpg",
                    CreationCollisionOption.ReplaceExisting);

                await captureManager.CapturePhotoToStorageFileAsync(format, file);

                BitmapImage bmpImage = new BitmapImage(new Uri(file.Path));

                ProfilePic.Source = bmpImage;//释放摄像头资源
                capturePreview.Visibility = Visibility.Visible;
                ProfilePic.Visibility = Visibility.Collapsed;
                //captureManager.Dispose();
                //captureManager = null;
            }

        }
        private void FocusValueSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            try
            {

                uint focus = Convert.ToUInt32(e.NewValue);
                SetFocus(focus);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        //设置摄像头焦点方法
        private async void SetFocus(uint? focusValue = null)
        {

            if (captureManager == null)
            {
                return;
            }

            try
            {

                if (!focusValue.HasValue)
                {
                    focusValue = 50;
                }

                if (captureManager.VideoDeviceController.FocusControl.Supported)
                {

                    captureManager.VideoDeviceController.FlashControl.AssistantLightEnabled = false;

                    captureManager.VideoDeviceController.FocusControl.Configure(new FocusSettings() { Mode = FocusMode.Manual, Value = focusValue, DisableDriverFallback = true });

                    await captureManager.VideoDeviceController.FocusControl.FocusAsync();
                }
            }
            catch { }
        }

        async private void Stop_Click(object sender, RoutedEventArgs e)
        {
            await captureManager.StopRecordAsync();
        }
    }
}
