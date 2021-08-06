using GameHost.Core;
using PataNext.Client.DataScripts.Models.Projectiles.City.Scenes;
using PataNext.Client.Rpc.City;
using StormiumTeam.GameBase.Utility.Misc;
using Unity.Entities;

namespace PataNext.Client.DataScripts.Models.CityLocations
{
	public class ObeliskLocation : CityScenePresentation
	{
		protected override void OnEnter()
		{
			World.DefaultGameObjectInjectionWorld.GetExistingSystem<GameHostConnector>()
			     .RpcClient
			     .SendNotification(new ObeliskStartMissionRpc
			     {
				     Path = new ResPath(ResPath.EType.ClientResource, "st", "pn", "mission/test").FullString
			     });
		}

		protected override void OnExit()
		{
		}
	}
}