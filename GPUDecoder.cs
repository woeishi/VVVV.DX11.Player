using System;
using System.IO;
using SlimDX.Direct3D11;


namespace VVVV.DX11.ImagePlayer
{
    class GPUDecoder : IDecoder
    {
        ImageLoadInformation ilf;
        Texture2D tex;
        public Device Device { get; set; }

        public int Width { get { return tex.Description.Width; } }
        public int Height { get { return tex.Description.Height; } }
        public SlimDX.DXGI.Format Format { get { return tex.Description.Format;  } }
        public Texture2DDescription Description { get; private set; }

        public GPUDecoder()
        {
            ilf = ImageLoadInformation.FromDefaults();
            ilf.BindFlags = BindFlags.None;
            ilf.CpuAccessFlags = CpuAccessFlags.Read;
            ilf.FirstMipLevel = 0;
            ilf.MipLevels = 1;
            ilf.Usage = ResourceUsage.Staging;
        }
        
        public void Load(Stream stream)
        {
            tex = Texture2D.FromStream(Device, stream, (int)stream.Length, ilf);
            
            Description = new Texture2DDescription()
            {
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.Write,
                Format = tex.Description.Format,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Dynamic,
                Width = tex.Description.Width,
                Height = tex.Description.Height,
                SampleDescription = new SlimDX.DXGI.SampleDescription(1, 0)
            };
        }

        public void Decode() { }

        public void CopyResource(FeralTic.DX11.Resources.DX11ResourceTexture2D texture)
        {
            texture.Context.CurrentDeviceContext.CopyResource(tex, texture.WritableResource);
        }

        public void Dispose()
        {
            if (tex != null)
                tex.Dispose();
        }
    }
}
