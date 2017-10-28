using System;
using System.Collections.Generic;
using System.Deployment.Internal;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using MSCLoader;
using UnityEngine;
using Random = UnityEngine.Random;

namespace WoodCarrier
{
	public class WoodCarrierBehaviour : MonoBehaviour
	{
		public class SaveData
		{
			public float posX, posY, posZ, rotX, rotY, rotZ;
			public int logs;
		}

		public string id;
		private int m_logs = 0;
		private List<GameObject> m_logObjects = new List<GameObject>();
		private AudioSource m_addWoodAudioSource;
		private AudioClip[] m_addWoodClips;
		private Collider m_woodTrigger;
		private Rigidbody m_rigidbody;

		private void Start()
		{
			if (id == "home")
			{
				transform.position = new Vector3(55.48f, -0.9f, -71.76f);
				transform.rotation = Quaternion.Euler(0, 96, 0);
			}
			else
			{
				transform.position = new Vector3(-851.6f, -2.8f, 505.66f);
				transform.rotation = Quaternion.Euler(0, 252, 0);
			}

			gameObject.name = "Wood carrier(Clone)";
			gameObject.layer = LayerMask.NameToLayer("Parts");
			gameObject.tag = "PART";

			Load();
			GameHook.InjectStateHook(GameObject.Find("ITEMS"), "Save game", Save);

			m_logObjects.Add(transform.FindChild("Logs/Log1").gameObject);
			m_logObjects.Add(transform.FindChild("Logs/Log2").gameObject);
			m_logObjects.Add(transform.FindChild("Logs/Log3").gameObject);
			m_logObjects.Add(transform.FindChild("Logs/Log4").gameObject);
			m_logObjects.Add(transform.FindChild("Logs/Log5").gameObject);
			m_logObjects.Add(transform.FindChild("Logs/Log6").gameObject);

			m_addWoodAudioSource = gameObject.AddComponent<AudioSource>();
			m_addWoodClips = new[]
			{
				GameObject.Find("COTTAGE/Fireplace/WoodTrigger/Sound1").GetComponent<AudioSource>().clip,
				GameObject.Find("COTTAGE/Fireplace/WoodTrigger/Sound2").GetComponent<AudioSource>().clip,
				GameObject.Find("COTTAGE/Fireplace/WoodTrigger/Sound3").GetComponent<AudioSource>().clip
			};
			
			m_woodTrigger = transform.FindChild("Trigger").GetComponent<Collider>();
			m_woodTrigger.gameObject.AddComponent<TriggerCallback>().onTriggerEnter += OnWoodTriggerEnter;

			m_rigidbody = GetComponent<Rigidbody>();
		}

		private void OnWoodTriggerEnter(Collider obj)
		{
			if (obj.tag == "PART" && obj.name.StartsWith("firewood") && m_logs < 6)
			{
				Destroy(obj.gameObject);
				m_logs += 1;
				m_addWoodAudioSource.clip = m_addWoodClips[Random.Range(0, m_addWoodClips.Count())];
				m_addWoodAudioSource.Play();
			}
		}

		private void Update()
		{
			m_rigidbody.mass = 2f + m_logs * 2f;
			for (var i = 0; i < m_logObjects.Count; ++i)
			{
				m_logObjects[i].SetActive(m_logs > i);
			}
				
			Interact();
		}

		private void Interact()
		{
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			var hits = Physics.RaycastAll(ray, 2f);
			foreach (var raycastHit in hits)
			{
				if (raycastHit.collider == m_woodTrigger && m_logs > 0)
				{
					PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse").Value = true;
					PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction").Value = "Take firewood";

					if (cInput.GetButtonDown("Use"))
					{
						m_logs--;
						SpawnLog();
					}
				}
			}
		}

		private void SpawnLog()
		{
			var log = Instantiate(transform.FindChild("LogSpawn/Log").gameObject);
			log.gameObject.SetActive(true);
			log.transform.SetParent(null,true);
			var spawn = transform.FindChild("LogSpawn");
			log.transform.position = spawn.position;
			log.transform.rotation = spawn.rotation;
			log.transform.localScale = Vector3.one;
			log.AddComponent<Rigidbody>();
			log.gameObject.name = "firewood(Clone)";
			log.gameObject.layer = LayerMask.NameToLayer("Parts");
			log.gameObject.tag = "PART";
		}

		private void Save()
		{
			var data = new SaveData
			{
				posX = transform.position.x,
				posY = transform.position.y,
				posZ = transform.position.z,
				rotX = transform.eulerAngles.x,
				rotY = transform.eulerAngles.y,
				rotZ = transform.eulerAngles.z,
				logs = m_logs
			};
			SaveUtil.SerializeWriteFile(data, SaveFilePath);
		}

		private void Load()
		{
			if (File.Exists(SaveFilePath))
			{
				var data = SaveUtil.DeserializeReadFile<SaveData>(SaveFilePath);
				transform.position = new Vector3(data.posX, data.posY, data.posZ);
				transform.eulerAngles = new Vector3(data.rotX, data.rotY, data.rotZ);
				m_logs = data.logs;
			}
		}

		public string SaveFilePath
		{
			get { return Path.Combine(Application.persistentDataPath, "wood_carrier_" + id + ".xml"); }
		}
	}
}
