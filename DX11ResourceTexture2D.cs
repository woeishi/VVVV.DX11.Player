using System;
using SlimDX.Direct3D11;
using SlimDX.DXGI;

namespace FeralTic.DX11.Resources
{
	public class DX11ResourceTexture2D : DX11Texture2D
	{
		public string Meta;
        IDisposable Handle;
		public DX11ResourceTexture2D()
		{
		 	this.desc = new Texture2DDescription()
	        {
	            ArraySize = 1,
	            BindFlags = BindFlags.ShaderResource,
	            CpuAccessFlags = CpuAccessFlags.None,
	            MipLevels = 1,
	            OptionFlags = ResourceOptionFlags.None,
	        	SampleDescription= new SampleDescription(1,0),
	            Usage = ResourceUsage.Default
	        };
		}

        public DX11RenderContext Context
        {
            get { return this.context; }
            set { this.context = value; }
        }

		public DX11ResourceTexture2D(DX11RenderContext context):base()
		{
			this.context = context;
		}
	
        public void SetBySRV(ShaderResourceView srv, IDisposable handle)
        {
            var _SRV = this.SRV; //just in case GC wants to cleanup on overwrite
            var _R = this.Resource;
            this.SRV = srv;
            this.Resource = (Texture2D)srv.Resource;
            this.desc = this.Resource.Description;
            Handle?.Dispose();
            Handle = handle;
        }
		
        public bool MatchesSizeByDescription(Texture2DDescription Description)
        {
            return (this.desc.Width == Description.Width) && (this.desc.Height == Description.Height) && (this.desc.Format == Description.Format);
        }

        public new void Dispose()
        {
            Handle?.Dispose();
            base.Dispose();
        }
    }
}