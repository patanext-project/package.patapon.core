using P4TLB.MasterServer;
using package.stormiumteam.shared.ecs;
using Unity.Entities;

namespace Patapon4TLB.Core.MasterServer
{
	public class MasterServerManageUnitSystem : BaseSystemMasterServerService
	{
		private MasterServerManagePendingEventSystem m_PendingSystem;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_PendingSystem = World.GetOrCreateSystem<MasterServerManagePendingEventSystem>();
		}

		protected override async void OnUpdate()
		{
			if (!StaticMasterServer.TryGetClient<P4UnitService.P4UnitServiceClient>(out var service))
				return;

			if (m_PendingSystem.IsPending(nameof(P4PlayerEvents.OnUnitUpdate)))
			{
				// Be sure to delete the event before calling it!
				m_PendingSystem.DeleteEvent(nameof(P4PlayerEvents.OnUnitUpdate));

				var client   = GetSingleton<ConnectedMasterServerClient>();
				var response = await service.GetPendingEventsAsync(new UnitServiceGetPendingEventRequest {ClientToken = client.Token.ToString()});
				if (response.Units.Count > 0)
				{
					var updateEntity = EntityManager.CreateEntity(typeof(MasterServerOnUnitUpdate));
					var buffer       = EntityManager.GetBuffer<MasterServerOnUnitUpdate>(updateEntity);
					foreach (var unit in response.Units)
					{
						buffer.Add(new MasterServerOnUnitUpdate {UnitId = unit.UnitId});
					}
				}
			}
		}
	}

	public struct MasterServerOnUnitUpdate : IBufferElementData
	{
		public ulong UnitId;
	}
}