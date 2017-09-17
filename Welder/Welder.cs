using System;
using System.Linq;
using System.Reflection;
using HutongGames.PlayMaker;
using MSCLoader;
using UnityEngine;

namespace Welder
{
	public class Welder : Mod
	{
		private bool m_satsumaDeformerBaseSwapped;
		public override string ID { get { return "Welder"; } }
		public override string Name { get { return "Welder"; } }
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
				if (Input.GetKeyDown(KeyCode.Keypad0))
				{
					if (!m_satsumaDeformerBaseSwapped)
					{
						if (GameObject.Find("SATSUMA(557kg)") == null)
							return;
						if (GameObject.Find("REPAIRSHOP") == null)
							return;

						var jobs = GameObject.Find("REPAIRSHOP").transform.Find("Jobs");
						// this forces job statemachines to load properly so we can get the juicy data out
						jobs.gameObject.SetActive(true);
						jobs.gameObject.SetActive(false);
						var bodyFixFsm = jobs.Find("Bodyfix").GetComponent<PlayMakerFSM>();
						var satsuma = GameObject.Find("SATSUMA(557kg)");
						var bodyMesh = new Mesh();

						var actions = bodyFixFsm.FsmStates.FirstOrDefault(x => x.Name == "Fix").Actions;
						foreach (var action in actions)
						{
							var fsmProperty = action.GetType().GetField("targetProperty").GetValue(action) as FsmProperty;
							bodyMesh = (Mesh)fsmProperty.ObjectParameter.Value;
							break;
						}
						
						var deformable = satsuma.GetComponents<Deformable>().FirstOrDefault(x => x.meshFilter.name.StartsWith("car body(xxxxx)"));
						var baseVerticesField = deformable.GetType().GetField("baseVertices", BindingFlags.NonPublic | BindingFlags.Instance);
						baseVerticesField.SetValue(deformable, bodyMesh.vertices);

						m_satsumaDeformerBaseSwapped = true;
					}

					const float repairArea = 0.3f;
					var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
					var hits = Physics.RaycastAll(ray, 2f);
					foreach (var hit in hits)
					{
						var deformables = hit.collider.GetComponents<Deformable>();
						if (!deformables.Any())
							deformables = GetDeformableRecursiveUp(hit.collider.transform.parent);
						
						foreach (var deformable in deformables)
						{
							//deformable.Repair(0.1f, hit.point, 0.4f);
							
							var type = deformable.GetType();
							var baseVerticesField = type.GetField("baseVertices", BindingFlags.NonPublic | BindingFlags.Instance);
							var verticesField = deformable.GetType().GetField("vertices", BindingFlags.NonPublic | BindingFlags.Instance);
							var meshUpdateField = deformable.GetType().GetField("meshUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
						
							var baseVertices = (Vector3[])baseVerticesField.GetValue(deformable);
							var vertices = (Vector3[])verticesField.GetValue(deformable);
						
							for (var i = 0; i < vertices.Length; ++i)
							{
								var d = Vector3.Distance(hit.point, deformable.meshFilter.transform.TransformPoint(baseVertices[i]));
								if (d < repairArea)
								{
									var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
									GameObject.Destroy(go.GetComponent<Collider>());
									GameObject.Destroy(go.GetComponent<Rigidbody>());
									go.transform.position = deformable.meshFilter.transform.TransformPoint(baseVertices[i]);
									go.transform.localScale = new Vector3(0.04f, 0.04f, 0.04f);
									GameObject.Destroy(go, 2f);
								}
								var repairAmount = Mathf.Clamp01(repairArea - d); // fix 20 cm area
								vertices[i] += (baseVertices[i] - vertices[i]) * repairAmount;
							}
						
							verticesField.SetValue(deformable,vertices);

							// set mesh update true so the mesh gets updated
							meshUpdateField.SetValue(deformable, true);
						}
					}
				}
			}
			catch (Exception e)
			{
				ModConsole.Error(e.ToString());
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
	}
}

