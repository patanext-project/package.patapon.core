using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Patapon4TLB.Core
{
	public struct ClientLoadedRpc : IRpcCommand
	{
		public void Execute(Entity connection, EntityCommandBuffer.Concurrent commandBuffer, int jobIndex)
		{
			commandBuffer.AddComponent(jobIndex, connection, new NetworkStreamInGame());

			var gamePlayerCreate = commandBuffer.CreateEntity(jobIndex);
			commandBuffer.AddComponent(jobIndex, gamePlayerCreate, new CreateGamePlayer
			{
				Connection = connection
			});
		}

		public void Serialize(DataStreamWriter writer)
		{

		}

		public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{

		}
	}

	public struct CreateGamePlayer : IComponentData
	{
		public Entity Connection;
	}
	
	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	[UpdateAfter(typeof(NetworkStreamReceiveSystem))]
	public class CreateGamePlayerSystem : JobComponentSystem
	{
		private struct CreateJob : IJobForEachWithEntity<CreateGamePlayer>
		{
			public EntityCommandBuffer.Concurrent CommandBuffer;

			[ReadOnly]
			public ComponentDataFromEntity<NetworkIdComponent> NetworkIdFromEntity;
			
			public void Execute(Entity entity, int jobIndex, ref CreateGamePlayer create)
			{
				CommandBuffer.DestroyEntity(jobIndex, entity);

				var networkId = NetworkIdFromEntity[create.Connection];

				var geEnt = CommandBuffer.CreateEntity(jobIndex);
				CommandBuffer.AddComponent(jobIndex, geEnt, new GamePlayer(0, false) {ServerId = networkId.Value});
				CommandBuffer.AddComponent(jobIndex, geEnt, new GamePlayerReadyTag());
				CommandBuffer.AddComponent(jobIndex, geEnt, new GhostComponent());
			}
		}

		private BeginSimulationEntityCommandBufferSystem m_Barrier;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Barrier = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps = new CreateJob
			{
				CommandBuffer       = m_Barrier.CreateCommandBuffer().ToConcurrent(),
				NetworkIdFromEntity = GetComponentDataFromEntity<NetworkIdComponent>()
			}.Schedule(this, inputDeps);
			m_Barrier.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}