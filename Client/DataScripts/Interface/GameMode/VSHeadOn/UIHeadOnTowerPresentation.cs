using UnityEngine;
using UnityEngine.UI;

namespace DataScripts.Interface.GameMode.VSHeadOn
{
	public class UIHeadOnTowerPresentation : UIHeadOnStructurePresentationBase
	{
		public Graphic[] graphics;

		public override void SetTeamColor(Color primary, Color secondary)
		{
			for (var i = 0; i != graphics.Length; i++)
			{
				graphics[i].color = primary;
			}
		}
	}

	public unsafe class UIHeadOnTowerRenderSystem : UIHeadOnStructureBaseRenderSystem<UIHeadOnTowerPresentation>
	{
		public override bool DefaultBehavior => true;
	}
}