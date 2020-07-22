using package.stormiumteam.shared.ecs;
using PataNext.Client.Core.Addressables;
using PataNext.Client.Graphics.Animation.Base;
using PataNext.Client.Systems;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Modules;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace PataNext.Client.DataScripts.Sounds
{
	[AlwaysSynchronizeSystem]
	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities))]
	[UpdateBefore(typeof(DoHitSoundSystem))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	// TEMPORARY SYSTEM! 
	// todo: this system should be removed once hit sounds will be attributed to current ability and weapons...
	public class DoEnergyFieldSoundSystem : AbsGameBaseSystem
	{
		public struct DataOp
		{
		}

		private AsyncOperationModule m_AsyncOp;
		private ECSoundDefinition    m_HitSound;

		protected override void OnCreate()
		{
			base.OnCreate();

			GetModule(out m_AsyncOp);
			
			var hitSoundFile = AddressBuilder.Client()
			                                 .Folder("Sounds")
			                                 .Folder("Effects")
			                                 .Folder("EnergyField")
			                                 .GetFile("EnergyFieldOnHitHalved.wav");
			m_AsyncOp.Add(Addressables.LoadAssetAsync<AudioClip>(hitSoundFile), new DataOp { });
		}

		protected override void OnUpdate()
		{
			for (var i = 0; i != m_AsyncOp.Handles.Count; i++)
			{
				var (handle, data) = DefaultAsyncOperation.InvokeExecute<AudioClip, DataOp>(m_AsyncOp, ref i);
				if (handle.Result == null)
					continue;

				m_HitSound = World.GetOrCreateSystem<ECSoundSystem>()
				                  .ConvertClip(handle.Result);
			}

			if (!m_HitSound.IsValid)
				return;

			Entities.WithNone<HitSoundAttachedTag>().WithAll<EnergyFieldBuff.DamageWasHalvedTag>().ForEach((Entity ent, in TargetDamageEvent damageEvent, in GameEvent gameEvent) =>
			{
				if (damageEvent.Damage >= 0)
					return;
				
				var soundEntity = EntityManager.CreateEntity(typeof(ECSoundEmitterComponent), typeof(ECSoundDefinition), typeof(ECSoundOneShotTag));
				var emitter     = new ECSoundEmitterComponent();

				emitter.make_flat();
				emitter.volume      = 0.75f;
				emitter.minDistance = 10;
				emitter.maxDistance = 25;

				if (EntityManager.TryGetComponentData(soundEntity, out Translation tr))
				{
					emitter.make_1d();
					emitter.position = tr.Value;
				}

				EntityManager.SetComponentData(soundEntity, emitter);
				EntityManager.SetComponentData(soundEntity, m_HitSound);

				EntityManager.AddComponent(ent, typeof(HitSoundAttachedTag));
			}).WithStructuralChanges().Run();
		}
	}
}