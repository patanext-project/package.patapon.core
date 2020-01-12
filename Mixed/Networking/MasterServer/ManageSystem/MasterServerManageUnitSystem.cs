using System.Threading.Tasks;
using EcsComponents.MasterServer;
using Grpc.Core;
using P4TLB.MasterServer;
using package.stormiumteam.shared.ecs;
using Patapon4TLB.Core.MasterServer.P4.EntityDescription;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Patapon4TLB.Core.MasterServer
{
	public class MasterServerManageUnitSystem : BaseSystemMasterServerService
	{
		private MasterServerManagePendingEventSystem m_PendingSystem;
		private MasterServerGetEntityWithDescriptionModule<MasterServerP4UnitMasterServerEntity> m_GetEntityModule;

		private EntityQuery m_OnUpdateQuery;
		
		public NativeList<Entity> Match;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_PendingSystem = World.GetOrCreateSystem<MasterServerManagePendingEventSystem>();
			GetModule(out m_GetEntityModule);

			m_OnUpdateQuery = GetEntityQuery(typeof(MasterServerOnUnitUpdate));
		}
		
		private AsyncUnaryCall<UnitServiceGetPendingEventResponse> m_LastPendingEventTask;

		protected override void OnUpdate()
		{
			Match = default;
			if (!m_OnUpdateQuery.IsEmptyIgnoreFilter)
				EntityManager.DestroyEntity(m_OnUpdateQuery);
			
			if (!StaticMasterServer.TryGetClient<P4UnitService.P4UnitServiceClient>(out var service))
				return;

			if (m_LastPendingEventTask != null && m_LastPendingEventTask.GetAwaiter().IsCompleted)
			{
				var response = m_LastPendingEventTask.ResponseAsync.Result;
				if (response.Units.Count > 0)
				{
					var updateEntity = EntityManager.CreateEntity(typeof(MasterServerOnUnitUpdate));
					var buffer       = EntityManager.GetBuffer<MasterServerOnUnitUpdate>(updateEntity);
					foreach (var unit in response.Units)
					{
						buffer.Add(new MasterServerOnUnitUpdate {UnitId = unit.UnitId});
					}

					m_GetEntityModule.Update();
					Match = m_GetEntityModule.GetMatch(buffer.Reinterpret<MasterServerP4UnitMasterServerEntity>().AsNativeArray());
				}
				
				m_LastPendingEventTask = null;
			}

			if (m_PendingSystem.IsPending(nameof(P4PlayerEvents.OnUnitUpdate)) && m_LastPendingEventTask == null)
			{
				// Be sure to delete the event before calling it!
				m_PendingSystem.DeleteEvent(nameof(P4PlayerEvents.OnUnitUpdate));

				var client = GetSingleton<ConnectedMasterServerClient>();
				m_LastPendingEventTask = service.GetPendingEventsAsync(new UnitServiceGetPendingEventRequest {ClientToken = client.Token.ToString()});
			}
		}
	}

	public struct MasterServerOnUnitUpdate : IBufferElementData
	{
		public ulong UnitId;
	}
}