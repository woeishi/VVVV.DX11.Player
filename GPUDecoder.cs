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
        
        public void Load(string filename, System.Threading.CancellationToken token)
        {
            tex = Texture2D.FromFile(Device, filename, ilf);
            Description = tex.Description;
            token.ThrowIfCancellationRequested();
            SRV = new ShaderResourceView(Device, tex);
        }

        public void Dispose()
        {
            tex?.Dispose();
            SRV?.Dispose();
        }
    }
}
