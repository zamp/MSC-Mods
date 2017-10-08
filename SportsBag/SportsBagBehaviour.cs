using System;
using System.Collections.Generic;
using System.Deployment.Internal;
using System.IO;
using MSCLoader;
using MSCStill;
using UnityEngine;
using Random = UnityEngine.Random;

//Standard unity MonoBehaviour class
namespace SportsBag
{
	public class SportsBagBehaviour : MonoBehaviour
	{
		public class SaveData
		{
			public float posX, posY, posZ, rotX, rotY, rotZ;
		}

		private Collider m_topTrigger;
		private bool m_isOpen = true;
		private SkinnedMeshRenderer m_meshRenderer;
		private float m_openAmount;
		private Transform m_hand;
		private Vector3 m_handPosition;
		private Quaternion m_handRotation;
		private readonly Dictionary<Collider, Joint> m_connectionPosition = new Dictionary<Collider, Joint>();
		private AudioSource m_audio;

		void Start()
		{
			Load();
			GameHook.InjectStateHook(GameObject.Find("ITEMS"), "Save game", Save);

			// these two things make this object carriable (probably)
			gameObject.name = "sports bag(Clone)";
			gameObject.layer = LayerMask.NameToLayer("Parts");
			gameObject.tag = "PART";

			m_topTrigger = transform.Find("TopTrigger").GetComponent<Collider>();

			m_meshRenderer = transform.Find("Mesh").GetComponent<SkinnedMeshRenderer>();

			m_hand = transform.Find("Hand");
			m_hand.gameObject.name = "severed hand(Clone)";
			m_hand.gameObject.layer = LayerMask.NameToLayer("Parts");
			m_hand.gameObject.tag = "PART";
			m_handPosition = m_hand.localPosition;
			m_handRotation = m_hand.localRotation;
			m_hand.gameObject.SetActive(false);

			var trigger = transform.Find("InsideTrigger").gameObject.AddComponent<TriggerCallback>();
			trigger.onTriggerStay += OnBagTriggerStay;

			m_audio = GetComponent<AudioSource>();
		}

		private void OnBagTriggerStay(Collider collider)
		{
			if (!m_isOpen || m_connectionPosition.ContainsKey(collider) || collider.transform.parent != null)
				return;

			var joint = gameObject.AddComponent<FixedJoint>();
			joint.connectedBody = collider.attachedRigidbody;
			joint.enableCollision = false;
			joint.breakForce = 5000f;
			m_connectionPosition.Add(collider, joint);
		}

		private void Save()
		{
			var data = new SaveData
			{
				posX = transform.position.x,
				posY = transform.position.y,
				posZ = transform.position.z,
				rotX = transform.rotation.eulerAngles.x,
				rotY = transform.rotation.eulerAngles.y,
				rotZ = transform.rotation.eulerAngles.z
			};
			SaveUtil.SerializeWriteFile(data, SaveFilePath);
		}

		private void Load()
		{
			if (File.Exists(SaveFilePath))
			{
				var data = SaveUtil.DeserializeReadFile<SaveData>(SaveFilePath);
				transform.position = new Vector3(data.posX, data.posY, data.posZ);
				transform.rotation = Quaternion.Euler(data.rotX, data.rotY, data.rotZ);
			}
			else
			{
				transform.position = new Vector3(-4.93f, 0.06f, 7.37f);
				transform.rotation = Quaternion.Euler(0.1f, 148f, 0.14f);
			}
		}

		void Update()
		{
			HandleBlendshape();
			Interact();
			RemoveJoints();
		}

		private void RemoveJoints()
		{
			var purgeList = new Queue<Collider>();
			foreach (var keypair in m_connectionPosition)
			{
				// was this removed?
				if (keypair.Key == null || keypair.Key.transform.parent != null)
				{
					Destroy(keypair.Value);
					purgeList.Enqueue(keypair.Key);
				}
			}

			while (purgeList.Count > 0)
			{
				m_connectionPosition.Remove(purgeList.Dequeue());
			} 
		}

		private void Interact()
		{
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			var hits = Physics.RaycastAll(ray, 1f);
			foreach (var hit in hits)
			{
				if (hit.collider == m_topTrigger)
				{
					PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse").Value = true;
					PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction").Value = m_isOpen ? "Close" : "Open";
					if (cInput.GetButtonDown("Use"))
					{
						m_isOpen = !m_isOpen;
						m_audio.Play();

						if (m_isOpen && Random.value < 0.1f)
						{
							m_hand.SetParent(transform);
							m_hand.localPosition = m_handPosition;
							m_hand.localRotation = m_handRotation;
							m_hand.gameObject.SetActive(true);
						}
					}
				}
			}
		}

		private void HandleBlendshape()
		{
			m_openAmount = Mathf.Clamp(m_openAmount + Time.deltaTime * (m_isOpen ? 500f : -500f), 0, 100f);
			m_meshRenderer.SetBlendShapeWeight(0, m_openAmount);
		}

		public string SaveFilePath { get { return Path.Combine(Application.persistentDataPath, "sportsbag.xml"); } }
	}
}