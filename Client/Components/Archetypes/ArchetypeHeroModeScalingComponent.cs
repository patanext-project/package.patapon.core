using System;
using System.Collections.Generic;
using GameHost.Core.Native.xUnity;
using GameHost.Simulation.Utility.Resource.Systems;
using package.stormiumteam.shared.ecs;
using PataNext.Client.DataScripts.Models.Equipments;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.Client.Systems;
using PataNext.Module.Simulation.Components.GamePlay.Abilities;
using PataNext.Module.Simulation.Components.Units;
using PataNext.Module.Simulation.Resources;
using PataNext.Module.Simulation.Resources.Keys;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Utility.AssetBackend;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Utility.GameResources;

namespace PataNext.Client.Components.Archetypes
{
	public class ArchetypeHeroModeScalingComponent : MonoBehaviour, IBackendReceiver
	{
		private float m_HeroModeScaling = 1;

		public RuntimeAssetBackendBase Backend { get; set; }
		public void                    OnBackendSet()
		{
			
		}

		public void                    OnPresentationSystemUpdate()
		{
			var entityMgr = Backend.DstEntityManager;
			var time      = entityMgr.World.Time;
			
			var scale        = 1f;
			// Scaling in general should be done in another system...
			/*if (EntityManager.TryGetComponentData(backend.DstEntity, out LivableHealth health) && health.IsDead)
			{
				scale = 0;
			}*/

			ref var heroModeScaling = ref m_HeroModeScaling;
			// Hero mode scaling shouldn't be done here.
			if (entityMgr.TryGetComponentData(Backend.DstEntity, out OwnerActiveAbility ownerAbility)
			    && entityMgr.TryGetComponentData(ownerAbility.Active, out AbilityActivation activation)
			    && activation.Type == EAbilityActivationType.HeroMode)
			{
				heroModeScaling = 1.325f;
			}
			else
			{
				heroModeScaling = math.lerp(heroModeScaling, 1, time.DeltaTime * 1.75f);
				heroModeScaling = math.lerp(heroModeScaling, 1, time.DeltaTime * 1.25f);
				heroModeScaling = math.clamp(heroModeScaling, 1, 1.325f);
			}

			transform.localScale = Vector3.one * (scale * m_HeroModeScaling);
		}
	}
}