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
			[NativeDisableParallelForRestriction]
			public NativeList<PlayerConnectedRpc> PreMadeEvents;

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

				PreMadeEvents.Add(new PlayerConnectedRpc
				{
					ServerId = networkId.Value
				});
			}
		}

		[RequireComponentTag(typeof(NetworkStreamInGame))]
		private struct SendRpcToConnectionsJob : IJobForEachWithEntity<NetworkStreamConnection>
		{
			[NativeDisableParallelForRestriction]
			public NativeList<PlayerConnectedRpc> PreMadeEvents;

			[NativeDisableParallelForRestriction]
			public BufferFromEntity<OutgoingRpcDataStreamBufferComponent> OutgoingDataFromEntity;

			public RpcQueue<PlayerConnectedRpc> RpcQueue;

			public void Execute(Entity entity, int jobIndex, ref NetworkStreamConnection connection)
			{
				for (var ev = 0; ev != PreMadeEvents.Length; ev++)
				{
					RpcQueue.Schedule(OutgoingDataFromEntity[entity], PreMadeEvents[ev]);
				}
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
			var preMadeEvents = new NativeList<PlayerConnectedRpc>(Allocator.TempJob);
			inputDeps = new CreateJob
			{
				PreMadeEvents = preMadeEvents,

				CommandBuffer       = m_Barrier.CreateCommandBuffer().ToConcurrent(),
				NetworkIdFromEntity = GetComponentDataFromEntity<NetworkIdComponent>()
			}.Schedule(this, inputDeps);
			inputDeps = new SendRpcToConnectionsJob
			{
				PreMadeEvents          = preMadeEvents,
				OutgoingDataFromEntity = GetBufferFromEntity<OutgoingRpcDataStreamBufferComponent>(),
				RpcQueue               = World.GetExistingSystem<P4ExperimentRpcSystem>().GetRpcQueue<PlayerConnectedRpc>()
			}.Schedule(this, inputDeps);
			inputDeps = preMadeEvents.Dispose(inputDeps); // dispose list after the end of jobs

			m_Barrier.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}