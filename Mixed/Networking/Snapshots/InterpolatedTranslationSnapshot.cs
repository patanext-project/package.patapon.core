using package.stormiumteam.shared.ecs;
using Patapon.Mixed.Rules;
using Patapon4TLB.Core.Snapshots;
using Revolution;
using Revolution.Utils;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Transforms;
using UnityEngine;

[assembly: RegisterGenericComponentType(typeof(Predicted<InterpolatedTranslationSnapshot>))]

namespace Patapon4TLB.Core.Snapshots
{
	public struct InterpolatedTranslationSnapshot : IReadWriteSnapshot<InterpolatedTranslationSnapshot>, IPredictable<InterpolatedTranslationSnapshot>,
	                                                ISynchronizeImpl<Translation>,
	                                                ISynchronizeImpl<InterpolatedTranslationSnapshot.NetSynchronize.TargetPosition>
	{
		public const int             dimension = 2;
		public       QuantizedFloat3 Value;

		private int  previous;
		private uint previousTick;

		public void WriteTo(DataStreamWriter writer, ref InterpolatedTranslationSnapshot baseline, NetworkCompressionModel compressionModel)
		{
			for (var i = 0; i < dimension; i++)
				writer.WritePackedIntDelta(Value[i], baseline.Value[i], compressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref InterpolatedTranslationSnapshot baseline, NetworkCompressionModel compressionModel)
		{
			for (var i = 0; i < dimension; i++)
				Value[i] = reader.ReadPackedIntDelta(ref ctx, baseline.Value[i], compressionModel);
		}

		public uint Tick { get; set; }

		public void SynchronizeFrom(in Translation component, in DefaultSetup setup, in SerializeClientData serializeData)
		{
			Value.Set(1000, component.Value);
		}

		public void SynchronizeTo(ref Translation component, in DeserializeClientData deserializeData)
		{
			component.Value = Value.Get(0.001f);
		}

		public void Interpolate(InterpolatedTranslationSnapshot target, float factor)
		{
			previous     = target.Value.Result.x;
			previousTick = target.Tick;
			Value.Result = (int3) math.lerp(Value.Result, target.Value.Result, factor);
		}

		public struct Use : IComponentData
		{
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronize : ComponentSnapshotSystemBasicPredicted<Translation, InterpolatedTranslationSnapshot>
		{
			public override NativeArray<ComponentType> EntityComponents =>
				new NativeArray<ComponentType>(3, Allocator.Temp)
				{
					[0] = typeof(Use),
					[1] = typeof(TranslationSnapshot.Exclude),
					[2] = typeof(Translation)
				};

			public override ComponentType ExcludeComponent => typeof(Exclude);

			protected override void AddComponentsToQuery(EntityQuery query)
			{
				EntityManager.AddComponent(query, typeof(TargetPosition));
			}

			public struct TargetPosition : IComponentData
			{
				public float3 Value;
			}

			public struct PreviousPosition : IComponentData
			{
				public float3 Value;
			}
		}

		[DisableAutoCreation]
		public class TranslationPredictedUpdate : ComponentUpdateSystemInterpolated<Translation, InterpolatedTranslationSnapshot>
		{
			protected override bool IsPredicted => true;
		}

		[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
		[UpdateAfter(typeof(OrderGroup.Simulation))]
		public class PredictedUpdate : JobComponentSystem
		{
			protected override JobHandle OnUpdate(JobHandle inputDeps)
			{
				if (GetSingleton<P4NetworkRules.Data>().UnitPresentationInterpolation != P4NetworkRules.Interpolation.DoublePredicted)
					return inputDeps;

				var dt = Time.DeltaTime;
				var velocityFromEntity = GetComponentDataFromEntity<Velocity>();
				inputDeps = Entities.ForEach((Entity entity, ref NetSynchronize.TargetPosition target, ref Translation translation) =>
				{
					if (!velocityFromEntity.TryGet(entity, out var velocity))
					{
						velocity.Value = target.Value - translation.Value;
					}
					
					for (var v = 0; v != 3; v++)
					{
						translation.Value[v] = math.isnan(translation.Value[v]) ? 0.0f : translation.Value[v];
						target.Value[v]      = math.isnan(target.Value[v]) ? 0.0f : target.Value[v];
					}

					translation.Value.y = target.Value.y;

					var distance = math.distance(translation.Value, target.Value);
					if (distance * dt > math.max(velocity.speed * dt * 4, dt * 4))
					{
						//Debug.Log("teleport");
						translation.Value = target.Value;
						return;
					}

					translation.Value = math.lerp(translation.Value, target.Value, dt * (velocity.speed + distance + 1f));
					translation.Value = Vector3.MoveTowards(translation.Value, target.Value, math.max(distance * dt, velocity.speed * dt) * 0.65f);
				}).WithNativeDisableParallelForRestriction(velocityFromEntity).Schedule(inputDeps);

				return inputDeps;
			}
		}

		[UpdateInGroup(typeof(GhostUpdateSystemGroup))]
		[UpdateAfter(typeof(Velocity.Synchronize))]
		public class LocalUpdate : ComponentUpdateSystemInterpolated<NetSynchronize.TargetPosition, InterpolatedTranslationSnapshot>
		{
			private LazySystem<TranslationPredictedUpdate> m_PredictedSystem;

			protected override bool FullFraction =>
				HasSingleton<P4NetworkRules.Data>()
				&& GetSingleton<P4NetworkRules.Data>().UnitPresentationInterpolation == P4NetworkRules.Interpolation.DoubleInterpolated;

			protected override bool IsPredicted =>
				!HasSingleton<P4NetworkRules.Data>()
				|| GetSingleton<P4NetworkRules.Data>().UnitPresentationInterpolation != P4NetworkRules.Interpolation.DoubleInterpolated;

			protected override JobHandle OnUpdate(JobHandle inputDeps)
			{
				inputDeps = base.OnUpdate(inputDeps);

				if (!FullFraction || IsPredicted)
				{
					m_PredictedSystem.Get(World).Update();
					return JobHandle.CombineDependencies(inputDeps, m_PredictedSystem.Value.Dependency);
				}

				var dt = Time.DeltaTime;
				var interpolationType = GetSingleton<P4NetworkRules.Data>().UnitPresentationInterpolation;
				var velocityFromEntity = GetComponentDataFromEntity<Velocity>();
				return Entities.ForEach((Entity entity, ref NetSynchronize.TargetPosition target, ref Translation translation) =>
				{
					if (interpolationType == P4NetworkRules.Interpolation.DoublePredicted)
					{
						translation.Value = target.Value;
						return;
					}
					
					if (!velocityFromEntity.TryGet(entity, out var velocity))
					{
						velocity.Value = target.Value - translation.Value;
					}
					
					for (var v = 0; v != 3; v++)
					{
						translation.Value[v] = math.isnan(translation.Value[v]) ? 0.0f : translation.Value[v];
						target.Value[v]      = math.isnan(target.Value[v]) ? 0.0f : target.Value[v];
					}

					translation.Value.y = target.Value.y;

					var distance = math.distance(translation.Value, target.Value);
					if (distance * dt > math.max(velocity.speed * dt * 3f, dt * 5f))
					{
						translation.Value = target.Value;
						return;
					}

					translation.Value = math.lerp(translation.Value, target.Value, dt * (velocity.speed + distance + 1f));
					translation.Value = Vector3.MoveTowards(translation.Value, target.Value, math.max(distance * dt, velocity.speed * dt) * 0.8f);
				}).WithReadOnly(velocityFromEntity).Schedule(inputDeps);
			}
		}

		public void PredictDelta(uint tick, ref InterpolatedTranslationSnapshot baseline1, ref InterpolatedTranslationSnapshot baseline2)
		{
			//var predictor                                = new GhostDeltaPredictor(tick, Tick, baseline1.Tick, baseline2.Tick);
			//for (var i = 0; i != 3; i++) Value.Result[i] = predictor.PredictInt(Value.Result[i], baseline1.Value.Result[i], baseline2.Value.Result[i]);
		}

		public void SynchronizeFrom(in NetSynchronize.TargetPosition component, in DefaultSetup setup, in SerializeClientData serializeData)
		{
		}

		public void SynchronizeTo(ref NetSynchronize.TargetPosition component, in DeserializeClientData deserializeData)
		{
			component.Value = Value.Get(0.001f);
		}
	}
}