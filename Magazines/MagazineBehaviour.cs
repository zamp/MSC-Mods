using System;
using System.IO;
using System.Linq;
using MSCLoader;
using UnityEngine;

namespace Magazines
{
	public class MagazineBehaviour : MonoBehaviour
	{
		public class Magazine
		{
			public string name;
			public bool isPorn;
			public float price;
		}

		public class Data
		{
			public bool isBought = false;
			public float posX, posY, posZ, rotX, rotY, rotZ;
		}

		private Magazine m_data;
		private string m_baseFile;
		private Rigidbody m_rigidbody;
		private HutongGames.PlayMaker.FsmBool m_guiUseBool;
		private HutongGames.PlayMaker.FsmString m_guiInteractString;
		private string[] m_files;
		private bool m_isBeingBought;
		private HutongGames.PlayMaker.FsmBool m_guiBuyBool;

		private Data m_saveData;
		private string m_magazineDirectory;

		void Update()
		{
			try
			{
				Interact();
			}
			catch (Exception e)
			{
				ModConsole.Error(e.ToString());
			}
		}

		internal void Setup(string path)
		{
			try
			{
				m_magazineDirectory = new FileInfo(path).Directory.Name;
				Load();
				
				GameHook.InjectStateHook(GameObject.Find("STORE/StoreCashRegister/Register"), "Purchase", OnBuyHook);

				m_baseFile = path;
				m_data = SaveUtil.DeserializeReadFile<Magazine>(m_baseFile);
				GameHook.InjectStateHook(GameObject.Find("ITEMS"), "Save game", Save);

				ModConsole.Print("Setting up " + m_data.name);
				m_guiUseBool = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse");
				m_guiBuyBool = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIbuy");
				m_guiInteractString = PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction");
				m_rigidbody = GetComponent<Rigidbody>();
				
				if (IsBought)
				{
					MakeInteractable();
					transform.position = new Vector3(m_saveData.posX, m_saveData.posY, m_saveData.posZ);
					transform.rotation = Quaternion.Euler(m_saveData.rotX, m_saveData.rotY, m_saveData.rotZ);
				}
				else
				{
					// add this for sale
					Magazines.Instance.Rack.AddMagazineForSale(this);
				}

				m_files = Directory.GetFiles(Path.Combine(Path.GetDirectoryName(path), "Pages"));
				m_files = m_files.ToList().OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f))).ToArray();

				StartCoroutine(Magazines.LoadImage(m_files[0], SetCover));
			}
			catch (Exception e)
			{
				ModConsole.Error(e.ToString());
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
					if (IsBought)
					{
						m_guiUseBool.Value = true;
						if (cInput.GetButton("Use"))
						{
							Read();
						}
					}
					else
					{
						m_guiBuyBool.Value = true;
						m_guiInteractString.Value = m_data.name + " " + m_data.price.ToString("F") + " MK";

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
			register.FsmVariables.GetFsmFloat("PriceTotal").Value += m_data.price;
			register.SendEvent("PURCHASE");
			gameObject.SetActive(false);
		}

		private void OnBuyHook()
		{
			if (m_isBeingBought)
			{
				m_isBeingBought = false;
				OnPaid();
			}
		}

		private void OnPaid()
		{
			gameObject.SetActive(true);
			m_saveData.isBought = true;

			transform.position = new Vector3(-1551.1f, 4.8f, 1182.8f);
			transform.rotation = Quaternion.Euler(0, 330, 270);

			MakeInteractable();
		}

		private void MakeInteractable()
		{
			gameObject.SetActive(true);
			transform.SetParent(null, true);
			gameObject.name = m_data.name + "(Clone)";
			gameObject.layer = LayerMask.NameToLayer("Parts");
			gameObject.tag = "PART";

			m_rigidbody.isKinematic = false;
		}

		private void Load()
		{
			if (File.Exists(SaveFilePath))
			{
				m_saveData = SaveUtil.DeserializeReadFile<Data>(SaveFilePath);
			}
			else
			{
				m_saveData = new Data();
			}
		}

		private void Save()
		{
			m_saveData.posX = transform.position.x;
			m_saveData.posY = transform.position.y;
			m_saveData.posZ = transform.position.z;
			m_saveData.rotX = transform.rotation.eulerAngles.x;
			m_saveData.rotY = transform.rotation.eulerAngles.y;
			m_saveData.rotZ = transform.rotation.eulerAngles.z;
			SaveUtil.SerializeWriteFile(m_saveData, SaveFilePath);
		}

		public void Read()
		{
			Magazines.Instance.MagazineReader.Show(m_files);
		}

		private void SetCover(Texture2D obj)
		{
			GetComponent<MeshRenderer>().materials[1].mainTexture = obj;
		}

		public bool IsPorn
		{
			get { return m_data.isPorn; }
		}

		public bool IsBought
		{
			get { return m_saveData.isBought; }
		}

		public float Price
		{
			get { return m_data.price; }
		}

		public string SaveFilePath
		{
			get { return Path.Combine(Application.persistentDataPath, "magazine_" + m_magazineDirectory + ".xml"); }
		}
	}
}
