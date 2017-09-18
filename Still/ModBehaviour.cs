using System;
using System.Collections;
using System.IO;
using MSCLoader;
using UnityEngine;

//Standard unity MonoBehaviour class
namespace MSCStill
{
	public class ModBehaviour : MonoBehaviour
	{
		private static string ModPath;
		private AssetBundle m_bundle;
		private GameObject m_stillPrefab;
		private GameObject m_bottlePrefab;
		private GameObject m_buyerPrefab;

		public static AudioClipContainer buyerBottleReturn,
			buyerGreet,
			buyerPayment,
			buyerTaste,
			buyerTasteGood,
			buyerTasteMethanol,
			buyerTasteWater,
			buyerNoBuyWater,
			buyerNoBuyMethanol,
			buyerByeBye,
			buyerAsk,
			buyerStory;
		private GameObject m_drinkBottlePrefab;
		private Animation m_drinkHandAnimation;
		private Transform m_drinkHand;
		private GameObject m_drinkBottle;
		private Transform m_handBottles;

		void Awake()
		{
			Instance = this;
			try
			{
				ModPath = Path.Combine(ModLoader.ModsFolder, "MSCStill");
				SetupMod();
			}
			catch (Exception e)
			{
				ModConsole.Error(e.ToString());
				throw;
			}
		}

		void OnDestroy()
		{
			m_bundle.Unload(true);
		}

		private void SetupMod()
		{
			ModConsole.Print("Still mod loading assetbundle...");
			var path = "";
			if (SystemInfo.graphicsDeviceVersion.StartsWith("OpenGL") && Application.platform == RuntimePlatform.WindowsPlayer)
				path = Path.Combine(ModPath, "bundle-linux"); // apparently fixes opengl
			else if (Application.platform == RuntimePlatform.WindowsPlayer)
				path = Path.Combine(ModPath, "bundle-windows");
			else if (Application.platform == RuntimePlatform.OSXPlayer)
				path = Path.Combine(ModPath, "bundle-osx");
			else if (Application.platform == RuntimePlatform.LinuxPlayer)
				path = Path.Combine(ModPath, "bundle-linux");

			if (!File.Exists(path))
			{
				ModConsole.Error("Couldn't find asset bundle from path " + path);
			}
			else
			{
				m_bundle = AssetBundle.CreateFromMemoryImmediate(File.ReadAllBytes(path));
				LoadAssets();
				SetupGameObjects();
				SetupBottle();
				ModConsole.Print("Still mod Setup!");
			}
		}

		private void SetupBottle()
		{
			ModConsole.Print("Setting up bottle...");
			var bottle = (GameObject)Instantiate(m_bottlePrefab, new Vector3(-839, -2f, 505), Quaternion.identity);
			bottle.AddComponent<Bottle>();
		}

