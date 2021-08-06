using package.stormiumteam.shared.ecs;
using PataNext.Module.Simulation.Components.GameModes.City;
using PataNext.Module.Simulation.Components.GameModes.City.Scenes;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.BaseSystems.Ext;
using Unity.Entities;

namespace PataNext.Client.Systems.GameModes.City.Scenes
{
	public struct SpawnBarrackSceneTag : IComponentData
	{
	}

	public class CityBarrackSceneSystem : AbsGameBaseSystem
	{
		private EntityQuery existingBarracksQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			existingBarracksQuery = GetEntityQuery(ComponentType.ReadWrite<SpawnBarrackSceneTag>());
		}

		protected override void OnUpdate()
		{
			var player = this.GetFirstSelfGamePlayer();
			if (!EntityManager.TryGetComponentData(player, out PlayerCurrentCityLocation currentCityScene)
			    || !EntityManager.HasComponent<CityBarrackScene>(currentCityScene.Entity))
			{
				if (existingBarracksQuery.IsEmptyIgnoreFilter == false)
					EntityManager.DestroyEntity(existingBarracksQuery);
				return;
			}

			if (existingBarracksQuery.IsEmptyIgnoreFilter)
				EntityManager.CreateEntity(typeof(SpawnBarrackSceneTag));
		}
	}
}