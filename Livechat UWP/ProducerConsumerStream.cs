using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    class ProducerConsumerStream : IRandomAccessStream
    {
        private readonly ClientWebSocket ws;

        private byte[] data;

        private ulong position;

        public ProducerConsumerStream()
        {
            ws = new ClientWebSocket();
            var webSocketUrl = "ws://127.0.0.1:9002/live";
            ws.ConnectAsync(new Uri(webSocketUrl), CancellationToken.None).Wait();
            data = new byte[4096];
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
        }

        public long Length
        {
            get
            {
                return data.Length;
            }
        }

        public ulong Position
        {
            get { return position; }
            set { position = value; }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return 0;
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
            return position;
        }
        public void Seek(ulong offset)
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

                    if (i == data.Length - 1)
                    {
                        position = 0;
                        this.ws.SendAsync(data, WebSocketMessageType.Binary, false, CancellationToken.None).Wait();
                        break;
                    }
                }
            }
            if (position > 0)
            {
                var buf = new byte[position];
                Array.Copy(data, buf, (int)position);
                this.ws.SendAsync(buf, WebSocketMessageType.Binary, false, CancellationToken.None).Wait();
            }
            position = 0;
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

        ulong IRandomAccessStream.Position => this.Position;




        public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
        {
            throw new NotImplementedException();
        }

        public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
        {
            var n = 0;
            var result = new AsyncOperationWithProgress<uint>(() =>
            {
                while (n < buffer.Length)
                {
                    for (var i = 0; i < (int)position + buffer.Length - n; i++)
                    {
                        data[i] = buffer.ToArray()[n];
                        n++;

                        if (i == data.Length - 1)
                        {
                            position = 0;
                            this.ws.SendAsync(data, WebSocketMessageType.Binary, false, CancellationToken.None).Wait();
                            break;
                        }
                    }
                }
                if (position > 0)
                {
                    var buf = new byte[position];
                    Array.Copy(data, buf, (int)position);
                    this.ws.SendAsync(buf, WebSocketMessageType.Binary, false, CancellationToken.None).Wait();
                }
                position = 0;
                return Task.Run(() =>
                {
                    return (uint)n;
                });
            });

            return result;
        }

        public IAsyncOperation<bool> FlushAsync()
        {
            var result = new AsyncOperation<bool>(() =>
            {
                this.Flush();
                return Task.Run(() =>
                {
                    return (uint)(0);
                });
            });
            return result;

        }

        public void Dispose()
        {
            this.ws.Dispose();
        }

        public ulong Size { get => (ulong)data.Length; set => throw new NotImplementedException(); }
    }

    internal class AsyncOperationWithProgress<TResult> : IAsyncOperationWithProgress<TResult, uint>
    {
        #region Fields

        private readonly Task<TResult> mWorker;

        private AsyncStatus mStatus;

        #endregion


        #region IAsyncOperationWithProgress properties

        /// <summary>
        /// Callback to report operation progress.
        /// </summary>
        AsyncOperationProgressHandler<TResult, uint> IAsyncOperationWithProgress<TResult, uint>.Progress { get; set; }


        /// <summary>
        /// Callback to report operation completion.
        /// </summary>
        AsyncOperationWithProgressCompletedHandler<TResult, uint> IAsyncOperationWithProgress<TResult, uint>.Completed { get; set; }


        Exception IAsyncInfo.ErrorCode => mWorker.Exception;


        uint IAsyncInfo.Id => (uint)mWorker.Id;


        AsyncStatus IAsyncInfo.Status => mStatus;

        #endregion


        #region Init and clean-up

        public AsyncOperationWithProgress(Func<Task<TResult>> workerFn)
        {
            mWorker = workerFn.Invoke();
            mWorker.ContinueWith(task =>
            {
                TResult res = default;

                if (task.IsFaulted)
                {
                    mStatus = AsyncStatus.Error;
                }
                else
                {
                    mStatus = AsyncStatus.Completed;
                    res = task.Result;
                }

                    (this as IAsyncOperationWithProgress<TResult, uint>).Completed?.Invoke(this, mStatus);
                return res;
            });
        }

        #endregion


        #region IAsyncOperationWithProgress API

        TResult IAsyncOperationWithProgress<TResult, uint>.GetResults()
        {
            if (mStatus != AsyncStatus.Completed)
                throw new ArgumentException($"Cannot get result when status is {mStatus}.");

            return mWorker.Result;
        }


        void IAsyncInfo.Cancel()
        {
            // Do nothing
        }


        void IAsyncInfo.Close()
        {
            // Do nothing
        }

        #endregion
    }
    internal class AsyncOperation<TResult> : IAsyncOperation<TResult>
    {
        private TResult mStatus;

        public AsyncOperation(Func<Task<uint>> value)
        {
        }

        public TResult GetResults()
        {
            return mStatus;
        }

        public AsyncOperationCompletedHandler<TResult> Completed { get; set; }

        public void Cancel()
        {
        }

        public void Close()
        {

        }

        public Exception ErrorCode => null;

        public uint Id => 0;

        public AsyncStatus Status => AsyncStatus.Completed;
    }
}
