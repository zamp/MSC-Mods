using System.IO;
using MSCLoader;
using UnityEngine;

namespace WoodCarrier
{
	public class WoodCarrier : Mod
	{
		private bool m_isLoaded;
		private AssetBundle m_bundle;
		public override string ID { get { return "WoodCarrier"; } }
		public override string Name { get { return "Wood Carrier"; } }
		public override string Author { get { return "zamp"; } }
		public override string Version { get { return "0.1"; } }

		//Called when mod is loading
		public override void OnLoad()
		{

		}

		public override void Update()
		{
			if (Application.loadedLevelName == "GAME")
			{
				if (!m_isLoaded)
				{
					if (GameObject.Find("PLAYER") == null)
						return;

					var path = Path.Combine(ModLoader.ModsFolder, "WoodCarrier");
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
						return;
					}

					m_bundle = AssetBundle.CreateFromMemoryImmediate(File.ReadAllBytes(path));

					var asset = m_bundle.LoadAsset<GameObject>("WoodCarrierPrefab");
					GameObject.Instantiate(asset).AddComponent<WoodCarrierBehaviour>().id = "home";
					GameObject.Instantiate(asset).AddComponent<WoodCarrierBehaviour>().id = "cottage";

					m_isLoaded = true;
				}
			}
			else if (Application.loadedLevelName != "GAME" && m_isLoaded)
			{
				m_bundle.Unload(true);
				m_isLoaded = false;
			}
		}
	}
}
