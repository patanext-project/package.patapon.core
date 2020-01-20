using EcsComponents.MasterServer;
using P4TLB.MasterServer;
using package.stormiumteam.shared.ecs;
using Patapon4TLB.Core.MasterServer.Data;
using Patapon4TLB.Core.MasterServer.Implementations;
using Patapon4TLB.Core.MasterServer.P4;
using Patapon4TLB.Core.MasterServer.P4.EntityDescription;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Patapon4TLB.Core.MasterServer
{
	[UpdateAfter(typeof(MasterServerManageUnitSystem))]
	public class MasterServerManageUnitKitSystem : BaseSystemMasterServerService
	{
		private BaseMsAutoRequestModule m_AutomaticGetUnitKitModule;

		private MsRequestModule<RequestGetUnitKit, RequestGetUnitKit.Processing, ResponseGetUnitKit, RequestGetUnitKit.CompletionStatus> m_GetUnitKitModule;
		private MsRequestModule<RequestSetUnitKit, RequestSetUnitKit.Processing, ResponseSetUnitKit, RequestSetUnitKit.CompletionStatus> m_SetUnitKitModule;

		private MasterServerManageUnitSystem m_UnitSystem;

		protected override void OnCreate()
		{
			base.OnCreate();

			GetModule(out m_GetUnitKitModule);
			GetModule(out m_SetUnitKitModule);

			m_AutomaticGetUnitKitModule = MsAutomatedRequestModule.From(default(RequestGetUnitKit.Automatic), m_GetUnitKitModule);
			m_AutomaticGetUnitKitModule.SetPushComponents(typeof(RequestGetUnitKit.PushRequest), typeof(MasterServerGlobalUnitPush));

			m_UnitSystem = World.GetOrCreateSystem<MasterServerManageUnitSystem>();
		}

		protected override async void OnUpdate()
		{
			if (!StaticMasterServer.TryGetClient<P4UnitKitService.P4UnitKitServiceClient>(out var service))
				return;

			Entities.ForEach((ref RequestGetUnitKit.Automatic automatic, ref MasterServerP4UnitMasterServerEntity masterServerEntity) => { automatic.UnitId = masterServerEntity.UnitId; });

			if (m_UnitSystem.Match.IsCreated)
			{
				foreach (var entityMatch in m_UnitSystem.Match)
				{
					if (EntityManager.HasComponent<RequestGetUnitKit.Automatic>(entityMatch))
						m_AutomaticGetUnitKitModule.AddRequest(entityMatch);
				}
			}

			m_AutomaticGetUnitKitModule.Update();

			m_GetUnitKitModule.Update();
			m_GetUnitKitModule.AddProcessTagToAllRequests();
			foreach (var kvp in m_GetUnitKitModule.GetRequests())
			{
				var (entity, request) = kvp;
				// If there is a MasterServer target, use it.
				if (EntityManager.TryGetComponentData(entity, out MasterServerP4UnitMasterServerEntity masterServerEntity))
					request.UnitId = masterServerEntity.UnitId;
				
				Debug.Log($"Requesting for {request.UnitId} ");

				var response = await service.GetCurrentKitAsync(new GetKitRequest {UnitId = request.UnitId});
				if (m_GetUnitKitModule.InvokeDefaultOnResult(entity, new RequestGetUnitKit.CompletionStatus {ErrorCode = response.Error}, out var targetResponseEntity))
				{
					EntityManager.SetComponentData(targetResponseEntity, new ResponseGetUnitKit
					{
						KitId           = (P4OfficialKit) response.KitId,
						KitCustomNameId = response.KitCustomNameId
					});
				}
			}

			// can't go further if we aren't connected
			if (!HasSingleton<ConnectedMasterServerClient>())
				return;
			var client = GetSingleton<ConnectedMasterServerClient>();

			m_SetUnitKitModule.Update();
			m_SetUnitKitModule.AddProcessTagToAllRequests();
			foreach (var kvp in m_SetUnitKitModule.GetRequests())
			{
				var (entity, request) = kvp;
				// If there is a MasterServer target, use it.
				if (EntityManager.TryGetComponentData(entity, out MasterServerP4UnitMasterServerEntity masterServerEntity))
					request.UnitId = masterServerEntity.UnitId;

				var response = await service.SetCurrentKitAsync(new SetKitRequest
				{
					ClientToken     = client.Token.ToString(),
					UnitId          = request.UnitId,
					KitId           = (uint) request.KitId,
					KitCustomNameId = request.KitCustomNameId.ToString()
				});
				if (m_SetUnitKitModule.InvokeDefaultOnResult(entity, new RequestSetUnitKit.CompletionStatus {ErrorCode = response.Error}, out _))
				{
					// until there are more field into SetUnitKitResponse, it's useless to continue the execution...
				}
			}
		}
	}
}