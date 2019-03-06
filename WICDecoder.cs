using System;
using SharpDX.WIC;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using System.Collections.Generic;

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
        
        public SlimDX.Direct3D11.Device Device { get; set; }
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
                var format = PixelToTextureFormat(frame.PixelFormat);
                bool knownFormat = format != Format.Unknown;

                var w = frame.Size.Width;
                var h = frame.Size.Height;
                stride = PixelFormat.GetStride(knownFormat ? frame.PixelFormat:PixelFormat.Format32bppRGBA, w);
                //stride = PixelFormat.GetStride(PixelFormat.Format32bppBGRA, w);
                FLength = stride * h;

                Description = new Texture2DDescription()
                {
                    ArraySize = 1,
                    BindFlags = BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.None,
                    Format = knownFormat?format:Format.R8G8B8A8_UNorm,
                    MipLevels = 1,
                    OptionFlags = ResourceOptionFlags.None,
                    Usage = ResourceUsage.Default,
                    Width = w,
                    Height = h,
                    SampleDescription = new SampleDescription(1, 0)
                };
                token.ThrowIfCancellationRequested();
                ptr = mp.UnmanagedPool.GetMemory(FLength);
                //if (frame.PixelFormat != PixelFormat.Format32bppBGRA)
                if (!knownFormat)
                {
                    using (var converter = new FormatConverter(imgF))
                    {
                        converter.Initialize(frame, PixelFormat.Format32bppRGBA);
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

        static Dictionary<Guid, Format> FPixelToTextureFormat = new Dictionary<Guid, Format>();

        static WICDecoder()
        {
            //https://github.com/sharpdx/Toolkit/blob/master/Source/Toolkit/SharpDX.Toolkit.Graphics/WICHelper.cs
            FPixelToTextureFormat.Add(PixelFormat.Format128bppRGBAFloat, Format.R32G32B32A32_Float);
            FPixelToTextureFormat.Add(PixelFormat.Format64bppRGBAHalf, Format.R16G16B16A16_Float);
            FPixelToTextureFormat.Add(PixelFormat.Format64bppRGBA, Format.R16G16B16A16_UNorm);
            FPixelToTextureFormat.Add(PixelFormat.Format32bppRGBA, Format.R8G8B8A8_UNorm);
            FPixelToTextureFormat.Add(PixelFormat.Format32bppBGRA, Format.B8G8R8A8_UNorm); // DXGI 1.1
            FPixelToTextureFormat.Add(PixelFormat.Format32bppBGR, Format.B8G8R8X8_UNorm); // DXGI 1.1
            FPixelToTextureFormat.Add(PixelFormat.Format32bppRGBA1010102XR, Format.R10G10B10_XR_Bias_A2_UNorm); // DXGI 1.1
            FPixelToTextureFormat.Add(PixelFormat.Format32bppRGBA1010102, Format.R10G10B10A2_UNorm);
            FPixelToTextureFormat.Add(PixelFormat.Format32bppRGBE, Format.R9G9B9E5_SharedExp);
            FPixelToTextureFormat.Add(PixelFormat.Format16bppBGRA5551, Format.B5G5R5A1_UNorm);
            FPixelToTextureFormat.Add(PixelFormat.Format16bppBGR565, Format.B5G6R5_UNorm);
            FPixelToTextureFormat.Add(PixelFormat.Format32bppGrayFloat, Format.R32_Float);
            FPixelToTextureFormat.Add(PixelFormat.Format16bppGrayHalf, Format.R16_Float);
            FPixelToTextureFormat.Add(PixelFormat.Format16bppGray, Format.R16_UNorm);
            FPixelToTextureFormat.Add(PixelFormat.Format8bppGray, Format.R8_UNorm);
            FPixelToTextureFormat.Add(PixelFormat.Format8bppAlpha, Format.A8_UNorm);
            FPixelToTextureFormat.Add(PixelFormat.FormatBlackWhite, Format.R1_UNorm);

            FPixelToTextureFormat.Add(PixelFormat.FormatDontCare, Format.Unknown);
        }

        static Format PixelToTextureFormat(Guid pixelFormat)
        {
            if (FPixelToTextureFormat.ContainsKey(pixelFormat))
                return FPixelToTextureFormat[pixelFormat];
            else
                return Format.Unknown;
        }
    }
}
