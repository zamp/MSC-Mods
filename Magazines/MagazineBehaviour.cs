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
			public bool isBought, isPorn;
			public float price, posX, posY, posZ, rotX, rotY, rotZ;
		}

		private Magazine m_data;
		private string m_file;
		private Rigidbody m_rigidbody;
		private HutongGames.PlayMaker.FsmBool m_guiUseBool;
		private HutongGames.PlayMaker.FsmString m_guiInteractString;
		private string[] m_files;
		private bool m_isBeingBought;
		private HutongGames.PlayMaker.FsmBool m_guiBuyBool;

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

		internal void Setup(string file)
		{
			try
			{
				GameHook.InjectStateHook(GameObject.Find("STORE/StoreCashRegister/Register"), "Purchase", OnBuyHook);

				m_file = file;
				m_data = SaveUtil.DeserializeReadFile<Magazine>(m_file);
				GameHook.InjectStateHook(GameObject.Find("ITEMS"), "Save game", Save);

				ModConsole.Print("Setting up " + m_data.name);
				m_guiUseBool = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse");
				m_guiBuyBool = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIbuy");
				m_guiInteractString = PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction");
				m_rigidbody = GetComponent<Rigidbody>();
				
				if (IsBought)
				{
					MakeInteractable();
					transform.position = new Vector3(m_data.posX, m_data.posY, m_data.posZ);
					transform.rotation = Quaternion.Euler(m_data.rotX, m_data.rotY, m_data.rotZ);
				}
				else
				{
					// add this for sale
					Magazines.Instance.Rack.AddMagazineForSale(this);
				}

				m_files = Directory.GetFiles(Path.Combine(Path.GetDirectoryName(file), "Pages"));
				m_files = m_files.ToList().OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f))).ToArray();

				StartCoroutine(Magazines.LoadImage(m_files[0], SetCover));
			}
			catch (Exception e)
			{
				ModConsole.Print(e);
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
			m_data.isBought = true;

			transform.position = new Vector3(-1551.1f, 4.8f, 1182.8f);
			transform.rotation = Quaternion.Euler(0, 330, 270);

			MakeInteractable();
		}

		private void MakeInteractable()
		{
			transform.SetParent(null, true);
			gameObject.name = m_data.name + "(Clone)";
			gameObject.layer = LayerMask.NameToLayer("Parts");
			gameObject.tag = "PART";

			m_rigidbody.isKinematic = false;
		}

		private void Save()
		{
			m_data.posX = transform.position.x;
			m_data.posY = transform.position.y;
			m_data.posZ = transform.position.z;
			m_data.rotX = transform.rotation.eulerAngles.x;
			m_data.rotY = transform.rotation.eulerAngles.y;
			m_data.rotZ = transform.rotation.eulerAngles.z;
			SaveUtil.SerializeWriteFile(m_data, m_file);
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
			get { return m_data.isBought; }
		}

		public float Price
		{
			get { return m_data.price; }
		}
	}
}
