using Unity.Entities;
using UnityEngine;

namespace Patapon4TLB.GameModes
{
	public class HeadOnTowerAuthoring : MonoBehaviour, IConvertGameObjectToEntity
	{
		public HeadOnDefineTeamAuthoring TeamDefine;

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentData(entity, TeamDefine.FindOrCreate(dstManager));
		}
	}
}