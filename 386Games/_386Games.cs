using MSCLoader;
using UnityEngine;

namespace _386Games
{
	public class _386Games : Mod
	{
		private bool m_isLoaded;
		public override string ID { get { return "386Games"; } }
		public override string Name { get { return "386Games"; } }
		public override string Author { get { return "zamp"; } }
		public override string Version { get { return "1.0"; } }

		public static Keybind nextGameKey;
		public static Keybind prevGameKey;

		public override void OnLoad()
		{
			nextGameKey = new Keybind("nextGame", "Next Game", KeyCode.KeypadPlus);
			prevGameKey = new Keybind("prevGame", "Previous Game", KeyCode.KeypadMinus);

			Keybind.Add(this, nextGameKey);
			Keybind.Add(this, prevGameKey);

			ModConsole.Print("386 Games Mod Loaded!");
		}

		public override void Update()
		{
			if (Application.loadedLevelName == "GAME")
			{
				if (GameObject.Find("PLAYER") == null)
					return;

				if (!m_isLoaded)
				{
					new GameObject("GameEnabler").AddComponent<GameEnabler>();
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
