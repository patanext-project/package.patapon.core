using System;
using GameHost.Simulation.Utility.Resource.Systems;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Components;
using PataNext.Client.Core.Addressables;
using PataNext.Client.DataScripts.Models.Projectiles;
using PataNext.Module.Simulation.Components.Roles;
using PataNext.Module.Simulation.Game.Visuals;
using StormiumTeam.GameBase.Modules;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Utility.Misc;
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

			[ReadOnly]
			private ComponentDataFromEntity<Owner> ownerFromEntity;

			[ReadOnly]
			private ComponentDataFromEntity<ShooterProjectileVisualTarget> predictionFromEntity;

			private NativeHashMap<Entity, VisualThrowableDefinition> definitionMap;

			public void OnSetup(ComponentSystemBase system)
			{
				visualFromEntity     = system.GetComponentDataFromEntity<EntityVisual>(true);
				ownerFromEntity      = system.GetComponentDataFromEntity<Owner>(true);
				predictionFromEntity = system.GetComponentDataFromEntity<ShooterProjectileVisualTarget>(true);

				definitionMap = (system as ProjectilePoolingSystem).definitionHashMap;
				definitionMap.Clear();
			}

			public bool IsValid(Entity target)
			{
				// If the server is unsure of the resource, don't directly spawn it. 
				// If a system on this client know what resource to use, it will re-update this component or spawn the backend itself.
				if (visualFromEntity[target].Resource == default || visualFromEntity[target].ClientPriority)
				{
					if (ownerFromEntity.TryGet(target, out var owner)
					    && predictionFromEntity.TryGet(owner.Target, out var prediction))
					{
						definitionMap[target] = prediction.Definition;
						return true;
					}

					return false;
				}

				return true;
			}
		}

		private NativeHashMap<Entity, VisualThrowableDefinition> definitionHashMap;

		private VisualThrowableDefinition        defaultDefinition;
		private VisualThrowableProjectileManager visualMgr;

		private GameResourceManager resourceMgr;

		protected override AssetPath AddressableAsset => AssetPath.Empty;

		protected override void OnCreate()
		{
			base.OnCreate();

			definitionHashMap = new NativeHashMap<Entity, VisualThrowableDefinition>(32, Allocator.Persistent);

			resourceMgr = World.GetExistingSystem<GameResourceManager>();

			visualMgr = World.GetExistingSystem<VisualThrowableProjectileManager>();
			defaultDefinition = visualMgr.Register(AddressBuilder.Client()
			                                                     .Folder("Models")
			                                                     .Folder("InGame")
			                                                     .Folder("Projectiles")
			                                                     .Folder("Cannon")
			                                                     .GetAsset("CannonProjectile"));
		}

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(ProjectileDescription), typeof(EntityVisual), typeof(Translation));
		}

		protected override Type[] AdditionalBackendComponents => new[] {typeof(SortingGroup)};

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

			if (definitionHashMap.TryGetValue(target, out var definition))
			{
				LastBackend.SetPresentationFromPool(visualMgr.GetPool(definition));
			}
			else
			{
				var visual = GetComponent<EntityVisual>(target);
				if (visual.Resource.TryGet(resourceMgr, out var resource))
				{
					// TODO: Allocate less GC (char dict?)
					definition = visualMgr.Register(new ResPath(resource.ToString()));
					LastBackend.SetPresentationFromPool(visualMgr.GetPool(definition));
				}
			}
		}
	}
}