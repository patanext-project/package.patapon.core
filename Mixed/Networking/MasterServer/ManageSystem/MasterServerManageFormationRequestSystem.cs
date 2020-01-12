using P4TLB.MasterServer;
using Unity.Entities;
using UnityEngine;

namespace Patapon4TLB.Core.MasterServer
{
	public class MasterServerManageFormationRequestSystem : BaseSystemMasterServerService
	{
		private MsRequestModule<RequestGetUserFormationData, RequestGetUserFormationData.Processing, ResultGetUserFormationData, RequestGetUserFormationData.CompletionStatus> m_RequestUserLoginModule;

		protected override void OnCreate()
		{
			base.OnCreate();

			GetModule(out m_RequestUserLoginModule);
		}

		protected override async void OnUpdate()
		{
			if (!StaticMasterServer.HasClient<P4ArmyFormationService.P4ArmyFormationServiceClient>())
				return;

			var client = StaticMasterServer.GetClient<P4ArmyFormationService.P4ArmyFormationServiceClient>();

			m_RequestUserLoginModule.Update();
			m_RequestUserLoginModule.AddProcessTagToAllRequests();

			var requestArray = m_RequestUserLoginModule.GetRequests();
			foreach (var kvp in requestArray)
			{
				var entity  = kvp.Entity;
				var request = kvp.Value;
				var rpc = new DataOfPlayerRequest
				{
					UserId    = request.UserId,
					UserLogin = request.UserLogin.ToString()
				};

				var result = await client.GetFormationOfPlayerAsync(rpc);
				if (!EntityManager.Exists(entity))
				{
					Debug.LogError($"You shouldn't destroy the '{nameof(RequestGetUserFormationData)}' entity.");
					continue;
				}

				if (m_RequestUserLoginModule.InvokeDefaultOnResult(entity, new RequestGetUserFormationData.CompletionStatus {ErrorCode = result.Error}, out var responseEntity))
				{
					var data = EntityManager.GetComponentData<ResultGetUserFormationData>(responseEntity);
					data.Root = result.Result;
				}
			}
		}
	}
}