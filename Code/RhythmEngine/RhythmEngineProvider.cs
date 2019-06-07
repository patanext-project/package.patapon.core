using package.patapon.core;
using package.patapon.def.Data;
using StormiumTeam.GameBase;
using Unity.Entities;

namespace Patapon4TLB.Default
{
	public class RhythmEngineProvider : BaseProviderBatch<RhythmEngineProvider.Create>
	{
		public struct Create
		{
			/// <summary>
			/// Default '0.5f'
			/// </summary>
			public float? BeatInterval;

			/// <summary>
			/// Default '4'
			/// </summary>
			public int? MaxBeats;
		}

		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = new[]
			{
				ComponentType.ReadWrite<DefaultRhythmEngineSettings>(),
				ComponentType.ReadWrite<DefaultRhythmEngineState>(),
				ComponentType.ReadWrite<DefaultRhythmEngineCurrentCommand>(),
				ComponentType.ReadWrite<FlowRhythmEngineSettingsData>(),
				ComponentType.ReadWrite<FlowRhythmEngineProcessData>(),
				ComponentType.ReadWrite<ShardRhythmEngine>(),
				ComponentType.ReadWrite<FlowCommandManagerTypeDefinition>(),
				ComponentType.ReadWrite<FlowCommandManagerSettingsData>(),
				ComponentType.ReadWrite<FlowCurrentCommand>(),

			};
		}

		public override void SetEntityData(Entity entity, Create data)
		{
			EntityManager.SetComponentData(entity, new ShardRhythmEngine {EngineType = ComponentType.ReadWrite<FlowRhythmEngineTypeDefinition>()});

			EntityManager.SetComponentData(entity, new FlowRhythmEngineSettingsData(data.BeatInterval ?? 0.5f));
			EntityManager.SetComponentData(entity, new FlowCommandManagerSettingsData(data.MaxBeats ?? 4));
		}
	}
}