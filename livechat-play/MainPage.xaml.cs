using Livechat_UWP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.Networking.Sockets;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace livechat_play
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {

        string host = "192.168.1.3";
        uint port = 9900;

        private const string webSocketUrl = "ws://192.168.1.3:9902/live/ws/pull";
        public MainPage()
        {
            this.InitializeComponent();
        }



        async private void play_Click(object sender, RoutedEventArgs e)
        {
            using (var stream = new SocketPullStream(host, port))
            {

                player.SetSource(stream, "MP4");
            }
        }
    }
}
