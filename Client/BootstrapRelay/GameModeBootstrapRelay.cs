using Bootstraps;
using DataScripts.Interface.Menu;
using DataScripts.Interface.Menu.ServerRoom;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace BootstrapRelay
{
[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class GameModeBootstrapRelay :  ComponentSystem
	{
		protected override void OnCreate()
		{
			base.OnCreate();
			
			RequireSingletonForUpdate<GameModeBootstrap.IsActive>();
		}

		protected override void OnStartRunning()
		{
			var menu = World.GetExistingSystem<ClientMenuSystem>();
			menu.SetBackgroundCanvasColor(Color.clear);
		}

		protected override void OnUpdate()
		{
			
		}
	}
}