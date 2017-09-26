using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HutongGames.PlayMaker;
using MSCLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeveloperToolset
{
	public static class Inspector
	{
		public static bool showGUI;
		private static List<Transform> m_rootTransforms = new List<Transform>();
		private static Dictionary<Transform, bool> m_hierarchyOpen = new Dictionary<Transform, bool>();
		private static bool m_filterItemX;
		private static Vector2 m_hierarchyScrollPosition;
		private static Transform m_inspect;
		private static Vector2 m_inspectScrollPosition;
		private static string m_search = "";

		private readonly static Dictionary<PlayMakerFSM, FsmToggle> m_fsmToggles = new Dictionary<PlayMakerFSM, FsmToggle>();
		private static bool m_bindingFlagPublic;
		private static bool m_bindingFlagNonPublic;
		private static StreamWriter m_dumpStream;

		private class FsmToggle
		{
			public bool showVars;
			public bool showStates;
			public bool showEvents;
			public bool showGlobalTransitions;
		}

		public static void DumpAll()
		{
			try
			{
				Search("");
				m_dumpStream = new StreamWriter(Path.Combine(ModLoader.ModsFolder, "GameObject Dump.txt"));
				foreach (var rootTransform in m_rootTransforms)
				{
					DumpData(0, rootTransform);
				}
				m_dumpStream.Close();
			}
			catch (Exception e)
			{
				m_dumpStream.Close();
				ModConsole.Error(e.ToString());
			}
		}

		private static void Write(int tabCount, string str)
		{
			var tabs = "";
			for (var i = 0; i < tabCount; ++i)
				tabs += "\t";
			m_dumpStream.WriteLine(tabs + str);
		}

		private static void DumpData(int tabCount, Transform trans)
		{
			Write(tabCount, trans.name);
			Write(tabCount, "Layer:" + LayerMask.LayerToName(trans.gameObject.layer));

			foreach (var comp in trans.GetComponents<Component>())
			{
				var type = comp.GetType();
				Write(tabCount + 1, type.ToString());

				if (comp is PlayMakerFSM)
				{
					DumpPlayMakerFsm(tabCount + 1, comp);
				}
				else
				{
					DumpGenerics(tabCount + 1, comp, BindingFlags.Public);
				}
			}

			foreach (Transform t in trans)
			{
				DumpData(tabCount + 1, t);
			}
		}

		private static void DumpGenerics(int tabCount, Component comp, BindingFlags flags)
		{
			var fields = comp.GetType().GetFields(flags | BindingFlags.Instance);
			foreach (var fieldInfo in fields)
			{
				try
				{
					var fieldValue = fieldInfo.GetValue(comp);
					Write(tabCount, fieldInfo.Name + ": " + fieldValue);
				}
				catch (Exception)
				{
					Write(tabCount, fieldInfo.Name);
				}
			}
		}

		private static void DumpPlayMakerFsm(int tabCount, Component comp)
		{
			var fsm = comp as PlayMakerFSM;

			Write(tabCount, "Name: " + fsm.Fsm.Name);

			Write(tabCount, "Float");
			DumpFsmVariables(tabCount + 1, fsm.FsmVariables.FloatVariables);
			Write(tabCount, "Int");
			DumpFsmVariables(tabCount + 1, fsm.FsmVariables.IntVariables);
			Write(tabCount, "Bool");
			DumpFsmVariables(tabCount + 1, fsm.FsmVariables.BoolVariables);
			Write(tabCount, "String");
			DumpFsmVariables(tabCount + 1, fsm.FsmVariables.StringVariables);

			Write(tabCount, "GLOBAL TRANSITION");
			foreach (var trans in fsm.FsmGlobalTransitions)
			{
				Write(tabCount, "Event(" + trans.EventName + ") To State(" + trans.ToState + ")");
			}

			Write(tabCount, "STATES");

			foreach (var state in fsm.FsmStates)
			{
				Write(tabCount + 1, state.Name);
				Write(tabCount + 1, "Transitions:");
				foreach (var transition in state.Transitions)
				{
					Write(tabCount + 1, "Event(" + transition.EventName + ") To State(" + transition.ToState + ")");
				}

				Write(tabCount + 1, "Actions:");
				try
				{
					foreach (var action in state.Actions)
					{
						var typename = action.GetType().ToString();
						typename = typename.Substring(typename.LastIndexOf(".", StringComparison.Ordinal) + 1);
						Write(tabCount + 1, typename);

						var fields = action.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
						foreach (var fieldInfo in fields)
						{
							try
							{
								var fieldValue = fieldInfo.GetValue(action);
								var fieldValueStr = fieldValue.ToString();
								fieldValueStr = fieldValueStr.Substring(fieldValueStr.LastIndexOf(".", System.StringComparison.Ordinal) + 1);
								if (fieldValue is NamedVariable)
								{
									var named = fieldValue as NamedVariable;
									Write(tabCount + 2, fieldInfo.Name + ": " + fieldValueStr + "(" + named.Name + ")");
								}
								else if (fieldValue is FsmEvent)
								{
									var evnt = fieldValue as FsmEvent;
									Write(tabCount + 2, fieldInfo.Name + ": " + fieldValueStr + "(" + evnt.Name + ")");
								}
								else
								{
									Write(tabCount + 2, fieldInfo.Name + ": " + fieldValueStr);
								}
							}
							catch (Exception)
							{
								Write(tabCount + 2, fieldInfo.Name);
							}
						}
					}
				}
				catch (Exception)
				{
					Write(tabCount + 2, "ERROR!");
				}
			}
			

			Write(tabCount, "EVENTS");
			foreach (var evnt in fsm.FsmEvents)
			{
				Write(tabCount + 1, evnt.Name + ": " + evnt.Path);
			}
		}

		private static void DumpFsmVariables(int tabCount, IEnumerable<FsmFloat> variables)
		{
			foreach (var fsmFloat in variables)
			{
				Write(tabCount, fsmFloat.Name + ": " + fsmFloat.Value);
			}
		}

		private static void DumpFsmVariables(int tabCount, IEnumerable<FsmInt> variables)
		{
			foreach (var fsmFloat in variables)
			{
				Write(tabCount, fsmFloat.Name + ": " + fsmFloat.Value);
			}
		}

		private static void DumpFsmVariables(int tabCount, IEnumerable<FsmBool> variables)
		{
			foreach (var fsmFloat in variables)
			{
				Write(tabCount, fsmFloat.Name + ": " + fsmFloat.Value);
			}
		}

		private static void DumpFsmVariables(int tabCount, IEnumerable<FsmString> variables)
		{
			foreach (var fsmFloat in variables)
			{
				Write(tabCount, fsmFloat.Name + ": " + fsmFloat.Value);
			}
		}

		internal static void Search(string keyword)
		{	
			m_hierarchyOpen.Clear();
			// get all objs
			var objs = GameObject.FindObjectOfType<GameObject>();
			if (string.IsNullOrEmpty(keyword))
			{
				m_rootTransforms = Object.FindObjectsOfType<Transform>().Where(x => x.parent == null).ToList();
			}
			else
			{
				m_rootTransforms = Object.FindObjectsOfType<Transform>().Where(x => x.name.Contains(keyword)).ToList();
			}
			m_rootTransforms.Sort(TransformNameAscendingSort);
		}

		private static int TransformNameAscendingSort(Transform x, Transform y)
		{
			return string.Compare(x.name,y.name);
		}

		internal static void OnGUI()
		{
			try
			{
				if (showGUI)
				{
					// show hierarchy
					GUILayout.BeginArea(new Rect(0, 0, 600, Screen.height));
					GUILayout.BeginVertical("box");
					m_hierarchyScrollPosition = GUILayout.BeginScrollView(m_hierarchyScrollPosition, false, true, GUILayout.Width(600));

					GUILayout.Label("Hierarchy");
					m_search = GUILayout.TextField(m_search);
					if (GUILayout.Button("Search"))
						Search(m_search);

					m_filterItemX = GUILayout.Toggle(m_filterItemX, "Filter itemx");
					foreach (var rootTransform in m_rootTransforms)
					{
						ShowHierarchy(rootTransform);
					}

					GUILayout.EndScrollView();
					GUILayout.EndVertical();
					GUILayout.EndArea();

					if (m_inspect != null)
					{
						GUILayout.BeginArea(new Rect(Screen.width - 300, 0, 300, Screen.height));
						m_inspectScrollPosition = GUILayout.BeginScrollView(m_inspectScrollPosition, false, true, GUILayout.Width(300));
						GUILayout.Label(m_inspect.name);
						if (GUILayout.Button("Close"))
							m_inspect = null;
						ShowInspect(m_inspect);
						GUILayout.EndScrollView();
						GUILayout.EndArea();
					}
				}
			}
			catch (Exception e)
			{
				ModConsole.Print(e.ToString());
			}
		}

		private static void ShowInspect(Transform trans)
		{
			if (trans != null)
			{
				if (trans.parent != null && GUILayout.Button("Parent"))
				{
					m_inspect = trans.parent;
					return;
				}
				trans.gameObject.SetActive(GUILayout.Toggle(trans.gameObject.activeSelf, "Is active"));
				GUILayout.Label("Layer:" + LayerMask.LayerToName(trans.gameObject.layer));
				GUILayout.BeginVertical("box");

				m_bindingFlagPublic = GUILayout.Toggle(m_bindingFlagPublic, "Show public");
				m_bindingFlagNonPublic = GUILayout.Toggle(m_bindingFlagNonPublic, "Show non-public");

				BindingFlags flags = m_bindingFlagPublic ? BindingFlags.Public : BindingFlags.Default;
				flags |= m_bindingFlagNonPublic ? BindingFlags.NonPublic : BindingFlags.Default;

				foreach (var comp in trans.GetComponents<Component>())
				{
					var type = comp.GetType();
					GUILayout.Label(type.ToString());

					if (comp is Transform)
					{
						TransformGUI(comp);
					}
					else if (comp is PlayMakerFSM)
					{
						FSMGUI(comp);
					}
					else if (comp is Light)
					{
						LightGUI(comp as Light);
					}
					else
					{
						GenericsGUI(comp,flags);
					}
				}
				GUILayout.EndVertical();
			}
		}

		private static void LightGUI(Light light)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Shadow bias:");
			light.shadowBias = (float) Convert.ToDouble(GUILayout.TextField(light.shadowBias.ToString()));
			GUILayout.EndHorizontal();
		}

		private static void GenericsGUI(Component comp, BindingFlags flags)
		{
			var fields = comp.GetType().GetFields(flags | BindingFlags.Instance);
			GUILayout.BeginHorizontal();
			GUILayout.Space(20);
			GUILayout.BeginVertical("box");
			foreach (var fieldInfo in fields)
			{
				GUILayout.BeginHorizontal();
				try
				{
					var fieldValue = fieldInfo.GetValue(comp);
					var fieldValueStr = fieldValue.ToString();
					if (fieldValue is bool)
					{
						GUILayout.Label(fieldInfo.Name);
						var val = GUILayout.Toggle((bool) fieldValue, fieldInfo.Name);
						fieldInfo.SetValue(comp, val);
					}
					else if (fieldValue is string)
					{
						GUILayout.Label(fieldInfo.Name);
						var val = GUILayout.TextField((string) fieldValue);
						fieldInfo.SetValue(comp, val);
					}
					else if (fieldValue is int)
					{
						GUILayout.Label(fieldInfo.Name);
						var val = Convert.ToInt32(GUILayout.TextField(fieldValue.ToString()));
						fieldInfo.SetValue(comp, val);
					}
					else if (fieldValue is float)
					{
						GUILayout.Label(fieldInfo.Name);
						var val = (float) Convert.ToDouble(GUILayout.TextField(fieldValue.ToString()));
						fieldInfo.SetValue(comp, val);
					}
					else
					{
						GUILayout.Label(fieldInfo.Name + ": " + fieldValueStr);
					}
				}
				catch (Exception)
				{
					GUILayout.Label(fieldInfo.Name);
				}
				//fieldInfo.SetValue(fieldInfo.Name, GUILayout.TextField(fieldInfo.GetValue(fieldInfo.Name).ToString()));
				GUILayout.EndHorizontal();
			}
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
		}

		private static void FSMGUI(Component comp)
		{
			var fsm = comp as PlayMakerFSM;

			GUILayout.BeginHorizontal();
			GUILayout.Space(20);
			GUILayout.BeginVertical("box");

			GUILayout.Label("Name: " + fsm.Fsm.Name);

			SetFsmVarsFor(fsm, GUILayout.Toggle(ShowFsmVarsFor(fsm), "Show Variables"));
			if (ShowFsmVarsFor(fsm))
			{
				GUILayout.Label("Float");
				ListFsmVariables(fsm.FsmVariables.FloatVariables);
				GUILayout.Label("Int");
				ListFsmVariables(fsm.FsmVariables.IntVariables);
				GUILayout.Label("Bool");
				ListFsmVariables(fsm.FsmVariables.BoolVariables);
				GUILayout.Label("String");
				ListFsmVariables(fsm.FsmVariables.StringVariables);
				GUILayout.Label("Vector2");
				ListFsmVariables(fsm.FsmVariables.Vector2Variables);
				GUILayout.Label("Vector3");
				ListFsmVariables(fsm.FsmVariables.Vector3Variables);
				GUILayout.Label("Rect");
				ListFsmVariables(fsm.FsmVariables.RectVariables);
				GUILayout.Label("Quaternion");
				ListFsmVariables(fsm.FsmVariables.QuaternionVariables);
				GUILayout.Label("Color");
				ListFsmVariables(fsm.FsmVariables.ColorVariables);
				GUILayout.Label("GameObject");
				ListFsmVariables(fsm.FsmVariables.GameObjectVariables);
				GUILayout.Label("Material");
				ListFsmVariables(fsm.FsmVariables.MaterialVariables);
				GUILayout.Label("Texture");
				ListFsmVariables(fsm.FsmVariables.TextureVariables);
				GUILayout.Label("Object");
				ListFsmVariables(fsm.FsmVariables.ObjectVariables);
			}

			SetFsmGlobalTransitionFor(fsm, GUILayout.Toggle(ShowFsmGlobalTransitionFor(fsm), "Show Global Transition"));
			if (ShowFsmGlobalTransitionFor(fsm))
			{
				GUILayout.Space(20);
				GUILayout.Label("Global transitions");
				foreach (var trans in fsm.FsmGlobalTransitions)
				{
					GUILayout.Label("Event(" + trans.EventName + ") To State(" + trans.ToState + ")");
				}
			}

			SetFsmStatesFor(fsm, GUILayout.Toggle(ShowFsmStatesFor(fsm), "Show States"));
			if (ShowFsmStatesFor(fsm))
			{
				GUILayout.Space(20);
				GUILayout.Label("States");
				foreach (var state in fsm.FsmStates)
				{
					GUILayout.Label(state.Name + (fsm.ActiveStateName == state.Name ? "(active)" : ""));
					GUILayout.BeginHorizontal();
					GUILayout.Space(20);
					GUILayout.BeginVertical("box");
					GUILayout.Label("Transitions:");
					foreach (var transition in state.Transitions)
					{
						GUILayout.Label("Event(" + transition.EventName + ") To State(" + transition.ToState + ")");
					}
					GUILayout.Space(20);

					GUILayout.Label("Actions:");
					foreach (var action in state.Actions)
					{
						var typename = action.GetType().ToString();
						typename = typename.Substring(typename.LastIndexOf(".", StringComparison.Ordinal) + 1);
						GUILayout.Label(typename);

						var fields = action.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
						GUILayout.BeginHorizontal();
						GUILayout.Space(20);
						GUILayout.BeginVertical("box");
						foreach (var fieldInfo in fields)
						{
							GUILayout.BeginHorizontal();
							try
							{
								var fieldValue = fieldInfo.GetValue(action);
								var fieldValueStr = fieldValue.ToString();
								fieldValueStr = fieldValueStr.Substring(fieldValueStr.LastIndexOf(".", System.StringComparison.Ordinal) + 1);
								if (fieldValue is FsmProperty)
								{
									var property = fieldValue as FsmProperty;
									GUILayout.Label(fieldInfo.Name + ": (" + property.PropertyName + ")");
									GUILayout.Label("target: " + property.TargetObject + "");
								}
								else if (fieldValue is NamedVariable)
								{
									var named = fieldValue as NamedVariable;
									GUILayout.Label(fieldInfo.Name + ": " + fieldValueStr + "(" + named.Name + ")");
								}
								else if (fieldValue is FsmEvent)
								{
									var evnt = fieldValue as FsmEvent;
									GUILayout.Label(fieldInfo.Name + ": " + fieldValueStr + "(" + evnt.Name + ")");
								}
								else
								{
									GUILayout.Label(fieldInfo.Name + ": " + fieldValueStr);
								}
							}
							catch (Exception)
							{
								GUILayout.Label(fieldInfo.Name);
							}
							//fieldInfo.SetValue(fieldInfo.Name, GUILayout.TextField(fieldInfo.GetValue(fieldInfo.Name).ToString()));
							GUILayout.EndHorizontal();
						}
						GUILayout.EndVertical();
						GUILayout.EndHorizontal();
					}

					GUILayout.Label("ActionData:");
					var fields2 = state.ActionData.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
					GUILayout.BeginHorizontal();
					GUILayout.Space(20);
					GUILayout.BeginVertical("box");
					foreach (var fieldInfo in fields2)
					{
						GUILayout.BeginHorizontal();
						try
						{
							var fieldValue = fieldInfo.GetValue(state.ActionData);
							var fieldValueStr = fieldValue.ToString();
							fieldValueStr = fieldValueStr.Substring(fieldValueStr.LastIndexOf(".", System.StringComparison.Ordinal) + 1);
							if (fieldValue is NamedVariable)
							{
								var named = fieldValue as NamedVariable;
								GUILayout.Label(fieldInfo.Name + ": " + fieldValueStr + "(" + named.Name + ")");
							}
							else if (fieldValue is FsmEvent)
							{
								var evnt = fieldValue as FsmEvent;
								GUILayout.Label(fieldInfo.Name + ": " + fieldValueStr + "(" + evnt.Name + ")");
							}
							else
							{
								GUILayout.Label(fieldInfo.Name + ": " + fieldValueStr);
							}
						}
						catch (Exception)
						{
							GUILayout.Label(fieldInfo.Name);
						}
						//fieldInfo.SetValue(fieldInfo.Name, GUILayout.TextField(fieldInfo.GetValue(fieldInfo.Name).ToString()));
						GUILayout.EndHorizontal();
					}
					GUILayout.EndVertical();
					GUILayout.EndHorizontal();

					GUILayout.EndVertical();
					GUILayout.EndHorizontal();
				}
			}

			SetFsmEventsFor(fsm, GUILayout.Toggle(ShowFsmEventsFor(fsm), "Show Events"));
			if (ShowFsmEventsFor(fsm))
			{
				GUILayout.Space(20);
				GUILayout.Label("Events");
				foreach (var evnt in fsm.FsmEvents)
				{
					GUILayout.BeginHorizontal();
					GUILayout.Label(evnt.Name + ": " + evnt.Path);
					if (GUILayout.Button("Send"))
					{
						fsm.SendEvent(evnt.Name);
					}
					GUILayout.EndHorizontal();
				}
			}

			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
		}

		private static void SetFsmGlobalTransitionFor(PlayMakerFSM fsm, bool p)
		{
			if (!m_fsmToggles.ContainsKey(fsm))
				m_fsmToggles.Add(fsm, new FsmToggle());
			m_fsmToggles[fsm].showGlobalTransitions = p;
		}

		private static bool ShowFsmGlobalTransitionFor(PlayMakerFSM fsm)
		{
			if (!m_fsmToggles.ContainsKey(fsm))
				m_fsmToggles.Add(fsm, new FsmToggle());
			return m_fsmToggles[fsm].showGlobalTransitions;
		}


		private static void SetFsmStatesFor(PlayMakerFSM fsm, bool p)
		{
			if (!m_fsmToggles.ContainsKey(fsm))
				m_fsmToggles.Add(fsm, new FsmToggle());
			m_fsmToggles[fsm].showStates = p;
		}

		private static bool ShowFsmStatesFor(PlayMakerFSM fsm)
		{
			if (!m_fsmToggles.ContainsKey(fsm))
				m_fsmToggles.Add(fsm, new FsmToggle());
			return m_fsmToggles[fsm].showStates;
		}

		private static void SetFsmEventsFor(PlayMakerFSM fsm, bool p)
		{
			if (!m_fsmToggles.ContainsKey(fsm))
				m_fsmToggles.Add(fsm, new FsmToggle());
			m_fsmToggles[fsm].showEvents = p;
		}

		private static bool ShowFsmEventsFor(PlayMakerFSM fsm)
		{
			if (!m_fsmToggles.ContainsKey(fsm))
				m_fsmToggles.Add(fsm, new FsmToggle());
			return m_fsmToggles[fsm].showEvents;
		}

		private static void SetFsmVarsFor(PlayMakerFSM fsm, bool p)
		{
			if (!m_fsmToggles.ContainsKey(fsm))
				m_fsmToggles.Add(fsm, new FsmToggle());
			m_fsmToggles[fsm].showVars = p;
		}

		private static bool ShowFsmVarsFor(PlayMakerFSM fsm)
		{
			if (!m_fsmToggles.ContainsKey(fsm))
				m_fsmToggles.Add(fsm, new FsmToggle());
			return m_fsmToggles[fsm].showVars;
		}

		private static void TransformGUI(Component comp)
		{
			GUILayout.Label("Tag:" + comp.gameObject.tag);

			var t = (Transform)comp;
			GUILayout.Label("localPosition:");
			GUILayout.BeginHorizontal();
			var pos = t.localPosition;
			pos.x = (float)Convert.ToDouble(GUILayout.TextField(pos.x.ToString()));
			pos.y = (float)Convert.ToDouble(GUILayout.TextField(pos.y.ToString()));
			pos.z = (float)Convert.ToDouble(GUILayout.TextField(pos.z.ToString()));
			t.localPosition = pos;
			GUILayout.EndHorizontal();

			GUILayout.Label("localRotation:");
			GUILayout.BeginHorizontal();
			pos = t.localRotation.eulerAngles;
			pos.x = (float)Convert.ToDouble(GUILayout.TextField(pos.x.ToString()));
			pos.y = (float)Convert.ToDouble(GUILayout.TextField(pos.y.ToString()));
			pos.z = (float)Convert.ToDouble(GUILayout.TextField(pos.z.ToString()));
			t.localRotation = Quaternion.Euler(pos);
			GUILayout.EndHorizontal();

			GUILayout.Label("localScale:");
			GUILayout.BeginHorizontal();
			pos = t.localScale;
			pos.x = (float)Convert.ToDouble(GUILayout.TextField(pos.x.ToString()));
			pos.y = (float)Convert.ToDouble(GUILayout.TextField(pos.y.ToString()));
			pos.z = (float)Convert.ToDouble(GUILayout.TextField(pos.z.ToString()));
			t.localScale = pos;

			t.gameObject.isStatic = false;
			GUILayout.EndHorizontal();
		}

		private static void ListFsmVariables(IEnumerable<FsmFloat> variables)
		{
			foreach (var fsmFloat in variables)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(fsmFloat.Name);
				fsmFloat.Value = (float) Convert.ToDouble(GUILayout.TextField(fsmFloat.Value.ToString()));
				GUILayout.EndHorizontal();
			}
		}

		private static void ListFsmVariables(IEnumerable<FsmBool> variables)
		{
			foreach (var fsmBool in variables)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(fsmBool.Name + ": " + fsmBool.Value);
				fsmBool.Value = GUILayout.Toggle(fsmBool.Value, "");
				GUILayout.EndHorizontal();
			}
		}

		private static void ListFsmVariables(IEnumerable<FsmString> variables)
		{
			foreach (var fsmFloat in variables)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(fsmFloat.Name);
				fsmFloat.Value = GUILayout.TextField(fsmFloat.Value);
				GUILayout.EndHorizontal();
			}
		}

		private static void ListFsmVariables(IEnumerable<FsmInt> variables)
		{
			foreach (var fsmFloat in variables)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(fsmFloat.Name);
				fsmFloat.Value = Convert.ToInt32(GUILayout.TextField(fsmFloat.Value.ToString()));
				GUILayout.EndHorizontal();
			}
		}

		private static void ListFsmVariables(IEnumerable<FsmColor> variables)
		{
			foreach (var fsmFloat in variables)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(fsmFloat.Name + ": " + fsmFloat.Value);
				GUILayout.EndHorizontal();
			}
		}

		private static void ListFsmVariables(IEnumerable<FsmGameObject> variables)
		{
			foreach (var fsmFloat in variables)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(fsmFloat.Name + ": " + fsmFloat.Value);
				GUILayout.EndHorizontal();
			}
		}

		private static void ListFsmVariables(IEnumerable<FsmVector2> variables)
		{
			foreach (var fsmFloat in variables)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(fsmFloat.Name + ": " + fsmFloat.Value);
				GUILayout.EndHorizontal();
			}
		}

		private static void ListFsmVariables(IEnumerable<FsmVector3> variables)
		{
			foreach (var fsmFloat in variables)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(fsmFloat.Name + ": " + fsmFloat.Value);
				GUILayout.EndHorizontal();
			}
		}

		private static void ListFsmVariables(IEnumerable<FsmRect> variables)
		{
			foreach (var fsmFloat in variables)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(fsmFloat.Name + ": " + fsmFloat.Value);
				GUILayout.EndHorizontal();
			}
		}

		private static void ListFsmVariables(IEnumerable<FsmQuaternion> variables)
		{
			foreach (var fsmFloat in variables)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(fsmFloat.Name + ": " + fsmFloat.Value);
				GUILayout.EndHorizontal();
			}
		}

		private static void ListFsmVariables(IEnumerable<FsmObject> variables)
		{
			foreach (var fsmFloat in variables)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(fsmFloat.Name + ": " + fsmFloat.Value);
				GUILayout.EndHorizontal();
			}
		}

		private static void ShowHierarchy(Transform trans)
		{
			if (m_filterItemX && trans.name.Contains("itemx"))
				return;

			if (!m_hierarchyOpen.ContainsKey(trans))
				m_hierarchyOpen.Add(trans,false);

			GUILayout.BeginHorizontal("box");

			GUILayout.Label(trans.name);
			if (GUILayout.Button("i", GUILayout.Width(20)))
			{
				m_inspect = trans;
			}
			var btn = GUILayout.Button(m_hierarchyOpen[trans] ? "<" : ">", GUILayout.Width(20));
			if (m_hierarchyOpen[trans] && btn)
				m_hierarchyOpen[trans] = false;
			else if (!m_hierarchyOpen[trans] && btn)
				m_hierarchyOpen[trans] = true;
			
			GUILayout.EndHorizontal();

			if (m_hierarchyOpen[trans])
			{
				GUILayout.BeginHorizontal("box");
				GUILayout.Space(20);
				GUILayout.BeginVertical();
			
				foreach (Transform t in trans)
				{
					ShowHierarchy(t);
				}

				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
			}
		}

		internal static void SetInspect(Transform transform)
		{
			m_inspect = transform;
			m_fsmToggles.Clear();
		}
	}
}
