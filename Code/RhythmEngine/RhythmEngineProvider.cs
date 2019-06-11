using package.patapon.core;
using package.patapon.def.Data;
using Runtime.EcsComponents;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.NetCode;

namespace Patapon4TLB.Default
{
	public class RhythmEngineProvider : BaseProviderBatch<RhythmEngineProvider.Create>
	{
		public struct Create
		{
			/// <summary>
			/// Default '500ms'
			/// </summary>
			public int? BeatInterval;

			/// <summary>
			/// Default '4'
			/// </summary>
			public int? MaxBeats;
		}

		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = new[]
			{
				ComponentType.ReadWrite<Owner>(),
				ComponentType.ReadWrite<NetworkOwner>(),
				ComponentType.ReadWrite<RhythmEngineSettings>(),
				ComponentType.ReadWrite<RhythmEngineState>(),
				ComponentType.ReadWrite<RhythmEngineCurrentCommand>(),
				ComponentType.ReadWrite<RhythmEngineClientPredictedCommand>(),
				ComponentType.ReadWrite<RhythmEngineClientRequestedCommand>(),
				ComponentType.ReadWrite<FlowRhythmEngineProcess>(),
				ComponentType.ReadWrite<FlowRhythmEngineSimulateTag>(),
				ComponentType.ReadWrite<ShardRhythmEngine>(),
				ComponentType.ReadWrite<FlowCommandState>(),
				ComponentType.ReadWrite<FlowCurrentCommand>(),
				ComponentType.ReadWrite<GhostComponent>(),
			};
		}

		public override void SetEntityData(Entity entity, Create data)
		{
			EntityManager.SetComponentData(entity, new RhythmEngineSettings {MaxBeats = data.MaxBeats ?? 4, BeatInterval = data.BeatInterval ?? 500});
		}
	}
}