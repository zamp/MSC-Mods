using System;
using MSCLoader;
using UnityEngine;

namespace DeveloperToolset
{
	public class DeveloperToolset : Mod
	{
		public override string ID => "DeveloperToolset";
		public override string Name => "DeveloperToolset";
		public override string Author => "zamp";
		public override string Version => "1.0";

		public Keybind showGui = new Keybind("showgui", "Show GUI", KeyCode.Z, KeyCode.LeftControl);
		public Keybind copy = new Keybind("copy", "Copy", KeyCode.C, KeyCode.LeftControl);
		public Keybind paste = new Keybind("paste", "Paste", KeyCode.V, KeyCode.LeftControl);
		public Keybind raycastTweakable = new Keybind("raycastTweakable", "Raycast tweakable", KeyCode.G, KeyCode.LeftControl);
		private Transform m_copy;
		private string m_copiedStr;
		private float m_copiedStrTime;

		public override void OnLoad()
		{
			Keybind.Add(this, showGui);
			Keybind.Add(this, raycastTweakable);
		}
		
		public override void Update()
		{
			if (showGui.IsDown())
				Inspector.showGUI = !Inspector.showGUI;

			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			Physics.Raycast(ray, out hit);
			if (hit.collider)
			{
				if (raycastTweakable.IsPressed())
				{
					Inspector.SetInspect(hit.collider.transform);
				}
				if (copy.IsDown())
				{
					m_copy = hit.collider.transform;
					m_copiedStr = m_copy.name + " copied!";
					m_copiedStrTime = 2f;
				}
				if (paste.IsDown())
				{
					var pos = Camera.main.transform.position + Camera.main.transform.forward * 2f;
					var clone = GameObject.Instantiate(m_copy.gameObject);
					clone.transform.position = pos;
				}
			}
		}

		private Transform FindClosesBoneTo(Transform transform, Vector3 point)
		{
			// go through children
			var d = Vector3.Distance(transform.position, point);
			foreach (Transform t in transform)
			{
				// is closer to point than parent
				if (Vector3.Distance(t.position, point) < d)
				{
					// return whatever child returns
					return FindClosesBoneTo(t, point);
				}
			}
			return transform;
		}

		public override void OnGUI()
		{
			if (m_copiedStrTime > 0)
			{
				m_copiedStrTime -= Time.deltaTime;
				GUI.Label(new Rect(Screen.width/2, Screen.height / 2, 200, 40), m_copiedStr);
			}
			Inspector.OnGUI();
		}
	}
}
