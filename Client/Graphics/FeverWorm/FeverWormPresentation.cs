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

		public TextMeshPro[] comboCountLabels;
		public TextMeshPro[] comboInfoLabels;

		public TextMeshPro[]    feverLabels;
		public VertexGradient feverLabelGradientNormal;
		public VertexGradient feverLabelGradientHighlighted;
		public Color          feverLabelOutlineNormal;
		public Color          feverLabelOutlineHighlighted;

		public GameObject comboFrame;
		public GameObject feverFrame;

		public Transform[] ranges;
		
		// set from animator
		public float currentPulse;

		public Color[] infoColors = new Color[]
		{
			new Color32(184, 208, 130, 255),
			new Color32(194, 186, 213, 255),
			new Color32(219, 218, 50, 255),
			new Color32(151, 184, 217, 255),
			new Color32(227, 180, 195, 255),
			new Color32(208, 150, 54, 255),
		};

		private static readonly int Progression    = Shader.PropertyToID("_SummonPercentage");
		private static readonly int DirectProgression    = Shader.PropertyToID("_SummonDirectPercentage");
		private static readonly int IsFever        = Shader.PropertyToID("_IsFever");
		private static readonly int SummonPosition = Shader.PropertyToID("_SummonPosition");

		private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");

		private StringBuilder m_ComboCountStrBuilder;
		private StringBuilder m_ComboInfoStrBuilder;
		private string[]      m_ComputedColorStrings;

		public Animator Animator { get; private set; }

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

			Animator = GetComponent<Animator>();
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
			
			foreach (var label in comboInfoLabels)
			{
				label.SetText(m_ComboInfoStrBuilder);
			}
		}

		public void SetProgression(float comboScore, int combo, float interpolatedProgression, float directProgression, bool isFever)
		{
			if (mpb == null)
				return;
			
			foreach (var r in rendererArray)
			{
				r.GetPropertyBlock(mpb);
				{
					mpb.SetFloat(Progression, interpolatedProgression);
					mpb.SetFloat(DirectProgression, directProgression);
					mpb.SetInt(IsFever, isFever ? 1 : 0);
					mpb.SetVector(SummonPosition, new Vector4(ranges[0].position.x, ranges[1].position.x, 0, 0));
				}
				r.SetPropertyBlock(mpb);
			}

			m_ComboCountStrBuilder.Clear();
			m_ComboCountStrBuilder.Append(combo);
			foreach (var label in comboCountLabels)
			{
				label.SetText(m_ComboCountStrBuilder);
			}

			comboFrame.SetActive(!isFever);
			feverFrame.SetActive(isFever);
		}

		public void SetColors(float factor)
		{
			if (mpb == null)
				return;
			
			Color          outline;
			VertexGradient gradient;

			outline = Color.Lerp(feverLabelOutlineNormal, feverLabelOutlineHighlighted, factor);

			gradient.topLeft     = Color.Lerp(feverLabelGradientNormal.topLeft, feverLabelGradientHighlighted.topLeft, factor);
			gradient.topRight    = Color.Lerp(feverLabelGradientNormal.topRight, feverLabelGradientHighlighted.topRight, factor);
			gradient.bottomLeft  = Color.Lerp(feverLabelGradientNormal.bottomLeft, feverLabelGradientHighlighted.bottomLeft, factor);
			gradient.bottomRight = Color.Lerp(feverLabelGradientNormal.bottomRight, feverLabelGradientHighlighted.bottomRight, factor);

			foreach (var label in feverLabels)
			{
				label.outlineColor  = outline;
				label.colorGradient = gradient;
			}

		}
	}
}