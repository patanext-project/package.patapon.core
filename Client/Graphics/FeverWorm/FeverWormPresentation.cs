using System.Collections.Generic;
using System.Text;
using StormiumTeam.GameBase.Utility.AssetBackend;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PataNext.Client.Graphics.FeverWorm
{
	public class FeverWormPresentation : RuntimeAssetPresentation<FeverWormPresentation>
	{
		private static readonly int Progression       = Shader.PropertyToID("_SummonPercentage");
		private static readonly int DirectProgression = Shader.PropertyToID("_SummonDirectPercentage");
		private static readonly int IsFever           = Shader.PropertyToID("_IsFever");
		private static readonly int SummonReady           = Shader.PropertyToID("_SummonReady");
		private static readonly int SummonPosition    = Shader.PropertyToID("_SummonPosition");

		private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");

		public TextMeshPro[] comboCountLabels;

		public GameObject    comboFrame;
		public TextMeshPro[] comboInfoLabels;

		// set from animator
		public float          currentPulse;
		public GameObject     feverFrame;
		public VertexGradient feverLabelGradientHighlighted;
		public VertexGradient feverLabelGradientNormal;
		public Color          feverLabelOutlineHighlighted;
		public Color          feverLabelOutlineNormal;

		public TextMeshPro[] feverLabels;

		public Color[] infoColors =
		{
			new Color32(184, 208, 130, 255),
			new Color32(194, 186, 213, 255),
			new Color32(219, 218, 50, 255),
			new Color32(151, 184, 217, 255),
			new Color32(227, 180, 195, 255),
			new Color32(208, 150, 54, 255)
		};

		private StringBuilder         m_ComboCountStrBuilder;
		private StringBuilder         m_TranslationStrBuilder;
		private string[]              m_ComputedColorStrings;
		public  MaterialPropertyBlock mpb;

		public Transform[] ranges;

		[Header("Properties")]
		public List<Renderer> rendererArray;

		public Animator Animator { get; private set; }

		private void Awake()
		{
			Canvas.willRenderCanvases += () => OnRebuild(CanvasUpdate.Layout);
		}

		private void OnEnable()
		{
			mpb                     = new MaterialPropertyBlock();
			m_ComboCountStrBuilder  = new StringBuilder();
			m_TranslationStrBuilder = new StringBuilder();

			m_ComputedColorStrings = new string[infoColors.Length];
			for (var i = 0; i != infoColors.Length; i++) m_ComputedColorStrings[i] = ColorUtility.ToHtmlStringRGB(infoColors[i]);

			SetStrings("Combo!", "Fever!!");

			Animator = GetComponent<Animator>();
		}

		private void OnDisable()
		{
			mpb.Clear();
			mpb = null;
		}

		public void SetStrings(string combo, string fever)
		{
			const string xmlColorOpenKey  = "<color=#";
			const string xmlColorCloseKey = ">";

			m_TranslationStrBuilder.Clear();
			for (var i = 0; i != combo.Length; i++)
			{
				m_TranslationStrBuilder.Append(xmlColorOpenKey);
				m_TranslationStrBuilder.Append(m_ComputedColorStrings[i % m_ComputedColorStrings.Length]);
				m_TranslationStrBuilder.Append(xmlColorCloseKey);
				m_TranslationStrBuilder.Append(combo[i]);
			}

			foreach (var label in comboInfoLabels) label.SetText(m_TranslationStrBuilder);

			m_TranslationStrBuilder.Clear();

			foreach (var label in feverLabels) label.SetText(fever);
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
					mpb.SetInt(SummonReady, interpolatedProgression >= directProgression ? 1 : 0);
					//mpb.SetVector(SummonPosition, new Vector4(ranges[0].position.x, ranges[1].position.x, 0, 0));
				}
				r.SetPropertyBlock(mpb);
			}

			m_ComboCountStrBuilder.Clear();
			m_ComboCountStrBuilder.Append(combo);
			foreach (var label in comboCountLabels) label.SetText(m_ComboCountStrBuilder);

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

		public void OnRebuild(CanvasUpdate update)
		{
			if (mpb == null)
				return;
			
			foreach (var r in rendererArray)
			{
				r.GetPropertyBlock(mpb);
				{
					mpb.SetVector(SummonPosition, new Vector4(ranges[0].position.x, ranges[1].position.x, 0, 0));
				}
				r.SetPropertyBlock(mpb);
			}
		}
	}
}