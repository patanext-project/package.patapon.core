using System.Collections.Generic;
using package.stormiumteam.networking;
using Unity.Entities;

namespace Patapon4TLB.Core.Networking
{
    public interface IModelCreateEntityCallback
    {
        Entity SnapshotCreateEntity(Entity origin, StSnapshotRuntime snapshotRuntime);
    }
    
    public class EntityModelManager : ComponentSystem
    {
        private PatternBank m_PatternBank;
        private Dictionary<int, IModelCreateEntityCallback> m_Callbacks;

        protected override void OnStartRunning()
        {
            m_PatternBank = World.GetExistingManager<NetPatternSystem>().GetLocalBank();
        }

        protected override void OnUpdate()
        {
            
        }

        public void Register<TCaller>(string name, TCaller caller)
            where TCaller : class, IModelCreateEntityCallback
        {
            var pattern = m_PatternBank.Register(new PatternIdent(name));

            m_Callbacks[pattern.Id] = caller;
        }

        public void SendCall(PatternIdent patternIdent, Entity origin, StSnapshotRuntime snapshotRuntime)
        {
            var id = m_PatternBank.GetPatternResult(patternIdent).Id;
            
            m_Callbacks[id].SnapshotCreateEntity(origin, snapshotRuntime);
        }
    }
}