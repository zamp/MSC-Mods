using System;
using System.IO;
using HutongGames.PlayMaker;
using MSCLoader;
using UnityEngine;

namespace Dynamite
{
	public class Dynamite : Mod
	{
		private bool m_isLoaded;
		private AssetBundle m_bundle;
		public override string ID { get { return "Dynamite"; } }
		public override string Name { get { return "Dynamite"; } }
		public override string Author { get { return "zamp"; } }
		public override string Version { get { return "0.2.3"; } }
		public override bool UseAssetsFolder { get { return true; } }

		public override void Update()
		{
			if (Application.loadedLevelName == "GAME")
			{
				try
				{
					if (!m_isLoaded)
					{
						if (GameObject.Find("fish trap(itemx)") == null)
							return;

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
							GameObject.Instantiate(m_bundle.LoadAsset<GameObject>("ExplosivesPrefab"))
								.AddComponent<BoxBehaviour>();
							m_bundle.Unload(false);
						}

						m_isLoaded = true;
					}
				}
				catch (Exception e)
				{
					m_isLoaded = true;
					ModConsole.Error(e.ToString());
				}
			}
			else if (Application.loadedLevelName != "GAME" && m_isLoaded)
			{
				m_isLoaded = false;
			}
		}
	}
}
