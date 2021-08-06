using System;
using package.stormiumteam.shared.ecs;
using PataNext.Client.DataScripts.Models.Projectiles;
using PataNext.Client.DataScripts.Models.Projectiles.City.Scenes;
using PataNext.Module.Simulation.Components.GameModes.City;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.BaseSystems.Ext;
using Unity.Entities;

namespace PataNext.Client.Systems.City
{
	public class EnterCityLocationSystem : AbsGameBaseSystem
	{
		protected override void OnUpdate()
		{
			var player = this.GetFirstSelfGamePlayer();
			if (player == Entity.Null)
				return;

			if (!EntityManager.TryGetComponentData(player, out PlayerCurrentCityLocation currentLocation))
				return;

			var location = currentLocation.Entity;
			Entities.WithAll<CityScenePresentation.IsObject>().ForEach((Entity entity, EntityVisualBackend backend) =>
			{
				var presentation = (CityScenePresentation)backend.Presentation;
				if (location != backend.DstEntity)
				{
					if (presentation.SetExitState())
						Console.WriteLine("exit: " + presentation.gameObject.name);
					return;
				}

				if (presentation.TrySoftEnter())
					Console.WriteLine("enter: " + backend.Presentation.gameObject.name);
			}).WithStructuralChanges().Run();
		}
	}
}