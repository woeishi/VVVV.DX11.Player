using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using FeralTic.DX11.Resources;
using SlimDX.Direct3D11;
using VVVV.Core.Logging;

namespace VVVV.DX11.ImagePlayer
{
	class Player : IDisposable
	{
        public string DirectoryName { get; private set; }
		public string FileMask { get; private set; }
        public int BufferSize { get; set; }
		DirectoryInfo directory;
		FileInfo[] files;
        public int FrameCount { get; private set; }

        Texture2DDescription description;
        Device device;
        public Device Device
        {
            get { return device; }
            set { device = value; }
        }

        Dictionary<string, Frame> frames;
        List<string> requestedKeys;
		public IEnumerable<bool> Loaded
		{
			get 
			{
		 		foreach (var key in requestedKeys)
					yield return frames[key].Loaded;
			}
		}

        readonly MemoryPool FMemoryPool;
        readonly ILogger FLogger;

        public Player(string dir, string fileMask, MemoryPool memoryPool, ILogger logger)
        {
            FMemoryPool = memoryPool;
            FLogger = logger;

			DirectoryName = dir;
            this.FileMask = fileMask;
			directory = new DirectoryInfo(dir);
            requestedKeys = new List<string>();
            frames = new Dictionary<string, Frame>();
            if (directory.Exists)
            {
                files = directory.GetFiles(fileMask).Where(f => (f.Attributes & FileAttributes.Hidden) == 0).ToArray();
                FrameCount = files.Length;
            }
            else
                files = new FileInfo[0]{ };
            device = Lib.Devices.DX11GlobalDevice.DeviceManager.RenderContexts[0].Device;
        }
		
        public Frame GetFrame(int index)
        {
            return frames[requestedKeys[index]];
        }

        public void Preload(IEnumerable<int> indices)
		{
            requestedKeys.Clear();
			var toDelete = new List<string>(frames.Keys);

			foreach (var id in indices)
			{
                var file = files[VVVV.Utils.VMath.VMath.Zmod(id, FrameCount)];
                string key = file.FullName;
                requestedKeys.Add(key);

                if (frames.ContainsKey(key))
					toDelete.Remove(key);
				else
				{
                    IDecoder decoder = Decoder.SelectFromFile(file);
					frames[key] = new Frame(key, decoder, device, FMemoryPool, FLogger);
                    frames[key].BufferSize = BufferSize;

                    if (description.Width == 0)
                        frames[key].LoadingCompleted = FrameLoaded;

                    frames[key].LoadAsync();
				}
			}
			
			foreach (var d in toDelete)
			{
                frames[d].Dispose();
                frames.Remove(d);
			}
		}

        public DX11ResourceTexture2D SetTexture(Frame frame, DX11ResourceTexture2D texture, FeralTic.DX11.DX11RenderContext context)
        {
            device = context.Device;
            if (texture == null)
                texture = new DX11ResourceTexture2D(context);

            if (description.Width != 0)
            {
                if (!texture.MatchesSizeByDescription(description))
                {
                    var tex = new Texture2D(device, description);
                    texture.SetResource(tex);
                    texture.Meta = "";
                }

                if (frame.Loaded && (texture.Meta != frame.Filename))
                {
                    frame.CopyResource(texture);
                    texture.Meta = frame.Filename;
                }
            }
            return texture;
        }

        private void FrameLoaded(Texture2DDescription Description)
        {
            if (description.Width == 0)
                description = Description;
        }

        public void Dispose()
        {
            foreach (var s in frames.Values)
                s.Dispose();
        }
    }
}