		private void LoadAssets()
		{
			m_stillPrefab = m_bundle.LoadAssetWithSubAssets<GameObject>("StillPrefab")[0];
			m_bottlePrefab = m_bundle.LoadAssetWithSubAssets<GameObject>("BottlePrefab")[0];
			m_buyerPrefab = m_bundle.LoadAssetWithSubAssets<GameObject>("BuyerPrefab")[0];
			m_drinkBottlePrefab = m_bundle.LoadAssetWithSubAssets<GameObject>("DrinkBottlePrefab")[0];

			buyerGreet = new AudioClipContainer();
			buyerGreet.AddClip(m_bundle.LoadAsset<AudioClip>("Greet1"));
			buyerGreet.AddClip(m_bundle.LoadAsset<AudioClip>("Greet2"));
			buyerGreet.AddClip(m_bundle.LoadAsset<AudioClip>("Greet3"));
			buyerGreet.AddClip(m_bundle.LoadAsset<AudioClip>("Greet4"));

			buyerTaste = new AudioClipContainer();
			buyerTaste.AddClip(m_bundle.LoadAsset<AudioClip>("TasteMoonshine1"));
			buyerTaste.AddClip(m_bundle.LoadAsset<AudioClip>("TasteMoonshine2"));
			buyerTaste.AddClip(m_bundle.LoadAsset<AudioClip>("TasteMoonshine3"));
			buyerTaste.AddClip(m_bundle.LoadAsset<AudioClip>("TasteMoonshine4"));

			buyerTasteGood = new AudioClipContainer();
			buyerTasteGood.AddClip(m_bundle.LoadAsset<AudioClip>("Good1"));
			buyerTasteGood.AddClip(m_bundle.LoadAsset<AudioClip>("Good2"));
			buyerTasteGood.AddClip(m_bundle.LoadAsset<AudioClip>("Good3"));

			buyerPayment = new AudioClipContainer();
			buyerPayment.AddClip(m_bundle.LoadAsset<AudioClip>("Payment1"));
			buyerPayment.AddClip(m_bundle.LoadAsset<AudioClip>("Payment2"));
			buyerPayment.AddClip(m_bundle.LoadAsset<AudioClip>("Payment3"));
			buyerPayment.AddClip(m_bundle.LoadAsset<AudioClip>("Payment4"));

			buyerTasteWater = new AudioClipContainer();
			buyerTasteWater.AddClip(m_bundle.LoadAsset<AudioClip>("Water1"));
			buyerTasteWater.AddClip(m_bundle.LoadAsset<AudioClip>("Water2"));
			buyerTasteWater.AddClip(m_bundle.LoadAsset<AudioClip>("Water3"));

			buyerNoBuyWater = new AudioClipContainer();
			buyerNoBuyWater.AddClip(m_bundle.LoadAsset<AudioClip>("NoBuyWater1"));
			buyerNoBuyWater.AddClip(m_bundle.LoadAsset<AudioClip>("NoBuyWater2"));
			buyerNoBuyWater.AddClip(m_bundle.LoadAsset<AudioClip>("NoBuyWater3"));
			buyerNoBuyWater.AddClip(m_bundle.LoadAsset<AudioClip>("NoBuyWater4"));

			buyerTasteMethanol = new AudioClipContainer();
			buyerTasteMethanol.AddClip(m_bundle.LoadAsset<AudioClip>("Methanol1"));
			buyerTasteMethanol.AddClip(m_bundle.LoadAsset<AudioClip>("Methanol2"));
			buyerTasteMethanol.AddClip(m_bundle.LoadAsset<AudioClip>("Methanol3"));

			buyerNoBuyMethanol = new AudioClipContainer();
			buyerNoBuyMethanol.AddClip(m_bundle.LoadAsset<AudioClip>("NoBuyMethanol1"));
			buyerNoBuyMethanol.AddClip(m_bundle.LoadAsset<AudioClip>("NoBuyMethanol2"));
			buyerNoBuyMethanol.AddClip(m_bundle.LoadAsset<AudioClip>("NoBuyMethanol3"));

			buyerBottleReturn = new AudioClipContainer();
			buyerBottleReturn.AddClip(m_bundle.LoadAsset<AudioClip>("BottleReturn1"));
			buyerBottleReturn.AddClip(m_bundle.LoadAsset<AudioClip>("BottleReturn2"));
			buyerBottleReturn.AddClip(m_bundle.LoadAsset<AudioClip>("BottleReturn3"));

			buyerByeBye = new AudioClipContainer();
			buyerByeBye.AddClip(m_bundle.LoadAsset<AudioClip>("Thanks1"));
			buyerByeBye.AddClip(m_bundle.LoadAsset<AudioClip>("Thanks2"));
			buyerByeBye.AddClip(m_bundle.LoadAsset<AudioClip>("Thanks3"));

			buyerAsk = new AudioClipContainer();
			buyerAsk.AddClip(m_bundle.LoadAsset<AudioClip>("AskBring1"));
			buyerAsk.AddClip(m_bundle.LoadAsset<AudioClip>("AskBring2"));
			buyerAsk.AddClip(m_bundle.LoadAsset<AudioClip>("AskBring3"));
			buyerAsk.AddClip(m_bundle.LoadAsset<AudioClip>("AskBring4"));

			buyerStory = new AudioClipContainer();
			buyerStory.AddClip(m_bundle.LoadAsset<AudioClip>("Story1"));
		}

		private void SetupGameObjects()
		{
			ModConsole.Print("Setting up still...");
			var still = Instantiate(m_stillPrefab).AddComponent<Still>();
			still.transform.position = new Vector3(-839, -3.15f, 504);

			ModConsole.Print("Setting up buyer...");
			var buyer = Instantiate(m_buyerPrefab).AddComponent<Buyer>();
			buyer.transform.position = new Vector3(-1527.8f, 5.158f, 1388.8f);
			buyer.transform.rotation = Quaternion.Euler(new Vector3(0, 270, 0));

			ModConsole.Print("Setting up drink bottle...");
			m_drinkHand = GameObject.Find("PLAYER/Pivot/Camera/FPSCamera/FPSCamera/Drink").transform.FindChild("Hand");
			m_drinkHandAnimation = m_drinkHand.GetComponent<Animation>();
			m_handBottles = m_drinkHand.transform.FindChild("HandBottles");

			var beer = m_drinkHand.transform.FindChild("BeerBottle");
			m_drinkBottle = Instantiate(m_drinkBottlePrefab);
			m_drinkBottle.transform.SetParent(beer.parent);
			m_drinkBottle.transform.localPosition = new Vector3(0.33f, 0.14f, 0f);
			m_drinkBottle.transform.localRotation = Quaternion.Euler(90f, 180f, 0f);
			m_drinkBottle.SetActive(false);
		}

		public void DrinkMoonshine()
		{
			StartCoroutine(Drink());
		}

		private IEnumerator Drink()
		{
			m_handBottles.gameObject.SetActive(true);
			m_drinkBottle.SetActive(true);
			m_drinkHand.gameObject.SetActive(true);
			m_drinkHandAnimation.Play("drink_rotate");
			yield return new WaitForSeconds(m_drinkHandAnimation.GetClip("drink_rotate").length);
			m_drinkBottle.SetActive(false);
			m_drinkHand.gameObject.SetActive(false);
			m_handBottles.gameObject.SetActive(false);
		}

		public static ModBehaviour Instance { get; set; }
	}
}
