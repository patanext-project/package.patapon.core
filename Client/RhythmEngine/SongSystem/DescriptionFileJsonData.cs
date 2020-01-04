using System;
using System.Collections.Generic;

namespace Patapon.Client.RhythmEngine
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

		// disk files start with 'file://'
		// addressables file start with 'core://'
		public string path;

		public Dictionary<string, string[]>                     bgmAudioSliced;
		public BgmAudioFull?                                    bgmAudioFull;
		public Dictionary<string, Dictionary<string, string[]>> commandsAudio;
	}
}