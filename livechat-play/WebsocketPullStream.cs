using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Livechat_UWP
{
    class WebsocketPullStream : IRandomAccessStream
    {
        private readonly StreamWebSocket ws;
        ulong pos = 0;

        private readonly string url;
        private bool cloed = false;
        public WebsocketPullStream(String _url)
        {
            this.url = _url;
            ws = new StreamWebSocket();
            ws.ConnectAsync(new Uri(url)).AsTask().Wait();
        }

        public bool CanRead { get { return true; } }

        public bool CanSeek { get { return true; } }

        public bool CanWrite { get { return false; } }

        public void Flush()
        {
            throw new NotImplementedException();
        }

        public long Length
        {
            get
            {
                return (long)(Position + 512);
            }
        }

        public ulong Position
        {
            get
            {
                return pos;
            }
            set
            {

            }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }


        void IRandomAccessStream.Seek(ulong offset)
        {
            pos += offset;
        }

        public void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public IInputStream GetInputStreamAt(ulong position)
        {
            return this;
        }

        public IOutputStream GetOutputStreamAt(ulong position)
        {
            throw new NotImplementedException();
        }


        public IRandomAccessStream CloneStream()
        {
            return new WebsocketPullStream(url);
        }

        ulong IRandomAccessStream.Position => this.Position;
        ulong IRandomAccessStream.Size
        {
            set
            {

            }
            get
            {
                return Position + 1024;
            }
        }




        public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
        {
            if (!this.cloed)
            {
                return this.ws.InputStream.ReadAsync(buffer, count, options);
            }
            throw new NotImplementedException("ws closed !");
        }


        IAsyncOperationWithProgress<uint, uint> IOutputStream.WriteAsync(IBuffer _buffer)
        {

            throw new NotImplementedException();
        }

        public IAsyncOperation<bool> FlushAsync()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            //if (!cloed)
            //{
            //    Debug.WriteLine("stream closed!");
            //    cloed = true;
            //    this.ws.Dispose();
            //}
        }

    }
}
