using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class TreeStuff : MonoBehaviour {
	private ErosionSim world;
	public static List<GameObject> openTrees = new List<GameObject>();
	float y;
	// Use this for initialization
	void Start () {
		openTrees.Add(this.gameObject);
		transform.Rotate(270,0,0);
		world = GameObject.Find("ErosionSim").GetComponent<ErosionSim>();
		y = world.getHeight(((this.transform.position.x+256)/511),((this.transform.position.z+256)/511));
	}
	
	// Update is called once per frame
	void Update () {
		if((y >= this.transform.position.y)&&this.GetComponent<Collider>().attachedRigidbody.useGravity){
			this.GetComponent<Collider>().attachedRigidbody.useGravity  = false;
			//Debug.Log("hello");
			this.GetComponent<Collider>().attachedRigidbody.velocity = new Vector3(0,0,0);
			openTrees.Add(this.gameObject);
		}
		if(world.onLavaOrWater((transform.position.x+256)/511,(transform.position.z+256)/511) && !this.GetComponent<Collider>().attachedRigidbody.useGravity){
			//Debug.Log("is kill");
			kill();
		}
		//check if tree is 25 units near water
	}
	void kill(){
			openTrees.Remove(gameObject);
			Destroy (gameObject);
	}
}
