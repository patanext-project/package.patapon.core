using System.Collections.Generic;
using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using Unity.Entities;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Patapon4TLB.UI.InGame
{
	public abstract class UIGameSystemBase : GameBaseSystem
	{
		public Entity GetRhythmEngineFromTarget(Entity entity)
		{
			return EntityManager.HasComponent<Relative<RhythmEngineDescription>>(entity)
				? EntityManager.GetComponentData<Relative<RhythmEngineDescription>>(entity).Target
				: default;
		}

		public Entity GetRhythmEngineFromView(CameraState cameraState)
		{
			return GetRhythmEngineFromTarget(cameraState.Target);
		}

		public bool TryGetRelative<TDescription>(Entity target, out Entity relative)
			where TDescription : struct, IEntityDescription
		{
			if (EntityManager.HasComponent<Relative<TDescription>>(target))
			{
				relative = EntityManager.GetComponentData<Relative<TDescription>>(target).Target;
				return true;
			}

			relative = default;
			return false;
		}
	}
}