using System.Collections;
using System.Collections.Generic;
using package.patapon.core;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using NotImplementedException = System.NotImplementedException;

namespace Patapon4TLB.UI
{
	public class UIBeatFrame : MonoBehaviour, IConvertGameObjectToEntity
	{
		public GameObject[] Lines = new GameObject[3];
		public Material Material;

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentObject(entity, transform);
			dstManager.AddComponentObject(entity, this);
		}

		[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
		public class ClientSystem : GameBaseSystem
		{
			public int   Beat;
			public bool  IsFever;
			public bool  IsCommand;
			public float LastBeatTime;

			private EntityQueryBuilder.F_DD<FlowRhythmEngineProcess, FlowCommandState> m_Delegate;

			private void ForEach(ref FlowRhythmEngineProcess process, ref FlowCommandState flowCommandState)
			{
				IsFever = false; // not yet amigo

				if (process.Beat != Beat)
				{
					Beat         = process.Beat;
					LastBeatTime = Time.time;
				}

				IsCommand = flowCommandState.IsActive;
			}

			protected override void OnCreate()
			{
				base.OnCreate();

				m_Delegate = ForEach;
			}

			protected override void OnUpdate()
			{
				Entities.ForEach(m_Delegate);
			}
		}

		[UpdateInGroup(typeof(PresentationSystemGroup))]
		[UpdateAfter(typeof(TickClientPresentationSystem))]
		public class System : GameBaseSystem
		{
			private void UpdateAll(UIBeatFrame uiBeatFrame)
			{
				void setGrayscale(ref Color c, float v)
				{
					c.r = v;
					c.g = v;
					c.b = v;
				}

				if (m_ClientSystem == null)
				{
					uiBeatFrame.gameObject.SetActive(false);
					return;
				}

				var color = new Color(1, 1, 1, 1);
				if (!m_ClientSystem.IsFever)
				{
					setGrayscale(ref color, 0.75f);

					uiBeatFrame.Lines[0].gameObject.SetActive(false);
					uiBeatFrame.Lines[1].gameObject.SetActive(true);
					uiBeatFrame.Lines[2].gameObject.SetActive(false);
				}

				if (m_ClientSystem.IsCommand)
				{
					setGrayscale(ref color, 0.5f);
					
					uiBeatFrame.Lines[0].gameObject.SetActive(true);
					uiBeatFrame.Lines[1].gameObject.SetActive(false);
					uiBeatFrame.Lines[2].gameObject.SetActive(true);
				}

				var noAlphaColor = color;
				noAlphaColor.a = 0.0f;

				uiBeatFrame.Material.SetColor(m_ColorId, Color.Lerp(color, noAlphaColor, (Time.time - m_ClientSystem.LastBeatTime) * 2.5f));
			}

			private EntityQueryBuilder.F_C<UIBeatFrame> m_QueryUpdateAll;
			private ClientSystem                        m_ClientSystem;

			private Material m_BeatMaterial;
			private int m_ColorId;

			protected override void OnCreate()
			{
				base.OnCreate();

				m_QueryUpdateAll = UpdateAll;
				m_ColorId = Shader.PropertyToID("_Color");
				
				RequireForUpdate(GetEntityQuery(typeof(UIBeatFrame)));
			}

			protected override void OnUpdate()
			{
				var clientWorld = GetActiveClientWorld();
				m_ClientSystem = clientWorld?.GetOrCreateSystem<ClientSystem>();

				Entities.ForEach(m_QueryUpdateAll);
			}
		}
	}
}