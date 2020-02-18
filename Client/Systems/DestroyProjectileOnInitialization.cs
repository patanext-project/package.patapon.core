using Revolution;
using StormiumTeam.GameBase;
using Unity.Entities;

namespace Patapon.Client.Systems
{
	[UpdateInGroup(typeof(AfterSnapshotIsAppliedSystemGroup))]
	public class DestroyProjectileOnInitialization : ComponentSystem
	{
		private BeginInitializationEntityCommandBufferSystem m_InitializationBarrier;

		protected override void OnCreate()
		{
			base.OnCreate();
			m_InitializationBarrier = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
		}

		protected override void OnUpdate()
		{
			Entities.WithNone<ManualDestroy>().WithAll<ProjectileDescription>().ForEach((Entity ent) => { EntityManager.AddComponent(ent, typeof(ManualDestroy)); });

			var ecb = m_InitializationBarrier.CreateCommandBuffer();
			Entities.WithAll<ManualDestroy, ProjectileDescription, IsDestroyedOnSnapshot>().ForEach((Entity ent) => { ecb.DestroyEntity(ent); });
		}
	}
}