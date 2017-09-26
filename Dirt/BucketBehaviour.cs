using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MSCDirtMod
{
	public class BucketBehaviour : MonoBehaviour
	{
		public class SaveData
		{
			public float posX, posY, posZ, rotX, rotY, rotZ;
		}

		private HutongGames.PlayMaker.FsmBool m_guiUseBool;
		private string m_id;
		private HutongGames.PlayMaker.FsmString m_guiInteraction;
private  MeshCollider m_trigger;
private  GameObject m_sponge;

		void Start()
		{
			m_trigger = transform.FindChild("Trigger").GetComponent<MeshCollider>();
			m_sponge = transform.FindChild("Sponge").gameObject;

			gameObject.name = "Bucket with sponge(Clone)";
			gameObject.layer = LayerMask.NameToLayer("Parts");
			gameObject.tag = "PART";

			m_guiUseBool = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse");
			m_guiInteraction = PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction");
			
			Load();
			GameHook.InjectStateHook(GameObject.Find("ITEMS"), "Save game", Save);
		}

		void Update()
		{
			var cam = Camera.main;
			var hits = Physics.RaycastAll(cam.transform.position, cam.transform.forward, 2f);
			if (hits.Any(raycastHit => raycastHit.collider == m_trigger))
			{
				if (cInput.GetButtonDown("Use"))
				{
					ModBehaviour.Instance.BucketUse(this, m_sponge.activeSelf);
				}
				else
				{
					m_guiUseBool.Value = true;
					//m_guiInteraction.Value = m_sponge.activeSelf ? "Take sponge" : "Drop sponge";
				}
			}
		}

		public void SetSponge(bool active)
		{
			m_sponge.SetActive(active);
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
				rotZ = transform.eulerAngles.z
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
			}
		}

		internal void Setup(string id)
		{
			m_id = id;
			transform.position = id == "home" ? new Vector3(-7.4f, -0.19f, 12.3f) : new Vector3(-1560f, 3.2f, 1177.3f);
		}

		public string SaveFilePath
		{
			get { return Path.Combine(Application.persistentDataPath, "dirt_bucket_" + m_id + ".xml"); }
		}
	}
}
