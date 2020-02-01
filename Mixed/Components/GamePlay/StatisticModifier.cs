using System;
using Patapon.Mixed.Units;
using Patapon4TLB.Default;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Patapon.Mixed.GamePlay
{
	public unsafe struct StatisticModifier
	{
#if USE_BLOB
		public BlobArray<StatusEffectModifier> StatusEffects;
#else
		public FixedList128<StatusEffectModifier> OffensiveStatusEffects;
		public FixedList128<StatusEffectModifier> DefensiveStatusEffects;
#endif

		public float Attack;
		public float Defense;

		public float ReceiveDamage;

		public float MovementSpeed;
		public float MovementAttackSpeed;
		public float MovementReturnSpeed;
		public float AttackSpeed;

		public float AttackSeekRange;

		public float Weight;

		public static readonly StatisticModifier Default = new StatisticModifier
		{
			Attack  = 1,
			Defense = 1,

			ReceiveDamage = 1,

			MovementSpeed       = 1,
			MovementAttackSpeed = 1,
			MovementReturnSpeed = 1,
			AttackSpeed         = 1,

			AttackSeekRange = 1,

			Weight = 1,
		};

		public void Multiply(ref UnitPlayState playState)
		{
			void mul_float(ref float left, in float multiplier)
			{
				left *= multiplier;
			}

			void mul_int(ref int left, in float multiplier)
			{
				var originalF = left * multiplier;
				left += ((int) Math.Round(originalF) - left);
			}

			mul_int(ref playState.Attack, Attack);
			mul_int(ref playState.Defense, Defense);

			mul_float(ref playState.ReceiveDamagePercentage, ReceiveDamage);

			mul_float(ref playState.MovementSpeed, MovementSpeed);
			mul_float(ref playState.MovementAttackSpeed, MovementAttackSpeed);
			mul_float(ref playState.MovementReturnSpeed, MovementReturnSpeed);
			mul_float(ref playState.AttackSpeed, AttackSpeed);

			mul_float(ref playState.AttackSeekRange, AttackSeekRange);

			mul_float(ref playState.Weight, Weight);
		}

#if USE_BLOB
		public static BlobAssetReference<StatisticModifier> Create(ref StatisticModifier modifier, StatusEffectModifier* modifiers, int modifierLength)
		{
			var builder = new BlobBuilder(Allocator.Temp, UnsafeUtility.SizeOf<StatisticModifier>() + (UnsafeUtility.SizeOf<StatusEffectModifier>() * modifierLength) + 16);
			var root = builder.ConstructRoot<StatisticModifier>();

			var array = builder.Allocate(ref root.StatusEffects, modifierLength);
			for (int i = 0; i != modifierLength; i++)
				array[i] = modifiers[i];

			var reference = builder.CreateBlobAssetReference<StatisticModifier>(Allocator.Persistent);

			builder.Dispose();

			return reference;
		}
#endif
		private int FindIndex(FixedList128<StatusEffectModifier> list, StatusEffect type)
		{
			var length = list.Length;
			for (var i = 0; i != length; i++)
			{
				if (list[i].Type == type)
					return i;
			}

			return -1;
		}

		public bool TryGetOffensiveEffect(StatusEffect type, out StatusEffectModifier modifier)
		{
			var index = FindIndex(OffensiveStatusEffects, type);
			if (index < 0)
			{
				modifier = new StatusEffectModifier {Type = StatusEffect.Unknown, Multiplier = 1};
				return false;
			}

			modifier = OffensiveStatusEffects[index];
			return true;
		}

		public void SetOffensiveEffect(StatusEffect type, float multiplier)
		{
			var index = FindIndex(OffensiveStatusEffects, type);
			if (index < 0)
				OffensiveStatusEffects.Add(new StatusEffectModifier {Type = type, Multiplier = multiplier});
			else
				OffensiveStatusEffects[index] = new StatusEffectModifier {Type = type, Multiplier = multiplier};
		}

		public bool TryGetDefensiveEffect(StatusEffect type, out StatusEffectModifier modifier)
		{
			var index = FindIndex(DefensiveStatusEffects, type);
			if (index < 0)
			{
				modifier = new StatusEffectModifier {Type = StatusEffect.Unknown, Multiplier = 1};
				return false;
			}

			modifier = DefensiveStatusEffects[index];
			return true;
		}

		public void SetDefensiveEffect(StatusEffect type, float multiplier)
		{
			var index = FindIndex(DefensiveStatusEffects, type);
			if (index < 0)
				DefensiveStatusEffects.Add(new StatusEffectModifier {Type = type, Multiplier = multiplier});
			else
				DefensiveStatusEffects[index] = new StatusEffectModifier {Type = type, Multiplier = multiplier};
		}
	}

	public struct StatusEffectModifier : IComparable<StatusEffectModifier>
	{
		public StatusEffect Type;
		public float        Multiplier;

		public int CompareTo(StatusEffectModifier other)
		{
			return Type.CompareTo(other.Type);
		}

		public static bool operator <(StatusEffectModifier left, StatusEffectModifier right)
		{
			return left.CompareTo(right) < 0;
		}

		public static bool operator >(StatusEffectModifier left, StatusEffectModifier right)
		{
			return left.CompareTo(right) > 0;
		}

		public static bool operator <=(StatusEffectModifier left, StatusEffectModifier right)
		{
			return left.CompareTo(right) <= 0;
		}

		public static bool operator >=(StatusEffectModifier left, StatusEffectModifier right)
		{
			return left.CompareTo(right) >= 0;
		}
	}
}