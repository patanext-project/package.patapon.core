using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Patapon.Mixed.GameModes.VSHeadOn
{
	public enum EHeadOnTeamTarget
	{
		Undefined = 0,
		Blue      = 1,
		Red       = 2
	}

	public class HeadOnDefineTeamAuthoring : MonoBehaviour
	{
		private readonly Dictionary<World, Entity> m_CurrentEntity = new Dictionary<World, Entity>();
		public           EHeadOnTeamTarget         PredefinedTeam;

		private Entity FindOrCreateEntity(EntityManager entityManager)
		{
			var query = entityManager.CreateEntityQuery(typeof(HeadOnTeam));
			using (var entities = query.ToEntityArray(Allocator.TempJob))
			using (var team = query.ToComponentDataArray<HeadOnTeam>(Allocator.TempJob))
			{
				for (var i = 0; i != team.Length; i++)
				{
					if (team[i].TeamIndex == (int) PredefinedTeam - 1)
						return entities[i];
				}
			}

			return entityManager.CreateEntity(typeof(HeadOnTeam));
		}
		
		public HeadOnTeamTarget FindOrCreate(EntityManager dstManager)
		{
			Debug.Assert(Application.isPlaying, "Application.isPlaying");

			Entity ent;
			if (!m_CurrentEntity.ContainsKey(dstManager.World))
			{
				// Create...
				ent = m_CurrentEntity[dstManager.World] = FindOrCreateEntity(dstManager);
				dstManager.SetComponentData(ent, new HeadOnTeam
				{
					IsPredefinedTeam = PredefinedTeam != EHeadOnTeamTarget.Undefined,
					TeamIndex        = (int) PredefinedTeam - 1
				});

				return new HeadOnTeamTarget
				{
					Custom    = PredefinedTeam != EHeadOnTeamTarget.Undefined ? default : ent,
					TeamIndex = (int) PredefinedTeam - 1
				};
			}

			ent = m_CurrentEntity[dstManager.World];

			var data = dstManager.GetComponentData<HeadOnTeam>(ent);
			return new HeadOnTeamTarget
			{
				Custom    = data.IsPredefinedTeam ? default : ent,
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