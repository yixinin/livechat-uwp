using Livechat_UWP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace livechat_play
{
    class SocketPullStream : IRandomAccessStream
    {
        private readonly StreamSocket socket;
        string host;
        uint port;
        ulong pos = 0;
        private readonly string url;
        private bool cloed = false;
        public SocketPullStream(string host, uint port)
        {
            this.host = host;
            this.port = port;
            socket = new StreamSocket();
            socket.ConnectAsync(new HostName(host), port.ToString()).AsTask().Wait();
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
            return new SocketPullStream(host, port);
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
                return this.socket.InputStream.ReadAsync(buffer, count, options);
            }
            throw new NotImplementedException("socket closed !");
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
            if (!cloed)
            {
                Debug.WriteLine("stream closed!");
                cloed = true;
                this.socket.Dispose();
                App.Current.Exit();
            }
        }

    }
}
