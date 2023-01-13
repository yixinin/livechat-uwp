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
            return _socket.OutputStream.WriteAsync(buffer);
        }

        public IAsyncOperation<bool> FlushAsync()
        {
            return _socket.OutputStream.FlushAsync();
        }

        public void Seek(ulong pos)
        {
            this.FlushAsync().AsTask().Wait();
        }

        public void Dispose()
        {
            _socket.Dispose();
        }

        public IInputStream GetInputStreamAt(ulong position)
        {
            throw new NotImplementedException();
        }

        public IOutputStream GetOutputStreamAt(ulong position)
        {
            return _socket.OutputStream;
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
                return 0;
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
