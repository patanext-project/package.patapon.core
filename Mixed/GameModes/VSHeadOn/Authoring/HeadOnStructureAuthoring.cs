using Revolution;
using Scripts.Utilities;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SocialPlatforms.Impl;

namespace Patapon.Mixed.GameModes.VSHeadOn
{
	public class HeadOnStructureAuthoring : MonoBehaviour, IConvertGameObjectToEntity
	{
		public float CaptureTime;

		public float HealthModifier;
		public HeadOnDefineTeamAuthoring TeamDefine;
		
		[FormerlySerializedAs("Type")]
		public HeadOnStructure.EScoreType     scoreType;

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentData(entity, new HeadOnStructure {ScoreType = scoreType, HealthModifier = HealthModifier, TimeToCapture = (int)(CaptureTime * 1000)});
			dstManager.AddComponentData(entity, new Relative<TeamDescription>());
			if (TeamDefine != null)
				dstManager.AddComponentData(entity, TeamDefine.FindOrCreate(dstManager));
			dstManager.AddComponent(entity, typeof(GhostEntity));
		}
	}

	public unsafe struct HeadOnStructure : IReadWriteComponentSnapshot<HeadOnStructure>, ISnapshotDelta<HeadOnStructure>
	{
		/// <summary>
		///     A type will give different points
		/// </summary>
		public enum EScoreType
		{
			TowerControl,
			Tower,
			Wall
		}

		public EScoreType ScoreType;
		public float        HealthModifier;

		public       int TimeToCapture;
		public fixed int CaptureProgress[2];

		public void WriteTo(DataStreamWriter writer, ref HeadOnStructure baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedUInt((uint) ScoreType, jobData.NetworkCompressionModel);
			writer.WritePackedInt(TimeToCapture, jobData.NetworkCompressionModel);
			writer.WritePackedInt(CaptureProgress[0], jobData.NetworkCompressionModel);
			writer.WritePackedInt(CaptureProgress[1], jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref HeadOnStructure baseline, DeserializeClientData jobData)
		{
			ScoreType          = (EScoreType) reader.ReadPackedUInt(ref ctx, jobData.NetworkCompressionModel);
			TimeToCapture      = reader.ReadPackedInt(ref ctx, jobData.NetworkCompressionModel);
			CaptureProgress[0] = reader.ReadPackedInt(ref ctx, jobData.NetworkCompressionModel);
			CaptureProgress[1] = reader.ReadPackedInt(ref ctx, jobData.NetworkCompressionModel);
		}

		public bool DidChange(HeadOnStructure baseline)
		{
			return UnsafeUtilityOp.AreNotEquals(ref this, ref baseline);
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronize : MixedComponentSnapshotSystemDelta<HeadOnStructure>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}

	// The gamemode will create a better event
	public struct HeadOnStructureOnCapture
	{
		public Entity Source;
	}
}