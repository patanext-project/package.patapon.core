using System;
using GameHost.Native;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using GameHost.Simulation.Utility.Resource;
using GameHost.Simulation.Utility.Resource.Interfaces;
using PataNext.Module.Simulation.Resources;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(GameResource<EquipmentResource>))]

namespace PataNext.Module.Simulation.Resources
{
	public readonly struct EquipmentResource : IGameResourceDescription, IEquatable<EquipmentResource>
	{
		public readonly CharBuffer64 Value;

		public class Register : RegisterGameHostComponentData<EquipmentResource>
		{
		}

		public EquipmentResource(CharBuffer64 value)
		{
			Value = value;
		}

		public EquipmentResource(string value)
		{
			Value = CharBufferUtility.Create<CharBuffer64>(value);
		}

		public static implicit operator EquipmentResource(string value)
		{
			return new EquipmentResource(value);
		}


		public bool Equals(EquipmentResource other)
		{
			return Value.Equals(other.Value);
		}

		public override bool Equals(object obj)
		{
			return obj is EquipmentResource other && Equals(other);
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}
	}
}