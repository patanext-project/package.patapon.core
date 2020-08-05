using System;
using System.Collections.Generic;
using GameHost.Simulation.Utility.Resource.Systems;
using package.stormiumteam.shared;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Systems;
using PataNext.Module.Simulation.Components.Units;
using PataNext.Module.Simulation.Resources.Keys;
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

			var resourceMgr = World.GetExistingSystem<GameResourceManager>();

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
				var unitArchetype = "UH.basic"; // this will be dynamic in the future (based on entity class)
				if (EntityManager.TryGetComponentData(backend.DstEntity, out UnitCurrentKit currentKit)
				    && currentKit.Resource.TryGet(resourceMgr, out UnitKitResourceKey key))
				{
					unitArchetype = $"UH.{key.Value.ToString()}";
				}

				if (backend.CurrentArchetype != unitArchetype)
				{
					Debug.Log($"{backend.CurrentArchetype} -> {unitArchetype}");

					backend.CurrentArchetype = unitArchetype;
					m_UpdateArchetypeList.Add((backend, unitArchetype));
				}
			}).WithoutBurst().Run();

			foreach (var (be, archetype) in m_UpdateArchetypeList)
			{
				Console.WriteLine($"{archetype}");
				if (World.GetExistingSystem<UnitVisualArchetypeManager>().TryGetArchetypePool(archetype, out var pool))
				{
					Console.WriteLine("return " + be.Presentation);
					be.ReturnPresentation();
					be.SetPresentationFromPool(pool);
					Console.WriteLine(pool.AssetId);
				}
			}
		}
	}
}