using System;
using package.patapon.core;
using package.stormiumteam.shared;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Patapon4TLB.Default
{
	public struct MarchDefenseAbility : IComponentData
	{
		public struct PredictedState : IComponentData
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

			public Entity Character;
			public Entity Movable;

			/// <summary>
			/// If enabled and the entity is marching, this will add a small defense buff (duration based on the sequence and if it's active)
			/// </summary>
			public bool AutoDefense;
		}
	}

	[UpdateInGroup(typeof(ActionSystemGroup))]
	public class MarchDefenseAbilitySystem : JobGameBaseSystem
	{
		struct JobProcess : IJobForEachWithEntity<MarchDefenseAbility.Settings>
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

			public void Execute(Entity entity, int _, ref MarchDefenseAbility.Settings settings)
			{
				var actionController = RhythmActionControllerFromLivable[settings.Movable];
				if (actionController.CurrentCommand != MarchCommand)
					return;

				var unitSettings = UnitSettingsFromLivable[settings.Character];

				var unitDirection = UnitDirectionFromLivable[settings.Movable];
				var groundState   = GroundStateFromMovable[settings.Movable];

				if (groundState.Value)
				{
					var velocity = VelocityFromMovable[settings.Movable];
					// to not make tanks op, we need to get the weight from entity and use it as an acceleration factor
					var accel = math.clamp(15 - unitSettings.Weight, 2.5f, 15) * 10;
					accel = math.min(accel * DeltaTime, 1);

					velocity.Value.x = math.lerp(velocity.Value.x, unitSettings.BaseSpeed * unitDirection.Value, accel);

					VelocityFromMovable[settings.Movable] = velocity;
				}
			}
		}

		private Entity m_MarchCommand;

		protected override void OnCreate()
		{
			base.OnCreate();

			var cmdBuilder = World.GetOrCreateSystem<RhythmCommandBuilder>();

			m_MarchCommand = cmdBuilder.GetOrCreate(new NativeArray<RhythmCommandSequence>(4, Allocator.Temp)
			{
				[0] = new RhythmCommandSequence(0, FlowRhythmEngine.KeyPata),
				[1] = new RhythmCommandSequence(1, FlowRhythmEngine.KeyPata),
				[2] = new RhythmCommandSequence(2, FlowRhythmEngine.KeyPata),
				[3] = new RhythmCommandSequence(3, FlowRhythmEngine.KeyPon)
			});
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new JobProcess
			{
				MarchCommand                      = m_MarchCommand,
				DeltaTime                         = GetSingleton<GameTimeComponent>().DeltaTime,
				RhythmActionControllerFromLivable = GetComponentDataFromEntity<RhythmActionController>(true),
				UnitSettingsFromLivable           = GetComponentDataFromEntity<UnitBaseSettings>(true),
				UnitDirectionFromLivable          = GetComponentDataFromEntity<UnitDirection>(true),
				GroundStateFromMovable            = GetComponentDataFromEntity<GroundState>(),
				VelocityFromMovable               = GetComponentDataFromEntity<Velocity>()
			}.Schedule(this, inputDeps);
		}
	}
}