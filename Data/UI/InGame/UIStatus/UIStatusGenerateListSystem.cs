using System.Collections.Generic;
using Patapon4TLB.Core;
using Patapon4TLB.UI.InGame;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Patapon4TLB.UI
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class UIStatusGenerateListSystem : UIGameSystemBase
	{
		private GameObject m_UIRoot;
		private List<UIStatusPresentation> m_PresentationList;

		private EntityQuery m_UnitQuery;
		
		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			m_PresentationList = new List<UIStatusPresentation>();
			m_UnitQuery = GetEntityQuery(typeof(UnitDescription), typeof(Relative<TeamDescription>));
		}

		protected override void OnUpdate()
		{
			var selfGamePlayer = GetFirstSelfGamePlayer();
			var cameraState    = GetCurrentCameraState(selfGamePlayer);
			if (cameraState.Target == default)
			{
				ManageForUnits(default);
			}

			if (!TryGetRelative<TeamDescription>(cameraState.Target, out var teamEntity))
			{
				var units = new NativeArray<Entity>(1, Allocator.Temp);
				if (EntityManager.HasComponent<UnitDescription>(cameraState.Target))
				{
					units[0] = cameraState.Target;
				}
				else if (TryGetRelative<UnitDescription>(cameraState.Target, out var relativeUnit))
				{
					if (relativeUnit == cameraState.Target)
						units[0] = cameraState.Target;
					else
					{
						units[0] = relativeUnit;
						Debug.Log("Relative? " + relativeUnit);
					}
				}

				ManageForUnits(units);
				units.Dispose();
			}
			else
			{
				var units = new NativeList<Entity>(16, Allocator.Temp);

				var entityType       = GetArchetypeChunkEntityType();
				var relativeTeamType = GetArchetypeChunkComponentType<Relative<TeamDescription>>(true);

				using (var chunks = m_UnitQuery.CreateArchetypeChunkArray(Allocator.TempJob))
				{
					foreach (var chunk in chunks)
					{
						var entityArray       = chunk.GetNativeArray(entityType);
						var relativeTeamArray = chunk.GetNativeArray(relativeTeamType);

						for (int i = 0, count = chunk.Count; i < count; ++i)
						{
							if (relativeTeamArray[i].Target != teamEntity)
								continue;

							units.Add(entityArray[i]);
						}
					}
				}

				ManageForUnits(units);
				units.Dispose();
			}
		}

		private void ManageForUnits(NativeArray<Entity> entities)
		{
			if (entities == default || !entities.IsCreated)
			{
				
				return;
			}
		}
	}
}