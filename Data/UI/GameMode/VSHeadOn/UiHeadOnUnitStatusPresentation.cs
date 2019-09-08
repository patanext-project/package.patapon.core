using System;
using Patapon4TLB.UI.GameMode.VSHeadOn;
using Patapon4TLB.UI.InGame;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.Shared.Gen;
using Unity.Collections;
using Unity.Entities;
using Revolution.NetCode;
using UnityEngine;
using UnityEngine.UI;

namespace Patapon4TLB.UI
{
	public class UiHeadOnUnitStatusBackend : RuntimeAssetBackend<UiHeadOnUnitStatusPresentation>
	{
		public UiHeadOnPresentation Hud;

		public RectTransform rectTransform { get; private set; }

		public override void OnTargetUpdate()
		{
			DstEntityManager.AddComponentData(BackendEntity, RuntimeAssetDisable.All);
		}
		
		public override void OnComponentEnabled()
		{
			rectTransform = GetComponent<RectTransform>();
		}
	}

	public class UiHeadOnUnitStatusPresentation : RuntimeAssetPresentation<UiHeadOnUnitStatusPresentation>
	{
		public Image HealthGauge;
		public MaskableGraphic TeamCircleQuad;
		public MaskableGraphic PossessionOutlineQuad;
		public Gradient Gradient;

		public override void OnBackendSet()
		{
			GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
		}

		public void SetHealth(int value, int max)
		{
			var real = 0.0f;
			if (value > 0 && max > 0)
			{
				real = value / (float) max;
			}

			HealthGauge.fillAmount = real;
			HealthGauge.color = Gradient.Evaluate(real);
		}

		public void SetTeamColor(Color color)
		{
			TeamCircleQuad.color = color;
		}

		public void SetPossessionColor(Color color)
		{
			PossessionOutlineQuad.color = color;
		}

		public override void OnReset()
		{
			base.OnReset();
		}
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[UpdateAfter(typeof(UiHeadOnStatusGenerateList))]
	public class UiHeadOnUnitStatusPresentationSystem : UIGameSystemBase
	{
		private struct SortBackend : IComparable<SortBackend>
		{
			public int Team;
			public LivableHealth Health;
			
			public int index;

			public Entity entity;

			public int CompareTo(SortBackend other)
			{
				return other.Team - Team;
			}
		}
		
		private EntityQuery m_Query;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Query = GetEntityQuery(typeof(UiHeadOnUnitStatusBackend));
		}

		protected override void OnUpdate()
		{
			return;
			
			var length = m_Query.CalculateEntityCount();
			var sorted = new NativeArray<SortBackend>(length, Allocator.Temp);

			UiHeadOnUnitStatusBackend backend = null;
			foreach (var (i, entity) in this.ToEnumerator_C(m_Query, ref backend))
			{
				LivableHealth health = default;
				if (EntityManager.HasComponent<LivableHealth>(backend.DstEntity))
				{
					health = EntityManager.GetComponentData<LivableHealth>(backend.DstEntity);
				}

				if (EntityManager.HasComponent<Relative<TeamDescription>>(backend.DstEntity))
				{
					
				}
				
				sorted[i] = new SortBackend {index = i, Health = health, entity = entity};
			}
			
			/*Entities.With(m_Query).ForEach((UiHeadOnUnitStatusBackend backend) =>
			{
				var chunk = EntityManager.GetChunk(backend.DstEntity);
				if (!chunk.Has(GetArchetypeChunkComponentType<Translation>()))
					return;
				
				var entityPosition = EntityManager.GetComponentData<Translation>(backend.DstEntity).Value;
				var drawerPosition = backend.Hud.GetPositionOnDrawer(entityPosition);
				
				if (backend.Presentation != null)
				{
					var presentation = backend.Presentation;
					if (chunk.Has(GetArchetypeChunkComponentType<LivableHealth>()))
					{
						var livableHealth = EntityManager.GetComponentData<LivableHealth>(backend.DstEntity);
						presentation.SetHealth(livableHealth.Value, livableHealth.Max);
					}

					if (chunk.Has(GetArchetypeChunkComponentType<Relative<TeamDescription>>()))
					{
						var relativeTeam = EntityManager.GetComponentData<Relative<TeamDescription>>(backend.DstEntity);
						if (EntityManager.HasComponent<Relative<ClubDescription>>(relativeTeam.Target))
						{
							var relativeClub = EntityManager.GetComponentData<Relative<ClubDescription>>(relativeTeam.Target).Target;
							var clubInfo     = EntityManager.GetComponentData<ClubInformation>(relativeClub);

							presentation.SetTeamColor(clubInfo.PrimaryColor);
						}
					}

					if (chunk.Has(GetArchetypeChunkComponentType<Relative<PlayerDescription>>()))
					{
						var relativePlayer = EntityManager.GetComponentData<Relative<PlayerDescription>>(backend.DstEntity);
						if (GetFirstSelfGamePlayer() == relativePlayer.Target)
						{
							presentation.SetPossessionColor(new Color32(242, 255, 36, 255));
						}
						else
						{
							presentation.SetPossessionColor(Color.clear);
						}
					}
					else
					{
						presentation.SetPossessionColor(Color.clear);
					}
				}
				
				backend.rectTransform.localPosition = new Vector3
				{
					x = drawerPosition.x,
					y = 0,
					z = 0
				};
			});*/
		}
	}
}