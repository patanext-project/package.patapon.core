using System.Text;
using Misc.Extensions;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon.Mixed.Units;
using Patapon4TLB.Default.Player;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Systems;
using TMPro;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

namespace Patapon4TLB.GameModes.Interface
{
	public class UIHeadOnUnitStatusPresentation : RuntimeAssetPresentation<UIHeadOnUnitStatusPresentation>
	{
		public RectTransform Rescale;
		
		public Image           HealthGauge;
		public MaskableGraphic TeamCircleQuad;
		public MaskableGraphic PossessionOutlineQuad;
		public Gradient        Gradient;

		public RectTransform RespawnQuad;
		public TextMeshProUGUI RespawnLabel;

		private StringBuilder m_StringBuilder;

		public override void OnBackendSet()
		{
			GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
			m_StringBuilder = new StringBuilder();
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
			Rescale.localScale = new Vector3
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

		public void SetRespawnMilliseconds(int ms)
		{
			if (ms < 100)
			{
				RespawnQuad.gameObject.SetActive(false);
				return;
			}

			RespawnQuad.gameObject.SetActive(true);
			m_StringBuilder.Clear();
			m_StringBuilder.Append(ms / 1000);
			RespawnLabel.SetText(m_StringBuilder);
		}

		public override void OnReset()
		{
			base.OnReset();
		}
	}

	public class UIHeadOnUnitStatusBackend : RuntimeAssetBackend<UIHeadOnUnitStatusPresentation>
	{
		public UIHeadOnDrawerType DrawerType;

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
		public Entity LocalTeam;

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

			if (this.TryGetCurrentCameraState(LocalPlayer, out var cameraState)
			    && EntityManager.TryGetComponentData(cameraState.Target, out Relative<TeamDescription> relativeTeam))
			{
				LocalTeam = relativeTeam.Target;
			}
		}

		protected override void Render(UIHeadOnUnitStatusPresentation definition)
		{
			var backend      = (UIHeadOnUnitStatusBackend) definition.Backend;
			var targetEntity = definition.Backend.DstEntity;
			var armyIndex    = 0;

			var targetDrawerType = UIHeadOnDrawerType.Enemy;
			if (EntityManager.TryGetComponentData(targetEntity, out Relative<TeamDescription> relativeTeam))
			{
				if (EntityManager.HasComponent<Relative<ClubDescription>>(relativeTeam.Target))
				{
					var relativeClub = EntityManager.GetComponentData<Relative<ClubDescription>>(relativeTeam.Target).Target;
					var clubInfo     = EntityManager.GetComponentData<ClubInformation>(relativeClub);

					definition.SetTeamColor(clubInfo.PrimaryColor);
				}

				if (relativeTeam.Target == LocalTeam)
					targetDrawerType = UIHeadOnDrawerType.Ally;
			}

			if (EntityManager.TryGetComponentData(targetEntity, out LivableHealth livableHealth))
			{
				definition.SetHealth(livableHealth.Value, livableHealth.Max);
				if (livableHealth.ShouldBeDead())
					targetDrawerType = UIHeadOnDrawerType.DeadUnit;
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

			EntityManager.TryGetComponentData(targetEntity, out VersusHeadOnUnit gmUnit, new VersusHeadOnUnit {DeadCount = -1, TickBeforeSpawn = -1});
			definition.SetRespawnMilliseconds(UTick.CopyDelta(ServerTick, gmUnit.TickBeforeSpawn).Ms - ServerTick.Ms);

			var drawerPosition = Hud.GetPositionOnDrawer(EntityManager.GetComponentData<Translation>(targetEntity).Value, DrawerAlignment.Bottom);
			drawerPosition.y += (armyIndex % 4) * 25 + 3;
			drawerPosition.z =  0;

			backend.rectTransform.localPosition = drawerPosition;

			if (backend.DrawerType != targetDrawerType)
			{
				backend.DrawerType = targetDrawerType;
				backend.transform.SetParent(Hud.DrawerFrame.GetDrawer(targetDrawerType), false);
			}
		}

		protected override void ClearValues()
		{
			LocalPlayer = default;
		}
	}
}