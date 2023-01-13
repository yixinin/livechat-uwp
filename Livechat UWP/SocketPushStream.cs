using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Livechat_UWP
{
    internal class SocketPushStream : IRandomAccessStream
    {
        private readonly StreamSocket socket;

        private byte[] data;
        string host;
        uint port;

        private ulong position;
        private long length;
        private readonly string url;
        private bool cloed = false;
        private ulong count = 0;
        public SocketPushStream(string host, uint port)
        {
            this.host = host;
            this.port = port;
            socket = new StreamSocket();
            socket.ConnectAsync(new HostName(host), port.ToString()).AsTask().Wait();

            data = new byte[10 * 1024 * 1024];
        }

        public bool CanRead { get { return false; } }

        public bool CanSeek { get { return true; } }

        public bool CanWrite { get { return true; } }

        public void Flush()
        {
            if (!cloed && this.Position > 0)
            {
                var buf = new byte[this.Position];
                Array.Copy(data, buf, (int)this.Position);
                var n = this.socket.OutputStream.WriteAsync(buf.AsBuffer()).GetResults();
                this.socket.OutputStream.FlushAsync().AsTask().Wait();
                Debug.WriteLine(string.Format("send data {0}", n));
            }
            this.Position = 0;
            this.length = 0;
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
            get
            {
                return position;
            }
            set
            {
                position = value;
            }
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
                    this.Position = offset;
                    break;
                case SeekOrigin.Current:
                    this.Position += offset;
                    break;
                case SeekOrigin.End:
                    this.Position -= offset;
                    break;
            }
            if (this.Position == 0)
            {
                length = 0;
            }
            return this.Position;
        }
        void IRandomAccessStream.Seek(ulong offset)
        {
            this.Seek(0, SeekOrigin.Begin);
        }

        public void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            if (offset > 0)
            {
                this.Position += (ulong)offset;
            }

            var n = 0;

            while (n < count)
            {
                for (var i = 0; i < data.Length && n < buffer.Length; i++)
                {
                    data[i] = buffer[n];
                    n++;
                    if ((ulong)length <= this.Position)
                    {
                        length++;
                    }

                    this.Position++;

                    if ((long)this.Position == data.Length - 1)
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
            var s = new SocketPushStream(this.host, port);
            s.Position = this.Position;
            this.data.CopyTo(s.data, 0);
            return s;
        }

        public IOutputStream Out()
        {
            return socket.OutputStream;
        }

        ulong IRandomAccessStream.Position => this.Position;
        ulong IRandomAccessStream.Size
        {
            set
            {
                this.Size = value;
            }
            get
            {
                return this.Size;
            }
        }




        public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
        {
            throw new NotImplementedException();
        }


        IAsyncOperationWithProgress<uint, uint> IOutputStream.WriteAsync(IBuffer _buffer)
        {

            var buffer = _buffer.ToArray();
            return AsyncInfo.Run<uint, uint>((token, progress) =>
                Task.Run<uint>(async () =>
                {

                    Debug.WriteLine(string.Format("[{0}]start pos:{1} size:{2}, datasize:{3}", this.count, this.Position, buffer.Length, data.Length));
                    var ct = this.count;
                    this.count++;
                    uint n = 0;
                    progress.Report(0);

                    while (n < buffer.Length)
                    {
                        for (var i = 0; i < data.Length && n < buffer.Length; i++)
                        {
                            data[i] = buffer[n];
                            n++;
                            this.Position++;
                            if ((ulong)length <= this.Position)
                            {
                                length++;
                            }
                            token.ThrowIfCancellationRequested();
                            progress.Report(n);
                            if ((long)this.Position == data.Length - 1)
                            {
                                var pp = this.Position;
                                var fulled = await this.FlushAsync();
                                Debug.WriteLine("buffer full, force flush {0}, pos:{1} -> {2}", fulled, pp, this.Position);
                                break;
                            }
                        }
                    }
                    var tailed = await this.FlushAsync();
                    Debug.WriteLine(string.Format("[{0}]end, flushed:{1}, pos:{2}", ct, tailed, this.Position));
                    return n;
                }, token));
        }

        public IAsyncOperation<bool> FlushAsync()
        {
            return AsyncInfo.Run((token) =>
              Task.Run(async () =>
              {
                  if (!cloed && this.Position > 0)
                  {
                      var buf = new byte[this.Position];
                      Array.Copy(this.data, buf, (int)this.Position);
                      var n = await this.socket.OutputStream.WriteAsync(buf.AsBuffer());
                      Debug.WriteLine(string.Format("send data: {0}", n));
                      await this.socket.OutputStream.FlushAsync();
                  }
                  this.Position = 0;
                  this.length = 0;
                  token.ThrowIfCancellationRequested();
                  return true;
              }, token));
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

        public ulong Size { get => (ulong)data.Length; set => this.data = new byte[value]; }
    }
}
