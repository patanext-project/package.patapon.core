using Revolution;
using StormiumTeam.GameBase;
using Unity.Entities;
using UnityEngine;

namespace Patapon.Mixed.GameModes.VSHeadOn
{
	public class HeadOnStructureAuthoring : MonoBehaviour, IConvertGameObjectToEntity
	{
		public int CaptureTime;

		public int                       HealthPercentage;
		public HeadOnDefineTeamAuthoring TeamDefine;
		public HeadOnStructure.EType     Type;

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentData(entity, new HeadOnStructure {Type = Type, TimeToCapture = CaptureTime});
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
		public enum EType
		{
			TowerControl,
			Tower,
			Wall
		}

		public EType Type;
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