using StormiumTeam.GameBase.Utility.AssetBackend;
using UnityEngine;

namespace PataNext.Client.DataScripts.Models.Equipments
{
	public class UnitEquipmentBackend : RuntimeAssetBackend<BaseUnitEquipmentPresentation>
	{
		public override bool PresentationWorldTransformStayOnSpawn => false;

		public override void OnPresentationSet()
		{
			base.OnPresentationSet();

			// v This is needed for animations
			Presentation.gameObject.name = "Presentation";
			foreach (var tr in gameObject.GetComponentsInChildren<Transform>())
				tr.gameObject.layer = gameObject.layer;
		}
	}

	public abstract class BaseUnitEquipmentPresentation : RuntimeAssetPresentation
	{
	}
}