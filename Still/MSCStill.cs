using MSCLoader;
using UnityEngine;

namespace MSCStill
{
	public class MSCStill : Mod
	{
		private bool m_isLoaded = false;
		public override string ID { get { return "MSCStill"; } }
		public override string Name { get { return "Still"; } }
		public override string Author { get { return "zamp"; } }
		public override string Version { get { return "1.0.10"; } }

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

					new GameObject("StillMod").AddComponent<ModBehaviour>();
					FixBrokenItems();
					m_isLoaded = true;
				}
			}
			else if (Application.loadedLevelName != "GAME" && m_isLoaded)
			{
				m_isLoaded = false;
			}
		}

		private void FixBrokenItems()
		{
			var objs = GameObject.FindObjectsOfType<GameObject>();
			foreach (var obj in objs)
			{
				if (obj.name == "empty plastic bottle(itemx)")
				{
					obj.name = "empty plastic can(itemx)";
				}
			}
		}
	}
}
