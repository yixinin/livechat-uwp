using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Livechat_UWP
{
    public class SocketStream : IOutputStream, IRandomAccessStream
    {

        private ulong pos = 0;

        //IOutputStream socket;
        private StreamSocket _socket;
        public SocketStream(string host, uint port)
        {
            _socket = new StreamSocket();
            _socket.ConnectAsync(new HostName(host), port.ToString()).AsTask().Wait();

            //this.socket = _socket.OutputStream;
            Debug.WriteLine(_socket.OutputStream == null);
        }
        public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
        {
            var p = pos;
            IProgress<uint> progress = new Progress<uint>((val) =>
            {
                pos = p + val;
            });
            var t = _socket.OutputStream.WriteAsync(buffer);
            t.AsTask(progress);
            return t;
        }

        public IAsyncOperation<bool> FlushAsync()
        {
            var t = _socket.OutputStream.FlushAsync();
            pos = 0;
            return t;
        }

        public void Seek(ulong pos)
        {
            this.FlushAsync().AsTask().Wait();
        }

        public void Dispose()
        {
            this.Seek(0);
        }

        public IInputStream GetInputStreamAt(ulong position)
        {
            throw new NotImplementedException();
        }

        public IOutputStream GetOutputStreamAt(ulong position)
        {
            throw new NotImplementedException();
        }

        public IRandomAccessStream CloneStream()
        {
            throw new NotImplementedException();
        }

        public bool CanRead => false;

        public bool CanWrite => true;

        public ulong Position
        {
            get
            {
                return pos;
            }
            set
            {
                Seek(0);
            }
        }

        public ulong size = 10 * 1024 * 1024;

        public ulong Size
        {
            set
            {
                size = value;
            }
            get
            {
                return size;
            }
        }

        public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
