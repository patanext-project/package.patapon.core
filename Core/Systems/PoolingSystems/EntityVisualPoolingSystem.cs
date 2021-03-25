using System;
using GameHost.Simulation.Utility.Resource.Systems;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Components;
using PataNext.Client.Core.Addressables;
using PataNext.Client.DataScripts.Models.Projectiles;
using PataNext.Module.Simulation.Components.Roles;
using PataNext.Module.Simulation.Game.Visuals;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Modules;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Utility.Misc;
using StormiumTeam.GameBase.Utility.Pooling.BaseSystems;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace PataNext.Client.Systems.PoolingSystems
{
	[UpdateInGroup(typeof(OrderGroup.Presentation.AfterSimulation), OrderFirst = true)]
	public class EntityVisualPoolingSystem : PoolingSystem<EntityVisualBackend, EntityVisualPresentation, EntityVisualPoolingSystem.Validator>
	{
		public struct Validator : ICheckValidity
		{
			[ReadOnly]
			private ComponentDataFromEntity<EntityVisual> visualFromEntity;

			[ReadOnly]
			private ComponentDataFromEntity<Owner> ownerFromEntity;

			[ReadOnly]
			private ComponentDataFromEntity<ShooterProjectileVisualTarget> predictionFromEntity;

			private NativeHashMap<Entity, EntityVisualDefinition> definitionMap;

			public void OnSetup(ComponentSystemBase system)
			{
				visualFromEntity     = system.GetComponentDataFromEntity<EntityVisual>(true);
				ownerFromEntity      = system.GetComponentDataFromEntity<Owner>(true);
				predictionFromEntity = system.GetComponentDataFromEntity<ShooterProjectileVisualTarget>(true);

				definitionMap = (system as EntityVisualPoolingSystem).definitionHashMap;
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

					if (visualFromEntity[target].Resource != default)
						return false;
				}

				return true;
			}
		}

		private NativeHashMap<Entity, EntityVisualDefinition> definitionHashMap;

		private EntityVisualDefinition defaultDefinition;
		private EntityVisualManager    visualMgr;

		private GameResourceManager resourceMgr;

		protected override AssetPath AddressableAsset => AssetPath.Empty;

		protected override void OnCreate()
		{
			base.OnCreate();

			definitionHashMap = new NativeHashMap<Entity, EntityVisualDefinition>(32, Allocator.Persistent);

			resourceMgr = World.GetExistingSystem<GameResourceManager>();

			visualMgr = World.GetExistingSystem<EntityVisualManager>();
			defaultDefinition = visualMgr.Register(AddressBuilder.Client()
			                                                     .Folder("Models")
			                                                     .Folder("InGame")
			                                                     .Folder("Projectiles")
			                                                     .Folder("Cannon")
			                                                     .GetAsset("CannonProjectile"));
		}

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(EntityVisual), typeof(Translation));
		}

		protected override Type[] AdditionalBackendComponents => new[] {typeof(SortingGroup)};

		protected override void ReturnBackend(EntityVisualBackend backend)
		{
			if (!backend.canBePooled && backend.Presentation != null)
				return;

			base.ReturnBackend(backend);
		}

		protected override void SpawnBackend(Entity target)
		{
			base.SpawnBackend(target);

			var targetDefinition = defaultDefinition;
			if (definitionHashMap.TryGetValue(target, out var definition))
			{
				targetDefinition = definition;
			}
			else
			{
				var visual = GetComponent<EntityVisual>(target);
				if (visual.Resource.TryGet(resourceMgr, out var resource))
				{
					// TODO: Allocate less GC (char dict?)
					targetDefinition = visualMgr.Register(new ResPath(resource.Value.ToString()));
				}
			}

			LastBackend.SetPresentationFromPool(visualMgr.GetPool(targetDefinition) ?? visualMgr.GetPool(defaultDefinition));
			LastBackend.transform.SetPositionAndRotation(new Vector3(1000, -1000), Quaternion.identity);
		}
	}
}