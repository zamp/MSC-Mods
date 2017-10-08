using MSCLoader;
using UnityEngine;

namespace MSCPaintMagazine
{
	public class PaintMagazine : Mod
	{
		private bool m_isLoaded = false;
		public override string ID { get { return "PaintMagazine"; } }
		public override string Name { get { return "PaintMagazine"; } }
		public override string Author { get { return "zamp"; } }
		public override string Version { get { return "0.1.1"; } }
		public override bool UseAssetsFolder { get { return true; } }
		public static string assetPath;

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
					new GameObject("Skinner").AddComponent<Painter>();
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
