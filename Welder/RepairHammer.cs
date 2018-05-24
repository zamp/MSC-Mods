using System;
using System.Linq;
using System.Reflection;
using HutongGames.PlayMaker;
using MSCLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RepairHammer
{
	public class RepairHammer : Mod
	{
		public override string ID => "RepairHammer";
		public override string Name => "Repair Hammer";
		public override string Author => "zamp";
		public override string Version => "0.1";

		private Animation _sledgehammerAnimation;
		private GameObject _sledgehammer;
		private float _sleepUntil;
		private bool _swinging;

		public override void OnLoad()
		{
			try
			{
				_sledgehammer = GameObject.Find("PLAYER/Pivot/Camera/FPSCamera/FPSCamera/Sledgehammer");
				_sledgehammerAnimation = _sledgehammer.transform.FindChild("Pivot").GetComponent<Animation>();

				var jobs = GameObject.Find("REPAIRSHOP").transform.Find("Jobs");

				// this forces job statemachines to load properly so we can get the juicy data out
				jobs.gameObject.SetActive(true);
				jobs.gameObject.SetActive(false);

				// get the juicy data (a car that's not fucked up) out from jobs
				var bodyFixFsm = jobs.Find("Bodyfix").GetComponent<PlayMakerFSM>();
				var satsuma = PlayMakerGlobals.Instance.Variables.GetFsmGameObject("TheCar").Value;
				var bodyMesh = new Mesh();
				
				var actions = bodyFixFsm.FsmStates.FirstOrDefault(x => x.Name == "Fix")?.Actions;
				if (actions == null)
				{
					ModConsole.Error("Could not find body fix.");
					return;
				}

				foreach (var action in actions)
				{
					var fsmProperty = action.GetType().GetField("targetProperty").GetValue(action) as FsmProperty;
					bodyMesh = (Mesh) fsmProperty.ObjectParameter.Value;
					break;
				}

				var deformable = satsuma.GetComponents<Deformable>()
					.FirstOrDefault(x => x.meshFilter.name.StartsWith("car body(xxxxx)", StringComparison.Ordinal));
				var baseVerticesField =
					deformable.GetType().GetField("baseVertices", BindingFlags.NonPublic | BindingFlags.Instance);

				baseVerticesField.SetValue(deformable, bodyMesh.vertices);

				// make sure the base vertices and actual vertice count is the same
				var verticesField =
					deformable.GetType().GetField("vertices", BindingFlags.NonPublic | BindingFlags.Instance);
				baseVerticesField.SetValue(deformable, bodyMesh.vertices);

				var baseVertices = (Vector3[])baseVerticesField.GetValue(deformable);
				var vertices = (Vector3[])verticesField.GetValue(deformable);

				if (baseVertices.Length != vertices.Length)
				{
					ModConsole.Error("Repair mesh discrepancy found. Please delete your mesh save.txt file.");
				}
			}
			catch (Exception e)
			{
				ModConsole.Error("Repair Hammer OnLoad Exception: " + e);
			}
		}
		
		public override void Update()
		{
			try
			{
				if (_swinging)
				{
					if (Time.realtimeSinceStartup > _sleepUntil)
					{
						_sleepUntil = Time.realtimeSinceStartup + 0.5f;
						_swinging = false;
						_sledgehammerAnimation.Play("sledgehammer_hit");

						RaycastRepair();
					}
				}
				else
				{
					// is the sledgehammer in use?
					if (!_sledgehammer.activeSelf)
						return;
					if (!Input.GetKeyDown(KeyCode.Mouse0))
						return;
					if (Time.realtimeSinceStartup < _sleepUntil)
						return;

					_sledgehammerAnimation.Play("sledgehammer_up");
					_swinging = true;

					_sleepUntil = Time.realtimeSinceStartup + 1f;
				}
			}
			catch (Exception e)
			{
#if DEBUG
				ModConsole.Error(e.ToString());
#endif
				// silently eat all errors
			}
		}

		private void RaycastRepair()
		{
			const float repairArea = 0.3f;
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			var hits = Physics.RaycastAll(ray, 1f);
			var playedHitSound = false;
			foreach (var hit in hits)
			{
				// TODO: cache found deformable by collider?
				// The up/down lookup operation is pretty expensive so might do good to do the caching bit.

				var deformables = hit.collider.GetComponents<Deformable>();
				// look up first
				if (deformables == null || !deformables.Any())
					deformables = GetDeformableRecursiveUp(hit.collider.transform.parent);
				// look down
				if (deformables == null || !deformables.Any())
					deformables = GetDeformableRecursiveDown(hit.collider.transform.parent);

				// welp.. we tried
				if (deformables == null || !deformables.Any())
					continue;

				// play crash at hit position
				if (!playedHitSound)
				{
					playedHitSound = true;
					MasterAudio.PlaySound3DAtVector3AndForget("Crashes", hit.point, 0.3f);
					if (UnityEngine.Random.value < 0.01f)
					{
						MasterAudio.PlaySoundAndForget("Fuck", 0.5f);
					}
				}

				foreach (var deformable in deformables)
				{
					// repair works but it moves all points the same amount without feathering
					//deformable.Repair(0.1f, hit.point, 0.4f);

					// do our own thing here
					var type = deformable.GetType();
					var baseVerticesField = type.GetField("baseVertices", BindingFlags.NonPublic | BindingFlags.Instance);
					var verticesField = deformable.GetType().GetField("vertices", BindingFlags.NonPublic | BindingFlags.Instance);
					var meshUpdateField =
						deformable.GetType().GetField("meshUpdate", BindingFlags.NonPublic | BindingFlags.Instance);

					// skip over if we got borked data.
					if (verticesField == null || baseVerticesField == null || meshUpdateField == null)
						continue;

					var baseVertices = (Vector3[])baseVerticesField.GetValue(deformable);
					var vertices = (Vector3[])verticesField.GetValue(deformable);

					for (var i = 0; i < vertices.Length; ++i)
					{
						var d = Vector3.Distance(hit.point, deformable.meshFilter.transform.TransformPoint(baseVertices[i]));
#if DEBUG
						if (d < repairArea)
						{
							var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
							Object.Destroy(go.GetComponent<Collider>());
							Object.Destroy(go.GetComponent<Rigidbody>());
							go.transform.position = deformable.meshFilter.transform.TransformPoint(baseVertices[i]);
							go.transform.localScale = new Vector3(0.04f, 0.04f, 0.04f);
							Object.Destroy(go, 2f);

							go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
							go.GetComponent<MeshRenderer>().material.color = Color.red;
							Object.Destroy(go.GetComponent<Collider>());
							Object.Destroy(go.GetComponent<Rigidbody>());
							go.transform.position = deformable.meshFilter.transform.TransformPoint(vertices[i]);
							go.transform.localScale = new Vector3(0.04f, 0.04f, 0.04f);
							Object.Destroy(go, 2f);
						}
#endif

						var repairAmount = Mathf.Clamp01(repairArea - d); // fix 20 cm area
						vertices[i] += (baseVertices[i] - vertices[i]) * repairAmount;
					}

					verticesField.SetValue(deformable, vertices);

					// set mesh update true so the mesh gets updated
					meshUpdateField.SetValue(deformable, true);
				}
			}
		}

		private static Deformable[] GetDeformableRecursiveUp(Transform transform)
		{
			if (transform.GetComponents<Deformable>().Any())
				return transform.GetComponents<Deformable>();
			if (transform.parent != null)
				return GetDeformableRecursiveUp(transform.parent);
			return null;
		}

		private static Deformable[] GetDeformableRecursiveDown(Transform transform)
		{
			if (transform.GetComponents<Deformable>().Any())
				return transform.GetComponents<Deformable>();
			foreach (Transform child in transform)
				return GetDeformableRecursiveDown(child);
			return null;
		}
	}
}

