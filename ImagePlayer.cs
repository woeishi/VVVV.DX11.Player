using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using SlimDX.Direct3D11;
using VVVV.Core.Logging;
using VVVV.Utils.VMath;

namespace VVVV.DX11.ImagePlayer
{
    class Player : IDisposable
    {
        FileInfo[] files;
        Dictionary<int, Frame> frames;
        List<int> requestedKeys;

        public string DirectoryName { get; private set; }
        public string FileMask { get; private set; }
        public int FrameCount { get; private set; }

        public Frame this[int index]
        {
            get
            {
                var id = VMath.Zmod(index, requestedKeys.Count);
                return frames[requestedKeys[id]];
            }
        }
        public IEnumerable<bool> Loaded => requestedKeys.Select(k => frames[k].Loaded);

        readonly Device device;
        readonly MemoryPool FMemoryPool;
        readonly ILogger FLogger;

        public Player(string dir, string fileMask, MemoryPool memoryPool, ILogger logger)
        {
            FMemoryPool = memoryPool;
            FLogger = logger;

			DirectoryName = dir;
            FileMask = fileMask;
			var directory = new DirectoryInfo(dir);
            requestedKeys = new List<int>();
            frames = new Dictionary<int, Frame>();
            if (directory.Exists)
            {
                files = directory.GetFiles(fileMask).Where(f => (f.Attributes & FileAttributes.Hidden) == 0).ToArray();
                FrameCount = files.Length;
            }
            else
                files = new FileInfo[0]{ };
            device = Lib.Devices.DX11GlobalDevice.DeviceManager.RenderContexts[0].Device;
        }
		
        public void Preload(IEnumerable<int> indices)
		{
            requestedKeys.Clear();
			var toDelete = new List<int>(frames.Keys);

			foreach (var id in indices)
			{
                var key = VMath.Zmod(id, FrameCount);

                requestedKeys.Add(key);

                if (frames.ContainsKey(key))
                    toDelete.Remove(key);
                else
                {
                    IDecoder decoder = Decoder.SelectFromFile(files[key], FMemoryPool);
                    decoder.Device = device;
                    frames[key] = new Frame(files[key].FullName, decoder, FLogger);

                    frames[key].LoadAsync();
                }
            }
			
			foreach (var d in toDelete)
			{
                frames[d].Dispose();
                frames.Remove(d);
			}
		}

        public void Dispose()
        {
            foreach (var frame in frames.Values)
                frame.Dispose();
        }
    }
}