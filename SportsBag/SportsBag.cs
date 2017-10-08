using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HutongGames.PlayMaker;
using MSCLoader;
using UnityEngine;

namespace SportsBag
{
	public class SportsBag : Mod
	{
		private bool m_isLoaded;
		private AssetBundle m_bundle;
		public override string ID { get { return "SportsBag"; } }
		public override string Name { get { return "SportsBag"; } }
		public override string Author { get { return "zamp"; } }
		public override string Version { get { return "0.3.1"; } }
		public override bool UseAssetsFolder{ get { return true; } }

		public override void Update()
		{
			if (Application.loadedLevelName == "GAME")
			{
				if (!m_isLoaded)
				{
					// load bundle
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
						GameObject.Instantiate(m_bundle.LoadAsset<GameObject>("SportsBagPrefab")).AddComponent<SportsBagBehaviour>();
						m_bundle.Unload(false);
					}

					m_isLoaded = true;
				}
			}
			else if (Application.loadedLevelName != "GAME" && m_isLoaded)
			{
				m_isLoaded = false;
			}
		}
	}
}

