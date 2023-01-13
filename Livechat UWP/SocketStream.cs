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
    public class SocketStream : IRandomAccessStream
    {

        private ulong pos = 0;

        IRandomAccessStream rs = null;

        //IOutputStream socket;
        private StreamSocket _socket;
        public SocketStream(string host, uint port)
        {
            _socket = new StreamSocket();
            _socket.ConnectAsync(new HostName(host), port.ToString()).AsTask().Wait();

            var ms = new MemoryStream();
            rs = ms.AsRandomAccessStream();
            //this.socket = _socket.OutputStream;
        }
        public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
        {

            rs.WriteAsync(buffer);
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
            rs.FlushAsync();
            var t = _socket.OutputStream.FlushAsync();
            pos = 0;
            return t;
        }

        public void Seek(ulong pos)
        {
            rs.Seek(pos);
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
            return rs.GetOutputStreamAt(position);
        }

        public IRandomAccessStream CloneStream()
        {
            return rs.CloneStream();
        }

        public bool CanRead => false;

        public bool CanWrite => true;

        public ulong Position => rs.Position;

        public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
        {
            throw new NotImplementedException();
        }

        ulong IRandomAccessStream.Size
        {
            get
            {
                return rs.Size;
            }
            set => throw new NotImplementedException();
        }
    }
}
