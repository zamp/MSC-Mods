using System.Collections.Generic;
using System.Deployment.Internal;
using System.IO;
using MSCLoader;
using MSCStill;
using UnityEngine;

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
		private bool m_isOpen;
		private SkinnedMeshRenderer m_meshRenderer;
		private float m_openAmount;

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

			var trigger = transform.Find("InsideTrigger").gameObject.AddComponent<TriggerCallback>();
			trigger.onTriggerStay += OnBagTriggerStay;
			trigger.onTriggerExit += OnBagTriggerExit;
		}

		private void OnBagTriggerExit(Collider obj)
		{
			obj.isTrigger = false;
			foreach (var col in obj.transform.GetComponentsInChildren<Collider>())
				col.isTrigger = false;
		}

		private void OnBagTriggerStay(Collider obj)
		{
			if (!m_isOpen)
				return;
			// item has no parent
			var rigidbody = obj.GetComponent<Rigidbody>();
			if (rigidbody != null && (rigidbody.transform.parent == null || rigidbody.transform.parent == transform))
			{
				// make this bag it's parent and make rigidbody kinetic
				rigidbody.isKinematic = true;
				rigidbody.transform.SetParent(transform, true);
				obj.isTrigger = true;
				foreach (var col in obj.transform.GetComponentsInChildren<Collider>())
					col.isTrigger = true;
			}
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
			SaveUtil.SerializeWriteFile(data, Path.Combine(ModLoader.ModsFolder, Path.Combine("SportsBag", "bag.xml")));
		}

		private void Load()
		{
			var path = Path.Combine(ModLoader.ModsFolder, Path.Combine("SportsBag", "bag.xml"));
			if (File.Exists(path))
			{
				var data = SaveUtil.DeserializeReadFile<SaveData>(path);
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
					}
				}
			}
		}

		private void HandleBlendshape()
		{
			m_openAmount = Mathf.Clamp(m_openAmount + Time.deltaTime * (m_isOpen ? 100f : -100f), 0, 100f);
			m_meshRenderer.SetBlendShapeWeight(0, m_openAmount);
		}
	}
}