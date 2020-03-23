using System;
using System.Collections.Generic;
using System.IO;
using DefaultNamespace;
using Newtonsoft.Json;
using package.stormiumteam.shared.ecs;
using Patapon.Client.RhythmEngine;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Patapon.Client
{
	public struct MapTargetSong : IComponentData
	{
		public NativeString64 Identifier;
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	[AlwaysUpdateSystem]
	public class SongSystem : AbsGameBaseSystem
	{
		public  Dictionary<string, DescriptionFileJsonData> Files;
		private NativeString64                              m_PreviousTarget;

		// should have a private setter in future
		public string MapTargetSongId { get; set; }

		protected override void OnCreate()
		{
			base.OnCreate();

			Files = new Dictionary<string, DescriptionFileJsonData>();

			var directory = new DirectoryInfo(Application.persistentDataPath + "/songs");
			directory.Create();
			
			var songFiles = new List<string>();
			songFiles.AddRange(Directory.GetFiles(Application.streamingAssetsPath + "/songs", "*.json", SearchOption.TopDirectoryOnly));
			songFiles.AddRange(Directory.GetFiles(Application.persistentDataPath + "/songs", "*.json", SearchOption.TopDirectoryOnly));
			foreach (var file in songFiles)
				try
				{
					var obj = JsonConvert.DeserializeObject<DescriptionFileJsonData>(File.ReadAllText(file));
					if (string.IsNullOrEmpty(obj.path)) // take addressable path
						obj.path = "core://Client/Songs";

					obj.path = obj.path.Replace("[StreamingAssetsPath]", Application.streamingAssetsPath);
					obj.path = obj.path.Replace("[PersistentDataPath]", Application.persistentDataPath);
					
					Debug.Log($"Found song: (id={obj.identifier}, name={obj.name}, path={obj.path})");

					Files[obj.identifier] = obj;
				}
				catch (Exception ex)
				{
					Debug.LogError("Couldn't parse song file: " + file);
					Debug.LogException(ex);
				}
		}

		protected override void OnUpdate()
		{
			if (MapTargetSongId == null)
				MapTargetSongId = GetSingleton<ForcedSongRule>().SongId.ToString();

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