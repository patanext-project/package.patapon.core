using System;
using GameHost.Core.Native.xUnity;
using GameHost.Simulation.Utility.Resource.Systems;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.Client.Systems;
using PataNext.Module.Simulation.Components.Units;
using PataNext.Module.Simulation.Resources;
using PataNext.Module.Simulation.Resources.Keys;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using Unity.Collections;
using Unity.Entities;
using Utility.GameResources;

namespace PataNext.Client.Components.Archetypes
{
	public class ArchetypeDefaultPresentation : UnitVisualPresentation
	{
		public override void UpdateData()
		{
		}
		
		[UpdateInGroup(typeof(OrderGroup.Presentation.AfterSimulation))]
		public class LocalSystem : AbsGameBaseSystem
		{
			protected override void OnUpdate()
			{
				Entities.ForEach((ArchetypeDefaultPresentation presentation) =>
				{
					presentation.OnSystemUpdate();
				}).WithStructuralChanges().Run();
			}
		}
	}
}