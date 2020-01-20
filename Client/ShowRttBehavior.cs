using System;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace DefaultNamespace
{
	public class ShowRttBehavior : MonoBehaviour
	{
		private void OnGUI()
		{
			SystemInstance._?.OnGUI();
		}
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class SystemInstance : GameBaseSystem
	{
		public static SystemInstance _;

		private float  refreshAt;
		private string txt;

		protected override void OnCreate()
		{
			base.OnCreate();
			_ = this;
		}

		protected override void OnUpdate()
		{
			if (refreshAt > UnityEngine.Time.time)
				return;

			refreshAt = UnityEngine.Time.time + 0.1f;

			if (!HasSingleton<NetworkStreamConnection>())
				return;

			var ack = GetSingleton<NetworkSnapshotAckComponent>();
			txt = $"RTT {ack.EstimatedRTT:F2}\t(DEVIATION={ack.DeviationRTT:F2})\t (PREDICTION_DEVIATION={GetTick(true).Value - GetTick(false).Value})\nFPS={(int) (1 / UnityEngine.Time.deltaTime)}";
		}

		public void OnGUI()
		{
			GUI.contentColor = Color.black;
			GUI.Label(new Rect(new Vector2(6, 6), new Vector2(600, 250)), txt);
			GUI.Label(new Rect(new Vector2(5.5f, 5.5f), new Vector2(600, 250)), txt);
			GUI.contentColor = Color.white;
			GUI.Label(new Rect(new Vector2(5, 5), new Vector2(600, 250)), txt);
		}
	}
}