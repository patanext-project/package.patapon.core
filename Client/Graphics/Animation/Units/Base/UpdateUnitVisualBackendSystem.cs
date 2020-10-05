using System;
using System.Collections.Generic;
using GameHost.Simulation.Utility.Resource.Systems;
using package.stormiumteam.shared;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Systems;
using PataNext.Module.Simulation.Components.Units;
using PataNext.Module.Simulation.Resources.Keys;
using StormiumTeam.GameBase.Utility.Pooling;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace PataNext.Client.Graphics.Animation.Units.Base
{
	[UpdateInGroup(typeof(PresentationSystemGroup))]
	[AlwaysSynchronizeSystem]
	public class UpdateUnitVisualBackendSystem : SystemBase
	{
		private EntityQuery                                         m_BackendQuery;
		private List<(UnitVisualBackend backend, string archetype)> m_UpdateArchetypeList;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_BackendQuery        = GetEntityQuery(typeof(Transform), typeof(UnitVisualBackend));
			m_UpdateArchetypeList = new List<(UnitVisualBackend backend, string archetype)>();
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
				if (backend.DstEntity == Entity.Null || !EntityManager.Exists(backend.DstEntity) || !EntityManager.HasComponent<Translation>(backend.DstEntity))
				{
					Debug.Log("null? " + backend.DstEntity);
					return;
				}

				ref var i = ref indexArray.AsRef();

				var pos = EntityManager.GetComponentData<Translation>(backend.DstEntity).Value;
				Debug.DrawRay(pos, Vector3.up, Color.green);
				pos.z = i++ * 2;

				transform.position = pos;

				EntityManager.TryGetComponentData(backend.DstEntity, out var direction, UnitDirection.Right);
				transform.localScale = new Vector3(direction.Value, 1, 1);

				// Load presentation
				AsyncAssetPool<GameObject> targetArchetypePool = default;
				if (EntityManager.TryGetComponentData(backend.DstEntity, out UnitArchetype archetype)
				    && archetype.Resource.TryGet(resourceMgr, out UnitArchetypeResourceKey archetypeResourceKey))
				{
					if (EntityManager.TryGetComponentData(backend.DstEntity, out UnitCurrentKit currentKit)
					    && currentKit.Resource.TryGet(resourceMgr, out UnitKitResourceKey kitResourceKey))
					{
						archetypeMgr.TryGetArchetypePool(archetypeResourceKey.Value.ToString(),
							kitResourceKey.Value.ToString(),
							out targetArchetypePool);
					}
					else
					{
						archetypeMgr.TryGetArchetypePool(archetypeResourceKey.Value.ToString(), out targetArchetypePool);
					}
				}

				if (targetArchetypePool != null
				    && backend.CurrentArchetype != targetArchetypePool.AssetId)
				{
					backend.CurrentArchetype = targetArchetypePool.AssetId;

					backend.ReturnPresentation();
					backend.SetPresentationFromPool(targetArchetypePool);
				}
			}).WithStructuralChanges().Run();
		}
	}
}