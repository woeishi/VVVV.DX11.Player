using System;

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VVVV.DX11.ImagePlayer
{
    class Frame : IDisposable
    {
        string filename;
        public string Filename { get { return filename; } }
        object streamLock;
        bool needsDispose;
        Stream memoryStream;
        IDecoder decoder;
        
        public int BufferSize { get; set; }
        public bool Loaded { get; private set; }

        public double ReadTime { get; private set; }
        public double DecodeTime { get; private set; }
        public double CopyTime { get; private set; }

        System.Diagnostics.Stopwatch sw;
        CancellationTokenSource cts;
        CancellationToken token;

        SlimDX.Direct3D11.Device device;
        SlimDX.Direct3D11.Texture2DDescription description;
        public SlimDX.Direct3D11.Texture2DDescription Description { get { return description; } }

        readonly MemoryPool FMemoryPool;
        readonly VVVV.Core.Logging.ILogger FLogger;

        public Frame(string name, IDecoder decoder, SlimDX.Direct3D11.Device device, MemoryPool memoryPool, VVVV.Core.Logging.ILogger logger)
        {
            needsDispose = false;
            Loaded = false;

            filename = name;
            this.decoder = decoder;

            sw = new System.Diagnostics.Stopwatch();
            cts = new CancellationTokenSource();
            token = cts.Token;

            streamLock = new object();

            this.device = device;
           
            FMemoryPool = memoryPool;
            FLogger = logger;
        }

        public void LoadAsync()
        {
            var readTask = Task.Factory.StartNew(() => { Read(token); }, token, TaskCreationOptions.PreferFairness, TaskScheduler.Default);
            readTask.ContinueWith((prev) => { LogExceptions(prev, "while reading"); }, token, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);

            var loadTask = readTask.ContinueWith((prev) => { Load(token); }, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
            loadTask.ContinueWith((prev) => { LogExceptions(prev," while loading"); }, token, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
        }

        private void LogExceptions(Task previous, string topic)
        {
            foreach (var e in previous.Exception.InnerExceptions)
                FLogger.Log(VVVV.Core.Logging.LogType.Debug, e.GetType().ToString() + topic + ": "  + e.Message);
        }
        
        private void Read(CancellationToken token)
        {
            sw.Start();
            byte[] buffer = FMemoryPool.ManagedPool.GetMemory(BufferSize);
            try
            {
                token.ThrowIfCancellationRequested();
               
                using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan))
                {
                    var length = (int)fs.Length;
                    //memoryStream = new MemoryStream(length);
                    memoryStream = FMemoryPool.ManagedStreamPool.GetStream(length);
                    needsDispose = true;
                    while (fs.Position < length)
                    {
                        int numBytesRead = fs.Read(buffer, 0, buffer.Length);
                        token.ThrowIfCancellationRequested();
                        memoryStream.Write(buffer, 0, numBytesRead);
                    }
                    memoryStream.Position = 0;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("read file: {0}", e);
                throw;
            }
            finally
            {
                FMemoryPool.ManagedPool.PutMemory(buffer);
            }
            ReadTime = sw.Elapsed.TotalMilliseconds;
        }

        private unsafe void Load(CancellationToken token)
        {
            sw.Restart();
            try
            {
                token.ThrowIfCancellationRequested();
                decoder.Device = device;
                lock (streamLock)
                {
                    decoder.Load(memoryStream);
                }
                token.ThrowIfCancellationRequested();

                description = decoder.Description;
                LoadingCompleted(description);
                decoder.Decode();
                token.ThrowIfCancellationRequested();

                Loaded = true;
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("load: {0}", e);
                throw e;
            }
            DecodeTime = sw.Elapsed.TotalMilliseconds;
        }

        public FeralTic.DX11.Resources.DX11ResourceTexture2D CopyResource(FeralTic.DX11.Resources.DX11ResourceTexture2D texture)
        {
            sw.Restart();
            try
            {
                decoder.CopyResource(texture);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("CopyResource: {0}", e);
            }
            CopyTime = sw.Elapsed.TotalMilliseconds;

            return texture;
        }

        public void Dispose()
        {
            cts.Cancel();
            try
            {
                decoder.Dispose();
                if (needsDispose)
                {
                    lock (streamLock)
                    {
                        FMemoryPool.ManagedStreamPool.PutStream(memoryStream);
                        //memoryStream.Dispose();
                    }
                    needsDispose = false;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
            }
            finally
            {
                decoder = null;
                memoryStream = null;
            }
            cts.Dispose();
        }
    }
}
