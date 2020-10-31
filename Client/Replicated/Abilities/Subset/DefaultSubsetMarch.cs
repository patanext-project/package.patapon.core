using System;
using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace PataNext.Simulation.Mixed.Abilities.Subset
{
	/// <summary>
	/// Represent a subset of the march ability that can be used on other abilities.
	/// </summary>
	public struct DefaultSubsetMarch : IComponentData
	{
		/// <summary>
		/// Which Movable target should we move?
		/// </summary>
		[Flags]
		public enum ETarget
		{
			None     = 0,
			Cursor   = 1 << 1,
			Movement = 1 << 2,
			All      = Cursor | Movement
		}

		/// <summary>
		/// Is the subset component active?
		/// </summary>
		public bool IsActive;

		/// <summary>
		/// What is our current movable target?
		/// </summary>
		public ETarget Target;

		/// <summary>
		/// The acceleration when marching
		/// </summary>
		public float AccelerationFactor;

		/// <summary>
		/// How much time was this subset active?
		/// </summary>
		/// <remarks>
		///	This variable was named 'Delta'
		/// </remarks>
		public float ActiveTime;

		public class Register : RegisterGameHostComponentData<DefaultSubsetMarch>
		{
			protected override ICustomComponentDeserializer CustomDeserializer => new DefaultSingleDeserializer<DefaultSubsetMarch>();
		}
	}
}