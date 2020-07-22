using UnityEngine;
using UnityEngine.UI;

namespace DataScripts.Interface.GameMode.VSHeadOn
{
	public class UIHeadOnInstantStructurePresentation : UIHeadOnStructurePresentationBase
	{
		public MaskableGraphic[] graphics;

		public override void SetTeamColor(Color primary, Color secondary)
		{
			for (var i = 0; i != graphics.Length; i++)
			{
				graphics[i].color = primary;
			}
		}
	}

	public class UIHeadOnInstantStructureRenderSystem : UIHeadOnStructureBaseRenderSystem<UIHeadOnInstantStructurePresentation>
	{
		public override bool DefaultBehavior => true;

		protected override void Render(UIHeadOnInstantStructurePresentation definition)
		{
			base.Render(definition);
		}
	}
}