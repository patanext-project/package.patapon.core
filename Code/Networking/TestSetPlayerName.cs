using System;
using package.stormiumteam.shared.ecs;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.NetCode;
using Random = UnityEngine.Random;

namespace P4.Core.Code.Networking
{
	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	public class TestSetPlayerName : GameBaseSystem
	{
		private EntityQuery             m_LocalPlayerWithoutNameQuery;
		private NetworkConnectionModule m_ConnectionModule;

		public string Name;

		protected override void OnCreate()
		{
			base.OnCreate();

			Name = "NN#" + Random.Range(0, 1001);

			m_LocalPlayerWithoutNameQuery = GetEntityQuery(new EntityQueryDesc
			{
				All  = new[] {ComponentType.ReadOnly<GamePlayerLocalTag>(), ComponentType.ReadOnly<GamePlayerReadyTag>()},
				None = new[] {ComponentType.ReadWrite<PlayerName>()}
			});
			GetModule(out m_ConnectionModule);
		}

		protected override void OnUpdate()
		{
			if (m_LocalPlayerWithoutNameQuery.CalculateEntityCount() == 0)
				return;

			var playerEntity = m_LocalPlayerWithoutNameQuery.GetSingletonEntity();
			if (Name.Length >= NativeString64.MaxLength)
			{
				Name = Name.Substring(0, NativeString64.MaxLength - 1);
			}

			m_ConnectionModule.Update(default);

			var rpcQueue     = World.GetOrCreateSystem<RpcQueueSystem<SetPlayerNameRpc>>().GetRpcQueue();
			var outgoingData = EntityManager.GetBuffer<OutgoingRpcDataStreamBufferComponent>(m_ConnectionModule.ConnectedEntities[0]);

			rpcQueue.Schedule(outgoingData, new SetPlayerNameRpc
			{
				Name = new NativeString64(Name)
			});

			EntityManager.SetOrAddComponentData(playerEntity, new PlayerName {Value = new NativeString64(Name)});
		}
	}
}