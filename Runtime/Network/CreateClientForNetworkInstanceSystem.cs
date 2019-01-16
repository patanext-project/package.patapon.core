using package.stormiumteam.networking.runtime.highlevel;
using Unity.Entities;

namespace Patapon4TLB.Core.Networking
{
    public class CreateClientForNetworkInstanceSystem : ComponentSystem
    {
        private EntityArchetype m_ClientArchetype;
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            
        }

        protected override void OnUpdate()
        {
            var length = m_Group.CalculateLength();
            var entityArray = m_Group.GetEntityArray();
            for (var i = 0; i != length; i++)
            {
                PostUpdateCommands.CreateEntity();
            }
        }
    }
}