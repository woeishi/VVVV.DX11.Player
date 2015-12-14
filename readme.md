#Player (DX11.Texture 2d)

###VVVV node for high performance playback of image stacks in dx11

modeled after original [DX9 implementation](https://github.com/vvvv/vvvv-sdk/tree/develop/vvvv45/src/nodes/plugins/Texture/ImagePlayer) by [elias](https://github.com/azeno) 
developed for dx11-pack by mr vux, release version for vvvv beta33.7 

development for v.1 sponsored by [meso.net](http://meso.net)

* * *

####what it does:

* threaded reading image into RAM
* threaded decoding (extensible via IDecoder)
* swapping to GPU (and decoding there if necessary)