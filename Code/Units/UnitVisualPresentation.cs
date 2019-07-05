using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Patapon4TLB.Core
{
	public class UnitVisualPresentation : RuntimeAssetPresentation<UnitVisualPresentation>
	{
		public Animator Animator;
	}

	public class UnitVisualBackend : RuntimeAssetBackend<UnitVisualPresentation>
	{
		protected override void Update()
		{
			if (DstEntityManager == null || DstEntityManager.IsCreated && DstEntityManager.Exists(DstEntity))
			{
				base.Update();
				return;
			}
			
			Return(true, true);
		}
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[UpdateAfter(typeof(RenderInterpolationSystem))]
	public class UpdateUnitVisualBackendSystem : ComponentSystem
	{
		protected override void OnUpdate()
		{
			EntityManager.CompleteAllJobs();
			
			Entities.ForEach((Transform transform, UnitVisualBackend backend) =>
			{				
				transform.position = EntityManager.GetComponentData<Translation>(backend.DstEntity).Value;
			});
		}
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[UpdateAfter(typeof(UpdateUnitVisualBackendSystem))]
	public class UnitPressureAnimationSystem : ComponentSystem
	{
		private EntityQuery                               m_PressureEventQuery;
		private EntityQueryBuilder.F_C<UnitVisualBackend> m_ForEachDelegate;

		private NativeArray<PressureEvent> m_PressureEvents;

		private readonly int[] KeyAnimTrigger = new[]
		{
			-1,
			Animator.StringToHash("Pata"),
			Animator.StringToHash("Pon"),
			Animator.StringToHash("Don"),
			Animator.StringToHash("Chaka")
		};

		protected override void OnCreate()
		{
			base.OnCreate();

			m_PressureEventQuery = GetEntityQuery(typeof(PressureEvent));
			m_ForEachDelegate    = ForEach;
		}

		protected override void OnUpdate()
		{
			using (m_PressureEvents = m_PressureEventQuery.ToComponentDataArray<PressureEvent>(Allocator.TempJob))
			{
				Entities.WithAll<UnitVisualBackend>().ForEach(m_ForEachDelegate);
			}
		}

		private void ForEach(UnitVisualBackend backend)
		{
			if (backend.Presentation == null)
				return;

			var relativeRhythmEngine = EntityManager.GetComponentData<Relative<RhythmEngineDescription>>(backend.DstEntity);
			if (relativeRhythmEngine.Target == default)
				return;

			var lastPressure = default(PressureEvent);
			for (var ev = 0; ev != m_PressureEvents.Length; ev++)
			{
				if (m_PressureEvents[ev].Engine == relativeRhythmEngine.Target)
				{
					lastPressure = m_PressureEvents[ev];
				}
				else if (ev == m_PressureEvents.Length - 1)
					return; // no events found
			}

			var animKey = KeyAnimTrigger[lastPressure.Key];
			backend.Presentation.Animator.SetTrigger(animKey);
		}
	}
}