using package.patapon.def.Data;
using package.stormiumteam.shared;
using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using UnityEngine;

namespace package.patapon.core
{
	public class FlowRhythmPressureEventProvider : BaseProviderBatch<FlowRhythmPressureEventProvider.Create>
	{
		public struct Create
		{
			public PressureEvent Ev;
		}

		protected override void OnCreate()
		{
			base.OnCreate();

			new RhythmEventDestroySystem<PressureEvent>(World);
		}

		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = new[]
			{
				ComponentType.ReadWrite<PressureEvent>()
			};
		}

		public override void SetEntityData(Entity entity, Create data)
		{
			EntityManager.SetComponentData(entity, data.Ev);
		}
	}
}