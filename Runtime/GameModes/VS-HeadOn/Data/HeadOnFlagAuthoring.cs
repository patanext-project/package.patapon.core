using System;
using Revolution;
using Revolution.Utils;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Networking.Transport;
using Unity.Transforms;
using UnityEngine;

namespace Patapon4TLB.GameModes
{
	public class HeadOnFlagAuthoring : MonoBehaviour, IConvertGameObjectToEntity
	{
		public HeadOnDefineTeamAuthoring TeamDefine;

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			Debug.Assert(TeamDefine != null, "TeamDefine != null");

			dstManager.AddComponentData(entity, new HeadOnFlag());
			dstManager.AddComponentData(entity, TeamDefine.FindOrCreate(dstManager));
			dstManager.AddComponent(entity, typeof(GhostEntity));
		}

		private void OnDrawGizmos()
		{
			Color color;
			if (TeamDefine != null)
			{
				switch (TeamDefine.PredefinedTeam)
				{
					case EHeadOnTeamTarget.Blue:
						color = Color.blue;
						break;
					case EHeadOnTeamTarget.Red:
						color = Color.red;
						break;
					case EHeadOnTeamTarget.Undefined:
						color = Color.green;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			else
			{
				color = Color.black;
			}

			Gizmos.color = Color.Lerp(color, Color.white, 0.33f);
			Gizmos.DrawWireSphere(transform.position, 0.25f);
		}
	}

	public struct HeadOnFlag : IComponentData
	{
	}

	public struct HeadOnFlagSnapshotData : ISnapshotData<HeadOnFlagSnapshotData>
	{
		public const int   Quantization   = 100;
		public const float DeQuantization = 1 / 100f;

		public uint            Style;
		public uint            DefinedTeam;
		public QuantizedFloat3 Position;
		public uint            Tick { get; set; }

		public void PredictDelta(uint tick, ref HeadOnFlagSnapshotData baseline1, ref HeadOnFlagSnapshotData baseline2)
		{
			throw new NotImplementedException();
		}

		public void Serialize(ref HeadOnFlagSnapshotData baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
		{
			writer.WritePackedUIntDelta(Style, baseline.Style, compressionModel);
			writer.WritePackedUIntDelta(DefinedTeam, baseline.DefinedTeam, compressionModel);
			for (var i = 0; i != 2; i++)
			{
				writer.WritePackedIntDelta(Position[i], baseline.Position[i], compressionModel);
			}
		}

		public void Deserialize(uint tick, ref HeadOnFlagSnapshotData baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
		{
			Tick = tick;

			Style       = reader.ReadPackedUIntDelta(ref ctx, baseline.Style, compressionModel);
			DefinedTeam = reader.ReadPackedUIntDelta(ref ctx, baseline.DefinedTeam, compressionModel);
			for (var i = 0; i != 2; i++)
			{
				Position[i] = reader.ReadPackedIntDelta(ref ctx, baseline.Position[i], compressionModel);
			}
		}

		public void Interpolate(ref HeadOnFlagSnapshotData target, float factor)
		{
			this = target;
		}
	}
}