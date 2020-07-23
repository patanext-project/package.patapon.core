using package.stormiumteam.shared;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Core.Addressables;
using PataNext.Client.Graphics;
using PataNext.Client.Graphics.Animation.Base;
using PataNext.Client.Systems;
using PataNext.Module.Simulation.Components.GamePlay.Abilities;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Modules;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace PataNext.Client.DataScripts.Sounds
{
	public class HeroModeActivationSoundSystem : AbsGameBaseSystem
	{
		public struct DataOp
		{
		}

		public struct AbilityInternalData : IComponentData
		{
			public bool HeroActive;
		}
		
		private AsyncOperationModule m_AsyncOp;
		private ECSoundDefinition    m_ActivationSound;

		private EntityQuery m_AbilityWithoutInternalQuery;
		
		protected override void OnCreate()
		{
			base.OnCreate();

			GetModule(out m_AsyncOp);

			var activationFile = AddressBuilder.Client()
			                                   .Folder("Sounds")
			                                   .Folder("Effects")
			                                   .Folder("HeroModeActivation")
			                                   .GetFile("HeroModeStart.wav");
			m_AsyncOp.Add(Addressables.LoadAssetAsync<AudioClip>(activationFile), new DataOp { });

			m_AbilityWithoutInternalQuery = GetEntityQuery(new EntityQueryDesc
			{
				All  = new ComponentType[] {typeof(AbilityState)},
				None = new ComponentType[] {typeof(AbilityInternalData)}
			});
		}

		protected override void OnUpdate()
		{
			for (var i = 0; i != m_AsyncOp.Handles.Count; i++)
			{
				var (handle, data) = DefaultAsyncOperation.InvokeExecute<AudioClip, DataOp>(m_AsyncOp, ref i);
				if (handle.Result == null)
					continue;

				m_ActivationSound = World.GetOrCreateSystem<ECSoundSystem>()
				                         .ConvertClip(handle.Result);
			}

			if (!m_ActivationSound.IsValid)
				return;

			if (!m_AbilityWithoutInternalQuery.IsEmptyIgnoreFilter)
				EntityManager.AddComponent(m_AbilityWithoutInternalQuery, typeof(AbilityInternalData));

			var playSound           = false;
			var playSoundAllocation = UnsafeAllocation.From(ref playSound);
			Entities.ForEach((Entity ent, ref AbilityInternalData internalData, in AbilityState state, in AbilityActivation activation) =>
			{
				if (activation.Type == EActivationType.Normal || (state.Phase & EAbilityPhase.HeroActivation) == 0)
				{
					internalData.HeroActive = false;
					return;
				}

				if (internalData.HeroActive)
					return;
				
				internalData.HeroActive   = true;
				playSoundAllocation.Value = true;
			}).Run();

			if (!playSound) 
				return;
			
			var soundEntity = EntityManager.CreateEntity(typeof(ECSoundEmitterComponent), typeof(ECSoundDefinition), typeof(ECSoundOneShotTag));
			var emitter     = new ECSoundEmitterComponent();

			emitter.make_flat();
			emitter.volume      = 1f;

			if (EntityManager.TryGetComponentData(soundEntity, out Translation tr))
			{
				emitter.make_1d();
				emitter.position = tr.Value;
			}

			EntityManager.SetComponentData(soundEntity, emitter);
			EntityManager.SetComponentData(soundEntity, m_ActivationSound);
		}
	}
}