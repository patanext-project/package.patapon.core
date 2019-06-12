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

			var builder = World.GetOrCreateSystem<RhythmCommandBuilder>();

			var march = Build(builder, new RhythmCommandData {BeatLength = 4}, new[]
			{
				new RhythmCommandSequence(0, RhythmKeys.Left),
				new RhythmCommandSequence(1, RhythmKeys.Left),
				new RhythmCommandSequence(2, RhythmKeys.Left),
				new RhythmCommandSequence(3, RhythmKeys.Right),
			});
			EntityManager.AddComponent(march, typeof(MarchCommand));

			var attack = Build(builder, new RhythmCommandData {BeatLength = 4}, new[]
			{
				new RhythmCommandSequence(0, RhythmKeys.Right),
				new RhythmCommandSequence(1, RhythmKeys.Right),
				new RhythmCommandSequence(2, RhythmKeys.Left),
				new RhythmCommandSequence(3, RhythmKeys.Right),
			});
			EntityManager.AddComponent(attack, typeof(AttackCommand));

			var defend = Build(builder, new RhythmCommandData {BeatLength = 4}, new[]
			{
				new RhythmCommandSequence(0, RhythmKeys.Up),
				new RhythmCommandSequence(1, RhythmKeys.Up),
				new RhythmCommandSequence(2, RhythmKeys.Left),
				new RhythmCommandSequence(3, RhythmKeys.Right),
			});
			EntityManager.AddComponent(defend, typeof(DefendCommand));

			var charge = Build(builder, new RhythmCommandData {BeatLength = 4}, new[]
			{
				new RhythmCommandSequence(0, RhythmKeys.Right),
				new RhythmCommandSequence(1, RhythmKeys.Right),
				new RhythmCommandSequence(2, RhythmKeys.Up),
				new RhythmCommandSequence(3, RhythmKeys.Up),
			});
			EntityManager.AddComponent(charge, typeof(ChargeCommand));

			var retreat = Build(builder, new RhythmCommandData {BeatLength = 4}, new[]
			{
				new RhythmCommandSequence(0, RhythmKeys.Right),
				new RhythmCommandSequence(1, RhythmKeys.Left),
				new RhythmCommandSequence(2, RhythmKeys.Right),
				new RhythmCommandSequence(3, RhythmKeys.Left),
			});
			EntityManager.AddComponent(retreat, typeof(RetreatCommand));

			var jump = Build(builder, new RhythmCommandData {BeatLength = 4}, new[]
			{
				new RhythmCommandSequence(0, RhythmKeys.Down),
				new RhythmCommandSequence(1, RhythmKeys.Down),
				new RhythmCommandSequence(2, RhythmKeys.Up),
				new RhythmCommandSequence(3, RhythmKeys.Up),
			});
			EntityManager.AddComponent(jump, typeof(JumpCommand));

			var party = Build(builder, new RhythmCommandData {BeatLength = 4}, new[]
			{
				new RhythmCommandSequence(0, RhythmKeys.Left),
				new RhythmCommandSequence(1, RhythmKeys.Right),
				new RhythmCommandSequence(2, RhythmKeys.Down),
				new RhythmCommandSequence(3, RhythmKeys.Up),
			});
			EntityManager.AddComponent(party, typeof(PartyCommand));

			var summon = Build(builder, new RhythmCommandData {BeatLength = 4}, new[]
			{
				new RhythmCommandSequence(0, RhythmKeys.Down),
				new RhythmCommandSequence(1, RhythmKeys.Down),
				new RhythmCommandSequence(1, RhythmKeys.Down),
				new RhythmCommandSequence(2, RhythmKeys.Down),
				new RhythmCommandSequence(3, RhythmKeys.Down),
			});
			EntityManager.AddComponent(summon, typeof(SummonCommand));

			var backward = Build(builder, new RhythmCommandData {BeatLength = 4}, new[]
			{
				new RhythmCommandSequence(0, RhythmKeys.Up),
				new RhythmCommandSequence(1, RhythmKeys.Left),
				new RhythmCommandSequence(2, RhythmKeys.Up),
				new RhythmCommandSequence(3, RhythmKeys.Left),
			});
			EntityManager.AddComponent(backward, typeof(BackwardCommand));

			var skip = Build(builder, new RhythmCommandData {BeatLength = 3}, new[]
			{
				new RhythmCommandSequence(0, RhythmKeys.Up),
				new RhythmCommandSequence(1, RhythmKeys.Up),
				new RhythmCommandSequence(2, RhythmKeys.Right),
				new RhythmCommandSequence(3, RhythmKeys.Right),
			});
			EntityManager.AddComponent(skip, typeof(SkipCommand));
		}

		private Entity Build(RhythmCommandBuilder builder, RhythmCommandData rhythmCommandData, RhythmCommandSequence[] sequences)
		{
			var entity = builder.GetOrCreate(new NativeArray<RhythmCommandSequence>(sequences, Allocator.Temp));

			EntityManager.AddComponent(entity, typeof(DefaultRhythmCommand));
			EntityManager.SetOrAddComponentData(entity, rhythmCommandData);

			return entity;
		}

		protected override void OnUpdate()
		{

		}
	}
}