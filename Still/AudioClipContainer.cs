using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MSCStill
{
	public class AudioClipContainer
	{
		private readonly Dictionary<string, string> m_subtitles = new Dictionary<string, string>
		{
			{"AskBring1", "Did you bring that moonshine?"},
			{"AskBring2", "Did you bring that moonshine?"},
			{"AskBring3", "Did you bring that moonshine?"},
			{"AskBring4", "Did you bring that moonshine?"},
			{"BottleReturn1", "I'll return this bottle to your home."},
			{"BottleReturn2", "When it's empty I'll bring this bottle to you."},
			{"BottleReturn3", "I'll bring this bottle to you at some point."},
			{"Good1", "God help me! This is good!"},
			{"Good2", "You're a fucking chef! This is fucking great!"},
			{"Good3", "God help me! This is tasty!"},
			{"Greet1", "Don't piss me off boy."},
			{"Greet2", "Don't piss me off boy."},
			{"Greet3", "No wonder I smell like shit when I stay here all the time."},
			{"Greet4", "I haven't had time to shower."},
			{"Methanol1", "Perkele that's awful!"},
			{"Methanol2", "Saatana! What the fuck?"},
			{"NoBuyMethanol1", "I can't buy this window cleaner. Make some new and we'll see again."},
			{"NoBuyMethanol2", "I can't buy this. This will kill me. Go make new and we'll see again."},
			{"NoBuyMethanol3", "I can't buy this. God damn. Go make new and we'll see again."},
			{"NoBuyWater1", "What you brought me bottled water? I can get that from tap. I won't buy this. Make new."},
			{"NoBuyWater2", "I won't buy water. Make some new and bring me, you know, a new bottle."},
			{"NoBuyWater3", "I won't buy water, hey. Bring me a new bottle."},
			{"NoBuyWater4", "I get the same shit from tap that's in the bottle. I won't buy it. Bring me new."},
			{"Payment1", "Here, lemme get some. Take that."},
			{"Payment2", "Here, lemme get some. Take that."},
			{"Payment3", "Here, take for the effort."},
			{"Payment4", "Wait a sec, I'll get some, here. That's for the effort."},
			{"PhoneCall", ""},
			{"Story1", "My job is soooo damn easy. All I do is sit in a room and push some buttons. Sometimes you need to clean a little and these coveralls...\nIt ain't human shit. I dunno what it is. Haven't cleaned them in a year, but I still do wear them every day."},
			{"Story2", "You're not going to believe this, couple days ago a coworker went to the toilet and I redirected waste into the toilet network at 30 bar pressure!\nThe shit flew out of the toilet so fast it reached the ceiling! Holy shit the guy was pissed off! He was so pissed off!"},
			{"TasteMoonshine1", "Ah! You brought moonshine. Let's taste!"},
			{"TasteMoonshine2", "Ah! Almost full! Let's taste!"},
			{"TasteMoonshine3", "Well then! Let's taste!"},
			{"TasteMoonshine4", "Wait, let me taste!"},
			{"Thanks1", "I'll get back to work. Thanks for bringin me moonshine. The rest of the day will go smoothly, slick, without effort."},
			{"Thanks2", "Good that you brought me some. I -- will just -- I'll see you around."},
			{"Thanks3", "I'll see you around."},
			{"Water1", "No, perkele. This is water!"},
			{"Water2", "No, saatana. This is just water!"},
			{"Water3", "What the hell? This is just water!"},
		};

		private List<AudioClip> m_clips = new List<AudioClip>();
		public void AddClip(AudioClip clip)
		{
			m_clips.Add(clip);
		}

		public void PlayClipThrough(AudioSource source)
		{
			source.clip = m_clips[Random.Range(0, m_clips.Count)];
			source.Play();

			ShowSubtitle(source.clip.name);
		}

		private void ShowSubtitle(string p)
		{
			PlayMakerGlobals.Instance.Variables.FindFsmString("GUIsubtitle").Value = m_subtitles[p];
		}
	}
}
