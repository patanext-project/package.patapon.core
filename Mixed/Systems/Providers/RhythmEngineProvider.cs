using Patapon.Mixed.GamePlay.RhythmEngine;
using Patapon.Mixed.GamePlay.Units;
using Patapon.Mixed.RhythmEngine.Flow;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Entities;

namespace Patapon.Mixed.RhythmEngine
{
	public class RhythmEngineProvider : BaseProviderBatch<RhythmEngineProvider.Create>
	{
		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = new[]
			{
				ComponentType.ReadWrite<EntityDescription>(),
				ComponentType.ReadWrite<RhythmEngineDescription>(),
				ComponentType.ReadWrite<RhythmEngineSettings>(),
				ComponentType.ReadWrite<RhythmEngineState>(),
				ComponentType.ReadWrite<RhythmEngineCommandProgression>(),
				ComponentType.ReadWrite<RhythmEngineClientPredictedCommandProgression>(),
				ComponentType.ReadWrite<RhythmEngineClientRequestedCommandProgression>(),
				ComponentType.ReadWrite<FlowEngineProcess>(),
				ComponentType.ReadWrite<FlowSimulateProcess>(),
				ComponentType.ReadWrite<GameCommandState>(),
				ComponentType.ReadWrite<GameComboState>(),
				ComponentType.ReadWrite<RhythmCurrentCommand>(),
				ComponentType.ReadWrite<RhythmHeroState>(), 
				typeof(PlayEntityTag)
			};
		}

		public override void SetEntityData(Entity entity, Create data)
		{
			EntityManager.SetComponentData(entity, EntityDescription.New<RhythmEngineDescription>());
			EntityManager.SetComponentData(entity, new RhythmEngineSettings {MaxBeats      = data.MaxBeats ?? 4, BeatInterval = data.BeatInterval ?? 500, UseClientSimulation = data.UseClientSimulation});
			EntityManager.SetComponentData(entity, new RhythmCurrentCommand {CustomEndTime = -1, ActiveAtTime                 = -1, Power                                     = 0});
			EntityManager.SetComponentData(entity, new GameComboState {JinnEnergyMax       = 350});
		}

		public struct Create
		{
			public bool UseClientSimulation;

			/// <summary>
			///     Default '500ms'
			/// </summary>
			public int? BeatInterval;

			/// <summary>
			///     Default '4'
			/// </summary>
			public int? MaxBeats;
		}
	}
}