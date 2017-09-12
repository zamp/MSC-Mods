using System.IO;
using MSCLoader;
using UnityEngine;

namespace MobilePhone
{
	public class MobilePhone : Mod
	{
		private bool m_isLoaded = false;
		public override string ID { get { return "MSCMobilePhone"; } }
		public override string Name { get { return "Mobile Phone"; } }
		public override string Author { get { return "zamp"; } }
		public override string Version { get { return "0.1"; } }

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
			var path = Path.Combine(ModLoader.ModsFolder, "MobilePhone");
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
				var bundle = AssetBundle.CreateFromMemoryImmediate(File.ReadAllBytes(path));

				var phone = GameObject.Instantiate(bundle.LoadAsset<GameObject>("PhonePrefab"));
				phone.AddComponent<PhoneBehaviour>();

				ModConsole.Print("Mobile Phone mod Setup!");
			}
		}
	}
}
