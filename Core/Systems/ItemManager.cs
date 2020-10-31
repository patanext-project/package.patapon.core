using System;
using System.Collections.Generic;
using System.IO;
using StormiumTeam.GameBase.BaseSystems;
using UnityEngine;
using Unity.Serialization.Json;
using Unity.VectorGraphics;
using Object = UnityEngine.Object;

namespace PataNext.Client.Systems
{
	public class ItemManager : AbsGameBaseSystem
	{
		private Dictionary<string, ItemJsonData>            itemMap;
		private Dictionary<(string id, string key), Sprite> itemSpriteMap;

		public ItemManager()
		{
			itemMap       = new Dictionary<string, ItemJsonData>();
			itemSpriteMap = new Dictionary<(string id, string key), Sprite>();
		}

		protected override void OnCreate()
		{
			base.OnCreate();

			// TODO: First it shouldn't be a variable, second there should be an utility for that (with automatic creation of these files)
			// (We need to replicate the same behavior of GameHost Storages)
			var Directories = new DirectoryInfo[]
			{
				new DirectoryInfo(Application.streamingAssetsPath + "/items"),
				new DirectoryInfo(Application.persistentDataPath + "/items")
			};
			foreach (var directory in Directories)
			{
				if (!directory.Exists)
					directory.Create();

				foreach (var file in directory.GetFiles("*.json", SearchOption.AllDirectories))
				{
					var data = JsonSerialization.FromJson<ItemJsonData>(file);
					if (string.IsNullOrEmpty(data.masterServerId))
						data.masterServerId = Path.GetFileNameWithoutExtension(file.Name);
					if (string.IsNullOrEmpty(data.displayName))
						data.displayName = data.masterServerId;
					if (string.IsNullOrEmpty(data.displayNameTranslationId))
						data.displayNameTranslationId = data.masterServerId + ":name";
					if (string.IsNullOrEmpty(data.descriptionTranslationId))
						data.descriptionTranslationId = data.masterServerId + ":desc";

					data.iconPathMap ??= new Dictionary<string, string>();

					if (string.IsNullOrEmpty(data.defaultIconPath))
						data.defaultIconPath = file.FullName.Replace(".json", ".svg");

					data.iconPathMap["default_icon"] = data.defaultIconPath;

					itemMap[data.masterServerId] = data;

					Debug.Log($"Item Found (Id={data.masterServerId}, Name={data.displayName})");
				}
			}
		}

		protected override void OnUpdate()
		{

		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			
			foreach (var sprite in itemSpriteMap.Values)
				Object.Destroy(sprite);
		}

		public Sprite GetSpriteOf(string itemId, string category, int tier)
		{
			return GetSpriteOf(itemId, $"{category}:{tier}");
		}

		public Sprite GetSpriteOf(string itemId, string key)
		{
			const string defaultIcon = "default_icon";
			
			if (itemSpriteMap.TryGetValue((itemId, key), out var sprite))
				return sprite;

			if (itemMap.TryGetValue(itemId, out var data)
			    && data.iconPathMap.TryGetValue(key, out var path))
			{
				Console.WriteLine("item: " + path);
				
				using var stream = new StreamReader(path);

				var sceneInfo = SVGParser.ImportSVG(stream, ViewportOptions.PreserveViewport, 0, 1, 100, 100);
				var rect      = sceneInfo.SceneViewport;

				var tessOptions = new VectorUtils.TessellationOptions
				{
					MaxCordDeviation     = float.MaxValue,
					MaxTanAngleDeviation = 1f,
					SamplingStepSize     = 1.0f / 100.0f,
					StepDistance         = 0.75f
				};

				var geometry = VectorUtils.TessellateScene(sceneInfo.Scene, tessOptions, sceneInfo.NodeOpacity);
				sprite = VectorUtils.BuildSprite(geometry, rect, 100, VectorUtils.Alignment.Center, default, 64, true);

				itemSpriteMap[(itemId, key)] = sprite;
			}

			if (sprite == null && key != defaultIcon)
				return GetSpriteOf(itemId, defaultIcon);

			return sprite;
		}
	}

	[Serializable]
	public struct ItemJsonData
	{
		public string masterServerId;

		public string displayName;
		public string displayNameTranslationId;

		public string description;
		public string descriptionTranslationId;

		public string                     defaultIconPath;
		public Dictionary<string, string> iconPathMap;

		public string json;
	}
}