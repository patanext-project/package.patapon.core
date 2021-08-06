using Cysharp.Threading.Tasks;
using GameHost.Core;
using Unity.Entities;

namespace PataNext.Client.Systems
{
	public class TestGetInventory : SystemBase
	{
		private GameHostConnector gameHostConnector;

		protected override void OnCreate()
		{
			base.OnCreate();

			gameHostConnector = World.GetExistingSystem<GameHostConnector>();
		}

		private bool isInvoked;
		
		protected override void OnUpdate()
		{
			if (!gameHostConnector.IsConnected || isInvoked)
				return;

			isInvoked = true;
			test();
		}

		private async UniTask test()
		{
			/*Debug.Log("GetInventory 0");
			await Task.Delay(3000);
			Debug.Log("Get Inventory 1");

			var inventory = await gameHostConnector.RpcClient.SendRequest<GetInventoryRpc, GetInventoryRpc.Response>(new GetInventoryRpc
			{
				FilterInclude = true,
				FilterCategories = new[]
				{
					"equipment"
				}
			});
			Debug.Log($"Inventory Count: {inventory.Items.Length}");

			var presets = await gameHostConnector.RpcClient.SendRequest<GetSavePresetsRpc, GetSavePresetsRpc.Response>(default);
			Debug.Log($"Presets Count: {presets.Presets.Length}");

			foreach (var preset in presets.Presets)
			{
				Console.WriteLine($"[{preset.Id}] > {preset.Name}, {preset.KitId}, {preset.RoleId}");
				
				if (preset.Name.Contains("Yarida"))
					gameHostConnector.RpcClient.SendNotification(new CopyPresetToUnitRpc
					{
						Preset = preset.Id,
						Unit = new MasterServerUnitId("8d969b25-0144-44b6-ac2e-a9ac70a9d125")
					});
			}*/
		}
	}
}