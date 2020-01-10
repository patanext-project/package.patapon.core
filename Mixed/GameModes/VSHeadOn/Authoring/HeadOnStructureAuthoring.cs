using Revolution;
using StormiumTeam.GameBase;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Patapon.Mixed.GameModes.VSHeadOn
{
	public class HeadOnStructureAuthoring : MonoBehaviour, IConvertGameObjectToEntity
	{
		public float CaptureTime;

		public int                       HealthPercentage;
		public HeadOnDefineTeamAuthoring TeamDefine;
		
		[FormerlySerializedAs("Type")]
		public HeadOnStructure.EScoreType     scoreType;

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentData(entity, new HeadOnStructure {ScoreType = scoreType, HealthPercentage = HealthPercentage, TimeToCapture = (int)(CaptureTime * 1000)});
			dstManager.AddComponentData(entity, new Relative<TeamDescription>());
			if (TeamDefine != null)
				dstManager.AddComponentData(entity, TeamDefine.FindOrCreate(dstManager));
			dstManager.AddComponent(entity, typeof(GhostEntity));
		}
	}

	public unsafe struct HeadOnStructure : IComponentData
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
		public int   HealthPercentage;

		public       int TimeToCapture;
		public fixed int CaptureProgress[2];
	}

	// The gamemode will create a better event
	public struct HeadOnStructureOnCapture
	{
		public Entity Source;
	}
}