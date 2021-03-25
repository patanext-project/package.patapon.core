using UnityEngine;

namespace PataNext.Client.Components
{
	public class RebindAnimatorOnEnable : MonoBehaviour
	{
		private void OnEnable()
		{
			GetComponent<Animator>().WriteDefaultValues();
			GetComponent<Animator>().Rebind();
		}
		
		private void OnDisable()
		{
			GetComponent<Animator>().WriteDefaultValues();
			GetComponent<Animator>().Rebind();
		}
	}
}