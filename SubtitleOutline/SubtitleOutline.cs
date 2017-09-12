using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MSCLoader;
using UnityEngine;

namespace SubtitleOutline
{
	public class SubtitleOutline : Mod
	{
		private bool m_isLoaded;
		private TextMesh m_subtitlesText;
		private TextMesh m_topLeft, m_topRight, m_bottomLeft, m_bottomRight;

		public override string ID { get { return "SubtitleOutline"; } }
		public override string Name { get { return "SubtitleOutline"; } }
		public override string Author { get { return "zamp"; } }
		public override string Version { get { return "0.1"; } }

		//Called when mod is loading
		public override void OnLoad()
		{
			
		}

		// Update is called once per frame
		public override void Update()
		{
			try
			{
				if (Application.loadedLevelName == "GAME")
				{
					if (!m_isLoaded)
					{
						if (GameObject.Find("GUI/Indicators/Subtitles") == null)
							return;

						m_subtitlesText = GameObject.Find("GUI/Indicators/Subtitles").GetComponent<TextMesh>();

						m_topLeft = GameObject.Instantiate(m_subtitlesText);
						m_topRight = GameObject.Instantiate(m_subtitlesText);
						m_bottomLeft = GameObject.Instantiate(m_subtitlesText);
						m_bottomRight = GameObject.Instantiate(m_subtitlesText);

						const float off = 0.02f;
						const float z = 0.1f;

						m_topLeft.transform.SetParent(m_subtitlesText.transform);
						m_topLeft.transform.localPosition = new Vector3(-off, off, z);

						m_topRight.transform.SetParent(m_subtitlesText.transform);
						m_topRight.transform.localPosition = new Vector3(off, off, z);

						m_bottomLeft.transform.SetParent(m_subtitlesText.transform);
						m_bottomLeft.transform.localPosition = new Vector3(-off, -off, z);

						m_bottomRight.transform.SetParent(m_subtitlesText.transform);
						m_bottomRight.transform.localPosition = new Vector3(-off, -off, z);

						m_topLeft.color = Color.black;
						m_topRight.color = Color.black;
						m_bottomLeft.color = Color.black;
						m_bottomRight.color = Color.black;

						m_isLoaded = true;
					}
				}
				else if (Application.loadedLevelName != "GAME" && m_isLoaded)
				{
					m_isLoaded = false;
				}

				if (m_isLoaded)
				{
					UpdateSubtitles();
				}
			}
			catch (Exception e)
			{
				ModConsole.Error(e.ToString());
			}
		}

		private void UpdateSubtitles()
		{
			m_topLeft.text = m_subtitlesText.text;
			m_topRight.text = m_subtitlesText.text;
			m_bottomLeft.text = m_subtitlesText.text;
			m_bottomRight.text = m_subtitlesText.text;
		}
	}
}
