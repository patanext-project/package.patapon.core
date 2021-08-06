using PataNext.Client.DataScripts.Interface.Inventory;
using TMPro;
using Unity.VectorGraphics;
using UnityEngine;

namespace PataNext.Client.DataScripts.Interface.Menu.__Barracks.Categories
{
	public class EquipmentRow : MonoBehaviour
	{
		public RectTransform      selectedRect;
		public EquipmentInventory inventory;
		
		public SVGImage        equipmentIcon;
		public SVGImage        typeIcon;
		public TextMeshProUGUI label;

		public RectTransform nameRect;

		public Animator animator;

		private static readonly int UnfocusHash = Animator.StringToHash("Unfocus");
		private static readonly int FocusHash   = Animator.StringToHash("Focus");

		private void Awake()
		{
			animator = GetComponent<Animator>();
			animator.WriteDefaultValues();
		}

		private void OnEnable()
		{
			animator.Rebind();
		}

		public void SetName(string value)
		{
			label.text = value;
			// we need to get the preferred width instantly and not at the next frame
			// which is why we call GetPreferredValues
			nameRect.sizeDelta = new Vector2(label.GetPreferredValues(value).x + 60f, nameRect.sizeDelta.y);
		}

		public void Focus()
		{
			animator.SetTrigger(FocusHash);
		}

		public void Unfocus()
		{
			animator.SetTrigger(UnfocusHash);
		}

		private bool hasFocus;

		public void SetFocus(bool b)
		{
			if (b == hasFocus)
				return;

			hasFocus = b;
			if (b) Focus();
			else Unfocus();
		}

		public void SetSelected(bool b)
		{
			selectedRect.gameObject.SetActive(b);
		}
	}
}