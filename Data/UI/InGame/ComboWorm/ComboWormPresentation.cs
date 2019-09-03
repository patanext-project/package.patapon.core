using package.patapon.core;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Misc;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.Rendering;

namespace Patapon4TLB.UI.InGame.ComboWorm
{
	public class ComboWormPresentation : RuntimeAssetPresentation<ComboWormPresentation>
	{
		public Vector2 screenPosition = new Vector2(0f, 0.75f);
		public Vector3 scale = new Vector3(0.15f, 0.15f, 0.15f);
		public Animator animator;

		private void OnEnable()
		{
			RenderPipelineManager.beginFrameRendering += OnBeginFrameRendering;
		}

		private void OnBeginFrameRendering(ScriptableRenderContext ctx, Camera[] cameras)
		{
			var clientWorld = ClientServerBootstrap.clientWorld[0];
			if (clientWorld == null || !clientWorld.IsCreated)
				return;
				
			var cam         = clientWorld.GetExistingSystem<ClientCreateCameraSystem>()?.Camera;
			if (cam == null)
				return;

			var camTr  = cam.transform;
			var selfTr = transform;

			var pos = cam.ViewportToWorldPoint(screenPosition);
			pos.z             = 1;
			selfTr.position   = pos;
			selfTr.localScale = scale * cam.orthographicSize;

			Debug.Log(clientWorld.GetOrCreateSystem<Sys>().TIME);
			animator.Play("score_m_25", -1, clientWorld.GetOrCreateSystem<Sys>().TIME * 2);	
		}

		[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
		private class Sys : ComponentSystem
		{
			public float TIME;

			protected override void OnUpdate()
			{
				Entities.ForEach((ref RhythmEngineProcess process) => { TIME = (process.Milliseconds + process.StartTime) * 0.001f; });
			}
		}
	}
}