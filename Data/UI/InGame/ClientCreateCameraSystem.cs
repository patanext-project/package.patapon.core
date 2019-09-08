using Unity.Entities;
using Revolution.NetCode;
using Unity.Transforms;

namespace Patapon4TLB.UI.InGame
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class ClientPresentationTransformSystemGroup : ComponentSystemGroup
	{
		private TransformSystemGroup m_Group;
		
		protected override void OnCreate()
		{
			base.OnCreate();
			m_Group = World.GetOrCreateSystem<TransformSystemGroup>();
		}

		protected override void OnUpdate()
		{
			EntityManager.CompleteAllJobs();
			m_Group.Update();
			
			base.OnUpdate();
		}
	}
}