using System.IO;
using MSCLoader;
using UnityEngine;

namespace MobilePhone
{
	public class MobilePhone : Mod
	{
		private bool m_isLoaded = false;
		private AssetBundle m_bundle;
		public override string ID { get { return "MobilePhone"; } }
		public override string Name { get { return "MobilePhone"; } }
		public override string Author { get { return "zamp"; } }
		public override string Version { get { return "0.2.5"; } }
		public override bool UseAssetsFolder { get { return true; }}

		public override void OnLoad()
		{

		}

		public override void Update()
		{
			if (Application.loadedLevelName == "GAME")
			{
				if (!m_isLoaded)
				{
					if (GameObject.Find("PLAYER") == null ||
						GameObject.Find("YARD/Building/Dynamics/Telephone/Logic") == null)
						return;

					SetupMod();

					m_isLoaded = true;
				}
			}
			else if (Application.loadedLevelName != "GAME" && m_isLoaded)
			{
				m_isLoaded = false;
			}
		}

		private void SetupMod()
		{
			ModConsole.Print("Mobile Phone mod loading assetbundle...");
			var path = ModLoader.GetModAssetsFolder(this);
			if (SystemInfo.graphicsDeviceVersion.StartsWith("OpenGL") && Application.platform == RuntimePlatform.WindowsPlayer)
				path = Path.Combine(path, "bundle-linux"); // apparently fixes opengl
			else if (Application.platform == RuntimePlatform.WindowsPlayer)
				path = Path.Combine(path, "bundle-windows");
			else if (Application.platform == RuntimePlatform.OSXPlayer)
				path = Path.Combine(path, "bundle-osx");
			else if (Application.platform == RuntimePlatform.LinuxPlayer)
				path = Path.Combine(path, "bundle-linux");

			if (!File.Exists(path))
			{
				ModConsole.Error("Couldn't find asset bundle from path " + path);
			}
			else
			{
				m_bundle = AssetBundle.CreateFromMemoryImmediate(File.ReadAllBytes(path));
				var prefab = m_bundle.LoadAsset<GameObject>("PhonePrefab");
				
				var headPhone = GameObject.Instantiate(prefab);
				GameObject.Destroy(headPhone.GetComponent<Collider>());
				GameObject.Destroy(headPhone.GetComponent<Rigidbody>());
				headPhone.transform.SetParent(GameObject.Find("PLAYER/Pivot/Camera/FPSCamera").transform, false);
				headPhone.transform.localPosition = new Vector3(0.2f, -0.05f, 0.1f);
				headPhone.transform.localRotation = Quaternion.Euler(new Vector3(300, 355, 0));
				headPhone.transform.localScale = new Vector3(1,1,1);
				headPhone.gameObject.SetActive(false);

				var phone = GameObject.Instantiate(prefab);
				phone.AddComponent<PhoneBehaviour>().headPhone = headPhone;

				m_bundle.Unload(false);

				ModConsole.Print("Mobile Phone mod Setup!");
			}
		}
	}
}
