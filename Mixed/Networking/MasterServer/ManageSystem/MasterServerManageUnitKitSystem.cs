using P4TLB.MasterServer;
using package.stormiumteam.shared.ecs;
using Patapon4TLB.Core.MasterServer.Implementations;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Patapon4TLB.Core.MasterServer
{
	public class MasterServerManageUnitKitSystem : BaseSystemMasterServerService
	{
		private MasterServerRequestModule<RequestGetUnitKit, RequestGetUnitKit.Processing, ResponseGetUnitKit> m_GetUnitKitModule;
		private MasterServerRequestModule<RequestSetUnitKit, RequestSetUnitKit.Processing, ResponseSetUnitKit> m_SetUnitKitModule;

		protected override void OnCreate()
		{
			base.OnCreate();

			GetModule(out m_GetUnitKitModule);
			GetModule(out m_SetUnitKitModule);
		}

		protected override async void OnUpdate()
		{
			if (!StaticMasterServer.TryGetClient<P4UnitKitService.P4UnitKitServiceClient>(out var service))
				return;

			m_GetUnitKitModule.Update();
			m_GetUnitKitModule.AddProcessTagToAllRequests();
			foreach (var kvp in m_GetUnitKitModule.GetRequests())
			{
				var (entity, request) = kvp;
				var response = await service.GetCurrentKitAsync(new GetKitRequest {UnitId = request.UnitId});
				request.ErrorCode = response.Error;
				if (request.error)
				{
					EntityManager.SetComponentData(entity, request);
				}
				else
				{
					EntityManager.RemoveComponent<RequestGetUnitKit>(entity);
					EntityManager.SetOrAddComponentData(entity, new ResponseGetUnitKit
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
				var response = await service.SetCurrentKitAsync(new SetKitRequest
				{
					ClientToken     = client.Token.ToString(),
					UnitId          = request.UnitId,
					KitId           = (uint) request.KitId,
					KitCustomNameId = request.KitCustomNameId.ToString()
				});
				request.ErrorCode = response.Error;
				if (request.error)
				{
					EntityManager.SetComponentData(entity, request);
				}
				else
				{
					EntityManager.RemoveComponent<RequestSetUnitKit>(entity);
					EntityManager.SetOrAddComponentData(entity, new ResponseSetUnitKit());
				}
			}
		}
	}

	public struct RequestGetUnitKit : IComponentData, IMasterServerRequest
	{
		public bool error => ErrorCode != GetKitResponse.Types.ErrorCode.Ok;

		public GetKitResponse.Types.ErrorCode ErrorCode;

		public ulong UnitId;

		public struct Processing : IComponentData
		{
		}
	}

	public struct ResponseGetUnitKit : IComponentData
	{
		public P4OfficialKit   KitId;
		public NativeString512 KitCustomNameId;
	}

	public struct RequestSetUnitKit : IComponentData, IMasterServerRequest
	{
		public bool error => ErrorCode != SetKitResponse.Types.ErrorCode.Ok;

		public SetKitResponse.Types.ErrorCode ErrorCode;

		public ulong           UnitId;
		public P4OfficialKit   KitId;
		public NativeString512 KitCustomNameId;

		public struct Processing : IComponentData
		{
		}
	}

	public struct ResponseSetUnitKit : IComponentData
	{

	}
}