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

        public Device Device { get; set; }
        public Texture2DDescription Description { get; private set; }
        public ShaderResourceView SRV { get; private set; }

        public void Load(string filename, System.Threading.CancellationToken token)
        {
            bitmap = new Bitmap(filename);

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
            token.ThrowIfCancellationRequested();
            bitmapData = bitmap.LockBits(
                                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                    ImageLockMode.ReadOnly,
                                    PixelFormat.Format32bppPArgb);

            ds = new SlimDX.DataStream(bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, true, false);
            var dr = new SlimDX.DataRectangle(bitmapData.Stride, ds);
            token.ThrowIfCancellationRequested();
            tex = new Texture2D(Device, Description, dr);
            SRV = new ShaderResourceView(Device, tex);
        }

        public void Dispose()
        {
            SRV?.Dispose();
            tex?.Dispose();
            ds?.Dispose();

            if (bitmapData != null)
                bitmap.UnlockBits(bitmapData);
            
            bitmap?.Dispose();
        }
    }
}
