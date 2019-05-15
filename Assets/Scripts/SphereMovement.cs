using UnityEngine;
using System.Collections;

public class SphereMovement : MonoBehaviour {
	bool clicked = false;
	bool hidden = false;
	public static bool block = false;
	public static string element;
	private ErosionSim world;
	public Transform Lizard;
	float y;
	void Start() {
		world = GameObject.Find("ErosionSim").GetComponent<ErosionSim>();
        if(this.CompareTag ("Egg")){
        	y = world.getHeight(((this.transform.position.x+256)/511),((this.transform.position.z+256)/511));
        	y += 2f;
        }
    }
	void Update () {
		if(clicked){
			Vector3 temp = Input.mousePosition;
			temp.z = 20f; // Set this to be the distance you want the object to be placed in front of the camera.
			this.transform.position = Camera.main.ScreenToWorldPoint(temp);
		}
		if(clicked && Input.GetKeyDown("left shift")){
			this.GetComponent<MeshRenderer>().enabled = false;
			hidden = true;
			block = true;
			element = this.tag;
		}
		else if(hidden == true && Input.GetKeyDown("left shift")){
			this.GetComponent<MeshRenderer>().enabled = true;
			hidden = false;
			block = false;
		}
		if(hidden == true && Input.GetMouseButtonDown(1)){
			block = false;
			Destroy (gameObject);
		}
		if(this.CompareTag ("Egg") && (y >= this.transform.position.y)){
			this.GetComponent<Collider>().attachedRigidbody.useGravity  = false;
			//Debug.Log("hello");
			this.GetComponent<Collider>().attachedRigidbody.velocity = new Vector3(0,0,0);
			Instantiate (Lizard, this.transform.position, Quaternion.identity);
			Destroy (gameObject);
		}
	}

	void OnMouseDown(){
		if (!clicked)
			clicked = true;
		else if (clicked && !hidden)
			clicked = false;
	}

	public bool getBlock(){
		return block;
	}

	public string getElement(){
		return element;
	}
}
