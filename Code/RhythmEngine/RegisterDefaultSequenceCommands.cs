using package.patapon.core;
using package.stormiumteam.shared.ecs;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;

namespace Patapon4TLB.Default
{
	[DisableAutoCreation]
	public class RegisterDefaultSequenceCommands : GameBaseSystem
	{
		protected override void OnCreate()
		{
			base.OnCreate();

			var builder = World.GetOrCreateSystem<FlowCommandBuilder>();

			var march = Build(builder, new FlowCommandData {BeatLength = 4}, new[]
			{
				new FlowCommandSequence(0, RhythmKeys.Left),
				new FlowCommandSequence(1, RhythmKeys.Left),
				new FlowCommandSequence(2, RhythmKeys.Left),
				new FlowCommandSequence(3, RhythmKeys.Right),
			});
			EntityManager.AddComponent(march, typeof(MarchCommand));

			var attack = Build(builder, new FlowCommandData {BeatLength = 4}, new[]
			{
				new FlowCommandSequence(0, RhythmKeys.Right),
				new FlowCommandSequence(1, RhythmKeys.Right),
				new FlowCommandSequence(2, RhythmKeys.Left),
				new FlowCommandSequence(3, RhythmKeys.Right),
			});
			EntityManager.AddComponent(attack, typeof(AttackCommand));

			var defend = Build(builder, new FlowCommandData {BeatLength = 4}, new[]
			{
				new FlowCommandSequence(0, RhythmKeys.Up),
				new FlowCommandSequence(1, RhythmKeys.Up),
				new FlowCommandSequence(2, RhythmKeys.Left),
				new FlowCommandSequence(3, RhythmKeys.Right),
			});
			EntityManager.AddComponent(defend, typeof(DefendCommand));

			var charge = Build(builder, new FlowCommandData {BeatLength = 4}, new[]
			{
				new FlowCommandSequence(0, RhythmKeys.Right),
				new FlowCommandSequence(1, RhythmKeys.Right),
				new FlowCommandSequence(2, RhythmKeys.Up),
				new FlowCommandSequence(3, RhythmKeys.Up),
			});
			EntityManager.AddComponent(charge, typeof(ChargeCommand));

			var retreat = Build(builder, new FlowCommandData {BeatLength = 4}, new[]
			{
				new FlowCommandSequence(0, RhythmKeys.Right),
				new FlowCommandSequence(1, RhythmKeys.Left),
				new FlowCommandSequence(2, RhythmKeys.Right),
				new FlowCommandSequence(3, RhythmKeys.Left),
			});
			EntityManager.AddComponent(retreat, typeof(RetreatCommand));

			var jump = Build(builder, new FlowCommandData {BeatLength = 4}, new[]
			{
				new FlowCommandSequence(0, RhythmKeys.Down),
				new FlowCommandSequence(1, RhythmKeys.Down),
				new FlowCommandSequence(2, RhythmKeys.Up),
				new FlowCommandSequence(3, RhythmKeys.Up),
			});
			EntityManager.AddComponent(jump, typeof(JumpCommand));

			var party = Build(builder, new FlowCommandData {BeatLength = 4}, new[]
			{
				new FlowCommandSequence(0, RhythmKeys.Left),
				new FlowCommandSequence(1, RhythmKeys.Right),
				new FlowCommandSequence(2, RhythmKeys.Down),
				new FlowCommandSequence(3, RhythmKeys.Up),
			});
			EntityManager.AddComponent(party, typeof(PartyCommand));

			var summon = Build(builder, new FlowCommandData {BeatLength = 4}, new[]
			{
				new FlowCommandSequence(0, RhythmKeys.Down),
				new FlowCommandSequence(1, RhythmKeys.Down),
				new FlowCommandSequence(1, RhythmKeys.Down),
				new FlowCommandSequence(2, RhythmKeys.Down),
				new FlowCommandSequence(3, RhythmKeys.Down),
			});
			EntityManager.AddComponent(summon, typeof(SummonCommand));

			var backward = Build(builder, new FlowCommandData {BeatLength = 4}, new[]
			{
				new FlowCommandSequence(0, RhythmKeys.Up),
				new FlowCommandSequence(1, RhythmKeys.Left),
				new FlowCommandSequence(2, RhythmKeys.Up),
				new FlowCommandSequence(3, RhythmKeys.Left),
			});
			EntityManager.AddComponent(backward, typeof(BackwardCommand));

			var skip = Build(builder, new FlowCommandData {BeatLength = 3}, new[]
			{
				new FlowCommandSequence(0, RhythmKeys.Up),
				new FlowCommandSequence(1, RhythmKeys.Up),
				new FlowCommandSequence(1, RhythmKeys.Right),
				new FlowCommandSequence(3, RhythmKeys.Right),
			});
			EntityManager.AddComponent(skip, typeof(SkipCommand));
		}

		private Entity Build(FlowCommandBuilder builder, FlowCommandData flowCommandData, FlowCommandSequence[] sequences)
		{
			var entity = builder.GetOrCreate(new NativeArray<FlowCommandSequence>(sequences, Allocator.Temp));

			EntityManager.AddComponent(entity, typeof(DefaultRhythmCommand));
			EntityManager.SetOrAddComponentData(entity, flowCommandData);

			return entity;
		}

		protected override void OnUpdate()
		{

		}
	}
}