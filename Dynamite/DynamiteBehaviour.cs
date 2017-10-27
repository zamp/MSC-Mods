using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HutongGames.PlayMaker;
using MSCLoader;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Dynamite
{
	public class DynamiteBehaviour : MonoBehaviour
	{
		private  Collider m_collider;
		private bool m_isLit;
		private bool m_isExploded = false;
		private bool m_shouldExplode = false;
		private float m_timer;
		private float m_fuseTimer;

		private void Start()
		{
			gameObject.name = "stick of dynamite(Clone)";
			gameObject.layer = LayerMask.NameToLayer("Parts");
			gameObject.tag = "PART";

			m_collider = GetComponent<Collider>();
		}

		void Update()
		{
			try
			{
				if (m_isLit)
				{
					m_timer -= Time.deltaTime;
					m_fuseTimer -= Time.deltaTime;

					if (m_fuseTimer < 0)
					{
						GetComponent<Animator>().StopPlayback();
						GetComponent<AudioSource>().Stop();
						transform.FindChild("FuseParticles").GetComponent<ParticleSystem>().Stop();
					}

					if (m_shouldExplode && m_timer < 0)
					{
						ExplodeNow();
					}
					return;
				}	

				var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
				var hits = Physics.RaycastAll(ray, 1f);
				foreach (var hit in hits)
				{
					if (hit.collider == m_collider)
					{
						PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse").Value = true;
						if (cInput.GetButtonDown("Use"))
						{
							m_isLit = true;
							GetComponent<Animator>().Play("BurnFuse");
							GetComponent<AudioSource>().Play();
							transform.FindChild("FuseParticles").GetComponent<ParticleSystem>().Play();
							Explode(Random.value < 0.05f ? Random.Range(18f, 1800f) : Random.Range(8f, 11f));
						}
					}
				}
			}
			catch (Exception e)
			{
				ModConsole.Error(e.ToString());
			}
		}

		public void Explode(float delay)
		{
			if (m_isExploded)
				return;
			m_isExploded = true;

			m_isLit = true;
			m_shouldExplode = true;
			m_timer = delay;
			m_fuseTimer = 10f;
		}

		private void ExplodeNow()
		{
			// spawn fish
			var underwater = false;
			if (transform.position.y < -4.6f)
			{
				RaycastHit hitInfo;
				if (Physics.Raycast(transform.position, Vector3.down, out hitInfo, 25f) &&
					hitInfo.collider.gameObject.name == "LAKEBED")
				{
					// spawn fish
					var randomCount = Random.Range(1, 8);
					for (var i = 0; i < randomCount; ++i)
					{
						var diff = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f));
						var pos = transform.position + diff;

						// is there still water here?
						if (Physics.Raycast(pos, Vector3.down, out hitInfo, 25f) &&
							hitInfo.collider.gameObject.name == "LAKEBED"
							&& pos.y < -4.6f)
						{
							// spawn fish
							var trap = GameObject.Find("fish trap(itemx)");
							var fsm = trap.transform.FindChild("Spawn").GetComponent<PlayMakerFSM>();
							var newFish = fsm.FsmVariables.GetFsmGameObject("New");
							fsm.SendEvent("SPAWNITEM");
							var fish = newFish.Value;
							fish.transform.parent = null;
							fish.transform.position = pos;
							fish.transform.rotation = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
							fish.AddComponent<Boyant>();
						}
					}

					underwater = true;
				}
			}
			var player = GameObject.Find("PLAYER");
			if (Vector3.Distance(player.transform.position, transform.position) < 5f)
			{
				PlayMakerFSM.BroadcastEvent("DEATH");
			}

			if (underwater)
			{
				var explosion = transform.FindChild("UnderwaterExplosion");
				explosion.SetParent(null, true);
				var p = explosion.transform.position;
				p.y = -4.57f;
				explosion.transform.position = p;
				explosion.transform.rotation = Quaternion.Euler(90, 0, 0);
				explosion.gameObject.SetActive(true);
				Destroy(explosion.gameObject, 6f);
			}
			else
			{
				var rigidbodies = new HashSet<Rigidbody>();
				var things = Physics.OverlapSphere(transform.position, 5f);
				foreach (var collider in things)
				{
					var dynamite = collider.GetComponent<DynamiteBehaviour>();
					if (dynamite && dynamite != this)
					{
						dynamite.Explode(Random.Range(0.02f,0.1f));
						continue;
					}

					var humanTrigger = collider.GetComponent<PlayMakerFSM>();
					if (humanTrigger != null && humanTrigger.FsmName == "CarHit")
					{
						// "fool the trigger to thinking there's a car here"
						var go = new GameObject("CarCollider");
						go.AddComponent<SphereCollider>().radius = 0.1f;
						go.transform.position = humanTrigger.transform.position;
						Destroy(go, 0.02f);

						// this bit kills cops
						humanTrigger.SendEvent("TRIGGER ENTER");
					}

					if (collider.attachedRigidbody != null && !rigidbodies.Contains(collider.attachedRigidbody))
					{
						// turn kinematic objects to nonkinematic so they fly around
						collider.attachedRigidbody.isKinematic = false;
						collider.attachedRigidbody.useGravity = true;
						rigidbodies.Add(collider.attachedRigidbody);
						collider.attachedRigidbody.AddExplosionForce(2500f, transform.position, 15f, 0, ForceMode.Impulse);

						var crashEvent = collider.attachedRigidbody.transform.FindChild("CrashEvent");
						if (crashEvent != null)
						{
							var fsm = crashEvent.GetComponent<PlayMakerFSM>();
							fsm.SendEvent("CRASH");
						}
					}
				}

				var explosion = transform.FindChild("Explosion");
				explosion.SetParent(null, true);
				explosion.gameObject.SetActive(true);
				Destroy(explosion.gameObject, 6f);
			}

			m_shouldExplode = false;
			Destroy(gameObject, 0.02f);
		}
	}
}
