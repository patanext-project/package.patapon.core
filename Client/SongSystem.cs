using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using package.stormiumteam.shared.ecs;
using Patapon.Client.RhythmEngine;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.NetCode;
using UnityEngine;
using Unity.Entities;

namespace Patapon.Client
{
	public struct MapTargetSong : IComponentData
	{
		public NativeString64 Identifier;
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	[AlwaysUpdateSystem]
	public class SongSystem : GameBaseSystem
	{
		public Dictionary<string, DescriptionFileJsonData> Files;

		// should have a private setter in future
		public  string         MapTargetSongId { get; set; }
		private NativeString64 m_PreviousTarget;

		protected override void OnCreate()
		{
			base.OnCreate();

			Files = new Dictionary<string, DescriptionFileJsonData>();

			var songFiles = Directory.GetFiles(Application.streamingAssetsPath + "/songs", "*.json", SearchOption.TopDirectoryOnly);
			foreach (var file in songFiles)
			{
				try
				{
					var obj = JsonConvert.DeserializeObject<DescriptionFileJsonData>(File.ReadAllText(file));
					if (string.IsNullOrEmpty(obj.path)) // take addressable path
					{
						obj.path = "core://Client/Songs";
					}
					Debug.Log($"Found song: (id={obj.identifier}, name={obj.name})");

					Files[obj.identifier] = obj;
				}
				catch (Exception ex)
				{
					Debug.LogError("Couldn't parse song file: " + file);
					Debug.LogException(ex);
				}
			}
		}

		protected override void OnUpdate()
		{
			//MapTargetSongId = null;
			if (!HasSingleton<ExecutingMapData>())
				return;

			if (EntityManager.TryGetComponentData(GetSingletonEntity<ExecutingMapData>(), out MapTargetSong targetSong)
			    && !m_PreviousTarget.Equals(targetSong.Identifier))
			{
				m_PreviousTarget = targetSong.Identifier;
				MapTargetSongId  = targetSong.Identifier.ToString();
			}
		}
	}
}