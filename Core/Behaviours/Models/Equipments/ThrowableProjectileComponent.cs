using UnityEngine;
using UnityEngine.AddressableAssets;

namespace PataNext.Client.DataScripts.Models.Equipments
{
	public class ThrowableProjectileComponent : MonoBehaviour
	{
		public GameObject projectilePrefab;
		public AssetReference assetReference;
		
		public Transform  projectileStart;
	}
}