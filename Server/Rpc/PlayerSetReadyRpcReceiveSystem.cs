using Patapon.Server.GameModes;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace Rpc
{
	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class PlayerSetReadyRpcReceiveSystem : AbsGameBaseSystem
	{
		private EndSimulationEntityCommandBufferSystem m_EndBarrier;

		protected override void OnCreate()
		{
			base.OnCreate();
			m_EndBarrier = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
		}

		protected override void OnUpdate()
		{
			var readyFromEntity         = GetComponentDataFromEntity<PreMatchPlayerIsReady>(true);
			var commandTargetFromEntity = GetComponentDataFromEntity<CommandTargetComponent>(true);
			var ecb = m_EndBarrier.CreateCommandBuffer()
			                      .ToConcurrent();
			Entities
				.ForEach((Entity entity, int nativeThreadIndex, in PlayerSetReadyRpc rpc, in ReceiveRpcCommandRequestComponent receive) =>
				{
					var commandTarget = commandTargetFromEntity[receive.SourceConnection];
					if (commandTarget.targetEntity == default)
						return;

					if (readyFromEntity.Exists(commandTarget.targetEntity) && !rpc.Value)
						ecb.RemoveComponent<PreMatchPlayerIsReady>(nativeThreadIndex, commandTarget.targetEntity);
					else if (!readyFromEntity.Exists(commandTarget.targetEntity) && rpc.Value)
						ecb.AddComponent<PreMatchPlayerIsReady>(nativeThreadIndex, commandTarget.targetEntity);

					ecb.DestroyEntity(nativeThreadIndex, entity);
				})
				.WithReadOnly(readyFromEntity)
				.WithReadOnly(commandTargetFromEntity)
				.Schedule();

			m_EndBarrier.AddJobHandleForProducer(Dependency);
		}
	}
}