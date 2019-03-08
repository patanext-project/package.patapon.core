using Unity.Entities;

namespace package.patapon.core
{
    public class PlayUpdateOrder
    {
        [UpdateInGroup(typeof(PresentationSystemGroup))]
        public class RhythmEngineOrder : ComponentSystemGroup
        {
            
        }
    }
}