using MSCLoader;
using UnityEngine;

namespace MSCDirtMod
{
	public class MSCDirtMod : Mod
	{
		private bool m_isLoaded = false;
		public override string ID { get { return "MSCDirtMod"; } }
		public override string Name { get { return "Dirt Mod"; } }
		public override string Author { get { return "zamp"; } }
		public override string Version { get { return "1.8.4"; } }

		public static Keybind keyMoreDirt = new Keybind("moredirt", "More Dirt", KeyCode.KeypadPlus, KeyCode.LeftControl);
		public static Keybind keyLessDirt = new Keybind("lessdirt", "Less Dirt", KeyCode.KeypadMinus, KeyCode.LeftControl);

		public override void OnLoad()
		{
			Keybind.Add(this, keyMoreDirt);
			Keybind.Add(this, keyLessDirt);
		}

		public override void Update()
		{
			if (Application.loadedLevelName == "GAME")
			{
				if (!m_isLoaded)
				{
					new GameObject("DirtMod").AddComponent<ModBehaviour>();
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
