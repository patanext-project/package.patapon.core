using Unity.Entities;
using UnityEngine;

namespace Patapon4TLB.GameModes
{
	public class HeadOnTowerAuthoring : MonoBehaviour, IConvertGameObjectToEntity
	{
		public HeadOnDefineTeamAuthoring TeamDefine;

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			if (TeamDefine == null)
			{
				Debug.LogError("No 'TeamDefine'!");
				return;
			}

			dstManager.AddComponentData(entity, TeamDefine.FindOrCreate(dstManager));
		}
	}
}