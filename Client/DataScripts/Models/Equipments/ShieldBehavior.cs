using StormiumTeam.GameBase.BaseSystems;
using Unity.Entities;
using UnityEngine;

namespace PataNext.Client.DataScripts.Models.Equipments
{
	public class ShieldBehavior : MonoBehaviour
	{
		private Vector3 m_Scale;

		[UpdateInGroup(typeof(PresentationSystemGroup))]
		public class UpdateSystem : AbsGameBaseSystem
		{
			protected override void OnUpdate()
			{
				Entities.ForEach((UnitEquipmentPresentation presentation, ShieldBehavior behavior) =>
				{
					if (presentation == null || presentation.Backend == null)
					{
						
					}
				
					var backend = presentation.Backend;
					if (!EntityManager.Exists(backend.DstEntity))
						return;

					var playState   = EntityManager.GetComponentData<UnitPlayState>(backend.DstEntity);
					var targetScale = Vector3.one * (1 + (1 - playState.ReceiveDamagePercentage) * 0.8f);
					behavior.m_Scale = Vector3.MoveTowards(behavior.m_Scale, targetScale, Time.DeltaTime * 0.75f);
					behavior.m_Scale = Vector3.Lerp(behavior.m_Scale, targetScale, Time.DeltaTime);

					presentation.transform.localScale = behavior.m_Scale;
				}).WithoutBurst().Run();
			}
		}
	}
}