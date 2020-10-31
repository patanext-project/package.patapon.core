using System;
using GameHost.Native;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using GameHost.Simulation.Utility.Resource;
using GameHost.Simulation.Utility.Resource.Interfaces;
using PataNext.Module.Simulation.Resources;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(GameResource<UnitAttachmentResource>))]

namespace PataNext.Module.Simulation.Resources
{
	public readonly struct UnitAttachmentResource : IGameResourceDescription, IEquatable<UnitAttachmentResource>
	{
		public readonly CharBuffer64 Value;

		public class Register : RegisterGameHostComponentData<UnitAttachmentResource>
		{
		}

		public UnitAttachmentResource(CharBuffer64 value)
		{
			Value = value;
		}

		public UnitAttachmentResource(string value)
		{
			Value = CharBufferUtility.Create<CharBuffer64>(value);
		}
		
		public static implicit operator UnitAttachmentResource(CharBuffer64 value)
		{
			return new UnitAttachmentResource(value);
		}

		public static implicit operator UnitAttachmentResource(string value)
		{
			return new UnitAttachmentResource(value);
		}

		public bool Equals(UnitAttachmentResource other)
		{
			return Value.Equals(other.Value);
		}

		public override bool Equals(object obj)
		{
			return obj is UnitAttachmentResource other && Equals(other);
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}
	}
}