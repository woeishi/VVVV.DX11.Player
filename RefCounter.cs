using System;
using System.Threading;

namespace VVVV.DX11.ImagePlayer
{
    public class RefCounter
    {
        public class Handle : IDisposable
        {
            RefCounter rc;
            internal Handle(RefCounter refCounter)
            {
                rc = refCounter;
                Interlocked.Increment(ref rc.count);
            }
            Handle() { }

            public void Dispose() => Interlocked.Decrement(ref rc.count);
        }

        int count = 0;
        public IDisposable Use() => new Handle(this);

        public bool Free => count <= 0;
    }
}
