using System;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Core.Addressables;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.Client.Systems.GameModes.City.Scenes;
using PataNext.Module.Simulation.Components.Army;
using StormiumTeam.GameBase.Utility.Misc;
using StormiumTeam.GameBase.Utility.Pooling.BaseSystems;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;

namespace PataNext.Client.PoolingSystems
{
	public struct IsBarracksUnitViewTag : IComponentData
	{
		
	}
	
	public class BarracksUnitPoolingSystem : PoolingSystem<UnitVisualBackend, UnitVisualPresentation>
	{
		protected override AssetPath AddressableAsset => AddressBuilder.Client()
		                                                               .Folder("Models")
		                                                               .Folder("UberHero")
		                                                               .GetAsset("EmptyPresentation");

		protected override Type[] AdditionalBackendComponents => new Type[] {typeof(SortingGroup)};

		protected override void OnCreate()
		{
			base.OnCreate();

			RequireForUpdate(GetEntityQuery(typeof(SpawnBarrackSceneTag)));
		}

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(ArmyUnitDescription));
		}

		protected override void SpawnBackend(Entity target)
		{
			base.SpawnBackend(target);
			
			EntityManager.SetOrAddComponentData(target, new UnitVisualSourceBackend {Backend = LastBackend.BackendEntity});
			EntityManager.SetOrAddComponentData(LastBackend.BackendEntity, new IsBarracksUnitViewTag());

			LastBackend.AutomaticTransform = false;

			var sortingGroup = LastBackend.GetComponent<SortingGroup>();
			sortingGroup.sortingLayerName = "Entities";
			sortingGroup.sortingOrder     = 0;

			LastBackend.gameObject.layer     = LayerMask.NameToLayer("Entities");
			LastBackend.transform.localScale = new Vector3(1, 1, 0.1f);
		}
	}
}