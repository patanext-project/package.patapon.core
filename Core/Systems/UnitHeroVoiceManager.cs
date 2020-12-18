using System.Collections.Generic;
using P4TLB.MasterServer;
using PataNext.Client.Core.Addressables;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Modules;
using StormiumTeam.GameBase.Utility.Misc;
using UnityEngine;

namespace PataNext.Client.Systems
{
	// todo: this class should be removed once there will be a correct way to get voices...
	public class UnitHeroVoiceManager : AbsGameBaseSystem
	{
		public struct DataOp
		{
			public string Key;
		}

		private AsyncOperationModule m_AsyncOp;

		private Dictionary<string, AudioClip> m_ClipMap;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_ClipMap = new Dictionary<string, AudioClip>();
			GetModule(out m_AsyncOp);

			var builder = AddressBuilder.Client()
			                            .Folder("Sounds")
			                            .Folder("Effects")
			                            .Folder("HeroModeActivation");
			m_AsyncOp.Add(AssetManager.LoadAssetAsync<AudioClip>(builder.GetAsset(nameof(P4OfficialAbilities.TateEnergyField) + ".ogg")), new DataOp
			{
				Key = nameof(P4OfficialAbilities.TateEnergyField)
			});
			m_AsyncOp.Add(AssetManager.LoadAssetAsync<AudioClip>(builder.GetAsset(nameof(P4OfficialAbilities.YariFearSpear) + ".ogg")), new DataOp
			{
				Key = nameof(P4OfficialAbilities.YariFearSpear)
			});
		}

		protected override void OnUpdate()
		{
			for (var i = 0; i != m_AsyncOp.Handles.Count; i++)
			{
				var (handle, data) = DefaultAsyncOperation.InvokeExecute<AudioClip, DataOp>(m_AsyncOp, ref i);
				if (handle.Result == null)
					continue;

				m_ClipMap[data.Key] = handle.Result;
			}
		}

		public AudioClip Get(string abilityId)
		{
			if (!m_ClipMap.TryGetValue(abilityId, out var clip))
				return null;
			return clip;
		}
	}
}