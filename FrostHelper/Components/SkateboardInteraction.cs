using Monocle;
using System;

namespace FrostHelper
{
    using SkateboardInteractionCallback = Action<Entity, Skateboard>;
    public class SkateboardInteraction : Component
    {
        public SkateboardInteractionCallback Callback;
        public SkateboardInteraction(SkateboardInteractionCallback callback) : base(false, false) 
        { 
        
        }

        public void DoInteraction(Entity other, Skateboard skateboard)
        {
            Callback?.Invoke(other, skateboard);
        }
    }
}
