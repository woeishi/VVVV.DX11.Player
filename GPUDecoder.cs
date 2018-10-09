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
        public ShaderResourceView SRV { get; private set; }

        public GPUDecoder()
        {
            ilf = ImageLoadInformation.FromDefaults();
            ilf.BindFlags = BindFlags.ShaderResource;
            ilf.CpuAccessFlags = CpuAccessFlags.None;
            ilf.FirstMipLevel = 0;
            ilf.MipLevels = 1;
            ilf.Usage = ResourceUsage.Default;
        }
        
        public void Load(string filename)
        {
            tex = Texture2D.FromFile(Device, filename, ilf);
            Description = tex.Description;
            SRV = new ShaderResourceView(Device, tex);
        }

        public void Dispose()
        {
            tex?.Dispose();
            SRV?.Dispose();
        }
    }
}
