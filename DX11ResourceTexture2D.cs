using System;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using SlimDX;

namespace FeralTic.DX11.Resources
{
	public class DX11ResourceTexture2D : DX11Texture2D
	{
		public string Meta;
		public DX11ResourceTexture2D()
		{
		 	this.desc = new Texture2DDescription()
	        {
	            ArraySize = 1,
	            BindFlags = BindFlags.ShaderResource,
	            CpuAccessFlags = CpuAccessFlags.Write,
	            MipLevels = 1,
	            OptionFlags = ResourceOptionFlags.None,
	        	SampleDescription= new SampleDescription(1,0),
	            Usage = ResourceUsage.Dynamic
	        };
		}

        public DX11RenderContext Context
        {
            get { return this.context; }
            set { this.context = value; }
        }

        public Texture2D WritableResource
		{
			get { return this.Resource; }
			set { SetResource(value); }
		}

		public DX11ResourceTexture2D(DX11RenderContext context):base()
		{
			this.context = context;
		}
	
		public void SetResource(Texture2D texture)
		{
			this.Resource = texture;
			this.desc = texture.Description;
	        this.SRV = new ShaderResourceView(context.Device, this.Resource);
		}
		
        public bool MatchesSizeByDescription(Texture2DDescription Description)
        {
            return (this.desc.Width == Description.Width) && (this.desc.Height == Description.Height) && (this.desc.Format == Description.Format);
        }
    }
}