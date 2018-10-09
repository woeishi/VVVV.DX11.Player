using System;

namespace VVVV.DX11.ImagePlayer
{
    public interface IDecoder : IDisposable
    {
        int Width { get; }
        int Height { get; }
        SlimDX.Direct3D11.Texture2DDescription Description { get; }
        SlimDX.Direct3D11.Device Device { get; set; }
        SlimDX.Direct3D11.ShaderResourceView SRV { get; }

        void Load(string filename);
    }

    static class Decoder
    {
        public static IDecoder SelectFromFile(System.IO.FileInfo file)
        {
            var ext = file.Extension.ToLower();
            if (BmpSupportedExtensions.Contains(ext))
                return new BitmapDecoder();
            else
                return new GPUDecoder();
        }

        private static readonly System.Collections.Generic.HashSet<string> BmpSupportedExtensions = new System.Collections.Generic.HashSet<string>
        {
             ".bmp",".exif",".gif",".ico",".jpg",".jpeg",".png",".tif",".tiff",".wmp"
        };
    }
}
