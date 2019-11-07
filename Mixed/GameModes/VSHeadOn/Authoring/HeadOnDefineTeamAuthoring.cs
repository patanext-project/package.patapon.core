using Unity.Entities;
using UnityEngine;

namespace Patapon.Mixed.GameModes.VSHeadOn
{
	public enum EHeadOnTeamTarget
	{
		Undefined = 0,
		Blue      = 1,
		Red       = 2,
	}

	public class HeadOnDefineTeamAuthoring : MonoBehaviour
	{
		public  EHeadOnTeamTarget PredefinedTeam;
		private Entity            m_CurrentEntity;

		public HeadOnTeamTarget FindOrCreate(EntityManager dstManager)
		{
			Debug.Assert(Application.isPlaying, "Application.isPlaying");

			if (m_CurrentEntity == Entity.Null)
			{
				// Create...
				m_CurrentEntity = dstManager.CreateEntity();
				dstManager.AddComponentData(m_CurrentEntity, new HeadOnTeam
				{
					IsPredefinedTeam = PredefinedTeam != EHeadOnTeamTarget.Undefined,
					TeamIndex        = (int) PredefinedTeam - 1
				});

				return new HeadOnTeamTarget
				{
					Custom    = PredefinedTeam != EHeadOnTeamTarget.Undefined ? default : m_CurrentEntity,
					TeamIndex = (int) PredefinedTeam - 1
				};
			}

			var data = dstManager.GetComponentData<HeadOnTeam>(m_CurrentEntity);
			return new HeadOnTeamTarget
			{
				Custom    = data.IsPredefinedTeam ? default : m_CurrentEntity,
				TeamIndex = data.TeamIndex
			};
		}
	}

	public struct HeadOnTeam : IComponentData
	{
		public bool IsPredefinedTeam;
		public int  TeamIndex;
	}

	public struct HeadOnTeamTarget : IComponentData
	{
		public Entity Custom;

		// <0 if custom, >=0 if not custom
		public int TeamIndex;
	}
}