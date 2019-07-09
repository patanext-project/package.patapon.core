using System.Collections.Generic;
using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
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

	public class GetAllBackendModule<T> : BaseSystemModule
		where T : MonoBehaviour
	{
		public override ModuleUpdateType UpdateType => ModuleUpdateType.Job;

		[BurstCompile]
		private struct FindBackend : IJobForEachWithEntity<ModelParent>
		{
			public NativeList<Entity> MissingTargets;

			public NativeList<Entity>      BackendWithoutModel;
			public NativeList<Entity>      AttachedBackendEntities;
			public NativeList<ModelParent> AttachedBackendDestination;

			public void Execute(Entity entity, int index, ref ModelParent parent)
			{
				var count = MissingTargets.Length;
				for (var i = 0; i != count; i++)
				{
					if (MissingTargets[i] == parent.Parent)
					{
						AttachedBackendEntities.Add(entity);
						AttachedBackendDestination.Add(parent);
						
						MissingTargets.RemoveAtSwapBack(i);
						return;
					}
				}

				BackendWithoutModel.Add(entity);
			}
		}

		public NativeArray<Entity> TargetEntities;
		public NativeList<Entity>  MissingTargets;

		public NativeList<Entity>      BackendWithoutModel;
		public NativeList<Entity>      AttachedBackendEntities;
		public NativeList<ModelParent> AttachedBackendDestination;

		private EntityQuery m_BackendQuery;

		protected override void OnEnable()
		{
			m_BackendQuery             = System.EntityManager.CreateEntityQuery(typeof(T), typeof(ModelParent));
			MissingTargets             = new NativeList<Entity>(Allocator.Persistent);
			BackendWithoutModel        = new NativeList<Entity>(Allocator.Persistent);
			AttachedBackendDestination = new NativeList<ModelParent>(Allocator.Persistent);
			AttachedBackendEntities    = new NativeList<Entity>(Allocator.Persistent);
		}

		protected override void OnUpdate(ref JobHandle jobHandle)
		{
			if (!TargetEntities.IsCreated)
				return;

			BackendWithoutModel.Clear();
			AttachedBackendEntities.Clear();
			AttachedBackendDestination.Clear();

			MissingTargets.Clear();
			MissingTargets.AddRange(TargetEntities);

			jobHandle = new FindBackend
			{
				MissingTargets             = MissingTargets,
				BackendWithoutModel        = BackendWithoutModel,
				AttachedBackendEntities    = AttachedBackendEntities,
				AttachedBackendDestination = AttachedBackendDestination
			}.ScheduleSingle(m_BackendQuery, jobHandle);
		}

		protected override void OnDisable()
		{
			AttachedBackendDestination.Dispose();
			AttachedBackendEntities.Dispose();
		}
	}
}