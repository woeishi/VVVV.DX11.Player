using System;
using SharpDX.WIC;
using SlimDX.Direct3D11;

namespace VVVV.DX11.ImagePlayer
{
    class WICDecoder : IDecoder
    {
        ImageLoadInformation ilf;
        MemoryPool mp;
        IntPtr ptr;
        SlimDX.DataStream ds;
        Texture2D tex;
        
        int FLength;
        
        public Device Device { get; set; }
        public Texture2DDescription Description { get; private set; }
        public ShaderResourceView SRV { get; private set; }

        public WICDecoder(MemoryPool mempool)
        {
            mp = mempool;
            ilf = ImageLoadInformation.FromDefaults();
            ilf.BindFlags = BindFlags.ShaderResource;
            ilf.CpuAccessFlags = CpuAccessFlags.None;
            ilf.FirstMipLevel = 0;
            ilf.MipLevels = 1;
            ilf.Usage = ResourceUsage.Default;
        }

        public void Load(string filename, System.Threading.CancellationToken token)
        {
            int stride;
            var imgF = new ImagingFactory();
            using (var decoder = new SharpDX.WIC.BitmapDecoder(imgF, filename, SharpDX.IO.NativeFileAccess.Read, DecodeOptions.CacheOnLoad))
            using (var frame = decoder.GetFrame(0))
            {
                var w = frame.Size.Width;
                var h = frame.Size.Height;
                stride = PixelFormat.GetStride(PixelFormat.Format32bppBGRA, w);
                FLength = stride * h;

                Description = new Texture2DDescription()
                {
                    ArraySize = 1,
                    BindFlags = BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.None,
                    Format = SlimDX.DXGI.Format.B8G8R8A8_UNorm,
                    MipLevels = 1,
                    OptionFlags = ResourceOptionFlags.None,
                    Usage = ResourceUsage.Default,
                    Width = w,
                    Height = h,
                    SampleDescription = new SlimDX.DXGI.SampleDescription(1, 0)
                };
                token.ThrowIfCancellationRequested();
                ptr = mp.UnmanagedPool.GetMemory(FLength);
                if (frame.PixelFormat != PixelFormat.Format32bppBGRA)
                {
                    using (var converter = new FormatConverter(imgF))
                    {
                        converter.Initialize(frame, PixelFormat.Format32bppBGRA);
                        converter.CopyPixels(stride, ptr, FLength);
                    }
                }
                else
                {
                    frame.CopyPixels(stride, ptr, FLength);
                }
            }
            token.ThrowIfCancellationRequested();
            ds = new SlimDX.DataStream(ptr, FLength, true, false);
            var dr = new SlimDX.DataRectangle(stride, ds);
            token.ThrowIfCancellationRequested();
            tex = new Texture2D(Device, Description, dr);
            token.ThrowIfCancellationRequested();
            SRV = new ShaderResourceView(Device, tex);
        }

        public void Dispose()
        {
            if (ptr != null)
                mp.UnmanagedPool.PutMemory(ptr, FLength);
            ds?.Dispose();
            tex?.Dispose();
            SRV?.Dispose();
        }
    }
}
