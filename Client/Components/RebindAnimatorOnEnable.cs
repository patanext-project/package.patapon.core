using UnityEngine;

namespace Components
{
	public class RebindAnimatorOnEnable : MonoBehaviour
	{
		private void OnEnable()
		{
			GetComponent<Animator>().WriteDefaultValues();
			GetComponent<Animator>().Rebind();
		}
	}
}