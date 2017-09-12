using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MSCDirtMod
{
	public class BucketTrigger : MonoBehaviour
	{
		public Action<BucketTrigger, bool> onTrigger;
		public Action<bool> onHover;

		private MeshCollider m_trigger;
		private GameObject m_sponge;

		void Start()
		{
			m_trigger = transform.FindChild("Trigger").GetComponent<MeshCollider>();
			m_sponge = transform.FindChild("Sponge").gameObject;
		}

		private void Update()
		{
			var cam = Camera.main;
			var hits = Physics.RaycastAll(cam.transform.position, cam.transform.forward, 2f);
			if (hits.Any(raycastHit => raycastHit.collider == m_trigger))
			{
				if (Input.GetMouseButtonDown(0))
				{
					onTrigger.Invoke(this, m_sponge.activeSelf);
				}
				else
				{
					onHover.Invoke(m_sponge.activeSelf);
				}
			}
		}

		public void SetSponge(bool active)
		{
			m_sponge.SetActive(active);
		}
	}
}
