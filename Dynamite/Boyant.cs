using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Dynamite
{
	public class Boyant : MonoBehaviour
	{
		public float force = 1f;
		private Rigidbody m_rigidbody;
		void Start()
		{
			m_rigidbody = GetComponent<Rigidbody>();
		}
		void Update()
		{
			var hits = Physics.RaycastAll(transform.position, Vector3.down, 25f);
			foreach (var raycastHit in hits)
			{
				if (raycastHit.collider.gameObject.name == "LAKEBED")
				{
					var diff = -4.6f - transform.position.y;
					diff = Mathf.Clamp(diff, 0, 0.4f);
					m_rigidbody.velocity = new Vector3(m_rigidbody.velocity.x, diff, m_rigidbody.velocity.z);
					transform.position += Vector3.up * Time.deltaTime * diff; // raise 10 cm per second
				}
			}
		}
	}
}
