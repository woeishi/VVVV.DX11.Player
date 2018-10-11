using System;

namespace VVVV.DX11.ImagePlayer
{
    public enum DecoderChoice
    {
        Automatic, DirectX, WIC
    }

    public interface IDecoder : IDisposable
    {
        SlimDX.Direct3D11.Device Device { get; set; }
        SlimDX.Direct3D11.Texture2DDescription Description { get; }
        SlimDX.Direct3D11.ShaderResourceView SRV { get; }

        void Load(string filename, System.Threading.CancellationToken token);
    }

    static class Decoder
    {
        public static IDecoder SelectFromFile(DecoderChoice decoderChoice, System.IO.FileInfo file, MemoryPool memPool)
        {
            if (decoderChoice == DecoderChoice.Automatic)
            {
                var ext = file.Extension.ToLower();
                decoderChoice = ext.Contains(".dds") ? DecoderChoice.DirectX : DecoderChoice.WIC;
            }
            if (decoderChoice == DecoderChoice.DirectX)
                return new GPUDecoder();
            else
                return new WICDecoder(memPool);
        }
    }
}
