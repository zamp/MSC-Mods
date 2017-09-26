using System.IO;
using UnityEngine;

namespace Dynamite
{
	public class BoxBehaviour : MonoBehaviour
	{
		private Collider m_dynamiteTrigger;
		private GameObject m_dynamitePrefab;
		private Collider m_lidTrigger;
		private bool m_isOpen;
		private AudioSource m_audio;

		public class SaveData
		{
			public float posX, posY, posZ, rotX, rotY, rotZ;
		}

		void Start()
		{
			gameObject.name = "box of dynamite(Clone)";
			gameObject.layer = LayerMask.NameToLayer("Parts");
			gameObject.tag = "PART";

			m_dynamiteTrigger = transform.FindChild("DynamiteTrigger").GetComponent<Collider>();
			m_lidTrigger = transform.FindChild("Lid/Trigger").GetComponent<Collider>();
			m_dynamitePrefab = transform.FindChild("Dynamite").gameObject;
			m_dynamitePrefab.AddComponent<DynamiteBehaviour>();

			m_audio = GetComponent<AudioSource>();

			Load();
			GameHook.InjectStateHook(GameObject.Find("ITEMS"), "Save game", Save);
		}

		void Update()
		{
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			var hits = Physics.RaycastAll(ray, 1f);
			foreach (var hit in hits)
			{
				if (hit.collider == m_lidTrigger)
				{
					PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse").Value = true;
					PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction").Value = m_isOpen ? "Close" : "Open";
					if (cInput.GetButtonDown("Use"))
					{
						m_isOpen = !m_isOpen;
						GetComponent<Animator>().Play(m_isOpen ? "Open" : "Close");
						m_audio.Play();
					}
					break;
				}

				if (hit.collider == m_dynamiteTrigger && m_isOpen)
				{
					PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse").Value = true;
					if (cInput.GetButtonDown("Use"))
					{
						var dynamite = Instantiate(m_dynamitePrefab);
						dynamite.transform.position = m_dynamitePrefab.transform.position;
						dynamite.transform.rotation = m_dynamitePrefab.transform.rotation;
						dynamite.gameObject.SetActive(true);
					}
					break;
				}
			}
		}

		private void Load()
		{
			var path = Path.Combine(Application.persistentDataPath, "dynamitebox.xml");
			if (File.Exists(path))
			{
				var data = SaveUtil.DeserializeReadFile<SaveData>(path);
				transform.position = new Vector3(data.posX, data.posY, data.posZ);
				transform.rotation = Quaternion.Euler(data.rotX, data.rotY, data.rotZ);
			}
			else
			{
				// reset to default position
				transform.position = new Vector3(-1535.9f,19.483f,-363.82f);
				transform.rotation = Quaternion.Euler(4.6f, 100f, 343f);
			}
		}

		private void Save()
		{
			var path = Path.Combine(Application.persistentDataPath, "dynamitebox.xml");
			SaveUtil.SerializeWriteFile(new SaveData
			{
				posX = transform.position.x,
				posY = transform.position.y,
				posZ = transform.position.z,
				rotX = transform.rotation.eulerAngles.x,
				rotY = transform.rotation.eulerAngles.y,
				rotZ = transform.rotation.eulerAngles.z,
			}, path);
		}
	}
}
