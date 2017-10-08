using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MSCLoader;
using UnityEngine;

namespace RadioSubtitles
{
	public class RadioSubtitles : Mod
	{
		private bool m_isLoaded;
		private AudioSource m_radioChannel1Source, m_radioFolkSource;
		private TextMesh m_subtitlesText;
		private string m_subtitlesFileOpen;
		private List<Subtitle> m_subtitles = new List<Subtitle>();
		private GameObject m_player;
		private List<string> m_missing;
		private string m_lastSubtitles;

		public override string ID { get { return "RadioSubtitles"; } }
		public override string Name { get { return "RadioSubtitles"; } }
		public override string Author { get { return "zamp"; } }
		public override string Version { get { return "0.1.6"; } }
		public override bool UseAssetsFolder { get { return true; }}

		private class ChangeRadioClip : ConsoleCommand
		{
			private RadioSubtitles m_mod;
			public ChangeRadioClip(RadioSubtitles mod)
			{
				m_mod = mod;
			}
			public override string Help { get { return "Usage: changeradioclip clipname"; } }
			public override string Name { get { return "changeradioclip"; } }
			public override void Run(string[] args)
			{
				m_mod.PlayClip(args[0]);
			}
		}

		private class Subtitle
		{
			public float start, end;
			public string text;
		}

		//Called when mod is loading
		public override void OnLoad()
		{
			ConsoleCommand.Add(new ChangeRadioClip(this));
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
						var path = Path.Combine(ModLoader.GetModAssetsFolder(this), "missing.txt");
						m_missing = new List<string>();
						if (File.Exists(path))
							m_missing = File.ReadAllLines(path).ToList();

						if (GameObject.Find("RadioChannels/Channel1") == null)
							return;
						if (GameObject.Find("GUI/Indicators/Subtitles") == null)
							return;
						if (GameObject.Find("PLAYER") == null)
							return;

						m_radioChannel1Source = GameObject.Find("RadioChannels/Channel1").GetComponent<AudioSource>();
						m_radioFolkSource = GameObject.Find("RadioChannels/Folk").GetComponent<AudioSource>();
						m_subtitlesText = GameObject.Find("GUI/Indicators/Subtitles").GetComponent<TextMesh>();
						m_subtitlesText.richText = true; // enable rich text because why would you not have this enabled?
						m_player = GameObject.Find("PLAYER");

						m_isLoaded = true;
					}
				}
				else if (Application.loadedLevelName != "GAME" && m_isLoaded)
				{
					m_isLoaded = false;
					m_radioChannel1Source = null;
					m_radioFolkSource = null;
					m_player = null;
					m_subtitlesText = null;
				}

				if (m_isLoaded)
				{
					UpdateRadioSubtitles();
				}
			}
			catch (Exception e)
			{
				ModConsole.Error(e.ToString());
			}
		}

		private void UpdateRadioSubtitles()
		{
			// only show subtitles when playing
			AudioSource source = null;

			if (m_radioChannel1Source.isPlaying && m_radioChannel1Source.volume > 0 && !m_radioChannel1Source.mute)
			{
				source = m_radioChannel1Source;
			}
			if (m_radioFolkSource.isPlaying && m_radioFolkSource.volume > 0 && !m_radioFolkSource.mute)
			{
				source = m_radioFolkSource;
			}

			if (source == null)
			{
				ClearSubtitles();
				return;
			}

			var distance = Vector3.Distance(m_player.transform.position, source.transform.position);
			if (distance > (5 + source.volume * 10f))
			{
				ClearSubtitles();
				return;
			}

			// load subtitles
			var clip = source.clip.name;
			if (m_subtitlesFileOpen != clip)
			{
				var path = Path.Combine(ModLoader.GetModAssetsFolder(this), clip + ".encore.txt");
				if (File.Exists(path))
				{
					m_subtitlesFileOpen = clip;
					var lines = File.ReadAllLines(path);
					m_subtitles = new List<Subtitle>();
					foreach (var line in lines)
					{
						var sub = new Subtitle();
						var split = line.Split(' ');
						sub.start = ConvertTime(split[1]);
						sub.end = ConvertTime(split[2]);
						var str = "";
						for (var i = 3; i < split.Length; ++i)
						{
							str += split[i] + " ";
						}
						sub.text = str;
						m_subtitles.Add(sub);
					}
				}
				else
				{
					if (!m_missing.Contains(clip) && source == m_radioChannel1Source)
					{
						m_missing.Add(clip);
						File.WriteAllLines(Path.Combine(ModLoader.GetModAssetsFolder(this), "missing.txt"), m_missing.ToArray());
					}
				}
			}
			else
			{
				// correct subtitle is open
				// has to be at least 6 meters from radio to get subtitles
				if (m_subtitles.Any())
				{
					var sub = m_subtitles[0];
					if (source.time > sub.start && (m_subtitlesText.text == m_lastSubtitles || m_subtitlesText.text == ""))
					{
						m_subtitlesText.text = sub.text;
						m_lastSubtitles = sub.text;
					}
					if (source.time > sub.end)
					{
						ClearSubtitles();
						m_subtitles.RemoveAt(0);
					}
				}
			}
		}

		private void ClearSubtitles()
		{
			if (m_subtitlesText.text == m_lastSubtitles)
				m_subtitlesText.text = "";
		}

		private float ConvertTime(string time)
		{
			// convert 00:01:01:16 to seconds.decimals
			var bits = time.Split(':');
			var seconds = (float)Convert.ToInt32(bits[0]) * 60 * 60;
			seconds += Convert.ToInt32(bits[1]) * 60;
			seconds += Convert.ToInt32(bits[2]);
			seconds += Convert.ToInt32(bits[3]) / 60f;
			return seconds;
		}

		internal void PlayClip(string clipname)
		{
			try
			{
				if (m_radioChannel1Source == null)
					return;

				var res = Resources.FindObjectsOfTypeAll<AudioClip>();
				foreach (var audioClip in res)
				{
					if (audioClip != null && audioClip.name == clipname)
					{
						m_radioChannel1Source.clip = audioClip;
						m_radioChannel1Source.Play();
						return;
					}
				}
				ModConsole.Print("no such clip");
			}
			catch (Exception e)
			{
				ModConsole.Error(e.ToString());
			}
		}
	}
}
