using MSCLoader;
using UnityEngine;

namespace TrunkSnap
{
	public class TrunkSnap : Mod
	{
		public override string ID => "TrunkSnap";
		public override string Version => "0.2";
		public override string Author => "zamp";

		public override void OnLoad()
		{
			base.OnLoad();
			var satsuma = PlayMakerGlobals.Instance.Variables.GetFsmGameObject("TheCar").Value;

			var trigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
			trigger.transform.SetParent(satsuma.transform, false);
			trigger.name = "TrunkTrigger";

			Object.Destroy(trigger.GetComponent<Rigidbody>());
#if DEBUG
			trigger.GetComponent<MeshRenderer>().material.color = new Color(1f,0,0,0.2f);
#else
			Object.Destroy(trigger.GetComponent<MeshRenderer>());
			Object.Destroy(trigger.GetComponent<MeshFilter>());
#endif
			trigger.transform.localPosition = new Vector3(0f, 0.125f, -1.412f);
			trigger.transform.localScale = new Vector3(1.16479f, 0.365304f, 0.4123382f);

			trigger.GetComponent<Collider>().isTrigger = true;
			trigger.AddComponent<TrunkSnapBehaviour>();


			trigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
			trigger.transform.SetParent(satsuma.transform, false);
			trigger.name = "GlobeBoxTrigger";

			Object.Destroy(trigger.GetComponent<Rigidbody>());
#if DEBUG
			trigger.GetComponent<MeshRenderer>().material.color = new Color(1f, 0, 0, 0.2f);
#else
			Object.Destroy(trigger.GetComponent<MeshRenderer>());
			Object.Destroy(trigger.GetComponent<MeshFilter>());
#endif
			trigger.transform.localPosition = new Vector3(0.321f, 0.3f, 0.62f);
			trigger.transform.localScale = new Vector3(0.3f, 0.15f, 0.1f);

			trigger.GetComponent<Collider>().isTrigger = true;
			trigger.AddComponent<TrunkSnapBehaviour>();
		}
	}
}
