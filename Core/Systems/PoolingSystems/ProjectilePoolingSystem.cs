using System;
using PataNext.Client.Core.Addressables;
using PataNext.Client.DataScripts.Models.Projectiles;
using PataNext.Module.Simulation.Components.Roles;
using PataNext.Module.Simulation.Game.Visuals;
using PataNext.Module.Simulation.GameBase.Physics.Components;
using StormiumTeam.GameBase.Modules;
using StormiumTeam.GameBase.Utility.Pooling.BaseSystems;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine.Rendering;

namespace PataNext.Client.Systems.PoolingSystems
{
	public class ProjectilePoolingSystem : PoolingSystem<ProjectileBackend, BaseProjectilePresentation, ProjectilePoolingSystem.Validator>
	{
		public struct Validator : ICheckValidity
		{
			[ReadOnly]
			private ComponentDataFromEntity<EntityVisual> visualFromEntity;

			public void OnSetup(ComponentSystemBase system)
			{
				visualFromEntity = system.GetComponentDataFromEntity<EntityVisual>(true);
			}

			public bool IsValid(Entity target)
			{
				// If the server is unsure of the resource, don't directly spawn it. 
				// If a system on this client know what resource to use, it will re-update this component or spawn the backend itself.
				if (visualFromEntity[target].Resource == default || visualFromEntity[target].ClientPriority)
					return false;

				return true;
			}
		}

		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Folder("Models")
			              .Folder("InGame")
			              .Folder("Projectiles")
			              .Folder("Cannon")
			              .GetFile("CannonProjectile.prefab");

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(ProjectileDescription), typeof(EntityVisual), typeof(Translation));
		}

		protected override Type[] AdditionalBackendComponents => new[] { typeof(SortingGroup) };

		protected override void ReturnBackend(ProjectileBackend backend)
		{
			if (!backend.canBePooled)
				return;
			
			base.ReturnBackend(backend);
		}

		protected override void SpawnBackend(Entity target)
		{
			base.SpawnBackend(target);
			
			LastBackend.GetComponent<SortingGroup>()
			           .sortingLayerName = "BattlegroundEffects";
		}
	}
}