using StormiumTeam.GameBase.Utility.AssetBackend;

namespace PataNext.Client.DataScripts.Interface.Bubble
{
	public struct CurrentText
	{
		public int    CharacterPerSecond;
		public string Target;
	}

	public abstract class UIBubblePresentation : RuntimeAssetPresentation<UIBubblePresentation>
	{
	}

	public class UIBubbleBackend : RuntimeAssetBackend<UIBubblePresentation>
	{

	}
}