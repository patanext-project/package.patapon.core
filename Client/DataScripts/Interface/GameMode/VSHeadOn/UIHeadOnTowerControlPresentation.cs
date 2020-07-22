using System;
using GameBase.Roles.Components;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace PataNext.Client.DataScripts.Interface.GameMode.VSHeadOn
{
	public class UIHeadOnTowerControlPresentation : UIHeadOnStructurePresentationBase
	{
		private bool? m_GaugeActiveState;

		public UITowerControlGauge[] gauges;
		public GameObject            GaugeActiveGo;
		public GameObject            GaugeInactiveGo;
		public Graphic[]             graphics;

		public override void SetTeamColor(Color primary, Color secondary)
		{
			for (var i = 0; i != graphics.Length; i++)
			{
				graphics[i].color = primary;
			}
		}

		public void SetGaugeColor(int team, Color color)
		{
			gauges[team].SetColor(color);
		}

		public void SetGaugeActive(bool value)
		{
			if (m_GaugeActiveState != value)
			{
				m_GaugeActiveState = value;
				GaugeActiveGo.SetActive(value);
				GaugeInactiveGo.SetActive(!value);
			}
		}

		private float[] m_PreviousProgressions = new float[2];

		public void UpdateProgression(Span<float> progressions)
		{
			for (var i = 0; i != progressions.Length; i++)
			{
				gauges[i].SetProgression(progressions[i]);
				if (m_PreviousProgressions[i] != progressions[i])
				{
					var capturingCount = 0;
					if (m_PreviousProgressions[i] < progressions[i])
						capturingCount++;

					gauges[i].SetCapturingCount(capturingCount);
					m_PreviousProgressions[i] = progressions[i];
					if (capturingCount > 0)
					{
						gauges[i].transform.SetAsLastSibling();
					}
				}
				else
				{
					gauges[i].SetCapturingCount(0);
				}
			}
		}
	}

	public unsafe class UIHeadOnTowerControlRenderSystem : UIHeadOnStructureBaseRenderSystem<UIHeadOnTowerControlPresentation>
	{
		public override bool DefaultBehavior => true;

		protected override void Render(UIHeadOnTowerControlPresentation definition)
		{
			base.Render(definition);

			var targetEntity = definition.Backend.DstEntity;
			var data         = EntityManager.GetComponentData<HeadOnStructure>(targetEntity);

			definition.SetGaugeActive(TeamRelative.Target == default);
			if (TeamRelative.Target == default)
				definition.Backend.transform.localScale = Vector3.one;

			Span<float> progressions = stackalloc float[2];
			for (var i = 0; i < progressions.Length; i++)
			{
				ref var value = ref progressions[i];
				if (data.CaptureProgress[i] > 0 && data.TimeToCapture > 0)
					value = data.CaptureProgress[i] / (float) data.TimeToCapture;
			}

			for (var i = 0; i != 2; i++)
			{
				var team = i == 0 ? GameMode.Team0 : GameMode.Team1;
				var color = Color.gray;
				if (EntityManager.TryGetComponentData(team, out Relative<ClubDescription> clubDesc))
				{
					color = EntityManager.GetComponentData<ClubInformation>(clubDesc.Target).PrimaryColor;
				}
				definition.SetGaugeColor(i, color);
			}

			definition.UpdateProgression(progressions);
		}
	}
}