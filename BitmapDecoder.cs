using System;

using System.Drawing;
using System.Drawing.Imaging;

using SlimDX.Direct3D11;

namespace VVVV.DX11.ImagePlayer
{
    class BitmapDecoder : IDecoder
    {
        Bitmap bitmap;
        BitmapData bitmapData;
        SlimDX.DataStream ds;
        Texture2D tex;
        bool loaded = false;
        bool decoded = false;

        public int Width { get { return bitmap.Width; } }
        public int Height { get { return bitmap.Height; } }
        public Texture2DDescription Description { get; private set; }
        public ShaderResourceView SRV { get; private set; }

        public Device Device { get; set; }

        public void Load(string filename)
        {
            bitmap = new Bitmap(filename);
            loaded = true;

            Description = new Texture2DDescription()
            {
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = SlimDX.DXGI.Format.B8G8R8A8_UNorm,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Default,
                Width = bitmap.Width,
                Height = bitmap.Height,
                SampleDescription = new SlimDX.DXGI.SampleDescription(1, 0)
            };

            bitmapData = bitmap.LockBits(
                                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                    ImageLockMode.ReadOnly,
                                    PixelFormat.Format32bppPArgb);
            decoded = true;

            ds = new SlimDX.DataStream(bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, true, false);
            var dr = new SlimDX.DataRectangle(bitmapData.Stride, ds);
            tex = new Texture2D(Device, Description, dr);
            SRV = new ShaderResourceView(Device, tex);
        }

        public void Dispose()
        {
            SRV?.Dispose();
            tex?.Dispose();

            if (decoded)
                bitmap.UnlockBits(bitmapData);
            if (loaded)
                bitmap.Dispose();
            ds?.Dispose();
            bitmapData = null;
            bitmap = null;
        }
    }
}
