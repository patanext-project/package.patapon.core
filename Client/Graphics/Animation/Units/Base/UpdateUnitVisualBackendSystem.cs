using System.Collections.Generic;
using package.stormiumteam.shared;
using package.stormiumteam.shared.ecs;
using Patapon.Client.Systems;
using Patapon.Mixed.Units;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Patapon.Client.Graphics.Animation.Units
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[UpdateAfter(typeof(RenderInterpolationSystem))]
	[AlwaysSynchronizeSystem]
	public class UpdateUnitVisualBackendSystem : JobComponentSystem
	{
		private EntityQuery                                         m_BackendQuery;
		private List<(UnitVisualBackend backend, string archetype)> m_UpdateArchetypeList;
		protected override void OnCreate()
		{
			base.OnCreate();

			m_BackendQuery        = GetEntityQuery(typeof(Transform), typeof(UnitVisualBackend));
			m_UpdateArchetypeList = new List<(UnitVisualBackend backend, string archetype)>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			m_UpdateArchetypeList.Clear();

			var __i = 0;
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
				
				EntityManager.TryGetComponentData(backend.DstEntity, out UnitDirection direction, UnitDirection.Right);
				transform.localScale = new Vector3(direction.Value, 1, 1);
				
				// Load presentation
				const string unitArchetype = "UH.basic"; // this will be dynamic in the future (based on entity class)
				if (backend.CurrentArchetype != unitArchetype)
				{
					backend.CurrentArchetype = unitArchetype;
					m_UpdateArchetypeList.Add((backend, unitArchetype));
				}		
			}).WithoutBurst().Run();

			foreach (var (be, archetype) in m_UpdateArchetypeList)
			{
				var pool = World.GetExistingSystem<UnitVisualArchetypeManager>().GetArchetypePool(archetype);

				be.ReturnPresentation();
				be.SetPresentationFromPool(pool);
			}

			return inputDeps;
		}
	}
}