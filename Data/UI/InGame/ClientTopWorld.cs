using Unity.Entities;
using Unity.NetCode;

namespace Patapon4TLB.UI.InGame
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class ClientTopWorld : ComponentSystem
	{
		private World m_World;

		protected override void OnUpdate()
		{

		}

		public World GetWorld()
		{
			return m_World;
		}

		internal void SetWorld(World world)
		{
			m_World = world ?? TopWorldSystem.InternalTopWorld;
		}
	}

	[UpdateBefore(typeof(TickClientPresentationSystem))]
	public class TopWorldSystem : ComponentSystem
	{
		internal static World InternalTopWorld;

		protected override void OnCreate()
		{
			base.OnCreate();

			InternalTopWorld = World;
		}

		protected override void OnUpdate()
		{
			if (ClientServerBootstrap.clientWorld == null)
				return;
			
			foreach (var clientWorld in ClientServerBootstrap.clientWorld)
			{
				clientWorld.GetOrCreateSystem<ClientTopWorld>().SetWorld(World);
			}
		}
	}
}