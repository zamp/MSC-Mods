using System;
using System.Collections.Generic;
using System.Deployment.Internal;
using System.IO;
using MSCLoader;
using MSCStill;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TrunkSnap
{
	/// <summary>
	/// Trunk snap behaviour
	/// </summary>
	public class TrunkSnapBehaviour : MonoBehaviour
	{
		public class SaveData
		{
			public float posX, posY, posZ, rotX, rotY, rotZ;
		}

		private readonly Dictionary<Collider, Joint> _joints = new Dictionary<Collider, Joint>();

		void Start()
		{
			var trigger = gameObject.AddComponent<TriggerCallback>();
			trigger.onTriggerStay += OnBagTriggerStay;
		}

		private void OnBagTriggerStay(Collider collider)
		{
			// bail out if this collider was connected or it has a parent (carried in hand)
			if (_joints.ContainsKey(collider) || collider.transform.parent != null)
				return;

			// bail out if collider object has no rigidbody
			var rigidbody = collider.GetComponent<Rigidbody>();
			if (rigidbody == null)
				rigidbody = collider.GetComponentInParent<Rigidbody>();
			if (rigidbody == null)
			{
#if DEBUG
				ModConsole.Print("no rigidbody");
#endif
				return;
			}

			// bail if rigidbody is still moving
			if (rigidbody.velocity.sqrMagnitude > 0.001f)
			{
#if DEBUG
				ModConsole.Print("still moving");
#endif
				return;
			}

			// all good attach to car and disable collision
			var joint = transform.parent.gameObject.AddComponent<FixedJoint>();
			joint.connectedBody = collider.attachedRigidbody;
			joint.enableCollision = false;
			joint.breakForce = 5000f;
			_joints.Add(collider, joint);

#if DEBUG
			ModConsole.Print($"connected {collider.gameObject.name}");
#endif
		}

		void Update()
		{
			RemoveJoints();
		}

		private void RemoveJoints()
		{
			// remove any joints that now have parents (was picked up by player)
			var purgeList = new Queue<Collider>();
			foreach (var keypair in _joints)
			{
				// was this removed?
				if (keypair.Key == null || keypair.Key.transform.parent != null)
				{
#if DEBUG
					ModConsole.Print($"disconnected {keypair.Key?.gameObject.name}");
#endif
					Destroy(keypair.Value);
					purgeList.Enqueue(keypair.Key);
				}
			}

			while (purgeList.Count > 0)
			{
				_joints.Remove(purgeList.Dequeue());
			} 
		}
	}
}