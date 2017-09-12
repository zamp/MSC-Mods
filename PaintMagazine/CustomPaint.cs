using MSCLoader;
using UnityEngine;

namespace MSCPaintMagazine
{
	public class MSCPaintMagazine : Mod
	{
		private bool m_isLoaded = false;
		public override string ID { get { return "MSC_CustomPaint"; } }
		public override string Name { get { return "Custom Paint"; } }
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
