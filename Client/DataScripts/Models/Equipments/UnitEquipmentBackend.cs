using StormiumTeam.GameBase;

namespace DataScripts.Models.Equipments
{
	public class UnitEquipmentBackend : RuntimeAssetBackend<BaseUnitEquipmentPresentation>
	{
		public override bool PresentationWorldTransformStayOnSpawn => false;
	}

	public abstract class BaseUnitEquipmentPresentation : RuntimeAssetPresentation<BaseUnitEquipmentPresentation>
	{
	}
}