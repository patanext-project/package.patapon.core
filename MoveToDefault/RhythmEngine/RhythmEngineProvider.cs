using package.patapon.core;
using package.patapon.def.Data;
using StormiumShared.Core.Networking;
using StormiumTeam.GameBase;
using Unity.Entities;

namespace Patapon4TLB.Default
{
	public class RhythmEngineProvider : SystemProvider
	{
		public override void GetComponents(out ComponentType[] entityComponents, out ComponentType[] excludedStreamerComponents)
		{
			entityComponents = new[]
			{
				ComponentType.ReadWrite<DefaultRhythmEngineData.Settings>(),
				ComponentType.ReadWrite<DefaultRhythmEngineData.Predicted>(),
				ComponentType.ReadWrite<DefaultRhythmEngineCurrentCommand>(),
				ComponentType.ReadWrite<FlowRhythmEngineSettingsData>(),
				ComponentType.ReadWrite<FlowRhythmEngineProcessData>(),
				ComponentType.ReadWrite<ShardRhythmEngine>(),
				ComponentType.ReadWrite<FlowCommandManagerTypeDefinition>(),
				ComponentType.ReadWrite<FlowCommandManagerSettingsData>(),
				ComponentType.ReadWrite<FlowCurrentCommand>(),
				
			};
			excludedStreamerComponents = null;
		}

		protected override Entity SpawnEntity(Entity origin, SnapshotRuntime snapshotRuntime)
		{
			var entity = base.SpawnEntity(origin, snapshotRuntime);

			EntityManager.SetComponentData(entity, new ShardRhythmEngine {EngineType = ComponentType.ReadWrite<FlowRhythmEngineTypeDefinition>()});
			EntityManager.SetComponentData(entity, new FlowRhythmEngineSettingsData(0.5f));
			
			EntityManager.SetComponentData(entity, new FlowCommandManagerSettingsData(4));
			
			return entity;
		}
	}
}