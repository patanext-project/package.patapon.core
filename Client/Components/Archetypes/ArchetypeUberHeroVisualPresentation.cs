using System;
using Patapon.Client.Graphics.Animation.Units;
using UnityEngine;

namespace Components.Archetypes
{
	public class ArchetypeUberHeroVisualPresentation : UnitVisualPresentation
	{
		public enum RootType
		{
			Mask,
			LeftWeapon,
			RightWeapon
		}

		public Transform LeftWeaponRoot;

		public Transform MaskRoot;
		public Transform RightWeaponRoot;

		public Transform GetRoot(RootType rootType)
		{
			switch (rootType)
			{
				case RootType.Mask:
					return MaskRoot;
				case RootType.LeftWeapon:
					return LeftWeaponRoot;
				case RootType.RightWeapon:
					return RightWeaponRoot;
			}

			throw new NullReferenceException("No mask found with rootType: " + rootType);
		}

		public override void UpdateData()
		{
		}
	}
}