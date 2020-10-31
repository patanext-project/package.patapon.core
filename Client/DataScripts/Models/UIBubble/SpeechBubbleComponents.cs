using System;
using Unity.Collections;
using Unity.Entities;

namespace PataNext.Client.DataScripts.Interface.Bubble
{
	public struct SpeechBubble : IComponentData
	{
		public bool IsEnabled;
	}

	public class SpeechBubbleText : IComponentData
	{
		public string Value;

		public SpeechBubbleText()
		{
			Value = string.Empty;
		}
		
		public SpeechBubbleText(string value)
		{
			Value = value;
		}
	}

	public struct SpeechBubbleCharacterPerSecond : IComponentData
	{
		public int      Current;
		public TimeSpan Value;

		public SpeechBubbleCharacterPerSecond(TimeSpan value)
		{
			Current = 0;
			Value   = value;
		}
	}
}