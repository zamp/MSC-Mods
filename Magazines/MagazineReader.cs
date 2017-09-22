using System;
using System.Linq;
using MSCLoader;
using UnityEngine;
using UnityEngine.UI;
using HutongGames.PlayMaker.Actions;

namespace Magazines
{
	public class MagazineReader : MonoBehaviour
	{
		private int m_index = 0;
		private string[] m_files;
		private PlayMakerFSM m_playerViewFsm;
		private RawImage m_rightImage;
		private RawImage m_leftImage;

		void Awake()
		{
			transform.FindChild("RightPage").GetComponent<Button>().onClick.AddListener(NextPage);
			transform.FindChild("LeftPage").GetComponent<Button>().onClick.AddListener(PrevPage);
			m_leftImage = transform.FindChild("LeftPage").GetComponent<RawImage>();
			m_rightImage = transform.FindChild("RightPage").GetComponent<RawImage>();
		}

		void Update()
		{
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				gameObject.SetActive(false);
			}
		}

		public void Show(string[] files)
		{
			try
			{
				m_index = 0;
				m_files = files;

				if (m_playerViewFsm == null)
				{
					var fsms = GameObject.Find("PLAYER").GetComponents<PlayMakerFSM>();
					foreach (var playMakerFsm in fsms)
					{
						if (playMakerFsm.FsmStates.Any(x => x.Name == "In Menu"))
						{
							m_playerViewFsm = playMakerFsm;
						}
					}
				}

				// set the whatever exit check bool to true in the fsm somewhere in there... yup.. it's very good system
				((BoolTest) m_playerViewFsm.FsmStates.First(x => x.Name == "In Menu").Actions.First(x => x is BoolTest))
					.boolVariable.Value = true;
				m_playerViewFsm.SendEvent("MENU");

				gameObject.SetActive(true);

				ShowIndex(m_index);
			}
			catch (Exception e)
			{
				ModConsole.Error(e.ToString());
			}
		}

		private void ShowIndex(int index)
		{
			m_index = index;

			m_leftImage.color = Color.clear;
			m_rightImage.color = Color.clear;

			if (m_leftImage.texture != null)
				DestroyImmediate(m_leftImage.texture);
			if (m_rightImage.texture != null)
				DestroyImmediate(m_rightImage.texture);

			ShowIndexOn(m_leftImage, m_index - 1);
			ShowIndexOn(m_rightImage, m_index);
		}

		private void ShowIndexOn(RawImage image, int index)
		{
			if (index >= 0 && index < m_files.Count())
			{
				StartCoroutine(Magazines.LoadImage(m_files[index], (d) => { image.texture = d; }));
				image.color = Color.white;
			}
			else
				image.color = Color.clear;
		}

		private void NextPage()
		{
			if (m_index < m_files.Length - 1)
				ShowIndex(m_index += 2);
		}

		private void PrevPage()
		{
			if (m_index > 0)
				ShowIndex(m_index -= 2);
		}

	}
}
