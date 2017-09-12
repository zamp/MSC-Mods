using System;
using System.Collections;
using System.IO;
using MSCLoader;
using UnityEngine;

//Standard unity MonoBehaviour class
namespace MSCStill
{
	public class ModBehaviour : MonoBehaviour
	{
		private static string ModPath;
		private AssetBundle m_bundle;
		private GameObject m_stillPrefab;
		private GameObject m_bottlePrefab;

		void Awake()
		{
			try
			{
				ModPath = Path.Combine(ModLoader.ModsFolder, "MSCStill");
				SetupMod();
			}
			catch (Exception e)
			{
				ModConsole.Error(e.ToString());
				throw;
			}
		}

		void OnDestroy()
		{
			m_bundle.Unload(true);
		}

		private void SetupMod()
		{
			ModConsole.Print("Still mod loading assetbundle...");
			var path = "";
			if (SystemInfo.graphicsDeviceVersion.StartsWith("OpenGL") && Application.platform == RuntimePlatform.WindowsPlayer)
				path = Path.Combine(ModPath, "bundle-linux"); // apparently fixes opengl
			else if (Application.platform == RuntimePlatform.WindowsPlayer)
				path = Path.Combine(ModPath, "bundle-windows");
			else if (Application.platform == RuntimePlatform.OSXPlayer)
				path = Path.Combine(ModPath, "bundle-osx");
			else if (Application.platform == RuntimePlatform.LinuxPlayer)
				path = Path.Combine(ModPath, "bundle-linux");

			if (!File.Exists(path))
			{
				ModConsole.Error("Couldn't find asset bundle from path " + path);
			}
			else
			{
				m_bundle = AssetBundle.CreateFromMemoryImmediate(File.ReadAllBytes(path));
				LoadAssets();
				SetupStill();
				SetupBottle();
				ModConsole.Print("Still mod Setup!");
			}
		}

		private void SetupBottle()
		{
			ModConsole.Print("Setting up bottle...");
			var bottle = (GameObject)Instantiate(m_bottlePrefab, new Vector3(-839, -2f, 505), Quaternion.identity);
			bottle.AddComponent<Bottle>();
		}

		private void LoadAssets()
		{
			m_stillPrefab = m_bundle.LoadAssetWithSubAssets<GameObject>("StillPrefab")[0];
			m_bottlePrefab = m_bundle.LoadAssetWithSubAssets<GameObject>("BottlePrefab")[0];
		}

		private void SetupStill()
		{
			ModConsole.Print("Setting up still...");
			var still = Instantiate(m_stillPrefab).AddComponent<Still>();
			still.transform.position = new Vector3(-839, -3.15f, 504); // TODO: set the real position
		}
	}
}
