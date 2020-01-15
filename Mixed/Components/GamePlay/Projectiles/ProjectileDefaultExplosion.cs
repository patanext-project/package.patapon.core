using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Patapon.Mixed.Projectiles
{
	// This file is from Stormium, we should maybe put it into gamebase?

	// direct hit (eg: scan projectile, RailGun, ...)
	public struct ScannerDefaultExplosion : IComponentData
	{
		public int MinDamage;
		public int MaxDamage;

		public float ImpulseMin;
		public float ImpulseMax;
	}

	// indirect hit (eg: explosion)
	public struct ProjectileDefaultExplosion : IComponentData
	{
		public float DamageRadius;
		public int   MinDamage;
		public int   MaxDamage;

		public float BumpRadius;
		public float HorizontalImpulseMax;
		public float HorizontalImpulseMin;

		public float VerticalImpulseMax;
		public float VerticalImpulseMin;

		public float SelfImpulseFactor;
	}

	public static class IDistanceFallOfExtension
	{
		public static TResult GetFallOfResult<T, TResult>(this DynamicBuffer<T> buffer, float distance, TResult defaultResult = default)
			where T : struct, IDistanceFallOf<T, TResult>
			where TResult : struct
		{
			var fallOf = buffer.ToNativeArray(Allocator.Temp);
			fallOf.Sort();
			for (var f = 0; f < fallOf.Length; f++)
			{
				var curr = fallOf[f];
				var next = fallOf[math.min(f + 1, fallOf.Length - 1)];
				if (curr.Distance < distance && next.Distance < distance)
					continue;

				return curr.Evaluate(next, distance);
			}

			return default;
		}
	}

	public interface IDistanceFallOf<in TSelf, out TResult> : IBufferElementData, IComparable<TSelf>
		where TSelf : struct, IDistanceFallOf<TSelf, TResult>
		where TResult : struct
	{
		bool  DistanceIsPercentage { get; set; }
		float Distance             { get; set; }

		TResult Evaluate(TSelf next, float distance);
	}

	public struct DistanceDamageFallOf : IDistanceFallOf<DistanceDamageFallOf, float>
	{
		public float Damage;

		public static DistanceDamageFallOf FromPercentage(float damage, float Distance)
		{
			if (damage > 1)
				throw new InvalidOperationException("damage is superior than 1");
			if (Distance > 1)
				throw new InvalidOperationException("Distance is superior than 1");

			DistanceDamageFallOf fallOf = default;
			fallOf.DistanceIsPercentage = true;
			fallOf.Damage               = damage;
			fallOf.Distance             = Distance;
			return fallOf;
		}

		public static DistanceDamageFallOf FromFixed(float damage, float Distance)
		{
			DistanceDamageFallOf fallOf = default;
			fallOf.DistanceIsPercentage = false;
			fallOf.Damage               = damage;
			fallOf.Distance             = Distance;
			return fallOf;
		}

		public int CompareTo(DistanceDamageFallOf other)
		{
			return Distance.CompareTo(other.Distance);
		}

		public bool  DistanceIsPercentage { get; set; }
		public float Distance             { get; set; }

		public float Evaluate(DistanceDamageFallOf next, float distance)
		{
			return math.lerp(Damage, next.Damage, math.remap(0, 1, Distance, next.Distance, distance));
		}
	}

	public struct DistanceImpulseFallOf : IDistanceFallOf<DistanceImpulseFallOf, float>
	{
		public float Impulse;

		public static DistanceImpulseFallOf FromPercentage(float Impulse, float Distance)
		{
			if (Impulse > 1)
				throw new InvalidOperationException("Impulse is superior than 1");
			if (Distance > 1)
				throw new InvalidOperationException("Distance is superior than 1");

			DistanceImpulseFallOf fallOf = default;
			fallOf.DistanceIsPercentage = true;
			fallOf.Impulse              = Impulse;
			fallOf.Distance             = Distance;
			return fallOf;
		}

		public static DistanceImpulseFallOf FromFixed(float Impulse, float Distance)
		{
			DistanceImpulseFallOf fallOf = default;
			fallOf.DistanceIsPercentage = false;
			fallOf.Impulse              = Impulse;
			fallOf.Distance             = Distance;
			return fallOf;
		}

		public int CompareTo(DistanceImpulseFallOf other)
		{
			return Distance.CompareTo(other.Distance);
		}

		public bool  DistanceIsPercentage { get; set; }
		public float Distance             { get; set; }

		public float Evaluate(DistanceImpulseFallOf next, float distance)
		{
			return math.lerp(Impulse, next.Impulse, math.remap(0, 1, Distance, next.Distance, distance));
		}
	}
}