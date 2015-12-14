using System;
using System.IO;

using System.Drawing;
using System.Drawing.Imaging;

using VVVV.Utils;

namespace VVVV.DX11.ImagePlayer
{
    class BitmapDecoder : IDecoder
    {
        Bitmap bitmap;
        BitmapData bitmapData;
        bool loaded = false;
        bool decoded = false;

        public int Width { get { return bitmap.Width; } }
        public int Height { get { return bitmap.Height; } }
        public SlimDX.Direct3D11.Texture2DDescription Description { get; private set; }
        public SlimDX.Direct3D11.Device Device { get; set; }

        public void Load(Stream stream)
        {
            bitmap = new Bitmap(stream);
            loaded = true;

            Description = new SlimDX.Direct3D11.Texture2DDescription()
            {
                ArraySize = 1,
                BindFlags = SlimDX.Direct3D11.BindFlags.ShaderResource,
                CpuAccessFlags = SlimDX.Direct3D11.CpuAccessFlags.Write,
                Format = SlimDX.DXGI.Format.B8G8R8A8_UNorm,
                MipLevels = 1,
                OptionFlags = SlimDX.Direct3D11.ResourceOptionFlags.None,
                Usage = SlimDX.Direct3D11.ResourceUsage.Dynamic,
                Width = bitmap.Width,
                Height = bitmap.Height,
                SampleDescription = new SlimDX.DXGI.SampleDescription(1, 0)
            };
        }

        public void Decode()
        {
            if (loaded)
            {
                bitmapData = bitmap.LockBits(
                                        new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                        ImageLockMode.ReadOnly,
                                        PixelFormat.Format32bppPArgb);
                decoded = true;
            }
        }

        public void Dispose()
        {
            if (decoded)
                bitmap.UnlockBits(bitmapData);
            if (loaded)
                bitmap.Dispose();
            bitmapData = null;
            bitmap = null;
        }

        public void CopyResource(FeralTic.DX11.Resources.DX11ResourceTexture2D texture)
        {
            SlimDX.Direct3D11.DeviceContext ctx = texture.Context.CurrentDeviceContext;
            SlimDX.DataBox db = ctx.MapSubresource(texture.WritableResource, 0, SlimDX.Direct3D11.MapMode.WriteDiscard, SlimDX.Direct3D11.MapFlags.None);
            unsafe
            {
                Memory.Copy(db.Data.DataPointer, bitmapData.Scan0, (uint)(bitmapData.Stride * bitmapData.Height));
            }
            ctx.UnmapSubresource(texture.WritableResource, 0);
        }
    }
}
