using System;
using System.Collections;
using System.IO;
using MSCLoader;
using UnityEngine;

namespace Magazines
{
	public class Magazines : Mod
	{
		public override string ID { get { return "Magazines"; } }
		public override string Name { get { return "Magazines"; } }
		public override string Author { get { return "zamp"; } }
		public override string Version { get { return "0.2.1"; } }
		public override bool UseAssetsFolder { get { return true; } }
		public static string assetPath;

		private bool m_isLoaded = false;
		private AssetBundle m_bundle;
		private RackBehaviour m_rack;
		private MagazineReader m_reader;
		private static Magazines m_instance;

		public override void OnLoad()
		{
			assetPath = ModLoader.GetModAssetsFolder(this);
		}

		public override void Update()
		{
			if (Application.loadedLevelName == "GAME")
			{
				if (!m_isLoaded)
				{
					if (GameObject.Find("PLAYER") == null)
						return;
					if (GameObject.Find("STORE/StoreCashRegister/Register") == null)
						return;

					m_instance = this;
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
			ModConsole.Print("Magazines mod loading assetbundle...");
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
				
				var canvas = GameObject.Instantiate(m_bundle.LoadAssetWithSubAssets<GameObject>("MagazineCanvas")[0]).GetComponent<Canvas>();
				canvas.gameObject.SetActive(false);
				m_reader = canvas.gameObject.AddComponent<MagazineReader>();

				var obj = GameObject.Instantiate(m_bundle.LoadAsset<GameObject>("MagazineRackPrefab"));
				m_rack = obj.AddComponent<RackBehaviour>();

				m_bundle.Unload(false);
			}
		}

		public static IEnumerator LoadImage(string file, Action<Texture2D> callback)
		{
			ModConsole.Print("loading file " + file);
			var www = new WWW("file://" + file);
			yield return www;
			callback.Invoke(www.texture);
			www.Dispose();
		}

		public static Magazines Instance
		{
			get { return m_instance; }
		}

		public RackBehaviour Rack
		{
			get { return m_rack; }
		}

		public MagazineReader MagazineReader
		{
			get { return m_reader; }
		}
	}
}
