using System;
using System.Collections.Generic;
using System.IO;
using MSCLoader;
using UnityEngine;

//Standard unity MonoBehaviour class
namespace _386Games
{
	public class GameEnabler : MonoBehaviour
	{
		private string[] m_software =
		{
			"DOS",
			"CONLINE",
			"Rami-Sormileikki",
			"Rami-JFK",
			"Rami-Filosofiapeli",
			"Rami-PasiInvaders",
			"Rami-CayRharles",
			"Kaappis-Grilli",
			"Kaappis-Fishgame",
			"Kaappis-WildVest"
		};
		private GameObject m_computer;
		private int m_softwareIndex = 0;

		// Use this for initialization
		void Start()
		{
			foreach (var transform in Resources.FindObjectsOfTypeAll<Transform>())
			{
				if (transform.name == "386_GAMES")
				{
					m_computer = Instantiate(transform.gameObject);
					m_computer.GetComponent<PlayMakerFSM>().enabled = false;
					m_computer.SetActive(true);

					//DumpGameObject(transform.gameObject);
				}
			}
		}

		void Update()
		{
			try
			{
				if (_386Games.nextGameKey.IsDown())
				{
					m_softwareIndex++;
					if (m_softwareIndex >= m_software.Length)
						m_softwareIndex = 0;

					BorkSounds();

					for (var i = 0; i < m_software.Length; ++i)
					{
						m_computer.transform.FindChild("SYSTEM/" + m_software[i]).gameObject.SetActive(i == m_softwareIndex);
					}
				}

				if (_386Games.prevGameKey.IsDown())
				{
					m_softwareIndex--;
					if (m_softwareIndex < 0)
						m_softwareIndex = m_software.Length-1;

					BorkSounds();

					for (var i = 0; i < m_software.Length; ++i)
					{
						m_computer.transform.FindChild("SYSTEM/" + m_software[i]).gameObject.SetActive(i == m_softwareIndex);
					}
				}
			}
			catch (Exception e)
			{
				ModConsole.Print(e.ToString());
				throw;
			}
		}

		private void BorkSounds()
		{
			var sounds = m_computer.transform.FindChild("Computer/Sounds").gameObject.GetComponentsInChildren<AudioSource>();
			foreach (var audioSource in sounds)
			{
				audioSource.Stop();
			}
		}

		private void DumpGameObject(GameObject target)
		{
			var lines = "";
			Recursive(ref lines, 0, target.transform);
			File.WriteAllText(ModLoader.ModsFolder + "/dump full.txt", lines);
		}

		private void Recursive(ref string lines, int tabs, Transform transform)
		{
			var tabsStr = "";
			for (var i = 0; i < tabs; ++i)
			{
				tabsStr += "\t";
			}
			var isEnabled = transform.gameObject.activeSelf ? "(enabled)" : "(disabeld)";
			lines += tabsStr + transform.name + isEnabled + "\n";
			lines += tabsStr + "l:" + LayerMask.LayerToName(transform.gameObject.layer) +"\n";
			lines += tabsStr + transform.localPosition + "\n";
			lines += tabsStr + transform.localScale + "\n";

			foreach (var comp in transform.GetComponents<Component>())
			{
				// skip transforms
				if (comp is Transform)
					continue;
				lines += tabsStr + "\tCOMP:" + comp.GetType() + "\n";
			}

			foreach (Transform t in transform)
			{
				Recursive(ref lines, tabs + 1, t);
			}
		}
	}
}
