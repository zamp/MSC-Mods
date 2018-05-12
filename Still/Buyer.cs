using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MSCStill
{
	public class Buyer : MonoBehaviour
	{
		private class CustomPhoneCallAction : FsmStateAction
		{
			public override void OnEnter()
			{
				// this action will force the phone to hang in the "choose playback" state
				var fsm = GameObject.Find("YARD/Building/LIVINGROOM/Telephone/Logic")
					   .transform.FindChild("Ring").GetComponent<PlayMakerFSM>();
				if (fsm.FsmVariables.FindFsmString("Topic").Value != "CUSTOM")
					Finish();
			}
		}
		
		private GameObject m_money, m_bottle;
		private Collider m_moneyTakeTrigger;
		private bool m_hasGreetedPlayerOnce;
		private AudioSource m_audioSource;
		private Animator m_animator;
		private FsmString m_guiInteraction;
		private GameObject m_player;
		private bool m_waitForBottleLeaveTrigger;
		private FsmBool m_guiUse;
		private FsmBool m_answeredBool;
		private FsmString m_topic;
		private PlayMakerFSM m_ringFsm;
		private AudioSource m_phoneAudioSource;
		private float m_ringTimer;
		private float m_ringDelay;
		private float m_greetTimer = 180f;
		private Transform m_headBone;
		private bool m_lookAtPlayer;

		void Awake()
		{
			m_audioSource = GetComponent<AudioSource>();
			m_animator = GetComponent<Animator>();

			m_money =
				transform.FindChild(
					"Pivot/skeleton/pelvis/spine_middle/spine_upper/collar_left/shoulder_left/arm_left/hand_left/Money").gameObject;

			m_moneyTakeTrigger = m_money.transform.FindChild("MoneyTrigger").gameObject.GetComponent<Collider>();

			m_bottle =
				transform.FindChild(
					"Pivot/skeleton/pelvis/spine_middle/spine_upper/collar_right/shoulder_right/arm_right/hand_right/Bottle").gameObject;

			m_money.SetActive(false);
			m_bottle.SetActive(false);

			var bottleTrigger = transform.FindChild("Pivot/BottleTrigger").gameObject.AddComponent<TriggerCallback>();
			bottleTrigger.onTriggerEnter += OnBottleTriggerEnter;
			bottleTrigger.onTriggerExit += OnBottleTriggerExit;

			m_player = GameObject.Find("PLAYER");

			m_guiInteraction = PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction");
			m_guiUse = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse");

			// inject our phone hook
			m_ringFsm = GameObject.Find("YARD/Building/LIVINGROOM/Telephone/Logic")
					.transform.FindChild("Ring").GetComponent<PlayMakerFSM>();
			m_answeredBool = m_ringFsm.FsmVariables.FindFsmBool("Answer");
			m_topic = m_ringFsm.FsmVariables.FindFsmString("Topic");
			m_phoneAudioSource = transform.FindChild("CallAudioSource").GetComponent<AudioSource>();

			m_ringFsm.gameObject.SetActive(true);
			var state = m_ringFsm.FsmStates.FirstOrDefault(x => x.Name == "State 2");
			var actions = new List<FsmStateAction>(state.Actions);
			actions.Insert(0, new CustomPhoneCallAction());
			state.Actions = actions.ToArray();
			m_ringFsm.gameObject.SetActive(false);

			m_headBone = transform.FindChild("Pivot/skeleton/pelvis/spine_middle/spine_upper/head");

			//m_ringDelay = Random.Range(750f, 3600f);
			m_ringDelay = 60f;
			m_lookAtPlayer = true;
		}

		void LateUpdate()
		{
			// look at camera
			if (Camera.main != null && m_lookAtPlayer)
			{
				m_headBone.LookAt(Camera.main.transform.position);
				m_headBone.Rotate(0, -90, -90);
			}
		}

		void Update()
		{
			if (m_ringDelay < 0)
			{
				RingPhone();
			}
			else
			{
				m_ringDelay -= Time.deltaTime;
			}

			var dist = Vector3.Distance(transform.position, m_player.transform.position);
			if (dist < 6f)
			{
				if (!m_hasGreetedPlayerOnce)
				{
					m_hasGreetedPlayerOnce = true;
					StartCoroutine(GreetPlayer());
				}
			}
			else
			{
				m_greetTimer -= Time.deltaTime;
				if (m_greetTimer < 0)
				{
					m_hasGreetedPlayerOnce = false;
					m_greetTimer = 180f;
				}
			}
		}

		private void RingPhone()
		{
			// should receive phone call
			if (Still.stillWasLitOnce && !Still.phoneCallAnswered)
			{
				// phone is not ringing
				if (!m_ringFsm.gameObject.activeSelf)
				{
					// start ringing
					m_ringTimer = 15f;
					m_topic.Value = "CUSTOM";
					m_answeredBool.Value = false;
					m_ringFsm.gameObject.SetActive(true);
				}
				else
				{
					// phone is ringing and answered
					if (m_answeredBool.Value)
					{
						if (!m_phoneAudioSource.isPlaying)
						{
							var sub = "Hey listen dude. I saw some light at your cottage last night, you've gotten your grampa's old still working...\nCould you bring me some moonshine? Come to the waste processing plant. I'm here every day.";
							PlayMakerGlobals.Instance.Variables.FindFsmString("GUIsubtitle").Value = sub;
							Still.phoneCallAnswered = true;
							m_phoneAudioSource.Play();
						}
					}
					else
					{
						// not answered and is still ringing
						m_ringTimer -= Time.deltaTime;
						if (m_ringTimer < 0)
						{
							m_ringDelay = Random.Range(1800f, 3600f);
							m_ringFsm.gameObject.SetActive(false);
						}
					}
				}
			}
			else if (m_phoneAudioSource.isPlaying && !m_ringFsm.gameObject.activeSelf)
			{
				m_phoneAudioSource.Stop();
			}
		}

		private IEnumerator GreetPlayer()
		{
			MasterAudio.PlaySound3DFollowTransformAndForget("Shit", m_player.transform);

			yield return new WaitForSeconds(3f);

			ModBehaviour.buyerGreet.PlayClipThrough(m_audioSource);
			yield return new WaitForSeconds(m_audioSource.clip.length);

			if (Still.phoneCallAnswered)
			{
				ModBehaviour.buyerAsk.PlayClipThrough(m_audioSource);
			}
		}

		private void OnBottleTriggerExit(Collider obj)
		{
			if (obj.GetComponent<Bottle>())
			{
				m_waitForBottleLeaveTrigger = false;
			}
		}

		private void OnBottleTriggerEnter(Collider obj)
		{
			if (m_waitForBottleLeaveTrigger)
				return;
			if (obj.GetComponent<Bottle>())
			{
				StopAllCoroutines();
				StartCoroutine(TasteTest(obj.gameObject.GetComponent<Bottle>()));
			}
		}

		private IEnumerator TasteTest(Bottle bottle)
		{
			m_bottle.SetActive(true);
			m_animator.SetBool("NoBottle", false);
			bottle.gameObject.SetActive(false);
			bottle.transform.SetParent(null, true);

			ModBehaviour.buyerTaste.PlayClipThrough(m_audioSource);
			yield return new WaitForSeconds(m_audioSource.clip.length);
			m_animator.Play("BuyerDrink");
			m_lookAtPlayer = false;
			yield return new WaitForSeconds(1.9f);
			m_lookAtPlayer = true;

			var ethanolPercentage = bottle.ethanol / bottle.total;
			var methanolPercentage = bottle.methanol / bottle.total;
			var ethanolMethanolRatio = ethanolPercentage / (methanolPercentage + 0.000001f);
			if (bottle.total < 0.01f)
			{
				// empty
				StartCoroutine(Empty(bottle));
			}
			else if (ethanolPercentage < 0.3f)
			{
				// it's water
				StartCoroutine(Water(bottle));
			}
			else if (ethanolMethanolRatio < 20)
			{
				// too much methanol
				StartCoroutine(Methanol(bottle));
			}
			else
			{
				// it's good!
				StartCoroutine(Good(bottle));
			}
		}

		private IEnumerator Empty(Bottle bottle)
		{
			PlayMakerGlobals.Instance.Variables.FindFsmString("GUIsubtitle").Value = "It's empty?";
			yield return new WaitForSeconds(1f);

			m_waitForBottleLeaveTrigger = true;
			bottle.gameObject.SetActive(true);
			m_bottle.SetActive(false);
			m_animator.SetBool("NoBottle", true);
		}

		private IEnumerator Methanol(Bottle bottle)
		{
			m_animator.SetBool("Coughing", true);
			m_animator.Play("BuyerBottleBad");
			ModBehaviour.buyerTasteMethanol.PlayClipThrough(m_audioSource);
			yield return new WaitForSeconds(m_audioSource.clip.length);
			yield return new WaitForSeconds(1f);
			m_animator.SetBool("Coughing", false);
			ModBehaviour.buyerNoBuyMethanol.PlayClipThrough(m_audioSource);
			yield return new WaitForSeconds(m_audioSource.clip.length);

			m_waitForBottleLeaveTrigger = true;
			bottle.gameObject.SetActive(true);
			m_bottle.SetActive(false);
			m_animator.SetBool("NoBottle", true);
		}

		private IEnumerator Water(Bottle bottle)
		{
			ModBehaviour.buyerTasteWater.PlayClipThrough(m_audioSource);
			yield return new WaitForSeconds(m_audioSource.clip.length);
			yield return new WaitForSeconds(1f);
			ModBehaviour.buyerNoBuyWater.PlayClipThrough(m_audioSource);
			yield return new WaitForSeconds(m_audioSource.clip.length);

			m_waitForBottleLeaveTrigger = true;
			bottle.gameObject.SetActive(true);
			m_bottle.SetActive(false);
			m_animator.SetBool("NoBottle", true);
		}

		private IEnumerator Good(Bottle bottle)
		{
			ModBehaviour.buyerTasteGood.PlayClipThrough(m_audioSource);
			yield return new WaitForSeconds(m_audioSource.clip.length);
			yield return new WaitForSeconds(1f);
			m_animator.SetBool("TookMoney", false);
			m_animator.Play("BuyerBottleBuy");
			ModBehaviour.buyerPayment.PlayClipThrough(m_audioSource);

			yield return new WaitForSeconds(0.5f);
			// show the money
			m_money.SetActive(true);

			var ethanolPercentage = bottle.ethanol / bottle.total;
			var money = Mathf.Floor(ethanolPercentage * bottle.total * 120) * 10;
			if (float.IsNaN(money))
				money = 0;

			while (true)
			{
				var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
				var hit = Physics.RaycastAll(ray, 1f).Any(x => x.collider == m_moneyTakeTrigger);
				if (hit)
				{
					m_guiInteraction.Value = "Take " + money + " mk";
					m_guiUse.Value = true;

					if (Input.GetMouseButtonDown(0))
					{
						break;
					}
				}
				yield return null;
			}

			m_money.SetActive(false);
			m_animator.SetBool("TookMoney", true);
			PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerMoney").Value += money;

			bottle.transform.position = new Vector3(-12.10657f, -0.293821f, 9.283916f);
			bottle.gameObject.SetActive(true);
			bottle.ethanol = 0;
			bottle.water = 0;
			bottle.methanol = 0;

			ModBehaviour.buyerBottleReturn.PlayClipThrough(m_audioSource);
			yield return new WaitForSeconds(m_audioSource.clip.length);

			yield return new WaitForSeconds(2f);

			if (Random.value < 0.3f)
			{
				ModBehaviour.buyerStory.PlayClipThrough(m_audioSource);
				yield return new WaitForSeconds(m_audioSource.clip.length);
				yield return new WaitForSeconds(2f);
			}

			ModBehaviour.buyerByeBye.PlayClipThrough(m_audioSource);
		}
	}
}
