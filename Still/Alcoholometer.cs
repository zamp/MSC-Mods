using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HutongGames.PlayMaker;
using MSCLoader;
using UnityEngine;

namespace MSCStill
{
	public class Alcoholometer :MonoBehaviour
	{
		[Serializable]
		public class SaveData
		{
			public float posX,posY,posZ,rotX,rotY,rotZ;
			public bool isBought;
		}

		private const float ZERO = -0.2838f;
		private const float HUNDRED = -0.1484f;
		private const float EMPTY = -0.3244f;
		private bool m_isBought;
		private Rigidbody m_rigidbody;
		private FsmBool m_guiUseBool;
		private FsmBool m_guiBuyBool;
		private FsmString m_guiInteractString;
		private bool m_isBeingBought;
		private bool m_isInUse;

		void Start()
		{
			m_rigidbody = GetComponent<Rigidbody>();
			m_guiUseBool = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse");
			m_guiBuyBool = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIbuy");
			m_guiInteractString = PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction");
			Load();
			GameHook.InjectStateHook(GameObject.Find("ITEMS"), "Save game", Save);
			GameHook.InjectStateHook(GameObject.Find("STORE/StoreCashRegister/Register"), "Purchase", OnBuyHook);

			transform.FindChild("Trigger").gameObject.AddComponent<TriggerCallback>().onTriggerEnter += OnTriggerEnter;
		}

		private void OnTriggerEnter(Collider obj)
		{
			var bottle = obj.GetComponent<Bottle>();
			if (bottle && bottle.total > 0)
			{
				var alcohol = bottle.ethanol / bottle.total;
				SetAlcohol(alcohol);
				GetComponent<Animator>().Play("AlcoholTest");
				GetComponent<AudioSource>().Play();
			}
			if (obj.name.StartsWith("bucket"))
			{
				var fsm = obj.GetComponents<PlayMakerFSM>().FirstOrDefault(x => x.FsmName == "Use");
				var alcohol = fsm.FsmVariables.GetFsmFloat("Alcohol").Value;
				SetAlcohol(alcohol);
				GetComponent<Animator>().Play("AlcoholTest");
				GetComponent<AudioSource>().Play();
			}

			if (obj.name.StartsWith("kilju"))
			{
				var fsm = obj.GetComponent<PlayMakerFSM>();
				var fsmBool = fsm.FsmVariables.GetFsmBool("ContainsKilju");
				if (fsmBool != null && fsmBool.Value)
				{
					var alc = fsm.FsmVariables.GetFsmFloat("KiljuAlc").Value;
					SetAlcohol(alc);
					GetComponent<Animator>().Play("AlcoholTest");
					GetComponent<AudioSource>().Play();
				}
			}
		}

		void Update()
		{
			Interact();
		}

		private void SetAlcohol(float percentage)
		{
			var y = Mathf.Lerp(ZERO, HUNDRED, 1-percentage);
			transform.Find("Level/Bobber").transform.localPosition = new Vector3(0,y,0);
		}

		private void OnBuyHook()
		{
			if (m_isBeingBought)
			{
				m_isBeingBought = false;
				m_isBought = true;

				gameObject.SetActive(true);

				transform.position = new Vector3(-1551.1f, 4.8f, 1182.8f);
				transform.rotation = Quaternion.Euler(0, 0, 0);

				MakeInteractable();
			}
		}

		private void MakeInteractable()
		{
			transform.SetParent(null, true);
			gameObject.name = "Alcoholometer (Clone)";
			gameObject.layer = LayerMask.NameToLayer("Parts");
			gameObject.tag = "PART";

			m_rigidbody.isKinematic = false;
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
				rotZ = transform.rotation.eulerAngles.z,
				isBought = m_isBought
			};

			SaveUtil.SerializeWriteFile(data, SaveFilePath);
		}

		private void Load()
		{
			var oldPath = Path.Combine(Path.Combine(ModLoader.ModsFolder, "MSCStill"), "alcometer.xml");
			if (File.Exists(oldPath))
			{
				// migrate file
				File.Move(oldPath, SaveFilePath);
			}

			if (File.Exists(SaveFilePath))
			{
				var data = SaveUtil.DeserializeReadFile<SaveData>(SaveFilePath);
				transform.position = new Vector3(data.posX, data.posY, data.posZ);
				transform.rotation = Quaternion.Euler(data.rotX, data.rotY, data.rotZ);
				m_isBought = data.isBought;
			}
			else
			{
				m_isBought = false;
			}

			if (m_isBought)
			{
				MakeInteractable();
			}
			else
			{
				// move to teimo's
				m_rigidbody.isKinematic = true;
				transform.position = new Vector3(-1548.2f, 5.3f, 1181.87f);
				transform.rotation = Quaternion.Euler(0, 180, 0);
			}
		}

		private void Interact()
		{
			if (Camera.main == null)
				return;

			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit, 1f))
			{
				if (hit.rigidbody != null && hit.rigidbody == m_rigidbody)
				{
					if (!m_isBought)
					{
						m_guiBuyBool.Value = true;
						m_guiInteractString.Value = "Alcoholometer 1295 MK";

						if (Input.GetMouseButton(0))
						{
							Buy();
						}
					}
				}
			}
		}

		private void Buy()
		{
			m_isBeingBought = true;
			var register = GameObject.Find("STORE/StoreCashRegister/Register").GetComponent<PlayMakerFSM>();
			register.FsmVariables.GetFsmFloat("PriceTotal").Value += 1295;
			register.SendEvent("PURCHASE");
			gameObject.SetActive(false);
		}

		public string SaveFilePath
		{
			get { return Path.Combine(Application.persistentDataPath, "mscstill_alcoholometer.xml"); }
		}
	}
}
