using package.patapon.core;
using package.stormiumteam.shared.ecs;
using Revolution;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;
using Utilities;

namespace Patapon.Mixed.RhythmEngine.Definitions
{
	public struct RhythmCommandDefinitionSequence : IBufferElementData
	{
		public RangeInt BeatRange;
		public int      Key;
		public float MaxTimeDifference;

		public RhythmCommandDefinitionSequence(int beatFract, int key)
		{
			BeatRange = new RangeInt(beatFract, 0);
			Key       = key;
			MaxTimeDifference = -1;
		}
		
		public RhythmCommandDefinitionSequence(int beatFract, int beatFractLength, int key)
		{
			BeatRange              = new RangeInt(beatFract, beatFractLength);
			Key                    = key;
			MaxTimeDifference = -1;
		}

		public RhythmCommandDefinitionSequence(int beatFract, int beatFractLength, int key, float maxTimeDifference)
		{
			BeatRange = new RangeInt(beatFract, beatFractLength);
			Key       = key;
			this.MaxTimeDifference = maxTimeDifference;
		}

		public int BeatEnd => BeatRange.end;

		public bool ContainsInRange(int beatVal)
		{
			return BeatRange.start <= beatVal && BeatRange.end >= beatVal;
		}
	}

	public struct RhythmCommandDefinition : IReadWriteComponentSnapshot<RhythmCommandDefinition, DefaultSetup>, ISnapshotDelta<RhythmCommandDefinition>
	{
		public NativeString64 Identifier;
		public int            BeatLength;

		public void WriteTo(DataStreamWriter writer, ref RhythmCommandDefinition baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedStringDelta(Identifier, default(NativeString64), jobData.NetworkCompressionModel);
			writer.WritePackedInt(BeatLength, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref RhythmCommandDefinition baseline, DeserializeClientData jobData)
		{
			Identifier = reader.ReadPackedStringDelta(ref ctx, default(NativeString64), jobData.NetworkCompressionModel);
			BeatLength = reader.ReadPackedInt(ref ctx, jobData.NetworkCompressionModel);
		}

		public bool DidChange(RhythmCommandDefinition baseline)
		{
			return !baseline.Identifier.Equals(Identifier);
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronize : MixedComponentSnapshotSystemDelta<RhythmCommandDefinition>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		[UpdateInGroup(typeof(AfterSnapshotIsAppliedSystemGroup))]
		public class SynchronizeToLocal : ComponentSystem
		{
			private EntityQuery m_LocalCommands;

			private SnapshotReceiveSystem m_ReceiveSystem;
			private EntityQuery           m_ServerCommands;

			protected override void OnCreate()
			{
				base.OnCreate();

				m_LocalCommands  = GetEntityQuery(typeof(RhythmCommandDefinition), typeof(RhythmCommandBuilder.RhythmCommandEntityTag));
				m_ServerCommands = GetEntityQuery(typeof(RhythmCommandDefinition), typeof(ReplicatedEntity), ComponentType.Exclude<RhythmCommandBuilder.RhythmCommandEntityTag>());

				m_ReceiveSystem = World.GetOrCreateSystem<SnapshotReceiveSystem>();
			}

			protected override void OnUpdate()
			{
				if (m_LocalCommands.IsEmptyIgnoreFilter || m_ServerCommands.IsEmptyIgnoreFilter)
					return;

				var jobData = m_ReceiveSystem.JobData;
				if (!jobData.GhostToEntityMap.IsCreated)
					return;

				var commandsEntityArray           = m_LocalCommands.ToEntityArray(Allocator.TempJob);
				var commandsDefinitionArray       = m_LocalCommands.ToComponentDataArray<RhythmCommandDefinition>(Allocator.TempJob);
				var serverCommandsEntityArray     = m_ServerCommands.ToEntityArray(Allocator.TempJob);
				var serverCommandsDefinitionArray = m_ServerCommands.ToComponentDataArray<RhythmCommandDefinition>(Allocator.TempJob);
				for (var ent = 0; ent != commandsEntityArray.Length; ent++)
				{
					var definition = commandsDefinitionArray[ent];
					for (var serverEnt = 0; serverEnt != serverCommandsEntityArray.Length; serverEnt++)
					{
						var serverDefinition = serverCommandsDefinitionArray[serverEnt];
						//Debug.Log($"{definition.Identifier.ToString()} {serverDefinition.Identifier.ToString()}");
						if (!definition.Identifier.Equals(serverDefinition.Identifier))
							continue;

						var replicated = EntityManager.GetComponentData<ReplicatedEntity>(serverCommandsEntityArray[serverEnt]);
						jobData.SetEntityForGhost(replicated.GhostId, commandsEntityArray[ent]);
						EntityManager.SetOrAddComponentData(commandsEntityArray[ent], replicated);
						EntityManager.AddComponent(commandsEntityArray[ent], typeof(ManualDestroy));
						EntityManager.DestroyEntity(serverCommandsEntityArray[serverEnt]);
					}
				}

				commandsEntityArray.Dispose();
				commandsDefinitionArray.Dispose();
				serverCommandsEntityArray.Dispose();
				serverCommandsDefinitionArray.Dispose();
			}
		}
	}
}