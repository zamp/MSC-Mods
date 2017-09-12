using System;
using System.Collections;
using System.IO;
using MSCLoader;
using UnityEngine;

//Standard unity MonoBehaviour class
namespace MSCStill
{
	public class Bottle : MonoBehaviour
	{
		[Serializable]
		public class SaveData
		{
			public float posX, posY, posZ;
			public float rotX, rotY, rotZ;
			public float water, ethanol, methanol;
		}
		public float water, ethanol, methanol, total;
		private Transform m_liquid;
		private GameObject m_plug;
		private GameObject m_funnel;
		private HutongGames.PlayMaker.FsmBool m_guiUseBool;
		private Collider m_plugCollider;

		void Awake()
		{
			m_liquid = transform.FindChild("Liquid");
			// these two things make this object carriable (probably)
			gameObject.name = "Moonshine Bottle(Clone)";
			gameObject.layer = LayerMask.NameToLayer("Parts");
			gameObject.tag = "PART";

			m_plug = transform.FindChild("Plug").gameObject;
			m_funnel = transform.FindChild("Funnel").gameObject;
			m_funnel.SetActive(false);

			m_guiUseBool = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse");
			m_plugCollider = transform.FindChild("PlugTrigger").GetComponent<Collider>();

			Load();
			GameHook.InjectStateHook(GameObject.Find("ITEMS"), "Save game", Save);
		}

		// Update is called once per frame
		void Update()
		{
			total = water + ethanol + methanol;
			m_liquid.localScale = new Vector3(30,30,total * 5f);

			if (!m_plug.activeSelf)
			{
				if (transform.up.y < -0.5f && total > 0)
				{
					water = 0;
					ethanol = 0;
					methanol = 0;
					transform.FindChild("EmptyAudioSource").GetComponent<AudioSource>().Play();
				}
			}

			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit, 1f))
			{
				if (hit.collider == m_plugCollider)
				{
					m_guiUseBool.Value = true;

					if (cInput.GetButtonDown("Use"))
						ShowPlug(!m_plug.activeSelf);
				} else if (hit.rigidbody == GetComponent<Rigidbody>() && !m_plug.activeSelf)
				{
					m_guiUseBool.Value = true;
					if (cInput.GetButtonDown("Use"))
					{
						StartCoroutine(Drink());
					}
				}
			}
		}

		private IEnumerator Drink()
		{
			var bottle = transform.FindChild("Bottle").gameObject;
			var liquid = transform.FindChild("Liquid").gameObject;
			var plug = transform.FindChild("Plug").gameObject;
			var funnel = transform.FindChild("Funnel").gameObject;

			bottle.SetActive(false);
			liquid.SetActive(false);
			plug.SetActive(false);
			var funnelActive = funnel.activeSelf;
			funnel.SetActive(false);

			yield return new WaitForSeconds(2f);

			var drinkEthanol = ethanol / total * 0.5f;
			var drinkMethanol = methanol / total * 0.5f;
			var drinkWater = water / total * 0.5f;

			water -= drinkWater;
			methanol -= drinkMethanol;
			ethanol -= drinkEthanol;

			bottle.SetActive(true);
			liquid.SetActive(true);
			plug.SetActive(true);
			funnel.SetActive(funnelActive);

			var boost = drinkEthanol * 6f;
			while (boost > 0)
			{
				boost -= Time.deltaTime;
				PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerDrunk").Value += Time.deltaTime;
				yield return null;
			}
			
			if (drinkEthanol < drinkMethanol) // more methanol than ethanol = d�d
			{
				PlayMakerFSM.BroadcastEvent("DEATH");
			}
			PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerThirst").Value -= drinkWater;
		}

		private void Save()
		{
			var data = new SaveData
			{
				ethanol = ethanol,
				methanol = methanol,
				water = water,
				posX = transform.position.x,
				posY = transform.position.y,
				posZ = transform.position.z,
				rotX = transform.rotation.eulerAngles.x,
				rotY = transform.rotation.eulerAngles.y,
				rotZ = transform.rotation.eulerAngles.z
			};

			var path = Path.Combine(Path.Combine(ModLoader.ModsFolder, "MSCStill"), "bottle.xml");
			SaveUtil.SerializeWriteFile(data, path);
		}

		private void Load()
		{
			var path = Path.Combine(Path.Combine(ModLoader.ModsFolder, "MSCStill"), "bottle.xml");
			if (!File.Exists(path))
				return;

			var data = SaveUtil.DeserializeReadFile<SaveData>(path);
			water = data.water;
			ethanol = data.ethanol;
			methanol = data.methanol;
			transform.position = new Vector3(data.posX, data.posY, data.posZ);
			transform.rotation = Quaternion.Euler(data.rotX, data.rotY, data.rotZ);
		}

		public void ShowFunnel(bool show)
		{
			m_funnel.SetActive(show);
		}

		public void ShowPlug(bool show)
		{
			m_plug.SetActive(show);
		}
	}
}
