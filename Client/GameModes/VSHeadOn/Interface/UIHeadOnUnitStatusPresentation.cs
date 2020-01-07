using Misc.Extensions;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.Units;
using Patapon4TLB.Default.Player;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Systems;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

namespace Patapon4TLB.GameModes.Interface
{
	public class UIHeadOnUnitStatusPresentation : RuntimeAssetPresentation<UIHeadOnUnitStatusPresentation>
	{
		public Image           HealthGauge;
		public MaskableGraphic TeamCircleQuad;
		public MaskableGraphic PossessionOutlineQuad;
		public Gradient        Gradient;

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
			HealthGauge.color      = Gradient.Evaluate(real);
		}

		public void SetDirection(UnitDirection direction)
		{
			transform.localScale = new Vector3
			{
				x = direction.Value,
				y = 1, z = 1
			};
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

	public class UIHeadOnUnitStatusBackend : RuntimeAssetBackend<UIHeadOnUnitStatusPresentation>
	{
		private RectTransform m_RectTransform;
		public  RectTransform rectTransform => m_RectTransform;

		public override void OnTargetUpdate()
		{
			DstEntityManager.AddComponentData(BackendEntity, RuntimeAssetDisable.All);
		}

		public override void OnComponentEnabled()
		{
			if (!gameObject.TryGetComponent(out m_RectTransform))
				m_RectTransform = gameObject.AddComponent<RectTransform>();
		}
	}

	[UpdateInGroup(typeof(OrderGroup.Presentation.InterfaceRendering))]
	[UpdateAfter(typeof(UIHeadOnPresentation))]
	public class UIHeadOnUnitStatusRenderSystem : BaseRenderSystem<UIHeadOnUnitStatusPresentation>
	{
		public Entity               LocalPlayer;
		public UIHeadOnPresentation Hud;

		private EntityQuery m_HudQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_HudQuery = GetEntityQuery(typeof(UIHeadOnPresentation));

			RequireForUpdate(m_HudQuery);
		}

		protected override void PrepareValues()
		{
			LocalPlayer = this.GetFirstSelfGamePlayer();
			Hud         = EntityManager.GetComponentObject<UIHeadOnPresentation>(m_HudQuery.GetSingletonEntity());
		}

		protected override void Render(UIHeadOnUnitStatusPresentation definition)
		{
			var backend      = (UIHeadOnUnitStatusBackend) definition.Backend;
			var targetEntity = definition.Backend.DstEntity;
			var armyIndex    = 0;
			if (EntityManager.TryGetComponentData(targetEntity, out LivableHealth livableHealth))
			{
				definition.SetHealth(livableHealth.Value, livableHealth.Max);
			}

			if (EntityManager.TryGetComponentData(targetEntity, out Relative<TeamDescription> relativeTeam))
			{
				if (EntityManager.HasComponent<Relative<ClubDescription>>(relativeTeam.Target))
				{
					var relativeClub = EntityManager.GetComponentData<Relative<ClubDescription>>(relativeTeam.Target).Target;
					var clubInfo     = EntityManager.GetComponentData<ClubInformation>(relativeClub);

					definition.SetTeamColor(clubInfo.PrimaryColor);
				}
			}

			if (EntityManager.TryGetComponentData(targetEntity, out Relative<PlayerDescription> relativePlayer))
			{
				if (relativePlayer.Target == LocalPlayer)
				{
					definition.SetPossessionColor(new Color32(242, 255, 36, 255));
				}
				else
				{
					definition.SetPossessionColor(Color.clear);
				}
			}
			else
			{
				definition.SetPossessionColor(Color.clear);
			}

			if (EntityManager.TryGetComponentData(targetEntity, out UnitAppliedArmyFormation unitFormation))
			{
				armyIndex = unitFormation.ArmyIndex;
			}

			EntityManager.TryGetComponentData(targetEntity, out UnitDirection direction, UnitDirection.Right);
			definition.SetDirection(direction);

			var drawerPosition = Hud.GetPositionOnDrawer(EntityManager.GetComponentData<Translation>(targetEntity).Value, DrawerAlignment.Bottom);
			drawerPosition.y += (armyIndex % 4) * 25 + 3;
			drawerPosition.z =  0;

			backend.rectTransform.localPosition = drawerPosition;
		}

		protected override void ClearValues()
		{
			LocalPlayer = default;
		}
	}
}