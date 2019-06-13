using System;
using System.Collections.Generic;

namespace Patapon4TLB.Core.json
{
	[Serializable]
	public struct DescriptionFileJsonData
	{
		public struct BgmAudioFull
		{
			public string filePath;
			public int    feverStartBeat;
		}

		public string name;
		public string description;
		public string identifier;

		public Dictionary<string, string[]>                     bgmAudioSliced;
		public BgmAudioFull?                                    bgmAudioFull;
		public Dictionary<string, Dictionary<string, string[]>> commandsAudio;
	}
}