using System.Globalization;
using PataNext.UnityCore.Utilities;
using TMPro;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UI;

namespace PataNext.Client.DataScripts.Interface.Menu.__Barracks.Categories
{
	public class StatusEffectTableRow : MonoBehaviour
	{
		public Image           background;
		public SVGImage icon;
		public TextMeshProUGUI category;

		public UILabel<int>   power;
		public UILabel<int>   resistance;
		public UILabel<float> gain;
		public UILabel<float> immunity;

		private void Awake()
		{
			power.Format      = v => v + "%";
			resistance.Format = v => v + "%";
			gain.Format       = v => v.ToString("F2", CultureInfo.InvariantCulture) + "%";
			immunity.Format   = v => v.ToString("F2", CultureInfo.InvariantCulture) + "%";
		}
	}
}