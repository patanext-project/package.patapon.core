using System;
using System.Collections.Generic;
using GameHost;
using GameHost.Simulation.Utility.Resource;
using GameHost.Simulation.Utility.Resource.Systems;
using package.stormiumteam.shared;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Graphics.Animation.Base;
using PataNext.Client.Systems;
using PataNext.Module.Simulation.Components.GamePlay;
using PataNext.Module.Simulation.Components.Units;
using PataNext.Module.Simulation.Game.Visuals;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Utility.Misc;
using StormiumTeam.GameBase.Utility.Pooling;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace PataNext.Client.Graphics.Animation.Units.Base
{
	[UpdateInGroup(typeof(OrderGroup.Presentation.AfterSimulation), OrderFirst = true)]
	[AlwaysSynchronizeSystem]
	public class UpdateUnitVisualBackendSystem : SystemBase
	{
		private List<(UnitVisualBackend backend, string archetype)> m_UpdateArchetypeList;
		private EntityVisualManager                                 entityVisualMgr;
		private GameResourceManager                                 gameResourceMgr;

		protected override void OnCreate()
		{
			base.OnCreate();
			
			m_UpdateArchetypeList = new List<(UnitVisualBackend backend, string archetype)>();
			entityVisualMgr  = World.GetExistingSystem<EntityVisualManager>();
			gameResourceMgr   = World.GetExistingSystem<GameResourceManager>();
		}

		protected override void OnUpdate()
		{
			m_UpdateArchetypeList.Clear();

			var resourceMgr  = World.GetExistingSystem<GameResourceManager>();
			var archetypeMgr = World.GetExistingSystem<UnitVisualArchetypeManager>();

			var __i        = 1;
			var indexArray = UnsafeAllocation.From(ref __i);
			Entities.ForEach((Transform transform, UnitVisualBackend backend) =>
			{
				if (backend.DstEntity == Entity.Null || !EntityManager.Exists(backend.DstEntity))
				{
					Debug.Log("null? " + backend.DstEntity);
					return;
				}

				if (backend.AutomaticTransform)
				{
					if (EntityManager.HasComponent<Translation>(backend.DstEntity))
					{
						ref var i = ref indexArray.AsRef();

						var pos = EntityManager.GetComponentData<Translation>(backend.DstEntity).Value;
						Debug.DrawRay(pos, Vector3.up, Color.green);
						pos.z = i++ * 2;

						transform.position = pos;
					}

					EntityManager.TryGetComponentData(backend.DstEntity, out var direction, UnitDirection.Right);
					transform.localScale = new Vector3(direction.Value, 1, 0.1f); // make it flat so that it doesn't occupy a large Z buffer range
				}

				if (EntityManager.TryGetComponentData(backend.DstEntity, out EntityVisual visual))
				{
					if (visual.Resource == default)
						visual = new EntityVisual(new GameResource<GameGraphicResource>(new GhGameEntitySafe {Id = 0, Version = uint.MaxValue}));
					else if (visual.Resource.TryGet(gameResourceMgr, out var resource))
					{
						if (backend.CurrentGameGraphicResource.Equals(resource) == false)
						{
							backend.CurrentGameGraphicResource = resource;
							
							var targetDefinition = entityVisualMgr.Register(new ResPath(resource.Value.ToString()));
							var pool = entityVisualMgr.GetPool(targetDefinition);
							if (pool != null && backend.CurrentArchetype != pool.AssetPath)
							{
								backend.CurrentArchetype = pool.AssetPath;
								
								backend.ReturnPresentation();
								backend.SetPresentationFromPool(pool);
								return;
							}
						}
						
						return;
					}
					
					if (visual.ClientPriority == false)
						return;

					visual = new EntityVisual(visual.Resource, true);
				}

				// Load presentation
				AsyncAssetPool<GameObject> targetArchetypePool = default;
				if (EntityManager.TryGetComponentData(backend.DstEntity, out UnitArchetype archetype)
				    && archetype.Resource.TryGet(resourceMgr, out var archetypeResourceKey))
				{
					if (EntityManager.TryGetComponentData(backend.DstEntity, out UnitCurrentKit currentKit)
					    && currentKit.Resource.TryGet(resourceMgr, out var kitResourceKey))
					{
						if (archetypeResourceKey.Equals(backend.CurrentArchetypeResource)
						    && kitResourceKey.Equals(backend.CurrentKitResource))
						{
							// Same archetype and kit, continue (the next call will alloc)
							if (!Input.GetKeyDown(KeyCode.R)) return;
						}

						if (archetypeMgr.TryGetArchetypePool(archetypeResourceKey.Value.ToString(),
							kitResourceKey.Value.ToString(),
							out targetArchetypePool))
						{
							backend.CurrentArchetypeResource = archetypeResourceKey;
							backend.CurrentKitResource       = kitResourceKey;
						}
					}
					else
					{
						if (backend.CurrentKitResource.Equals(default) && archetypeResourceKey.Equals(backend.CurrentArchetypeResource))
						{
							// Same archetype and kit, continue (the next call will alloc)
							if (!Input.GetKeyDown(KeyCode.R)) return;
						}

						archetypeMgr.TryGetArchetypePool(archetypeResourceKey.Value.ToString(), out targetArchetypePool);
					}
				}
				
				if (targetArchetypePool != null
				    && (backend.CurrentArchetype != targetArchetypePool.AssetPath || Input.GetKeyDown(KeyCode.R)))
				{
					backend.CurrentArchetype = targetArchetypePool.AssetPath;

					backend.ReturnPresentation();
					backend.SetPresentationFromPool(targetArchetypePool);
				}
			}).WithStructuralChanges().Run();
		}
	}
}