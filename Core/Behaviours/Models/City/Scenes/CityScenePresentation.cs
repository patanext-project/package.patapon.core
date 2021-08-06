using System;
using GameHost.Core;
using PataNext.Client.Rpc.City;
using StormiumTeam.GameBase.Utility.AssetBackend;
using Unity.Entities;
using UnityEngine;

namespace PataNext.Client.DataScripts.Models.Projectiles.City.Scenes
{
	public abstract class CityScenePresentation : EntityVisualPresentation
	{
		// as of now, there isn't a good way to know if it's a CityLocationPresentation when querying entities since it use the topmost type
		public struct IsObject : IComponentData
		{
		}

		public override void OnBackendSet()
		{
			Backend.DstEntityManager.AddComponentData(Backend.BackendEntity, new IsObject());
			
			base.OnBackendSet();
		}

		protected abstract void OnEnter();

		protected virtual void OnRequestExit()
		{
			Backend.DstEntityManager.World.GetExistingSystem<GameHostConnector>()
			       .RpcClient
			       .SendNotification(new ModifyPlayerCityLocationRpc());
		}

		protected abstract void OnExit();

		private void OnDisable()
		{
			// force exit
			if (isIn)
				OnExit();
		}

		private bool isIn;

		public void RequestExit()
		{
			OnRequestExit();
		}
		
		public bool TrySoftEnter()
		{
			if (isIn) 
				return false;
			
			isIn = true;
			OnEnter();
			return true;
		}

		public bool SetExitState()
		{
			if (!isIn) 
				return false;

			isIn = false;
			OnExit();
			return true;
		}
	}
}