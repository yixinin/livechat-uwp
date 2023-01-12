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
using Windows.Storage.Streams;

namespace Livechat_UWP
{
    class WebsocketPushStream : IRandomAccessStream
    {
        private readonly ClientWebSocket ws;

        private byte[] data;

        private ulong position;
        private long length;

        public WebsocketPushStream(String url)
        {
            ws = new ClientWebSocket();
            ws.ConnectAsync(new Uri(url), CancellationToken.None).Wait();

            data = new byte[10 * 1024 * 1024];
        }

        public bool CanRead { get { return false; } }

        public bool CanSeek { get { return true; } }

        public bool CanWrite { get { return true; } }

        public void Flush()
        {
            if (position > 0)
            {
                var buf = new byte[position];
                Array.Copy(data, buf, (int)position);
                this.ws.SendAsync(buf, WebSocketMessageType.Binary, false, CancellationToken.None).Wait();
            }
            position = 0;
            length = 0;
        }

        public long Length
        {
            get
            {
                return length;
            }
        }

        public ulong Position
        {
            get { return position; }
            set { position = value; }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public ulong Seek(ulong offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    position = offset;
                    break;
                case SeekOrigin.Current:
                    position += offset;
                    break;
                case SeekOrigin.End:
                    position -= offset;
                    break;
            }
            if (position == 0)
            {
                length = 0;
            }
            return position;
        }
        void IRandomAccessStream.Seek(ulong offset)
        {
            this.Seek(offset, SeekOrigin.Begin);
        }

        public void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            if (offset > 0)
            {
                position += (ulong)offset;
            }

            var n = 0;

            while (n < count)
            {
                for (var i = 0; i < (int)position + count - n; i++)
                {
                    data[i] = buffer[n];
                    n++;
                    if ((ulong)length <= position)
                    {
                        length++;
                    }

                    position++;

                    if (i == data.Length - 1)
                    {
                        this.Flush();
                        break;
                    }
                }
            }
            this.Flush();
        }

        public IInputStream GetInputStreamAt(ulong position)
        {
            throw new NotImplementedException();
        }

        public IOutputStream GetOutputStreamAt(ulong position)
        {
            this.Position = position;
            return this;
        }


        public IRandomAccessStream CloneStream()
        {
            var webSocketUrl = "ws://172.16.67.134:9902/live/ws";
            var s = new WebsocketPushStream(webSocketUrl);
            s.Position = this.Position;
            this.data.CopyTo(s.data, 0);
            return s;
        }

        ulong IRandomAccessStream.Position => this.Position;
        ulong IRandomAccessStream.Size
        {
            set => this.Size = value;
            get
            {
                return this.Size;
            }
        }




        public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
        {
            throw new NotImplementedException();
        }


        IAsyncOperationWithProgress<uint, uint> IOutputStream.WriteAsync(IBuffer buffer)
        {

            return AsyncInfo.Run<uint, uint>((token, progress) =>
                Task.Run<uint>(() =>
                {
                    uint n = 0;
                    progress.Report(0);

                    while (n < buffer.Length)
                    {
                        for (var i = 0; i < (int)position + buffer.Length - n; i++)
                        {
                            data[i] = buffer.ToArray()[n];
                            n++;
                            position++;
                            if ((ulong)length <= position)
                            {
                                length++;
                            }
                            token.ThrowIfCancellationRequested();
                            progress.Report(n);
                            if (i == data.Length - 1)
                            {
                                this.Flush();
                                break;
                            }
                        }
                    }
                    this.Flush();
                    return n;

                }, token));
        }

        public IAsyncOperation<bool> FlushAsync()
        {
            return AsyncInfo.Run((token) =>
              Task.Run(() =>
                {
                    this.Flush();
                    token.ThrowIfCancellationRequested();
                    return true;
                }, token));
        }

        public void Dispose()
        {
            this.ws.Dispose();
        }

        public ulong Size { get => (ulong)data.Length; set => this.data = new byte[value]; }
    }
}
