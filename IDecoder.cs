using System;

namespace VVVV.DX11.ImagePlayer
{
    public interface IDecoder : IDisposable
    {
        SlimDX.Direct3D11.Device Device { get; set; }
        SlimDX.Direct3D11.Texture2DDescription Description { get; }
        SlimDX.Direct3D11.ShaderResourceView SRV { get; }

        void Load(string filename, System.Threading.CancellationToken token);
    }

    static class Decoder
    {
        public static IDecoder SelectFromFile(System.IO.FileInfo file, MemoryPool memPool)
        {
            var ext = file.Extension.ToLower();
            if (ext.Contains(".dds"))
                return new GPUDecoder();
            else
                return new WICDecoder(memPool);
        }
    }
}
