using PataNext.UnityCore.Utilities;
using TMPro;
using UnityEngine;

namespace PataNext.Client.DataScripts.Interface.Menu.__Barracks.Categories.UIPresets
{
	public class PresetInventoryRow : MonoBehaviour
	{
		public GameObjectSwitchEnable classSwitch;
		public GameObjectSwitchEnable nameSwitch;
		public GameObjectSwitchEnable selectionSwitch;

		public TextMeshProUGUI classLabel;
		public TextMeshProUGUI nameLabel;
	}
}