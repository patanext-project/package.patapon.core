using P4TLB.MasterServer;
using Unity.Entities;
using UnityEngine;

namespace Patapon4TLB.Core.MasterServer
{
	public class MasterServerManageFormationRequestSystem : BaseSystemMasterServerService
	{
		private MasterServerRequestModule<RequestGetUserFormationData, RequestGetUserFormationData.Processing, ResultGetUserFormationData> m_RequestUserLoginModule;

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
			foreach (var (entity, request) in requestArray)
			{
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

				if (result.Error != GetFormationOfPlayerResult.Types.ErrorCode.Success)
				{
					var requestMut = request;
					requestMut.ErrorCode = (int) result.Error;
					EntityManager.SetComponentData(entity, requestMut);
				}
				else
				{
					EntityManager.RemoveComponent<RequestGetUserFormationData>(entity);
					EntityManager.AddComponentObject(entity, new ResultGetUserFormationData
					{
						Root = result.Result
					});
				}
			}
		}
	}
}