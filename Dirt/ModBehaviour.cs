using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HutongGames.PlayMaker;
using MSCLoader;
using UnityEngine;

//Standard unity MonoBehaviour class
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace MSCDirtMod
{
	public class ModBehaviour : MonoBehaviour
	{
		public class SaveData
		{
			public float bodyDirtCutoff;
			public float windowWipeAmount;
			public float wheelDirtCutoff;
		}

		private static AssetBundle m_bundle;
		private Transform m_wiperPivot;
		private float m_oldWiperAngle;
		private float m_windowWipeAmount = 0;
		private float m_bodyDirtCutoff = 1;
		private GameObject m_satsuma;
		private CarDynamics m_carDynamics;
		private float m_rainAmount;
		private float m_wiperOnAmount;
		private AudioSource m_rainAudioSource;
		private GameObject m_rightFist, m_leftFist;
		private BoxCollider m_cleanCollider;
		private AudioSource m_smackAudioSource;
		private FsmFloat m_playerDirtiness, m_playerStress;

		private readonly List<Material> m_wheelMaterials = new List<Material>();
		private readonly List<Material> m_opaqueMaterials = new List<Material>();
		private readonly List<Material> m_transparentMaterials = new List<Material>();

		private readonly Dictionary<string, Transform> m_bodyParts = new Dictionary<string, Transform>();
		private readonly Dictionary<string, Transform> m_wheels = new Dictionary<string, Transform>();
		private readonly Dictionary<string, Transform> m_rims = new Dictionary<string, Transform>();
		private readonly Dictionary<string, Transform> m_windowParts = new Dictionary<string, Transform>();

		private Texture2D m_genericDirtTexture;
		private Texture2D m_bodyDirtTexture;
		private Texture2D m_windowDirtTexture;
		private Shader m_bodyDirtShader;
		private Shader m_windowDirtShader;
		private Material m_noDrawMaterial;
		private Texture2D m_windowDirtWiperHoleTexture;
		private AudioSource m_spongeAudioSource;
		private AudioSource m_spongeOffAudioSource;
		private GameObject m_spongePrefab;
		private readonly List<AudioClip> m_easterAudioClips = new List<AudioClip>();
		private AudioSource m_easterAudioSource;
		private Texture2D m_tireDirtTextureNew2;
		private Texture2D m_tireDirtTextureRally;
		private Texture2D m_tireDirtTextureRallye;
		private Texture2D m_tireDirtTextureStandard;
		private Texture2D m_tireDirtTextureSlicks;
		private Texture2D m_rimsDirtTexture;
		private float m_wheelDirtCutoff = 1;
		private FsmBool m_handLeft;
		private FsmBool m_handRight;
		private Material m_oldBooMaterial;
		private bool m_halt = false;
		private bool m_isSetup;

		void Start()
		{
			StartCoroutine(SetupMod());
			Load();
			GameHook.InjectStateHook(GameObject.Find("ITEMS"), "Save game", Save);
		}

		private void Save()
		{
			var data = new SaveData
			{
				bodyDirtCutoff = m_bodyDirtCutoff,
				windowWipeAmount = m_windowWipeAmount,
				wheelDirtCutoff = m_wheelDirtCutoff
			};
			var path = Path.Combine(Path.Combine(ModLoader.ModsFolder, "MSCDirtMod"), "dirt.xml");
			SaveUtil.SerializeWriteFile(data, path);
		}

		private void Load()
		{
			var path = Path.Combine(Path.Combine(ModLoader.ModsFolder, "MSCDirtMod"), "dirt.xml");
			if (!File.Exists(path))
				return;

			var data = SaveUtil.DeserializeReadFile<SaveData>(path);
			m_bodyDirtCutoff = data.bodyDirtCutoff;
			m_windowWipeAmount = data.windowWipeAmount;
			m_wheelDirtCutoff = data.wheelDirtCutoff;
		}

		private IEnumerator SetupMod()
		{
			while (GameObject.Find("PLAYER") == null ||
				    GameObject.Find("SATSUMA(557kg)") == null ||
					GameObject.Find("PLAYER/Pivot/Camera/FPSCamera/FPSCamera/AudioRain") == null)
			{
				yield return null;
			}

			ModConsole.Print("Dirt mod loading assetbundle...");
			var path = Path.Combine(ModLoader.ModsFolder, "MSCDirtMod");
			if (SystemInfo.graphicsDeviceVersion.StartsWith("OpenGL") && Application.platform == RuntimePlatform.WindowsPlayer)
				path = Path.Combine(path, "bundle-linux"); // apparently fixes opengl
			else if (Application.platform == RuntimePlatform.WindowsPlayer)
				path = Path.Combine(path, "bundle-windows");
			else if (Application.platform == RuntimePlatform.OSXPlayer)
				path = Path.Combine(path, "bundle-osx");
			else if (Application.platform == RuntimePlatform.LinuxPlayer)
				path = Path.Combine(path, "bundle-linux");

			if (!File.Exists(path))
			{
				ModConsole.Error("Couldn't find asset bundle from path " + path);
				yield break;
			}

			m_bundle = AssetBundle.CreateFromMemoryImmediate(File.ReadAllBytes(path));
			LoadAssets();

			ModConsole.Print("Dirt mod doing final setup...");
			m_rainAudioSource = GameObject.Find("PLAYER/Pivot/Camera/FPSCamera/FPSCamera/AudioRain").GetComponent<AudioSource>();

			m_satsuma = GameObject.Find("SATSUMA(557kg)");
			m_carDynamics = m_satsuma.GetComponentInChildren<CarDynamics>();
			m_wiperPivot = m_satsuma.transform.FindChild("Wipers/WiperLeftPivot");

			ModConsole.Print("Setting up buckets...");
			SetupBuckets();
			ModConsole.Print("Setting up audio...");
			SetupAudio();
			ModConsole.Print("Dirt Mod Setup!");
			m_isSetup = true;
		}

		private void LoadAssets()
		{
			// only load one set of textures
			m_bodyDirtTexture = m_bundle.LoadAsset<Texture2D>("BodyDirt");
			m_genericDirtTexture = m_bundle.LoadAsset<Texture2D>("GenericDirt");
			m_windowDirtTexture = m_bundle.LoadAsset<Texture2D>("WindowDirt");
			m_windowDirtWiperHoleTexture = m_bundle.LoadAsset<Texture2D>("WindowWiperHole");

			// load shaders
			m_bodyDirtShader = m_bundle.LoadAsset<Shader>("BodyDirtShader");
			m_windowDirtShader = m_bundle.LoadAsset<Shader>("WindowDirtShader");

			m_noDrawMaterial = new Material(m_bundle.LoadAsset<Shader>("NoDraw"));

			m_spongePrefab = m_bundle.LoadAssetWithSubAssets<GameObject>("SpongePrefab")[0];

			m_easterAudioClips.Add(m_bundle.LoadAsset<AudioClip>("easter_1"));
			m_easterAudioClips.Add(m_bundle.LoadAsset<AudioClip>("easter_2"));
			m_easterAudioClips.Add(m_bundle.LoadAsset<AudioClip>("easter_3"));
			m_easterAudioClips.Add(m_bundle.LoadAsset<AudioClip>("easter_4"));
			m_easterAudioClips.Add(m_bundle.LoadAsset<AudioClip>("easter_5"));
			m_easterAudioClips.Add(m_bundle.LoadAsset<AudioClip>("easter_6"));

			m_tireDirtTextureNew2 = m_bundle.LoadAsset<Texture2D>("TireDirtNew2");
			m_tireDirtTextureRally = m_bundle.LoadAsset<Texture2D>("TireDirtRally");
			m_tireDirtTextureRallye = m_bundle.LoadAsset<Texture2D>("TireDirtRallye");
			m_tireDirtTextureStandard = m_bundle.LoadAsset<Texture2D>("TireDirtStandard");
			m_tireDirtTextureSlicks = m_bundle.LoadAsset<Texture2D>("TireDirtSlicks");

			m_rimsDirtTexture = m_bundle.LoadAsset<Texture2D>("RimsDirt");
		}

		void Update()
		{
			try
			{
				if (m_isSetup && !m_halt)
				{
					DebugKeys();
					UseSponge();
					SetupBody();
					SetupWindows();
					SetupWheels();
					SetupMisc();
					UpdateMaterialValues();
				}
			}
			catch (Exception e)
			{
				ModConsole.Error(e.ToString());
				m_halt = true;
			}
		}

		void OnDestroy()
		{
			m_bundle.Unload(true);
		}

		private void DebugKeys()
		{
			if (MSCDirtMod.keyLessDirt.IsPressed())
			{
				m_bodyDirtCutoff += Time.deltaTime * 0.1f;
				m_windowWipeAmount += Time.deltaTime * 0.1f;
				m_wheelDirtCutoff += Time.deltaTime * 0.1f;
			}
			else if (MSCDirtMod.keyMoreDirt.IsPressed())
			{
				m_bodyDirtCutoff -= Time.deltaTime * 0.1f;
				m_windowWipeAmount -= Time.deltaTime * 0.1f;
				m_wheelDirtCutoff -= Time.deltaTime * 0.1f;
			}
		}

		private void UseSponge()
		{
			// block hand usage
			if (m_handRight.Value == false && m_rightFist.activeSelf)
				m_handRight.Value = true;
			if (m_handLeft.Value == false && m_leftFist.activeSelf)
				m_handLeft.Value = true;

			if (Input.GetMouseButtonDown(0) && m_rightFist.activeSelf)
			{
				m_rightFist.transform.FindChild("Pivot").GetComponent<Animation>().Play();
				CleanCar();
			}
			if (Input.GetMouseButtonDown(1) && m_leftFist.activeSelf)
			{
				m_leftFist.transform.FindChild("Pivot").GetComponent<Animation>().Play();
				CleanCar();
			}
		}

		private void CleanCar()
		{
			// enable our collider
			m_cleanCollider.gameObject.SetActive(true);

			var cam = Camera.main;
			var hits = Physics.RaycastAll(cam.transform.position, cam.transform.forward, 2f);
			if (hits.Any(raycastHit => raycastHit.collider == m_cleanCollider))
			{
				m_smackAudioSource.pitch = Random.Range(0.9f, 1.1f);
				m_smackAudioSource.Play();
				m_bodyDirtCutoff += 0.05f;
				m_windowWipeAmount += 0.05f;
				m_wheelDirtCutoff += 0.05f;
				m_playerDirtiness.Value += 5;
				m_playerStress.Value -= 2;
			}

			// disable our collider
			m_cleanCollider.gameObject.SetActive(false);
		}

		private Transform DupeWindow(Transform glass)
		{
			var dupeGlass = Instantiate(glass.gameObject).transform;
			dupeGlass.SetParent(glass.parent);
			dupeGlass.localPosition = glass.localPosition;
			dupeGlass.localScale = glass.localScale;
			dupeGlass.localRotation = glass.localRotation;
			return dupeGlass;
		}

		private void SetupMisc()
		{
			SwapMiscPartMaterial("Body/pivot_fender_right/fender right(Clone)/pivot_flares_fr/fender flare fr(Clone)", m_genericDirtTexture);
			SwapMiscPartMaterial("Body/pivot_fender_left/fender left(Clone)/pivot_flares_fl/fender flare fl(Clone)", m_genericDirtTexture);
			SwapMiscPartMaterial("Body/pivot_flares_rr/fender flare rr(Clone)", m_genericDirtTexture);
			SwapMiscPartMaterial("Body/pivot_flares_rl/fender flare rl(Clone)", m_genericDirtTexture);
			SwapMiscPartMaterial("Body/pivot_grille/grille(Clone)", m_genericDirtTexture);
			SwapMiscPartMaterial("Body/pivot_hood/fiberglass hood(Clone)", m_genericDirtTexture);
			SwapMiscPartMaterial("Body/pivot_bumper_front/bumper front(Clone)", m_genericDirtTexture);
			SwapMiscPartMaterial("Body/pivot_bumper_rear/bumper rear(Clone)", m_genericDirtTexture);
			SwapMiscPartMaterial("Body/pivot_spoiler_front/fender flare spoiler(Clone)", m_genericDirtTexture);
			SwapMiscPartMaterial("Body/pivot_bootlid/bootlid(Clone)/pivot_spoiler/rear spoiler(Clone)", m_genericDirtTexture);
			SwapMiscPartMaterial("Body/pivot_bootlid/bootlid(Clone)/bootlid_emblem", m_genericDirtTexture);
			SwapMiscPartMaterial("Body/pivot_bootlid/bootlid(Clone)/RegPlateRear", m_genericDirtTexture);
			SwapMiscPartMaterial("Body/pivot_bootlid/bootlid(Clone)/RegPlateRear", m_genericDirtTexture);
		}

		private void SwapMiscPartMaterial(string miscpart, Texture2D texture)
		{
			var part = m_satsuma.transform.FindChild(miscpart);
			if (part != null)
			{
				if (m_windowParts.ContainsKey(miscpart) && m_windowParts[miscpart] == part)
					return;

				if (!m_windowParts.ContainsKey(miscpart))
					m_windowParts.Add(miscpart, part);
				m_windowParts[miscpart] = part;

				var mr = part.GetComponent<MeshRenderer>();
				mr.material = SwapOpaqueShader(mr.material, texture);
				m_opaqueMaterials.Add(mr.material);
			}
		}

		private void SetupWindows()
		{
			RemoveBrokenWindshield();
			SwapWindowPartMaterial("Body/Windshield/mesh", true);
			SwapWindowPartMaterial("Body/rear_windows/standard", false);
			SwapWindowPartMaterial("Body/rear_windows/black windows(xxxxx)", false);
			SwapWindowPartMaterial("Body/pivot_door_right/door right(Clone)/windows_pivot/coll/glass", true);
			SwapWindowPartMaterial("Body/pivot_door_left/door left(Clone)/windows_pivot/coll/glass", true);
		}

		private void RemoveBrokenWindshield()
		{
			const string part = "Body/Windshield/mesh";
			if (m_windowParts.ContainsKey(part))
			{
				m_windowParts[part].parent.FindChild("DupeDirt").gameObject.SetActive(m_windowParts[part].gameObject.activeSelf);
			}
		}

		private void SwapWindowPartMaterial(string windowpart, bool flipMaterials)
		{
			var part = m_satsuma.transform.FindChild(windowpart);
			if (part != null)
			{
				if (m_windowParts.ContainsKey(windowpart) && m_windowParts[windowpart] == part)
					return;

				if (!m_windowParts.ContainsKey(windowpart))
					m_windowParts.Add(windowpart, part);
				m_windowParts[windowpart] = part;

				// already swapped glass
				if (part.parent.FindChild("DupeDirt") == null)
				{
					var dupe = DupeWindow(part);
					dupe.name = "DupeDirt";

					// turn window shadow receive off so they don't compete
					part.GetComponent<MeshRenderer>().receiveShadows = false;

					//dupe.localScale = new Vector3(1.005f,1.005f,1.005f);
					var mr = dupe.GetComponent<MeshRenderer>();
					var dirtInside = SwapWindowMaterialShaderDirt(mr.materials[0], flipMaterials ? -1 : 1);
					var dirtOutside = SwapWindowMaterialShaderDirt(mr.materials[0], flipMaterials ? 1 : -1);
					mr.shadowCastingMode = ShadowCastingMode.On;
					mr.receiveShadows = true;
					mr.materials = new[] { dirtInside, dirtOutside };
					m_transparentMaterials.Add(dirtInside);
					m_transparentMaterials.Add(dirtOutside);
				}
			}
		}

		private void SetupBody()
		{
			SwapBodyPartMaterial("Body/car body(xxxxx)");
			SwapBodyPartMaterial("Body/pivot_hood/hood(Clone)");
			SwapBodyPartMaterial("Body/pivot_door_left/door left(Clone)");
			SwapBodyPartMaterial("Body/pivot_door_right/door right(Clone)");
			SwapBodyPartMaterial("Body/pivot_fender_left/fender left(Clone)");
			SwapBodyPartMaterial("Body/pivot_fender_right/fender right(Clone)");
			SwapBodyPartMaterial("Body/pivot_bootlid/bootlid(Clone)");
		}

		private void SwapBodyPartMaterial(string bodypart)
		{
			var part = m_satsuma.transform.FindChild(bodypart);
			if (part != null)
			{
				if (m_bodyParts.ContainsKey(bodypart) && m_bodyParts[bodypart] == part)
					return;

				if (!m_bodyParts.ContainsKey(bodypart))
					m_bodyParts.Add(bodypart, part);
				m_bodyParts[bodypart] = part;

				ModConsole.Print("Swapped body " + bodypart);
				var mr = part.GetComponent<MeshRenderer>();
				mr.shadowCastingMode = ShadowCastingMode.On;
				mr.material = SwapOpaqueShader(mr.material, m_bodyDirtTexture);
				m_opaqueMaterials.Add(mr.material);
			}
		}

		private void SetupWheels()
		{
			SwapWheelMaterial("wheelFL/TireFL/OFFSET/pivot_wheel_standard");
			SwapWheelMaterial("wheelFR/TireFR/OFFSET/pivot_wheel_standard");
			SwapWheelMaterial("wheelRL/TireRL/pivot_wheel_standard");
			SwapWheelMaterial("wheelRR/TireRR/pivot_wheel_standard");
		}

		private void SwapWheelMaterial(string pivotPart)
		{
			var pivot = m_satsuma.transform.FindChild(pivotPart);
			if (pivot == null)
				return;

			var rim = pivot.transform.Cast<Transform>().FirstOrDefault(x => x.name.Contains("wheel"));
			if (rim == null)
				return;

			var wheel = rim.transform.Cast<Transform>().FirstOrDefault(x => x.name.Contains("Tire"));
			if (wheel == null)
				return;

			SwapTireMaterial(pivotPart + "/" + wheel.name, wheel);
			SwapRimMaterial(pivotPart + "/" + rim.name, rim);
		}

		private void SwapRimMaterial(string rim, Transform part)
		{
			if (m_rims.ContainsKey(rim) && m_rims[rim] == part)
				return;

			if (!m_rims.ContainsKey(rim))
				m_rims.Add(rim, part);
			m_rims[rim] = part;

			var texture = m_rimsDirtTexture;

			ModConsole.Print("Swapped rim " + rim);
			var mr = part.GetComponent<MeshRenderer>();
			// use the body shader for wheels too
			mr.material = SwapOpaqueShader(mr.material, texture);
			m_wheelMaterials.Add(mr.material);
		}

		private void SwapTireMaterial(string tire, Transform part)
		{
			if (m_wheels.ContainsKey(tire) && m_wheels[tire] == part)
				return;

			if (!m_wheels.ContainsKey(tire))
				m_wheels.Add(tire, part);
			m_wheels[tire] = part;

			var texture = m_tireDirtTextureNew2;
			if (tire.Contains("Standard"))
				texture = m_tireDirtTextureStandard;
			else if (tire.Contains("Slicks"))
				texture = m_tireDirtTextureSlicks;
			else if (tire.Contains("Gobra"))
				texture = m_tireDirtTextureRallye;
			else if (tire.Contains("Rally"))
				texture = m_tireDirtTextureRally;

			ModConsole.Print("Swapped tire " + tire);
			var mr = part.GetComponent<MeshRenderer>();
			// use the body shader for wheels too
			mr.material = SwapOpaqueShader(mr.material, texture);
			m_wheelMaterials.Add(mr.material);
		}

		private void SetupAudio()
		{
			var fpsCamera = GameObject.Find("FPSCamera");
			var go = new GameObject("SmackSound");
			m_smackAudioSource = go.AddComponent<AudioSource>();
			m_smackAudioSource.playOnAwake = false;
			m_smackAudioSource.clip = m_bundle.LoadAsset<AudioClip>("WetSmack");
			m_smackAudioSource.transform.SetParent(fpsCamera.transform);

			m_spongeAudioSource = go.AddComponent<AudioSource>();
			m_spongeAudioSource.playOnAwake = false;
			m_spongeAudioSource.clip = m_bundle.LoadAsset<AudioClip>("Sponge");
			m_spongeAudioSource.transform.SetParent(fpsCamera.transform);

			m_spongeOffAudioSource = go.AddComponent<AudioSource>();
			m_spongeOffAudioSource.playOnAwake = false;
			m_spongeOffAudioSource.clip = m_bundle.LoadAsset<AudioClip>("SpongeOff");
			m_spongeOffAudioSource.transform.SetParent(fpsCamera.transform);

			m_easterAudioSource = go.AddComponent<AudioSource>();
			m_easterAudioSource.playOnAwake = false;
			m_easterAudioSource.clip = null;
			m_easterAudioSource.transform.SetParent(fpsCamera.transform);
		}

		private void SetupBuckets()
		{
			var bucket = Instantiate(m_bundle.LoadAssetWithSubAssets<GameObject>("BucketPrefab")[0]);
			bucket.transform.position = new Vector3(-7.4f, -0.59f, 12.3f);
			bucket.transform.localRotation = Quaternion.Euler(0, -10f, 0);
			bucket.transform.localScale = new Vector3(1, 1, 1);
			var trigger = bucket.AddComponent<BucketTrigger>();
			trigger.onTrigger += HandleBucketTrigger;
			trigger.onHover += HandleBucketHover;

			// setup bucket 2 here at teimo's
			bucket = Instantiate(bucket);
			//bucket.transform.position = new Vector3(-7.4f, 0.59f, 12.3f);
			bucket.transform.position = new Vector3(-1560f, 2.9f, 1177.3f);
			bucket.transform.localRotation = Quaternion.Euler(0, -10f, 0);
			bucket.transform.localScale = new Vector3(1, 1, 1);
			trigger = bucket.GetComponent<BucketTrigger>();
			trigger.onTrigger += HandleBucketTrigger;
			trigger.onHover += HandleBucketHover;
			
			// setup satsuma trigger
			var go = new GameObject("CleaningTrigger");
			go.transform.SetParent(m_satsuma.transform, false);
			go.transform.localPosition = Vector3.zero;
			go.transform.localRotation = Quaternion.identity;
			//go.transform.localScale = new Vector3(1.9f, 1.7f, 3.7f);

			var collider = go.AddComponent<BoxCollider>();
			collider.isTrigger = true;
			collider.size = new Vector3(1.9f, 1.7f, 3.7f);
			m_cleanCollider = collider;

			// disable our collider
			m_cleanCollider.gameObject.SetActive(false);
			m_playerDirtiness = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerDirtiness");
			m_playerStress = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerStress");
			m_handLeft = PlayMakerGlobals.Instance.Variables.FindFsmBool("PlayerHandLeft");
			m_handRight = PlayMakerGlobals.Instance.Variables.FindFsmBool("PlayerHandRight");

			// setup hands
			var fpsCamera = GameObject.Find("FPSCamera");
			m_rightFist = Instantiate(fpsCamera.transform.FindChild("Fist")).gameObject;
			m_rightFist.transform.SetParent(fpsCamera.transform, false);
			m_rightFist.SetActive(false);
			m_rightFist.transform.localPosition += new Vector3(0, 0, 0.2f);

			var sponge = Instantiate(m_spongePrefab);
			sponge.transform.SetParent(m_rightFist.transform.FindChild("Pivot/hand/Armature/Bone/Bone_001/Bone_006").transform,
				false);
			
			sponge.transform.localPosition = Vector3.zero;
			sponge.transform.FindChild("Sponge").GetComponent<MeshRenderer>().sortingLayerID =
				m_rightFist.transform.FindChild("Pivot/hand/hand_rigged").GetComponent<SkinnedMeshRenderer>().sortingLayerID;

			m_leftFist = Instantiate(m_rightFist);
			m_leftFist.transform.SetParent(fpsCamera.transform, false);
			m_leftFist.SetActive(false);
			m_leftFist.transform.localScale = new Vector3(
				-m_leftFist.transform.localScale.x,
				m_leftFist.transform.localScale.y,
				m_leftFist.transform.localScale.z);
			m_leftFist.transform.localPosition += new Vector3(-0.1f, 0, 0f);
		}

		private void HandleBucketHover(bool spongeAvailable)
		{
			var str = spongeAvailable ? "Take sponge" : "Drop sponge";
			GameObject.Find("GUI/Indicators/Interaction").GetComponent<TextMesh>().text = str;
		}

		private void PlayTakeSponge()
		{
			m_spongeAudioSource.pitch = Random.Range(0.9f, 1.1f);
			m_spongeAudioSource.Play();
		}

		private void PlayDropSponge()
		{
			m_spongeOffAudioSource.pitch = Random.Range(0.9f, 1.1f);
			m_spongeOffAudioSource.Play();
		}

		private void HandleBucketTrigger(BucketTrigger trigger, bool takeSponge)
		{
			try
			{
				if (takeSponge)
				{
					if (!m_handRight.Value)
					{
						m_handRight.Value = true;
						m_rightFist.SetActive(true);
						trigger.SetSponge(false);
						PlayTakeSponge();
					}
					else if (!m_handLeft.Value)
					{
						m_handLeft.Value = true;
						m_leftFist.SetActive(true);
						trigger.SetSponge(false);
						PlayTakeSponge();
					}
					if (m_rightFist.activeSelf && m_leftFist.activeSelf)
						StartCoroutine(UltraClean());
				}
				else
				{
					if (m_leftFist.activeSelf)
					{
						m_handLeft.Value = false;
						m_leftFist.SetActive(false);
						trigger.SetSponge(true);
						PlayDropSponge();
					} else 
					{
						m_handRight.Value = false;
						m_rightFist.SetActive(false);
						trigger.SetSponge(true);
						PlayDropSponge();
					}
				}
			}
			catch (Exception e)
			{
				ModConsole.Print(e.ToString());
				throw;
			}
		}

		private IEnumerator UltraClean()
		{
			var i = Random.Range(0, m_easterAudioClips.Count - 1);
			m_easterAudioSource.clip = m_easterAudioClips[i];
			m_easterAudioSource.Play();

			var str = "";
			switch (i)
			{
				case 0: str = "God help me! Double goat!"; break;
				case 1: str = "No, perkele, two pieces!"; break;
				case 2: str = "Yup, gets moving!"; break;
				case 3: str = "Perkele, now the dirt will go!"; break;
				case 4: str = "Yes! Doubles!"; break;
				case 5: str = "With two hands! God help me!"; break;
			}

			var tm = GameObject.Find("GUI/Indicators/Subtitles").GetComponent<TextMesh>();
			while (m_easterAudioSource.isPlaying)
			{
				tm.text = str;
				yield return new WaitForSeconds(1f);
			}
			tm.text = "";
		}

		private void UpdateMaterialValues()
		{
			// remove null materials (old parts that were destroyed)
			m_transparentMaterials.RemoveAll(x => x == null);
			m_opaqueMaterials.RemoveAll(x => x == null);
			m_wheelMaterials.RemoveAll(x => x == null);

			// wipers are moving
			if (!Mathf.Approximately(m_wiperPivot.localRotation.eulerAngles.z, m_oldWiperAngle))
			{
				m_oldWiperAngle = m_wiperPivot.localRotation.eulerAngles.z;
				m_windowWipeAmount += Time.deltaTime * 0.15f;
				m_wiperOnAmount += Time.deltaTime * 5f;
			}
			else
			{
				m_wiperOnAmount -= Time.deltaTime * 0.2f * m_rainAmount;
			}
			
			// clean car when it rains
			if (m_rainAudioSource.isPlaying)
			{
				m_wheelDirtCutoff += Time.deltaTime * 0.01f;
				m_bodyDirtCutoff += Time.deltaTime * 0.01f;
				m_windowWipeAmount += Time.deltaTime * 0.01f;
				m_rainAmount += Time.deltaTime * 0.1f;
			}
			else
			{
				m_rainAmount -= Time.deltaTime * 0.1f;
			}
			
			// dirty car when it moves, based on surface
			var wheel = m_satsuma.GetComponentInChildren<Wheel>();
			if (wheel != null)
			{
				var minDirt = 0f;
				var minWheelDirt = 0f;
				var amount = 0f;

				switch (wheel.surfaceType)
				{
					case CarDynamics.SurfaceType.sand: 
						amount = 0.0002f;
						minDirt = 0.4f;
						minWheelDirt = 0.6f;
					break;
					case CarDynamics.SurfaceType.grass: 
						amount = 0.0006f;
						minDirt = 0;
						minWheelDirt = 0;
					break;
					case CarDynamics.SurfaceType.offroad: 
						amount = 0.0002f;
						minDirt = 0.4f;
						minWheelDirt = 0.6f;
					break;
					case CarDynamics.SurfaceType.track:
						amount = 0.0001f;
						minDirt = 0.8f;
						minWheelDirt = 0.8f;
					break;
					case CarDynamics.SurfaceType.oil:
						amount = 0.0001f;
						minDirt = 0.1f;
						minWheelDirt = 0f;
					break;
				}
				
				amount *= m_bodyDirtCutoff + 0.1f; // diminishing returns to dirt (accumulate at 110% at no dirt, 10% at full dirt)
				var v = Time.deltaTime * m_carDynamics.velo * amount;

				if (m_bodyDirtCutoff > minDirt)
					m_bodyDirtCutoff -= v;
				if (m_windowWipeAmount > minDirt)
					m_windowWipeAmount -= v * 50f;

				// clean wheels (based off of surface)
				if (m_wheelDirtCutoff > minWheelDirt)
					m_wheelDirtCutoff -= v * 100f;
				else
					m_wheelDirtCutoff += v * 100f; // tires dirty real fast
			}

			m_bodyDirtCutoff = Mathf.Clamp01(m_bodyDirtCutoff);
			m_windowWipeAmount = Mathf.Clamp(m_windowWipeAmount, 0, 0.8f);
			m_wiperOnAmount = Mathf.Clamp01(m_wiperOnAmount);
			m_rainAmount = Mathf.Clamp01(m_rainAmount);

			SetTransparentDirtMaterialFloat("_Cutoff", m_bodyDirtCutoff + 0.2f);
			SetTransparentDirtMaterialFloat("_WipeAmount", m_windowWipeAmount);

			SetOpaqueDirtMaterialFloat("_Cutoff", m_bodyDirtCutoff);
			SetWheelMaterialFloat("_Cutoff", m_wheelDirtCutoff);
		}

		private void SetOpaqueDirtMaterialFloat(string key, float value)
		{
			foreach (var mat in m_opaqueMaterials)
			{
				mat.SetFloat(key, value);
			}
		}

		private void SetTransparentDirtMaterialFloat(string key, float value)
		{
			foreach (var mat in m_transparentMaterials)
			{
				mat.SetFloat(key, value);
			}
		}

		private void SetWheelMaterialFloat(string key, float value)
		{
			foreach (var mat in m_wheelMaterials)
			{
				mat.SetFloat(key, value);
			}
		}

		private Material SwapOpaqueShader(Material material, Texture2D texture)
		{
			var newMat = new Material(m_bodyDirtShader);
			newMat.CopyPropertiesFromMaterial(material);
			newMat.SetTexture("_DirtTex", texture);
			return newMat;
		}

		private Material SwapTransparentShader(Material material, Texture2D texture, int offset)
		{
			var newMat = new Material(m_windowDirtShader);
			newMat.CopyPropertiesFromMaterial(material);
			newMat.SetTexture("_MainTex", texture);
			newMat.SetInt("_Offset", offset);
			return newMat;
		}

		private Material SwapWindowMaterialShaderDirt(Material material, int offset)
		{
			var newMat = new Material(m_windowDirtShader);
			newMat.CopyPropertiesFromMaterial(material);
			newMat.SetTexture("_MainTex", m_windowDirtTexture);
			newMat.SetTexture("_WiperHole", m_windowDirtWiperHoleTexture);
			newMat.SetInt("_Offset", offset);
			return newMat;
		}
	}
}
