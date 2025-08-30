namespace FrostHelper.Helpers;

internal static class GraphicsDeviceExt {
    public static PrevRenderTargetsHolder StoreRenderTargets(this GraphicsDevice graphicsDevice) {
        return new(graphicsDevice);
    }
    
    public struct PrevRenderTargetsHolder : IDisposable {
        private static readonly RenderTargetBinding[]?[] Pool = [
            [],
            new RenderTargetBinding[1],
            new RenderTargetBinding[2],
        ];
        
        private readonly GraphicsDevice _graphicsDevice;
        private RenderTargetBinding[]? _targets;
        
        public PrevRenderTargetsHolder(GraphicsDevice gd) {
            _graphicsDevice = gd;
            
            var amt = gd.GetRenderTargetsNoAllocEXT(null);
            var pool = Pool;
            if (amt < pool.Length && pool[amt] is {} available) {
                _targets = available;
                pool[amt] = null;
            } else {
                _targets = new RenderTargetBinding[amt];
            }
            gd.GetRenderTargetsNoAllocEXT(_targets);
        }

        public void Dispose() {
            if (_targets is null)
                return;
            
            _graphicsDevice.SetRenderTargets(_targets);
            var amt = _targets.Length;
            if (amt < Pool.Length)
                Pool[amt] ??= _targets;
            _targets = null;
        }
        
        public void Restore() => Dispose();
    }
}