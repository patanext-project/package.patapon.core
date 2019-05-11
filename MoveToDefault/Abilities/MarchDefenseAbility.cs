using System;
using package.patapon.core;
using package.stormiumteam.shared;
using package.StormiumTeam.GameBase;
using StormiumShared.Core;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Patapon4TLB.Default
{
	public struct MarchDefenseAbility : IComponentData
	{
		public struct PredictedState : IComponentData, IPredictable<PredictedState>
		{
			// We only make the movement on the movable target.
			public Entity Target;

			public bool VerifyPrediction(in PredictedState real)
			{
				return false;
			}
		}

		public struct Settings : IComponentData
		{
			public byte Flags;

			/// <summary>
			/// If enabled and the entity is marching, this will add a small defense buff (duration based on the sequence and if it's active)
			/// </summary>
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
	public class MarchDefenseAbilitySystem : GameBaseSystem
	{
		struct JobProcess : IJobProcessComponentDataWithEntity<MarchDefenseAbility.Settings, OwnerState<LivableDescription>, OwnerState<MovableDescription>>
		{
			public Entity MarchCommand;
			public float  DeltaTime;

			[ReadOnly]
			public ComponentDataFromEntity<RhythmActionController> RhythmActionControllerFromLivable;

			[ReadOnly]
			public ComponentDataFromEntity<GroundState> GroundStateFromMovable;

			[ReadOnly]
			public ComponentDataFromEntity<UnitBaseSettings> UnitSettingsFromLivable;

			[ReadOnly]
			public ComponentDataFromEntity<UnitDirection> UnitDirectionFromLivable;

			[NativeDisableParallelForRestriction]
			public ComponentDataFromEntity<Velocity> VelocityFromMovable;

			public void Execute(Entity entity, int _, ref MarchDefenseAbility.Settings settings, ref OwnerState<LivableDescription> livableOwner, ref OwnerState<MovableDescription> movableOwner)
			{
				var livable = livableOwner.Target;
				var movable = movableOwner.Target;

				if (!RhythmActionControllerFromLivable.Exists(livable))
					throw new InvalidOperationException($"Livable {livable} has no '{nameof(RhythmActionController)}'");

				var actionController = RhythmActionControllerFromLivable[livable];
				if (actionController.CurrentCommand != MarchCommand)
					return;

				var unitSettings  = UnitSettingsFromLivable[livable];
				var unitDirection = UnitDirectionFromLivable[livable];
				
				var groundState = GroundStateFromMovable[movable];

				if (groundState.Value)
				{
					var velocity = VelocityFromMovable[movable];
					// to not make tanks op, we need to get the weight from entity and use it as an acceleration factor
					var accel = math.clamp(15 - unitSettings.Weight, 2.5f, 15) * 10;
					accel = math.min(accel * DeltaTime, 1);

					velocity.Value.x = math.lerp(velocity.Value.x, unitSettings.BaseSpeed * unitDirection.Value, accel);

					VelocityFromMovable[movable] = velocity;
				}
			}
		}

		private Entity m_MarchCommand;

		protected override void OnCreateManager()
		{
			base.OnCreateManager();

			var cmdBuilder = World.GetOrCreateSystem<FlowCommandBuilder>();

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
				MarchCommand                      = m_MarchCommand,
				DeltaTime                         = GetSingleton<SingletonGameTime>().DeltaTime,
				RhythmActionControllerFromLivable = GetComponentDataFromEntity<RhythmActionController>(true),
				UnitSettingsFromLivable           = GetComponentDataFromEntity<UnitBaseSettings>(true),
				UnitDirectionFromLivable          = GetComponentDataFromEntity<UnitDirection>(true),
				GroundStateFromMovable            = GetComponentDataFromEntity<GroundState>(),
				VelocityFromMovable               = GetComponentDataFromEntity<Velocity>()
			}.Schedule(this).Complete();
		}
	}
}