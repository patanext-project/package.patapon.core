using System;
using package.patapon.core;
using package.stormiumteam.shared;
using package.StormiumTeam.GameBase;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Patapon4TLB.Default
{
	public struct TaterazayKitMarchAction : IComponentData
	{
		public struct Settings : IComponentData
		{
			public byte Flags;

			public bool AutoDefense
			{
				get => MainBit.GetBitAt(Flags, 0) != 0;
				set => MainBit.SetBitAt(ref Flags, 0, value);
			}

			public Settings(bool autoDefense)
			{
				Flags = 0;

				MainBit.SetBitAt(ref Flags, 0, autoDefense);
			}
		}
	}

	[UpdateInGroup(typeof(ActionSystemGroup))]
	public class TaterazayKitMarchActionSystem : GameBaseSystem
	{
		struct JobProcess : IJobProcessComponentDataWithEntity<TaterazayKitMarchAction.Settings, OwnerState<LivableDescription>>
		{
			public Entity MarchCommand;

			[ReadOnly]
			public ComponentDataFromEntity<RhythmActionController> RhythmActionControllerFromEntity;

			[ReadOnly]
			public ComponentDataFromEntity<UnitBaseSettings> UnitSettingsFromEntity;

			[ReadOnly]
			public ComponentDataFromEntity<UnitDirection> UnitDirectionFromEntity;

			[NativeDisableParallelForRestriction]
			public ComponentDataFromEntity<Velocity> VelocityFromEntity;

			public void Execute(Entity entity, int index, ref TaterazayKitMarchAction.Settings settings, ref OwnerState<LivableDescription> owner)
			{
				var livable = owner.Target;

				if (!RhythmActionControllerFromEntity.Exists(livable))
					throw new InvalidOperationException($"Livable {livable} has no '{nameof(RhythmActionController)}'");

				var actionController = RhythmActionControllerFromEntity[livable];
				if (actionController.CurrentCommand != MarchCommand)
					return;

				var unitSettings  = UnitSettingsFromEntity[livable];
				var unitDirection = UnitDirectionFromEntity[livable];

				// that a test for now
				VelocityFromEntity[livable] = new Velocity(new float3(unitSettings.BaseSpeed * unitDirection.Value, 0, 0));
			}
		}

		private Entity m_MarchCommand;

		protected override void OnCreateManager()
		{
			base.OnCreateManager();

			var cmdBuilder = World.GetOrCreateManager<FlowCommandBuilder>();

			m_MarchCommand = cmdBuilder.GetOrCreate(new NativeArray<FlowCommandSequence>(4, Allocator.Temp)
			{
				[0] = new FlowCommandSequence(0, FlowRhythmEngine.KeyPata),
				[1] = new FlowCommandSequence(1, FlowRhythmEngine.KeyPata),
				[2] = new FlowCommandSequence(2, FlowRhythmEngine.KeyPata),
				[3] = new FlowCommandSequence(3, FlowRhythmEngine.KeyPon)
			});
		}

		protected override void OnUpdate()
		{
			new JobProcess
			{
				MarchCommand                     = m_MarchCommand,
				RhythmActionControllerFromEntity = GetComponentDataFromEntity<RhythmActionController>(),
				UnitSettingsFromEntity           = GetComponentDataFromEntity<UnitBaseSettings>(),
				UnitDirectionFromEntity          = GetComponentDataFromEntity<UnitDirection>(),
				VelocityFromEntity               = GetComponentDataFromEntity<Velocity>()
			}.Schedule(this).Complete();
		}
	}
}