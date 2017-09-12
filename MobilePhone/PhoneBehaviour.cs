using HutongGames.PlayMaker;
using MSCLoader;
using UnityEngine;
using System.IO;

//Standard unity MonoBehaviour class
namespace MobilePhone
{
	public class PhoneBehaviour : MonoBehaviour
	{
		public class SaveData
		{
			public float posX, posY, posZ, rotX, rotY, rotZ;
		}

		private GameObject m_ringing;
		private FsmBool m_answeredBool;
		private AudioSource m_ringAudioSource;
		private FsmBool m_guiUseBool;
		private bool m_onFace;
		private float m_blinkTimer;
		private MeshRenderer m_phoneRenderer;
		private MeshRenderer m_antennaRenderer;
		private bool m_blink;

		void Awake()
		{
			transform.position = new Vector3(-7.08f, 0.768f, 7.9f);
			transform.rotation = Quaternion.Euler(new Vector3(0, 77.86f, 270f));
			Load();

			gameObject.name = "Mobile Phone(Clone)";
			gameObject.layer = LayerMask.NameToLayer("Parts");
			gameObject.tag = "PART";

			var fsm = GameObject.Find("YARD/Building/Dynamics/Telephone/Logic")
				.transform.FindChild("Ring").GetComponent<PlayMakerFSM>();
			m_answeredBool = fsm.FsmVariables.FindFsmBool("Answer");
			m_ringing = fsm.gameObject;
			m_ringAudioSource = GetComponent<AudioSource>();

			m_guiUseBool = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse");

			m_phoneRenderer = transform.FindChild("Phone").GetComponent<MeshRenderer>();
			m_antennaRenderer = transform.FindChild("Antenna").GetComponent<MeshRenderer>();

			GameHook.InjectStateHook(GameObject.Find("ITEMS"), "Save game", Save);
		}

		private void Update()
		{
			if (m_ringing.activeSelf && !m_answeredBool.Value)
			{
				// ringing and not answered
				Ring();
			} else if (m_ringing.activeSelf && m_answeredBool.Value)
			{
				// answered
				if (m_ringAudioSource.isPlaying)
					m_ringAudioSource.Stop();

				HangUp();
			}
			else
			{
				// not ringing
				if (m_ringAudioSource.isPlaying)
					m_ringAudioSource.Stop();

				m_phoneRenderer.material.SetColor("_EmissionColor", Color.black);
				m_phoneRenderer.material.SetFloat("_EmissionScaleUI", 0f);
			}
		}

		private void HangUp()
		{
			if (cInput.GetButtonDown("Use") && m_onFace)
			{
				m_answeredBool.Value = false;
				m_onFace = false;

				m_phoneRenderer.enabled = true;
				m_antennaRenderer.enabled = true;
				m_ringing.SetActive(false);

				MasterAudio.StopAllOfSound("Teimo");
				MasterAudio.StopAllOfSound("Callers");
			}
		}


		private void Ring()
		{
			m_blinkTimer -= Time.deltaTime;
			if (m_blinkTimer < 0)
			{
				m_blinkTimer = 0.5f;
				m_blink = !m_blink;
			}

			m_phoneRenderer.material.SetColor("_EmissionColor", m_blink ? Color.white : Color.black);
			m_phoneRenderer.material.SetFloat("_EmissionScaleUI", m_blink ? 1f : 0f);

			if (!m_ringAudioSource.isPlaying)
				m_ringAudioSource.Play();

			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit, 1f))
			{
				if (hit.rigidbody == GetComponent<Rigidbody>())
				{
					m_guiUseBool.Value = true;
					if (cInput.GetButtonDown("Use"))
					{
						m_ringAudioSource.Stop();
						m_answeredBool.Value = true;
						m_onFace = true;

						m_phoneRenderer.enabled = false;
						m_antennaRenderer.enabled = false;
					}
				}
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
				rotZ = transform.rotation.eulerAngles.z,
			};

			var path = Path.Combine(Path.Combine(ModLoader.ModsFolder, "MobilePhone"), "phone.xml");
			SaveUtil.SerializeWriteFile(data, path);
		}

		private void Load()
		{
			var path = Path.Combine(Path.Combine(ModLoader.ModsFolder, "MobilePhone"), "phone.xml");
			if (!File.Exists(path))
				return;

			var data = SaveUtil.DeserializeReadFile<SaveData>(path);
			transform.position = new Vector3(data.posX, data.posY, data.posZ);
			transform.rotation = Quaternion.Euler(data.rotX, data.rotY, data.rotZ);
		}
	}
}
