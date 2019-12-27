using System.Collections.Generic;
using System.Text;
using StormiumTeam.GameBase;
using TMPro;
using UnityEngine;

namespace package.patapon.core.FeverWorm
{
	public class FeverWormPresentation : RuntimeAssetPresentation<FeverWormPresentation>
	{
		public MaterialPropertyBlock mpb;

		[Header("Properties")]
		public List<Renderer> rendererArray;

		public TextMeshPro comboCountLabel;
		public TextMeshPro comboInfoLabel;

		public Color[] infoColors = new Color[]
		{
			new Color32(184, 208, 130, 255),
			new Color32(194, 186, 213, 255),
			new Color32(219, 218, 50, 255),
			new Color32(151, 184, 217, 255),
			new Color32(227, 180, 195, 255),
			new Color32(208, 150, 54, 255),
		};

		private static readonly int Progression = Shader.PropertyToID("_SummonPercentage");
		private static readonly int IsFever     = Shader.PropertyToID("_IsFever");

		public float progression;
		public bool  fever;

		private StringBuilder m_ComboCountStrBuilder;
		private StringBuilder m_ComboInfoStrBuilder;
		private string[]      m_ComputedColorStrings;

		private void OnEnable()
		{
			mpb                    = new MaterialPropertyBlock();
			m_ComboCountStrBuilder = new StringBuilder();
			m_ComboInfoStrBuilder  = new StringBuilder();

			m_ComputedColorStrings = new string[infoColors.Length];
			for (var i = 0; i != infoColors.Length; i++)
			{
				m_ComputedColorStrings[i] = ColorUtility.ToHtmlStringRGB(infoColors[i]);
			}

			SetComboString("Combo!");
		}

		private void OnDisable()
		{
			mpb.Clear();
			mpb = null;
		}

		public void SetComboString(string str)
		{
			const string xmlColorOpenKey  = "<color=#";
			const string xmlColorCloseKey = ">";

			m_ComboInfoStrBuilder.Clear();
			for (var i = 0; i != str.Length; i++)
			{
				m_ComboInfoStrBuilder.Append(xmlColorOpenKey);
				m_ComboInfoStrBuilder.Append(m_ComputedColorStrings[i % m_ComputedColorStrings.Length]);
				m_ComboInfoStrBuilder.Append(xmlColorCloseKey);
				m_ComboInfoStrBuilder.Append(str[i]);
			}

			comboInfoLabel.SetText(m_ComboInfoStrBuilder);
		}

		public void SetProgression(float comboScore, int combo, float specialProgression, bool isFever)
		{
			foreach (var r in rendererArray)
			{
				r.GetPropertyBlock(mpb);
				{
					mpb.SetFloat(Progression, specialProgression);
					mpb.SetInt(IsFever, isFever ? 1 : 0);

				}
				r.SetPropertyBlock(mpb);
			}

			m_ComboCountStrBuilder.Clear();
			m_ComboCountStrBuilder.Append(combo);
			comboCountLabel.SetText(m_ComboCountStrBuilder);
		}
	}
}