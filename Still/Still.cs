using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MSCLoader;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MSCStill
{
	public class Still : MonoBehaviour
	{
		[Serializable]
		public class SaveData
		{
			public float waterAmount, kiljuAmount;
			public float solutionEthanol, solutionMethanol;
			public bool isBroken;
			public bool wasLitOnce;
			public bool phoneAnswered;
		}

		public static bool stillWasLitOnce, phoneCallAnswered;
		public float waterAmount, kiljuAmount;
		public bool isOpen;
		public int logCount;

		private Animator m_animator;
		private Transform[] m_logs;

		private float m_temperature,
			m_solutionMethanol,
			m_solutionEthanol,
			m_vaporMethanol,
			m_vaporEthanol,
			m_liquidEthanol,
			m_lightFlickerTimer,
			m_liquidMethanol,
			m_pressure = 1,
			m_woodTimer;

		private bool m_isLit, m_halt;

		private Collider m_hatTrigger, m_logTrigger;
		private readonly List<Light> m_lights = new List<Light>();
		private AudioClip[] m_addWoodClips;

		private AudioSource m_addWoodAudioSource,
			m_gonnaBlowAudioSource,
			m_fireAudioSource,
			m_explodeAudioClip,
			m_releasePressureAudioSource;

		private ParticleSystem m_smallFire,
			m_bigFire,
			m_overPressure,
			m_dripLeft,
			m_dripRight,
			m_explodeParticleSystem,
			m_releasePressure;
		private Bottle m_bottle;
		private float m_liquidWater;
		private float m_vaporWater;
		private Collider m_kiljuTrigger;

		private void Awake()
		{
			transform.FindChild("Working").gameObject.SetActive(true);
			transform.FindChild("Broken").gameObject.SetActive(false);

			m_animator = transform.FindChild("Working").GetComponent<Animator>();
			m_logs = new Transform[5];
			m_logs[0] = transform.FindChild("Working/Logs/log1");
			m_logs[1] = transform.FindChild("Working/Logs/log2");
			m_logs[2] = transform.FindChild("Working/Logs/log3");
			m_logs[3] = transform.FindChild("Working/Logs/log4");
			m_logs[4] = transform.FindChild("Working/Logs/log5");

			transform.FindChild("Working/LogTrigger").gameObject.AddComponent<TriggerCallback>().onTriggerEnter += OnLogTrigger;
			transform.FindChild("Working/KiljuTrigger").gameObject.AddComponent<TriggerCallback>().onTriggerEnter += OnKiljuTrigger;
			transform.FindChild("Working/WaterTrigger").gameObject.AddComponent<TriggerCallback>().onTriggerEnter += OnWaterTrigger;
			
			var dripTriggers = transform.FindChild("Working/DripTrigger").gameObject.AddComponent<TriggerCallback>();
			dripTriggers.onTriggerEnter += OnDripTriggerEnter;
			dripTriggers.onTriggerExit += OnDripTriggerExit;
			m_hatTrigger = transform.FindChild("Working/OpenHinge/HatTrigger").GetComponent<Collider>();
			m_logTrigger = transform.FindChild("Working/LogTrigger").GetComponent<Collider>();
			m_kiljuTrigger = transform.FindChild("Working/KiljuTrigger").GetComponent<Collider>();

			m_addWoodAudioSource = gameObject.AddComponent<AudioSource>();
			m_addWoodClips = new []
			{
				GameObject.Find("COTTAGE/Fireplace/WoodTrigger/Sound1").GetComponent<AudioSource>().clip,
				GameObject.Find("COTTAGE/Fireplace/WoodTrigger/Sound2").GetComponent<AudioSource>().clip,
				GameObject.Find("COTTAGE/Fireplace/WoodTrigger/Sound3").GetComponent<AudioSource>().clip
			};

			var light = transform.FindChild("Working/Light").GetComponent<Light>();
			m_lights.Add(light);
			foreach (Transform t in transform.FindChild("Working/RingLights"))
			{
				m_lights.Add(t.GetComponent<Light>());
			}

			m_gonnaBlowAudioSource = transform.FindChild("Working/GonnaBlow").GetComponent<AudioSource>();
			m_fireAudioSource = transform.FindChild("Working/FireSound").GetComponent<AudioSource>();
			m_releasePressureAudioSource = transform.FindChild("Working/ReleasePressure").GetComponent<AudioSource>();
			m_explodeAudioClip = transform.FindChild("Explode").GetComponent<AudioSource>();
			m_explodeParticleSystem = transform.FindChild("Explode").GetComponent<ParticleSystem>();
			m_explodeParticleSystem.enableEmission = false;

			m_smallFire = transform.FindChild("Working/SmallFire").GetComponent<ParticleSystem>();
			m_bigFire = transform.FindChild("Working/BigFire").GetComponent<ParticleSystem>();

			m_dripLeft = transform.FindChild("Working/OpenHinge/Drip/DripLeft").GetComponent<ParticleSystem>();
			m_dripRight = transform.FindChild("Working/OpenHinge/Drip/DripRight").GetComponent<ParticleSystem>();
			m_overPressure = transform.FindChild("Working/OpenHinge/Drip/Overpressure").GetComponent<ParticleSystem>();
			m_releasePressure = transform.FindChild("Working/ReleasePressure").GetComponent<ParticleSystem>();
			m_releasePressure.enableEmission = false;
			m_overPressure.enableEmission = false;

			m_woodTimer = Random.Range(60f, 120f);

			Load();

			GameHook.InjectStateHook(GameObject.Find("ITEMS"), "Save game", Save);
		}

		/*void OnGUI()
		{
			GUILayout.BeginVertical();
			GUILayout.Label("Temperature: " + m_temperature);
			GUILayout.Label("Kilju: " + kiljuAmount);
			GUILayout.Label("Water: " + waterAmount);
			GUILayout.Label("Pressure: " + m_pressure);
			GUILayout.Label("Methanol: " + m_solutionMethanol + "/" + m_vaporMethanol + "/" + m_liquidMethanol);
			GUILayout.Label("Ethanol: " + m_solutionEthanol + "/" + m_vaporEthanol + "/" + m_liquidEthanol);
			GUILayout.EndVertical();
		}*/

		private void Update()
		{
			if (m_halt)
				return;
			try
			{
				//DebugKeys();
				m_animator.Play("BoilerWater", 0, waterAmount / 50.5f);
				m_animator.Play("BoilerKilju", 1, kiljuAmount / 30.5f);
				m_animator.SetBool("isOpen", isOpen);

				Interact();

				for (var i = 0; i < 5; ++i)
				{
					m_logs[i].gameObject.SetActive(logCount > i);
				}

				foreach (var light in m_lights)
				{
					light.enabled = m_isLit;
				}
				m_smallFire.gameObject.SetActive(m_isLit);
				m_bigFire.gameObject.SetActive(m_isLit && logCount >= 5);

				if (m_isLit)
				{
					stillWasLitOnce = true;
					BurnWood();
					Boil();
					Condense();
					FlickerLights();
				}
				else
				{
					CoolOff();
				}
			}
			catch (Exception e)
			{
				ModConsole.Error(e.ToString());
				m_halt = true;
			}
		}

		private void Save()
		{
			var data = new SaveData
			{
				kiljuAmount = kiljuAmount,
				waterAmount = waterAmount,
				solutionEthanol = m_solutionEthanol,
				solutionMethanol = m_solutionMethanol,
				isBroken = transform.FindChild("Broken").gameObject.activeSelf,
				wasLitOnce = stillWasLitOnce,
				phoneAnswered = phoneCallAnswered
			};

			var path = Path.Combine(Path.Combine(ModLoader.ModsFolder, "MSCStill"), "still.xml");
			SaveUtil.SerializeWriteFile(data, path);
		}

		private void Load()
		{
			var path = Path.Combine(Path.Combine(ModLoader.ModsFolder, "MSCStill"), "still.xml");
			if (!File.Exists(path))
				return;

			var data = SaveUtil.DeserializeReadFile<SaveData>(path);
			waterAmount = data.waterAmount;
			kiljuAmount = data.kiljuAmount;
			m_solutionEthanol = data.solutionEthanol;
			m_solutionMethanol = data.solutionMethanol;
			stillWasLitOnce = data.wasLitOnce;
			phoneCallAnswered = data.phoneAnswered;

			if (data.isBroken)
			{
				transform.FindChild("Working").gameObject.SetActive(false);
				transform.FindChild("Broken").gameObject.SetActive(true);
			}
		}

		private void FlickerLights()
		{
			if (m_lightFlickerTimer < 0)
			{
				m_lightFlickerTimer = 0.05f;
				foreach (var light in m_lights)
				{
					light.intensity = Random.Range(3f, 5f);
				}
			}
		}

		private void CoolOff()
		{
			m_gonnaBlowAudioSource.volume = 0f;
			m_temperature -= Time.deltaTime / 300f;
			if (m_temperature < 20f)
				m_temperature = 20f;
		}

		private void DebugKeys()
		{
			if (Input.GetKeyDown(KeyCode.KeypadPlus))
				AddWood();
			if (Input.GetKeyDown(KeyCode.KeypadMinus))
			{
				AddKilju(1f, 0.22f);
				AddWater(1f);
			}
			if (Input.GetKeyDown(KeyCode.Keypad0))
			{
				m_temperature = 55f;
			}
		}

		private void BurnWood()
		{
			m_woodTimer -= Time.deltaTime;
			if (m_woodTimer < 0)
			{
				m_woodTimer = Random.Range(60f, 120f);
				logCount--;
				if (logCount <= 0)
				{
					m_isLit = false;
					logCount = 0;
				}
			}

			m_temperature += (Time.deltaTime * (logCount * logCount)) / 8f; // five wood adds 0.23 C per second
			m_temperature -= Time.deltaTime / 6f; // loses heat 1 celsius per 10 seconds

			if (isOpen && m_temperature > 100 && kiljuAmount > 0)
			{
				// can't go over 100 if there's something left to boil
				m_temperature = 100;
			}
		}

		private void Condense()
		{
			var gains = (Time.deltaTime * (waterAmount / 50f)) / 200f;

			m_dripLeft.enableEmission = false;
			m_dripRight.enableEmission = false;

			if (m_vaporMethanol > 0)
			{
				m_liquidMethanol += gains;
				m_vaporMethanol -= gains;
				waterAmount -= gains;
				if (m_vaporMethanol < 0)
					m_vaporMethanol = 0;

				m_dripLeft.enableEmission = true;
				m_dripRight.enableEmission = true;
			}
			if (m_vaporEthanol > 0)
			{
				m_liquidEthanol += gains;
				m_vaporEthanol -= gains;
				waterAmount -= gains;
				if (m_vaporEthanol < 0)
					m_vaporEthanol = 0;

				m_dripLeft.enableEmission = true;
				m_dripRight.enableEmission = true;
			}
			if (m_vaporWater > 0)
			{
				m_liquidWater += gains;
				m_vaporWater -= gains;
				waterAmount -= gains;
				if (m_liquidWater < 0)
					m_liquidWater = 0;

				m_dripLeft.enableEmission = true;
				m_dripRight.enableEmission = true;
			}

			if (m_bottle != null && m_bottle.total < 6f)
			{
				m_bottle.water += m_liquidWater;
				m_bottle.ethanol += m_liquidEthanol;
				m_bottle.methanol += m_liquidMethanol;
			}
			m_vaporMethanol = 0;
			m_vaporEthanol = 0;
			m_vaporWater = 0;

			m_liquidEthanol = 0;
			m_liquidMethanol = 0;
			m_liquidWater = 0;
		}

		private void Boil()
		{
			if (isOpen)
			{
				m_pressure = 1;
			}

			m_overPressure.enableEmission = m_pressure > 1f;
			m_overPressure.emissionRate = 8f * m_pressure;
			m_overPressure.startSpeed = m_pressure / 2f;

			if (m_pressure > 3)
			{
				Explode();
			}

			var diff = (Time.deltaTime * (m_temperature / 75f)) / 200f;

			// this math should leave bottle at ~60% alcohol
			if (kiljuAmount > 0 && m_temperature >= 101f)
			{
				m_pressure += diff * 3f;
			}
			else
			{
				m_pressure -= diff;
				if (m_pressure < 1)
					m_pressure = 1;
			}

			if (m_temperature > 65f && m_solutionMethanol > 0f)
			{
				m_solutionMethanol -= diff;
				kiljuAmount -= diff;
				if (!isOpen)
				{
					m_vaporMethanol += diff;
					m_vaporWater += diff;
				}
				// on low medium heat keep temperature static
				if (logCount <= 3 && m_temperature > 65f)
					m_temperature += (65f - m_temperature) * Time.deltaTime;
			}

			if (m_temperature > 78.4f && m_solutionEthanol > 0f)
			{
				m_solutionEthanol -= diff;
				kiljuAmount -= diff;
				if (!isOpen)
				{
					m_vaporEthanol += diff;
					m_vaporWater += diff;
				}
				// on low medium heat keep temperature static
				if (logCount <= 3 && m_temperature > 78.4f)
					m_temperature += (78.4f - m_temperature) * Time.deltaTime;
			}

			if (m_temperature > 100f && kiljuAmount > 0)
			{
				kiljuAmount -= Time.deltaTime / 60f;
				m_vaporWater += Time.deltaTime / 60f;
				// on low medium heat keep temperature static
				if (logCount <= 3)
					m_temperature += (100f - m_temperature) * Time.deltaTime;
			}

			var basevol = 0f;
			if (m_vaporEthanol > 0 || m_vaporMethanol > 0 || m_vaporWater > 0)
				basevol = 0.01f;
			m_gonnaBlowAudioSource.volume = basevol + (m_pressure - 1f);
			m_gonnaBlowAudioSource.pitch = 1f + ((m_pressure - 1f) * 0.3f);
		}

		private void Explode()
		{
			m_pressure = 1f;
			m_isLit = false;
			logCount = 0;
			m_temperature = 20f;
			m_explodeAudioClip.Play();
			m_explodeParticleSystem.Emit(500);
			kiljuAmount = 0;
			m_gonnaBlowAudioSource.Stop();

			var things = Physics.OverlapSphere(transform.position, 15f);
			foreach (var collider in things)
			{
				if (collider.attachedRigidbody != null)
				{
					collider.attachedRigidbody.AddExplosionForce(1500f, transform.position, 15f, 1f);
				}
			}

			transform.FindChild("Working").gameObject.SetActive(false);
			transform.FindChild("Broken").gameObject.SetActive(true);

			var player = GameObject.Find("PLAYER");
			if (Vector3.Distance(player.transform.position, transform.position) < 5f)
			{
				PlayMakerFSM.BroadcastEvent("DEATH");
			}
		}

		private void StartFire()
		{
			m_isLit = true;
			m_fireAudioSource.Play();
			m_gonnaBlowAudioSource.Play();
			m_gonnaBlowAudioSource.volume = 0f;
		}

		private void Interact()
		{
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			var hits = Physics.RaycastAll(ray, 2f);
			foreach (var raycastHit in hits)
			{
				if (raycastHit.collider == m_kiljuTrigger && isOpen && kiljuAmount > 0)
				{
					PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse").Value = true;
					PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction").Value = "Empty out";

					if (cInput.GetButtonDown("Use"))
					{
						kiljuAmount = 0;
					}
				}
				if (raycastHit.collider == m_hatTrigger)
				{
					var str = isOpen ? "Close lid" : "Open lid";
					PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse").Value = true;
					PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction").Value = str;

					if (Input.GetMouseButtonDown(0))
					{
						if (!isOpen)
							Open();
						else
							Close();
					}
				} else if (raycastHit.collider == m_logTrigger)
				{
					if (logCount > 0 && !m_isLit)
					{
						PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction").Value = "Start fire";
						PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse").Value = true;
						if (cInput.GetButtonDown("Use"))
						{
							StartFire();
						}
					}
				}
			}
		}

		private void Close()
		{
			isOpen = false;
		}

		private void Open()
		{
			isOpen = true;
			if (m_pressure > 1.5f)
			{
				m_releasePressureAudioSource.Play();
				kiljuAmount /= m_pressure;
				m_solutionEthanol /= m_pressure;
				m_solutionMethanol /= m_pressure;
				m_temperature -= 100f;
				m_releasePressure.enableEmission = true;
				Invoke("ShutoffReleasePressure", 1f);
			}
		}

		private void OnWaterTrigger(Collider obj)
		{
			if (obj.name.StartsWith("water bucket"))
			{
				var fsm = obj.transform.FindChild("Water").GetComponent<PlayMakerFSM>();
				var amount = fsm.FsmVariables.GetFsmFloat("Water").Value;
				if (AddWater(amount))
				{
					fsm.FsmVariables.GetFsmFloat("Water").Value = 0;
				}
			}
		}

		private void OnKiljuTrigger(Collider obj)
		{
			if (obj.name.StartsWith("water bucket"))
			{
				var fsm = obj.transform.FindChild("Water").GetComponent<PlayMakerFSM>();
				var amount = fsm.FsmVariables.GetFsmFloat("Water").Value;
				if (amount > 0 && AddKilju(amount, 0))
				{
					fsm.FsmVariables.GetFsmFloat("Water").Value = 0;
					transform.FindChild("Working/KiljuTrigger").GetComponent<AudioSource>().Play();
				}
			}

			if (obj.name.StartsWith("kilju"))
			{
				var fsm = obj.GetComponent<PlayMakerFSM>();
				var fsmBool = fsm.FsmVariables.GetFsmBool("ContainsKilju");
				if (fsmBool != null && fsmBool.Value)
				{
					var alc = fsm.FsmVariables.GetFsmFloat("KiljuAlc").Value;

					if (AddKilju(2, alc))
					{
						fsm.FsmVariables.GetFsmBool("ContainsKilju").Value = false;
						fsm.gameObject.name = "empty plastic can(itemx)";
						transform.FindChild("Working/KiljuTrigger").GetComponent<AudioSource>().Play();
					}
				}
			}
		}

		private void OnLogTrigger(Collider obj)
		{
			if (obj.tag == "PART" && obj.name.StartsWith("firewood") && logCount < 5)
			{
				Destroy(obj.gameObject);
				AddWood();
			}
		}

		private void AddWood()
		{
			if (logCount < 5)
			{
				logCount++;
				m_addWoodAudioSource.clip = m_addWoodClips[Random.Range(0, m_addWoodClips.Count())];
				m_addWoodAudioSource.Play();
			}
		}

		private bool AddKilju(float kilju, float vol)
		{
			ModConsole.Print("add kilju with " + vol);
			if (kiljuAmount + kilju <= 30f)
			{
				m_solutionMethanol += 0.02f * kilju;
				m_solutionEthanol += vol * kilju;
				kiljuAmount += kilju;
				return true;
			}
			return false;
		}

		private bool AddWater(float amount)
		{
			if (waterAmount + amount <= 50f)
			{
				waterAmount += amount;
				return true;
			}
			return false;
		}


		private void OnDripTriggerExit(Collider obj)
		{
			var bottle = obj.GetComponent<Bottle>();
			if (bottle != null)
			{
				m_bottle = null;
				bottle.ShowFunnel(false);
				bottle.ShowPlug(true);
			}
		}

		private void OnDripTriggerEnter(Collider obj)
		{
			var bottle = obj.GetComponent<Bottle>();
			if (bottle != null)
			{
				m_bottle = bottle;
				bottle.ShowFunnel(true);
				bottle.ShowPlug(false);
			}
		}
	}
}
