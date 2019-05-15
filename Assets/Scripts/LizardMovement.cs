using UnityEngine;
using System.Collections;
using System;

public class LizardMovement : MonoBehaviour {
	private ErosionSim world;
	//private TreeStuff treeStuff;
	private float timer = 0f;
	private GameObject tree = null;
	private Vector3 treePos;
	public static int numOfLizards = 0;
	float y;
	float yPrime;
	int count = 30;
	// Use this for initialization
	void Start () {
		numOfLizards++;
		world = GameObject.Find("ErosionSim").GetComponent<ErosionSim>();
		//treeStuff = GameObject.Find("TreeStuff").GetComponent<TreeStuff>();
	}
	
	// Update is called once per frame
	void Update () {
		timer += Time.deltaTime;
		if(timer >= 3.2 && TreeStuff.openTrees.Count>=1 && tree == null){
			treePos = findTree();
		}else if(timer >= 3.2 && tree != null){
			GetComponent<Animator>().SetBool("isMoving",true);
			transform.LookAt(treePos);
			transform.position = Vector3.MoveTowards(transform.position, treePos, 5*Time.deltaTime);
			Vector3 temp =  transform.position;
			temp.y = world.getHeight((transform.position.x+256)/511,(transform.position.z+256)/511);
			transform.position = temp;
		}else if(timer >= 3.2){
			GetComponent<Animator>().SetBool("isMoving",false);
		}
		//xAngle();
		if(world.onLavaOrWater((transform.position.x+256)/511,(transform.position.z+256)/511)){
			//Debug.Log("is kill");
			kill();
		}
		//check every 30 seconds to see if lizard is near tree
		if(timer >= count && tree != null){
			if(transform.position.x != treePos.x && transform.position.z != treePos.z){
				kill();
			}
			count += 30;
		}else if(timer >= count){
			kill();
		}
	}

	void xAngle(){
		y = world.getHeight(((this.transform.position.x+256)/511),((this.transform.position.z+256)/511));
		yPrime = world.getHeight(((this.transform.position.x+256)/511),((this.transform.position.z+256+2.5f)/511));
		y = Math.Min((float)Math.Asin((yPrime-y)/2.5),1f);
		y = Math.Max(y,-1f);
		Debug.Log(y);
		//transform.Rotate((float)Math.Asin(y),0,0);
	}
	Vector3 findTree(){
		tree = TreeStuff.openTrees[0];
		TreeStuff.openTrees.RemoveAt(0);
		return tree.transform.position;
	}

	void kill(){
		if(tree != null){
				TreeStuff.openTrees.Add(tree);
			}
			numOfLizards--;
			Destroy (gameObject);
	}
}
