using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MSCLoader;
using UnityEngine;

namespace Magazines
{
	public class RackBehaviour : MonoBehaviour
	{
		private readonly List<Transform> m_topSlots = new List<Transform>();
		private readonly List<Transform> m_bottomSlots = new List<Transform>();
		private Transform m_magazinePrefab;

		void Start()
		{
			transform.position = new Vector3(-1549.6f,6.23f,1183.4f);
			transform.rotation = Quaternion.Euler(0, 238, 0);

			m_magazinePrefab = transform.Find("Magazine");
			m_bottomSlots.Add(transform.FindChild("BottomSlot1"));
			m_bottomSlots.Add(transform.FindChild("BottomSlot2"));
			m_bottomSlots.Add(transform.FindChild("BottomSlot3"));
			m_bottomSlots.Add(transform.FindChild("BottomSlot4"));

			m_topSlots.Add(transform.FindChild("TopSlot1"));
			m_topSlots.Add(transform.FindChild("TopSlot2"));
			m_topSlots.Add(transform.FindChild("TopSlot3"));
			m_topSlots.Add(transform.FindChild("TopSlot4"));

			var path = Path.Combine(Magazines.assetPath, "Magazines");
			foreach (var file in Directory.GetFiles(path, "*.xml", SearchOption.AllDirectories))
			{
				var magazine = Instantiate(m_magazinePrefab);
				magazine.gameObject.AddComponent<MagazineBehaviour>().Setup(file);
			}
		}

		public void AddMagazineForSale(MagazineBehaviour magazine)
		{
			var slots = magazine.IsPorn ? m_topSlots : m_bottomSlots;
			var slot = slots[Random.Range(0, slots.Count)];
			if (slot.childCount > 0)
				return;

			magazine.transform.SetParent(slot);
			magazine.transform.localPosition = Vector3.zero;
			magazine.transform.localRotation = Quaternion.identity;
			magazine.transform.localScale = Vector3.one;
			magazine.gameObject.SetActive(true);
			magazine.GetComponent<Rigidbody>().isKinematic = true;
			slots.RemoveAt(0);
		}
	}
}
