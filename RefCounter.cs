using System;

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
                rc.count++;
            }
            private Handle()
            { }
            public void Dispose() => rc.count--;
        }

        int count = 0;
        public IDisposable Use() => new Handle(this);

        public bool Free => count <= 0;
    }
}
