using System;
using System.IO;

using System.Windows;
using System.Windows.Media.Imaging;

namespace VVVV.DX11.ImagePlayer
{
    class BitmapImageDecoder : IDecoder
    {
        BitmapImage bmpImage;
        int stride;

        public int Width { get { return bmpImage.PixelWidth; } }
        public int Height { get { return bmpImage.PixelHeight; } }
        public SlimDX.Direct3D11.Texture2DDescription Description { get; private set; }
        public SlimDX.Direct3D11.Device Device { get; set; }

        public void Load(Stream stream)
        {
            bmpImage = new BitmapImage();
            bmpImage.BeginInit();
            bmpImage.StreamSource = stream;
            bmpImage.CacheOption = BitmapCacheOption.OnLoad;
            bmpImage.EndInit();
            bmpImage.Freeze();

            Description = new SlimDX.Direct3D11.Texture2DDescription()
            {
                ArraySize = 1,
                BindFlags = SlimDX.Direct3D11.BindFlags.ShaderResource,
                CpuAccessFlags = SlimDX.Direct3D11.CpuAccessFlags.Write,
                Format = SlimDX.DXGI.Format.B8G8R8A8_UNorm,
                MipLevels = 1,
                OptionFlags = SlimDX.Direct3D11.ResourceOptionFlags.None,
                Usage = SlimDX.Direct3D11.ResourceUsage.Dynamic,
                Width = bmpImage.PixelWidth,
                Height = bmpImage.PixelHeight,
                SampleDescription = new SlimDX.DXGI.SampleDescription(1, 0)
            };
        }

        public void Decode()
        {
            int bytesPerPixel = (bmpImage.Format.BitsPerPixel + 7) / 8;
            stride = 4 * ((bmpImage.PixelWidth * bytesPerPixel + 3) / 4);
        }

        public void CopyResource(FeralTic.DX11.Resources.DX11ResourceTexture2D texture)
        {
            SlimDX.Direct3D11.DeviceContext ctx = texture.Context.CurrentDeviceContext;
            SlimDX.DataBox db = ctx.MapSubresource(texture.WritableResource, 0, SlimDX.Direct3D11.MapMode.WriteDiscard, SlimDX.Direct3D11.MapFlags.None);
            bmpImage.CopyPixels(Int32Rect.Empty, db.Data.DataPointer, stride * bmpImage.PixelHeight, stride);
            ctx.UnmapSubresource(texture.WritableResource, 0);
        }

        public void Dispose()
        {
            bmpImage = null;
        }
    }
}
