using System;
using package.stormiumteam.shared.ecs;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Patapon4TLB.Core.MasterServer
{
	public static class MsAutomatedRequestModule
	{
		public static MsAutoRequestModule<TAutoRequest, TOriginalRequest, TProcessing, TResponse, TCompletion> From<TAutoRequest, TOriginalRequest, TProcessing, TResponse, TCompletion>(TAutoRequest request, MsRequestModule<TOriginalRequest, TProcessing, TResponse, TCompletion> original)
			where TAutoRequest : struct, IAutomaticRequestComponent<TOriginalRequest>
			where TOriginalRequest : struct, IComponentData
			where TCompletion : struct, IRequestCompletionStatus
			where TResponse : IComponentData
			where TProcessing : IComponentData
		{
			MsAutoRequestModule<TAutoRequest, TOriginalRequest, TProcessing, TResponse, TCompletion> n;
			switch (original.System)
			{
				case AbsGameBaseSystem gameBaseSystem:
					gameBaseSystem.GetModule(out n);
					break;
				default:
					throw new Exception("Invalid system");
			}

			return n;
		}
	}

	public abstract class BaseMsAutoRequestModule : BaseSystemModule
	{
		public abstract void SetPushComponents(params ComponentType[] types);

		protected override void OnEnable()
		{

		}

		protected override void OnUpdate(ref JobHandle jobHandle)
		{

		}

		protected override void OnDisable()
		{

		}

		public abstract void AddRequest(Entity entity);

		public abstract void AddRequest<TOriginalRequest>(Entity entity, TOriginalRequest request)
			where TOriginalRequest : struct, IComponentData;
	}

	public class MsAutoRequestModule<TAutoRequest, TOriginalRequest, TProcessing, TResponse, TCompletion> : BaseMsAutoRequestModule
		where TAutoRequest : struct, IAutomaticRequestComponent<TOriginalRequest>
		where TOriginalRequest : struct, IComponentData
		where TCompletion : struct, IRequestCompletionStatus
	{
		public override ModuleUpdateType UpdateType => ModuleUpdateType.MainThread;

		public EntityQuery     RequesterWithoutBuffer;
		public EntityQuery     RequesterQuery;
		public EntityQuery     PushQuery;
		public ComponentType[] PushComponents;

		private bool m_HasRanOnce;

		public override void SetPushComponents(params ComponentType[] types)
		{
			PushQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
			{
				All = new[] {ComponentType.ReadWrite<TAutoRequest>()},
				Any = types
			});
			PushComponents = types;
		}

		protected override void OnEnable()
		{
			RequesterWithoutBuffer = System.EntityManager.CreateEntityQuery(new EntityQueryDesc
			{
				All  = new ComponentType[] {typeof(TAutoRequest)},
				None = new ComponentType[] {typeof(TrackedAutomatedRequest)}
			});
			RequesterQuery = System.EntityManager.CreateEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(TAutoRequest), typeof(TrackedAutomatedRequest)}
			});

			m_HasRanOnce = false;
		}

		protected override void OnUpdate(ref JobHandle jobHandle)
		{
			if (!RequesterWithoutBuffer.IsEmptyIgnoreFilter)
				System.EntityManager.AddComponent(RequesterWithoutBuffer, typeof(TrackedAutomatedRequest));

			m_HasRanOnce = true;

			if (PushQuery != null)
			{
				using (var entities = PushQuery.ToEntityArray(Allocator.TempJob))
				{
					foreach (var entity in entities)
					{
						AddRequest(entity);
						foreach (var comp in PushComponents)
						{
							if (EntityManager.HasComponent(entity, comp))
								EntityManager.RemoveComponent(entity, comp);
						}
					}
				}
			}

			using (var entities = RequesterQuery.ToEntityArray(Allocator.TempJob))
			{
				foreach (var entity in entities)
				{
					var buffer = System.EntityManager.GetBuffer<TrackedAutomatedRequest>(entity);
					if (buffer.Length == 0)
						continue;

					var automated = EntityManager.GetComponentData<TAutoRequest>(entity);
					for (var i = 0; i != buffer.Length; i++)
					{
						if (EntityManager.HasComponent<TProcessing>(buffer[i].Value))
							continue;

						if (EntityManager.HasComponent<TCompletion>(buffer[i].Value))
						{
							EntityManager.SetOrAddComponentData(entity, EntityManager.GetComponentData<TCompletion>(buffer[i].Value));
							buffer.RemoveAt(i);
							i--;
							continue;
						}
					}
				}
			}
		}

		protected override void OnDisable()
		{

		}

		public override void AddRequest(Entity entity)
		{
			if (!m_HasRanOnce)
				throw new InvalidOperationException(GetType() + " need to at least run once...");

			var initialRequest = System.EntityManager.GetComponentData<TAutoRequest>(entity);
			var requestEntity  = System.EntityManager.CreateEntity(typeof(TOriginalRequest), typeof(SpawnedAsAutomaticRequest));
			var trackedBuffer  = System.EntityManager.GetBuffer<TrackedAutomatedRequest>(entity);

			var request = default(TOriginalRequest);
			initialRequest.SetRequest(ref request);
			System.EntityManager.SetComponentData(requestEntity, request);
			System.EntityManager.SetComponentData(requestEntity, new SpawnedAsAutomaticRequest {Origin = entity});

			trackedBuffer.Add(new TrackedAutomatedRequest {Value = requestEntity});

			Debug.Log("Added request!");
		}

		public override void AddRequest<TAbstractOriginalRequest>(Entity entity, TAbstractOriginalRequest abstractRequest)
		{
			if (!(abstractRequest is TOriginalRequest original))
				throw new InvalidOperationException();
			AddRequest(entity, original);
		}

		public void AddRequest(Entity entity, TOriginalRequest originalRequest)
		{
			if (!m_HasRanOnce)
				throw new InvalidOperationException(GetType() + " need to at least run once...");

			var requestEntity = System.EntityManager.CreateEntity(typeof(TOriginalRequest), typeof(SpawnedAsAutomaticRequest));
			var trackedBuffer = System.EntityManager.GetBuffer<TrackedAutomatedRequest>(entity);

			System.EntityManager.SetComponentData(requestEntity, originalRequest);
			System.EntityManager.SetComponentData(requestEntity, new SpawnedAsAutomaticRequest {Origin = entity});

			trackedBuffer.Add(new TrackedAutomatedRequest {Value = requestEntity});
		}
	}
}