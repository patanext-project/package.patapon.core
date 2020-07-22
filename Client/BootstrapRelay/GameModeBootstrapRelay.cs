using DataScripts.Interface.Menu;
using Unity.Entities;
using UnityEngine;

namespace BootstrapRelay
{
	public class GameModeBootstrapRelay :  ComponentSystem
	{
		protected override void OnCreate()
		{
			base.OnCreate();
			
			RequireSingletonForUpdate<GameModeBootstrap.IsActive>();
		}

		protected override void OnStartRunning()
		{
			var map = StatisticModifierJson.FromMap(@"
{
	""modifiers"": [
		{
			""id"": ""charge"",
			""attack"": 0.5
		}
	]
}
");
			Debug.Log("RESULT>>>" + map["charge"].Attack);
			
			var menu = World.GetExistingSystem<ClientMenuSystem>();
			menu.SetBackgroundCanvasColor(Color.clear);
		}

		protected override void OnUpdate()
		{
			
		}
	}
}