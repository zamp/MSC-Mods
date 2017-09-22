using UnityEngine;
using System.Collections;

public class fixer : MonoBehaviour 
{

	// Use this for initialization
	void Start () {
		var rb = GetComponent<Rigidbody>();
		rb.detectCollisions = false;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
