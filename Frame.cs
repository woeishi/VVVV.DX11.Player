using System;

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VVVV.DX11.ImagePlayer
{
    class Frame : IDisposable
    {
        string filename;
        IDecoder decoder;
        public bool WaitForFrame { get; set; }

        public bool Loaded { get; private set; }

        public double ReadTime { get; private set; }
        public double DecodeTime { get; private set; }
        public double CopyTime { get; private set; }

        System.Diagnostics.Stopwatch sw;
        CancellationTokenSource cts;
        Task LoadTask;
        RefCounter RefCounter;

        public SlimDX.Direct3D11.Texture2DDescription Description { get; private set; }

        readonly VVVV.Core.Logging.ILogger FLogger;

        public Frame(string name, IDecoder decoder, VVVV.Core.Logging.ILogger logger)
        {
            WaitForFrame = true;
            Loaded = false;

            filename = name;
            this.decoder = decoder;

            sw = new System.Diagnostics.Stopwatch();
            cts = new CancellationTokenSource();
            RefCounter = new RefCounter();

            FLogger = logger;
        }

        public void LoadAsync()
        {
            LoadTask = Task.Factory.StartNew(() => { Load(cts.Token); }, cts.Token, TaskCreationOptions.PreferFairness, TaskScheduler.Default);
            LoadTask.ContinueWith((prev) => { LogExceptions(prev, "while reading"); }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            var disposeTask = LoadTask.ContinueWith(prev => { DisposeAsync(); }, CancellationToken.None, TaskContinuationOptions.OnlyOnCanceled, TaskScheduler.Default);
            disposeTask.ContinueWith((prev) => { LogExceptions(prev, "while disposing"); }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
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

                decoder.Load(filename, token);

                Description = decoder.Description;

                Loaded = true;
            }
            //catch (OperationCanceledException) { }
            finally
            {
                DecodeTime = sw.Elapsed.TotalMilliseconds;
            }
        }

        public FeralTic.DX11.Resources.DX11ResourceTexture2D SetSRV(FeralTic.DX11.Resources.DX11ResourceTexture2D texture, FeralTic.DX11.DX11RenderContext context)
        {
            if (texture == null)
                texture = new FeralTic.DX11.Resources.DX11ResourceTexture2D(context);
            
            if (texture.Meta != this.filename && (WaitForFrame || this.Loaded))
            {
                sw.Restart();
                var handle = RefCounter.Use();
                if (!this.Loaded)
                    LoadTask.Wait();
                texture.SetBySRV(decoder.SRV, handle);
                CopyTime = sw.Elapsed.TotalMilliseconds;
            }
            return texture;
        }

        void DisposeAsync()
        {
            while (!RefCounter.Free)
            {
                Thread.Sleep(3);
            }
            decoder.Dispose();
            
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
            else
            {
                decoder.Dispose();
                cts.Dispose();
            }
        }
    }
}
