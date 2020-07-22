using System.Collections.Generic;
using package.stormiumteam.shared;
using package.stormiumteam.shared.ecs;
using Patapon.Client.Systems;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Patapon.Client.Graphics.Animation.Units
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[UpdateAfter(typeof(RenderInterpolationSystem))]
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
				if (EntityManager.TryGetComponentData(backend.DstEntity, out UnitCurrentKit currentKit))
				{
					unitArchetype = $"UH.{currentKit.Value}";
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
				if (World.GetExistingSystem<UnitVisualArchetypeManager>().TryGetArchetypePool(archetype, out var pool))
				{
					be.ReturnPresentation();
					be.SetPresentationFromPool(pool);
				}
			}
		}
	}
}