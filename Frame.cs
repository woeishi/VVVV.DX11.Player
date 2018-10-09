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
        IDecoder decoder;
        
        public int BufferSize { get; set; }
        public bool Loaded { get; private set; }

        public double ReadTime { get; private set; }
        public double DecodeTime { get; private set; }
        public double CopyTime { get; private set; }

        System.Diagnostics.Stopwatch sw;
        CancellationTokenSource cts;
        Task LoadTask;
        RefCounter RefCounter;

        SlimDX.Direct3D11.Device device;
        SlimDX.Direct3D11.Texture2DDescription description;
        public SlimDX.Direct3D11.Texture2DDescription Description { get { return description; } }

        readonly MemoryPool FMemoryPool;
        readonly VVVV.Core.Logging.ILogger FLogger;

        public Frame(string name, IDecoder decoder, SlimDX.Direct3D11.Device device, MemoryPool memoryPool, VVVV.Core.Logging.ILogger logger)
        {
            Loaded = false;

            filename = name;
            this.decoder = decoder;

            sw = new System.Diagnostics.Stopwatch();
            cts = new CancellationTokenSource();
            RefCounter = new RefCounter();

            this.device = device;
           
            FMemoryPool = memoryPool;
            FLogger = logger;
        }

        public void LoadAsync()
        {
            LoadTask = Task.Factory.StartNew(() => { Load(cts.Token); }, cts.Token, TaskCreationOptions.PreferFairness, TaskScheduler.Default);
            LoadTask.ContinueWith((prev) => { LogExceptions(prev, "while reading"); }, cts.Token, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            LoadTask = LoadTask.ContinueWith(prev => { DisposeAsync(); }, CancellationToken.None, TaskContinuationOptions.NotOnRanToCompletion, TaskScheduler.Default);
        }

        private void LogExceptions(Task previous, string topic)
        {
            foreach (var e in previous.Exception.InnerExceptions)
                FLogger.Log(VVVV.Core.Logging.LogType.Debug, e.GetType().ToString() + topic + ": "  + e.Message);
        }
        
        private unsafe void Load(CancellationToken token)
        {
            sw.Start();
            try
            {
                token.ThrowIfCancellationRequested();
                decoder.Device = device;

                decoder.Load(filename);

                token.ThrowIfCancellationRequested();

                description = decoder.Description;

                Loaded = true;
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("load: {0}", e);
                throw e;
            }
            finally
            {
                DecodeTime = sw.Elapsed.TotalMilliseconds;
            }
        }

        public FeralTic.DX11.Resources.DX11ResourceTexture2D CopyResource(FeralTic.DX11.Resources.DX11ResourceTexture2D texture, FeralTic.DX11.DX11RenderContext context)
        {
            if (texture == null)
                texture = new FeralTic.DX11.Resources.DX11ResourceTexture2D(context);
            if (Loaded && (texture.Meta != this.Filename))
            {
                sw.Restart();
                try
                {
                    texture.SetBySRV(decoder.SRV, RefCounter.Use());
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("CopyResource: {0}", e);
                }
                finally
                {
                    CopyTime = sw.Elapsed.TotalMilliseconds;
                }
            }
            return texture;
        }

        void DisposeAsync()
        {
            try
            {
                while (!RefCounter.Free)
                {
                    Thread.Sleep(1);
                }
                decoder.Dispose();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
            }
            finally
            {
                decoder = null;
            }
            cts.Dispose();
        }

        public void Dispose()
        {
            if (LoadTask != null)
            {
                if (!LoadTask.IsCompleted)
                    cts.Cancel();
                else
                    Task.Factory.StartNew(() => DisposeAsync(), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            }
        }
    }
}
