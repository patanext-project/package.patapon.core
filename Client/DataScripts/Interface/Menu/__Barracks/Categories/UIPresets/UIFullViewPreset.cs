using System;
using System.Threading.Tasks;
using PataNext.UnityCore.Rpc;
using UnityEngine;

namespace PataNext.Client.DataScripts.Interface.Menu.__Barracks.Categories.UIPresets
{
	public class UIFullViewPreset : MonoBehaviour
	{
		public Action ExitAction;

		public PresetInventory                  inventory;

		public bool                             launchRequest { get; set; }
		public Task<GetSavePresetsRpc.Response> Request       { get; set; }
	}
}