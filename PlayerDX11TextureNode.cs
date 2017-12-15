#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

using VVVV.Core.Logging;

using FeralTic.DX11;
using FeralTic.DX11.Resources;

using VVVV.DX11.ImagePlayer;
#endregion usings

namespace VVVV.DX11
{
	#region PluginInfo
	[PluginInfo(Name = "Player", 
                Category = "DX11.Texture", 
                Version = "2d", 
                Help = "Optimized playback of image stacks", 
                Tags = "",
                Credits = "sponsored by http://meso.net", 
                Author = "woei")]
	#endregion PluginInfo
	public class PlayerDX11Node : IPluginEvaluate, IPartImportsSatisfiedNotification, IDX11ResourceHost
	{
		#region fields & pins
		[Input("Directory", StringType = StringType.Directory)]
		public ISpread<string> FDirectory;

        [Input("Filemask", DefaultString = "*.*")]
        public ISpread<string> FFileMask;

        [Input("Reload", IsBang = true)]
        public ISpread<bool> FReload;

        [Input("IO Buffer Size", DefaultValue = 32768, MinValue = 128, Visibility = PinVisibility.OnlyInspector)]
        public ISpread<int> FBufferSize;

        [Input("Preload Frames")]
		public ISpread<ISpread<int>> FPreloadFrames;
		
		[Input("Visible Frame Indices", BinSize = 1)]
		public ISpread<ISpread<int>> FVisibleFrameId;

		[Output("Texture Out")]
        public ISpread<DX11Resource<DX11ResourceTexture2D>> FTextureOutput;

        [Output("Width")]
        public ISpread<int> FWidth;

        [Output("Height")]
        public ISpread<int> FHeight;

        [Output("Frame Loaded")]
        public ISpread<bool> FLoaded;

        [Output("Duration IO")]
        public ISpread<double> FReadTime;

        [Output("Duration Decode")]
        public ISpread<double> FDecodeTime;

        [Output("Duration Texture")]
        public ISpread<double> FGPUTime;

        [Output("Frame Count")]
        public ISpread<int> FFrameCount;

        [Output("Loaded")]
        public ISpread<ISpread<bool>> FPreloaded;

		[Import()]
		public ILogger FLogger;
		
		private Spread<Player> FPlayers = new Spread<Player>(0);
        private readonly MemoryPool FMemoryPool = new MemoryPool();
        #endregion fields & pins

        public void OnImportsSatisfied()
		{
			FTextureOutput.SliceCount = 0;
            FPreloaded.SliceCount = 0;
		}
		
		//called when data for any output pin is requested
		public void Evaluate(int spreadMax)
		{
            spreadMax = FDirectory.SliceCount
                .CombineWith(FFileMask)
                .CombineWith(FReload)
                .CombineWith(FBufferSize)
                .CombineSpreads(FPreloadFrames.SliceCount)
                .CombineSpreads(FVisibleFrameId.SliceCount);
                
			
			FPlayers.ResizeAndDispose(spreadMax, (int slice) => new Player(FDirectory[slice], FFileMask[slice], FMemoryPool, FLogger));
            FFrameCount.SliceCount = spreadMax;
            FPreloaded.SliceCount = spreadMax;

            int texSliceCount = spreadMax.CombineSpreads(SpreadUtils.SpreadMax(FVisibleFrameId as System.Collections.Generic.IEnumerable<ISpread<int>>));

            FTextureOutput.Resize(texSliceCount, () => new DX11Resource<DX11ResourceTexture2D>(), (t) => t.Dispose());
            FWidth.SliceCount = texSliceCount;
            FHeight.SliceCount = texSliceCount;
            FLoaded.SliceCount = texSliceCount;
            
			FReadTime.SliceCount = texSliceCount;
            FDecodeTime.SliceCount = texSliceCount;
			FGPUTime.SliceCount = texSliceCount;
			
			for (int i=0; i<spreadMax; i++)
			{
				if (FPlayers[i].DirectoryName != FDirectory[i] || FPlayers[i].FileMask != FFileMask[i] || FReload[i])
				{
					FPlayers[i].Dispose();
					FPlayers[i] = new Player(FDirectory[i], FFileMask[i], FMemoryPool, FLogger);
				}
                FPlayers[i].BufferSize = FBufferSize[i];
                if (FPlayers[i].FrameCount > 0)
				    FPlayers[i].Preload(FPreloadFrames[i]);

                FFrameCount[i] = FPlayers[i].FrameCount;
				FPreloaded[i].AssignFrom(FPlayers[i].Loaded);
            }
		}
		
		public void Update(DX11RenderContext context)
        {
            int i = 0;
            for (int b = 0; b < FVisibleFrameId.SliceCount; b++)
            {
                
                for (int s = 0; s < FVisibleFrameId[b].SliceCount; s++)
                {
                    if (FPlayers[b].FrameCount > 0)
                    {
                        try
                        {
                            var frame = FPlayers[b].GetFrame(FVisibleFrameId[b][s]);
                            FTextureOutput[i][context] = FPlayers[b].SetTexture(frame, FTextureOutput[i][context], context);

                            FWidth[i] = frame.Description.Width;
                            FHeight[i] = frame.Description.Height;
                            FLoaded[i] = frame.Loaded;

                            FReadTime[i] = frame.ReadTime;
                            FDecodeTime[i] = frame.DecodeTime;
                            FGPUTime[i] = frame.CopyTime;
                        }
                        catch (Exception e)
                        {
                            FLogger.Log(e);
                        }
                        
                    }
                    else
                    {
                        FWidth[i] = 0;
                        FHeight[i] = 0;
                        FLoaded[i] = false;

                        FReadTime[i] = 0;
                        FDecodeTime[i] = 0;
                        FGPUTime[i] = 0;
                    }
                    i++;
                }
            }
        }
		
		public void Destroy(DX11RenderContext context, bool force)
        {
            this.FTextureOutput[0].Dispose(context);
        }
	}
}
