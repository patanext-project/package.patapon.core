using System;
using GameHost.Native;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using GameHost.Simulation.Utility.Resource;
using GameHost.Simulation.Utility.Resource.Interfaces;
using PataNext.Module.Simulation.Resources;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(GameResource<UnitKitResource>))]

namespace PataNext.Module.Simulation.Resources
{
	public readonly struct UnitKitResource : IGameResourceDescription, IEquatable<UnitKitResource>
	{
		public readonly CharBuffer64 Value;

		public class Register : RegisterGameHostComponentData<UnitKitResource>
		{
		}

		public UnitKitResource(CharBuffer64 value)
		{
			Value = value;
		}

		public UnitKitResource(string value)
		{
			Value = CharBufferUtility.Create<CharBuffer64>(value);
		}

		public bool Equals(UnitKitResource other)
		{
			return Value.Equals(other.Value);
		}

		public override bool Equals(object obj)
		{
			return obj is UnitKitResource other && Equals(other);
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}
	}
}