using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Threading;
using JetBrains.Annotations;
using RummeltSoftware.Gelf.Listener.Internal;

namespace RummeltSoftware.Gelf.Listener {
    /// <summary>
    /// Base class for GELF listener implementations, featuring a parse queue and multiple worker threads.
    /// Does NOT support chunking by itself, see <see cref="ChunkedGelfListenerBase"/> for chunking support.
    /// </summary>
    public abstract class GelfListenerBase : IGelfListener {
        private readonly BlockingCollection<RawGelfMessage> _receiveQueue;
        private readonly Thread[] _workerThreads;
        private readonly CancellationTokenSource _closedTokenSource;

        private bool _isClosed;

        // ========================================

        public abstract bool SupportsChunking { get; }

        public abstract bool SupportsCompression { get; }

        public bool AllowChunking { get; }

        public int MaxChunkSize { get; }

        public TimeSpan ChunkTimeout { get; }

        public int MaxMessageSize { get; }

        public int MaxUncompressedSize { get; }

        public int NumWorkerThreads { get; }

        public int MaxIncomingQueueSize { get; }


        public bool IsOpen {
            get => !_isClosed;
        }


        // ========================================


        internal GelfListenerBase([CanBeNull] GelfListenerSettings settings) {
            AllowChunking = settings?.AllowChunking ?? true;
            MaxChunkSize = settings?.MaxChunkSize ?? 8192;
            ChunkTimeout = settings?.ChunkTimeout ?? TimeSpan.FromSeconds(5);
            MaxMessageSize = settings?.MaxMessageSize ?? 1048576;
            MaxUncompressedSize = settings?.MaxUncompressedSize ?? 8388608;
            NumWorkerThreads = settings?.NumWorkerThreads ?? 2;
            MaxIncomingQueueSize = settings?.MaxIncomingQueueSize ?? 1024;

            if ((MaxMessageSize % MaxChunkSize) != 0)
                throw new ArgumentException($"{nameof(MaxMessageSize)} must be multiples of {nameof(MaxChunkSize)}");
            if ((MaxUncompressedSize % MaxChunkSize) != 0)
                throw new ArgumentException($"{nameof(MaxUncompressedSize)} must be multiples of {nameof(MaxChunkSize)}");

            _receiveQueue = new BlockingCollection<RawGelfMessage>(MaxIncomingQueueSize);
            _workerThreads = new Thread[NumWorkerThreads];
            _closedTokenSource = new CancellationTokenSource();
        }


        // ========================================

        public event EventHandler<GelfMessageReceivedEventArgs> MessageReceived;


        protected void NotifyMessageReceived(GelfMessage msg, IPEndPoint receivedFrom, DateTime receiveTime) {
            MessageReceived?.Invoke(this, new GelfMessageReceivedEventArgs(msg, receivedFrom, receiveTime));
        }


        protected void NotifyMessageReceived(GelfMessageReceivedEventArgs eventArgs) {
            MessageReceived?.Invoke(this, eventArgs);
        }


        public event EventHandler<GelfMessageDroppedEventArgs> MessageDropped;


        protected void NotifyMessageDropped(string reason) {
            MessageDropped?.Invoke(this, new GelfMessageDroppedEventArgs(reason));
        }


        public event EventHandler<GelfMessageReadExceptionEventArgs> ReadException;


        protected void NotifyReadException(Exception ex) {
            ReadException?.Invoke(this, new GelfMessageReadExceptionEventArgs(ex));
        }


        // ========================================


        public void Open(IPEndPoint endpoint) {
            if (_isClosed)
                throw new ObjectDisposedException("Already closed");

            OnOpening(endpoint);

            var className = GetType().Name;
            for (var i = 0; i < _workerThreads.Length; i++) {
                (_workerThreads[i] = new Thread(ProcessQueue) {
                    Name = $"{className}_WorkerThread#{i}",
                    IsBackground = true,
                }).Start();
            }

            OnOpened();
        }


        public void Close() {
            if (_isClosed)
                return;

            _isClosed = true;

            _receiveQueue.CompleteAdding();
            _closedTokenSource.Cancel();

            foreach (var t in _workerThreads) {
                t.Interrupt();
            }

            _receiveQueue.Dispose();

            OnClosed();
        }


        public virtual void Dispose() {
            Close();
        }


        internal void NotifyRawMessageReceived(RawGelfMessage message) {
            if (!_receiveQueue.TryAdd(message)) {
                NotifyMessageDropped("Incoming queue is full");
            }
        }


        // ========================================

        protected abstract void OnOpening(IPEndPoint endpoint);

        protected abstract void OnOpened();

        protected abstract void OnClosed();


        [Pure, NotNull]
        internal virtual GelfMessageDecoder GetDecoder() {
            return new GelfMessageDecoder();
        }


        // ========================================


        private void ProcessQueue() {
            var parser = GetDecoder();

            try {
                foreach (var rawMessage in _receiveQueue.GetConsumingEnumerable(_closedTokenSource.Token)) {
                    Debug.WriteLine($"[{Thread.CurrentThread.Name}] Decoding message");

                    try {
                        var eventArgs = parser.Decode(rawMessage);
                        if (eventArgs == null)
                            continue;

                        NotifyMessageReceived(eventArgs);
                    }
                    catch (TooManyChunksException ex) {
                        Debug.WriteLine($"[{Thread.CurrentThread.Name}] Message dropped (too many chunks)");
                        NotifyMessageDropped(ex.Message);
                    }
                    catch (GelfMessageFormatException ex) {
                        Debug.WriteLine($"[{Thread.CurrentThread.Name}] Message dropped (format error)");
                        NotifyMessageDropped(ex.Message);
                    }
                    catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is ThreadInterruptedException)) {
                        Debug.WriteLine($"[{Thread.CurrentThread.Name}] Exception");
                        NotifyReadException(ex);
                    }
                }
            }
            catch (OperationCanceledException) {
                Debug.WriteLine($"[{Thread.CurrentThread.Name}] OCE");
            }
            catch (ThreadInterruptedException) {
                Debug.WriteLine($"[{Thread.CurrentThread.Name}] TIE");
            }

            Debug.WriteLine($"[{Thread.CurrentThread.Name}] exiting");
        }
    }
}