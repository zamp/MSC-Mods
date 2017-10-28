using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HutongGames.PlayMaker.Actions;
using MSCLoader;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace MSCPaintMagazine
{
	public class Painter : MonoBehaviour
	{
		private Canvas m_canvas;
		private List<PaintJob> m_paintJobs = new List<PaintJob>();
		private AssetBundle m_bundle;
		private GameObject m_satsuma;
		private Texture2D m_satsumaDecalTexture;
		private PlayMakerFSM m_playerViewFsm;
		private MeshCollider m_magazineCollider;

		private class PaintJob
		{
			public string name, creator, url, previewUrl;
		}

		void Awake()
		{
			StartCoroutine(SetupMod());
		}

		private IEnumerator SetupMod()
		{
			while (GameObject.Find("PLAYER") == null)
			{
				yield return null;
			}

			var fsms = GameObject.Find("PLAYER").GetComponents<PlayMakerFSM>();
			foreach (var playMakerFsm in fsms)
			{
				if (playMakerFsm.FsmStates.Any(x => x.Name == "In Menu"))
				{
					m_playerViewFsm = playMakerFsm;
				}
			}

			var path = PaintMagazine.assetPath;
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
			}
			else
			{
				m_bundle = AssetBundle.CreateFromMemoryImmediate(File.ReadAllBytes(path));
				SetupGUI();
				SetupMagazine();
				var www = new WWW("https://zamp.github.io/mscskins.txt");
				yield return www;

				var lines = www.text.Split(new [] { "\r\n", "\n" }, StringSplitOptions.None);
				foreach (var line in lines)
				{
					var splits = line.Split('|');
					if (splits.Length >= 4)
					{
						m_paintJobs.Add(new PaintJob
						{
							name = splits[0],
							creator = splits[1],
							url = splits[2],
							previewUrl = splits[3]
						});
					}
				}
				ModConsole.Print("Loaded " + m_paintJobs.Count + " paintjobs!");
				ModConsole.Print("Custom Paint Setup!");

				path = Path.Combine(Application.persistentDataPath, "lastpainturl.txt");
				if (File.Exists(path))
				{
					LoadImageAndSetSatsuma(File.ReadAllText(path));
				}
				m_bundle.Unload(false);
			}
		}

		private void SetupMagazine()
		{
			var magazine = Instantiate(m_bundle.LoadAsset<GameObject>("MagazinePrefab"));
			magazine.transform.position = new Vector3(1552.9f, 5.10f, 737.1f);
			m_magazineCollider = magazine.transform.FindChild("Mesh").gameObject.AddComponent<MeshCollider>();
			m_magazineCollider.convex = true;
			m_magazineCollider.isTrigger = true;
		}

		private void SetupGUI()
		{
			m_canvas = Instantiate(m_bundle.LoadAssetWithSubAssets<GameObject>("PaintJobCanvas")[0]).GetComponent<Canvas>();
			m_canvas.gameObject.SetActive(false);
		}

		void Update()
		{
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit, 2f) && hit.collider == m_magazineCollider)
			{
				GameObject.Find("GUI/Indicators/Interaction").GetComponent<TextMesh>().text = "Paint Magazine";
				if (Input.GetMouseButtonDown(0))
				{
					m_canvas.gameObject.SetActive(true);
					// set the whatever exit check bool to true in the fsm somewhere in there... yup.. it's very good system
					((BoolTest)m_playerViewFsm.FsmStates.First(x => x.Name == "In Menu").Actions.First(x => x is BoolTest))
						.boolVariable.Value = true;
					m_playerViewFsm.SendEvent("MENU");

					if (m_canvas.transform.FindChild("Background/ScrollRect/View/Content").childCount > 0)
						return;

					var container = m_canvas.transform.FindChild("Background/ScrollRect/View/Content");
					var itemTemplate = m_canvas.transform.FindChild("Background/Item");
					itemTemplate.gameObject.SetActive(false);

					foreach (var paintJob in m_paintJobs)
					{
						var go = Instantiate(itemTemplate);
						go.transform.SetParent(container, false);
						go.transform.FindChild("Text").GetComponent<Text>().text = paintJob.name + "\nBy: " + paintJob.creator;
						go.gameObject.SetActive(true);
						StartCoroutine(LoadPreviewForImage(paintJob.previewUrl, go.GetComponent<Image>()));
						var copy = paintJob.url;
						go.GetComponent<Button>().onClick.AddListener(() => LoadImageAndSetSatsuma(copy));
					}
				}
			}

			if (m_canvas.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
			{
				m_canvas.gameObject.SetActive(false);
			}
		}

		private IEnumerator LoadPreviewForImage(string url, Image image)
		{
			var www = new WWW(url);
			yield return www;
			var t = www.texture;
			image.sprite = Sprite.Create(t, new Rect(0,0,t.width,t.height), Vector2.zero);
		}

		private void LoadImageAndSetSatsuma(string url)
		{
			StartCoroutine(SetSatsumaImage(url));
			((BoolTest)m_playerViewFsm.FsmStates.First(x => x.Name == "In Menu").Actions.First(x => x is BoolTest))
					.boolVariable.Value = false;
			m_canvas.gameObject.SetActive(false);
		}

		private IEnumerator SetSatsumaImage(string url)
		{
			File.WriteAllText(Path.Combine(Application.persistentDataPath, "lastpainturl.txt"), url);
			
			var www = new WWW(url);
			yield return www;
			m_satsumaDecalTexture = www.texture;
			m_satsuma = PlayMakerGlobals.Instance.Variables.FindFsmGameObject("TheCar").Value;

			ChangeSatsumaDecalTexture("Body/car body(xxxxx)");
			ChangeSatsumaDecalTexture("Body/pivot_hood/hood(Clone)");
			ChangeSatsumaDecalTexture("Body/pivot_door_left/door left(Clone)");
			ChangeSatsumaDecalTexture("Body/pivot_door_right/door right(Clone)");
			ChangeSatsumaDecalTexture("Body/pivot_fender_left/fender left(Clone)");
			ChangeSatsumaDecalTexture("Body/pivot_fender_right/fender right(Clone)");
			ChangeSatsumaDecalTexture("Body/pivot_bootlid/bootlid(Clone)");
		}

		private void ChangeSatsumaDecalTexture(string p)
		{
			var part = m_satsuma.transform.FindChild(p);
			if (part != null)
			{
				var mr = part.GetComponent<MeshRenderer>();
				mr.sharedMaterial.SetTexture("_DetailAlbedoMap", m_satsumaDecalTexture);
			}
		}
	}
}
