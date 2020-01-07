using package.patapon.core;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Definitions;
using Revolution;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;

namespace Patapon.Mixed.Systems
{
	public class RegisterDefaultSequenceCommands : GameBaseSystem
	{
		protected override void OnCreate()
		{
			base.OnCreate();

			var builder = World.GetOrCreateSystem<RhythmCommandBuilder>();

			var march = Build(builder, new RhythmCommandDefinition {Identifier = new NativeString64("march"), BeatLength = 4}, new[]
			{
				new RhythmCommandDefinitionSequence(0, RhythmKeys.Left),
				new RhythmCommandDefinitionSequence(1, RhythmKeys.Left),
				new RhythmCommandDefinitionSequence(2, RhythmKeys.Left),
				new RhythmCommandDefinitionSequence(3, RhythmKeys.Right)
			});
			EntityManager.AddComponent(march, typeof(MarchCommand));

			var attack = Build(builder, new RhythmCommandDefinition {Identifier = new NativeString64("attack"), BeatLength = 4}, new[]
			{
				new RhythmCommandDefinitionSequence(0, RhythmKeys.Right),
				new RhythmCommandDefinitionSequence(1, RhythmKeys.Right),
				new RhythmCommandDefinitionSequence(2, RhythmKeys.Left),
				new RhythmCommandDefinitionSequence(3, RhythmKeys.Right)
			});
			EntityManager.AddComponent(attack, typeof(AttackCommand));

			var defend = Build(builder, new RhythmCommandDefinition {Identifier = new NativeString64("defend"), BeatLength = 4}, new[]
			{
				new RhythmCommandDefinitionSequence(0, RhythmKeys.Up),
				new RhythmCommandDefinitionSequence(1, RhythmKeys.Up),
				new RhythmCommandDefinitionSequence(2, RhythmKeys.Left),
				new RhythmCommandDefinitionSequence(3, RhythmKeys.Right)
			});
			EntityManager.AddComponent(defend, typeof(DefendCommand));

			var charge = Build(builder, new RhythmCommandDefinition {Identifier = new NativeString64("charge"), BeatLength = 4}, new[]
			{
				new RhythmCommandDefinitionSequence(0, RhythmKeys.Right),
				new RhythmCommandDefinitionSequence(1, RhythmKeys.Right),
				new RhythmCommandDefinitionSequence(2, RhythmKeys.Up),
				new RhythmCommandDefinitionSequence(3, RhythmKeys.Up)
			});
			EntityManager.AddComponent(charge, typeof(ChargeCommand));

			var retreat = Build(builder, new RhythmCommandDefinition {Identifier = new NativeString64("retreat"), BeatLength = 4}, new[]
			{
				new RhythmCommandDefinitionSequence(0, RhythmKeys.Right),
				new RhythmCommandDefinitionSequence(1, RhythmKeys.Left),
				new RhythmCommandDefinitionSequence(2, RhythmKeys.Right),
				new RhythmCommandDefinitionSequence(3, RhythmKeys.Left)
			});
			EntityManager.AddComponent(retreat, typeof(RetreatCommand));

			var jump = Build(builder, new RhythmCommandDefinition {Identifier = new NativeString64("jump"), BeatLength = 4}, new[]
			{
				new RhythmCommandDefinitionSequence(0, RhythmKeys.Down),
				new RhythmCommandDefinitionSequence(1, RhythmKeys.Down),
				new RhythmCommandDefinitionSequence(2, RhythmKeys.Up),
				new RhythmCommandDefinitionSequence(3, RhythmKeys.Up)
			});
			EntityManager.AddComponent(jump, typeof(JumpCommand));

			var party = Build(builder, new RhythmCommandDefinition {Identifier = new NativeString64("party"), BeatLength = 4}, new[]
			{
				new RhythmCommandDefinitionSequence(0, RhythmKeys.Left),
				new RhythmCommandDefinitionSequence(1, RhythmKeys.Right),
				new RhythmCommandDefinitionSequence(2, RhythmKeys.Down),
				new RhythmCommandDefinitionSequence(3, RhythmKeys.Up)
			});
			EntityManager.AddComponent(party, typeof(PartyCommand));

			var summon = Build(builder, new RhythmCommandDefinition {Identifier = new NativeString64("summon"), BeatLength = 4}, new[]
			{
				new RhythmCommandDefinitionSequence(0, 0, RhythmKeys.Down),
				new RhythmCommandDefinitionSequence(1, 0, RhythmKeys.Down),
				new RhythmCommandDefinitionSequence(1, 1, RhythmKeys.Down, 0.4f),
				new RhythmCommandDefinitionSequence(2, 1, RhythmKeys.Down),
				new RhythmCommandDefinitionSequence(3, 0, RhythmKeys.Down, 0.4f)
			});
			EntityManager.AddComponent(summon, typeof(SummonCommand));

			var backward = Build(builder, new RhythmCommandDefinition {Identifier = new NativeString64("backward"), BeatLength = 4}, new[]
			{
				new RhythmCommandDefinitionSequence(0, RhythmKeys.Up),
				new RhythmCommandDefinitionSequence(1, RhythmKeys.Left),
				new RhythmCommandDefinitionSequence(2, RhythmKeys.Up),
				new RhythmCommandDefinitionSequence(3, RhythmKeys.Left)
			});
			EntityManager.AddComponent(backward, typeof(BackwardCommand));

			var skip = Build(builder, new RhythmCommandDefinition {Identifier = new NativeString64("skip"), BeatLength = 3}, new[]
			{
				new RhythmCommandDefinitionSequence(0, RhythmKeys.Up),
				new RhythmCommandDefinitionSequence(1, RhythmKeys.Up),
				new RhythmCommandDefinitionSequence(2, RhythmKeys.Right),
				new RhythmCommandDefinitionSequence(3, RhythmKeys.Right)
			});
			EntityManager.AddComponent(skip, typeof(SkipCommand));
		}

		private Entity Build(RhythmCommandBuilder builder, RhythmCommandDefinition rhythmCommandData, RhythmCommandDefinitionSequence[] sequences)
		{
			var entity = builder.GetOrCreate(new NativeArray<RhythmCommandDefinitionSequence>(sequences, Allocator.Temp));

			EntityManager.AddComponent(entity, typeof(DefaultRhythmCommand));
			if (IsServer)
				EntityManager.AddComponent(entity, typeof(GhostEntity));

			EntityManager.SetOrAddComponentData(entity, rhythmCommandData);

			return entity;
		}

		protected override void OnUpdate()
		{
		}
	}
}