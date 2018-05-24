using System;
using MSCLoader;
using UnityEngine;

//Standard unity MonoBehaviour class
namespace MSCStill
{
	public class TriggerCallback : MonoBehaviour
	{
		public Action<Collider> onTriggerEnter, onTriggerExit, onTriggerStay;

		void OnTriggerEnter(Collider other)
		{
			onTriggerEnter.Invoke(other);
		}

		void OnTriggerExit(Collider other)
		{
			onTriggerExit.Invoke(other);
		}

		void OnTriggerStay(Collider other)
		{
			onTriggerStay.Invoke(other);
		}
	}
}
