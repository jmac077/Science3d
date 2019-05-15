using UnityEngine;
using System.Collections;

public class SphereCollide : MonoBehaviour {
	// Update is called once per frame
	public Transform Stone;
	public Transform Fire;
	public Transform Energy;
	public Transform Mud;
	public Transform Life;
	public Transform Egg;
	public Transform Lizard;
	public Transform Tree;
	void Update () {
	
	}
	void OnCollisionEnter (Collision col)
	{
		ContactPoint contact = col.contacts[0];
		if (this.CompareTag ("Fire") && col.gameObject.CompareTag ("Air")) {
			Destroy (col.gameObject);
			Instantiate (Energy, contact.point, Quaternion.identity);
		}else if (this.CompareTag ("Fire") && col.gameObject.CompareTag ("Water")) {
			Destroy (col.gameObject);
		}else if (this.CompareTag ("Air") && col.gameObject.CompareTag ("Fire")) {
			Destroy (col.gameObject);
		}else if (this.CompareTag ("Earth") && col.gameObject.CompareTag ("Water")) {
			Destroy (col.gameObject);
			Instantiate (Mud, contact.point, Quaternion.identity);
		}else if (this.CompareTag ("Earth") && col.gameObject.CompareTag ("Egg")) {
			Destroy (col.gameObject);
			Instantiate (Lizard, contact.point, Quaternion.identity);
		}else if (this.CompareTag ("Water") && col.gameObject.CompareTag ("Fire")) {
			Destroy (col.gameObject);
		}else if (this.CompareTag ("Water") && col.gameObject.CompareTag ("Earth")) {
			Destroy (col.gameObject);
		}else if (this.CompareTag ("Energy") && col.gameObject.CompareTag ("Mud")) {
			Destroy (col.gameObject);
			Instantiate (Life, contact.point, Quaternion.identity);
		}else if (this.CompareTag ("Mud") && col.gameObject.CompareTag ("Energy")) {
			Destroy (col.gameObject);
		}else if (this.CompareTag ("Life") && col.gameObject.CompareTag ("Stone")) {
			Destroy (col.gameObject);
			Instantiate (Egg, contact.point, Quaternion.identity);
		}else if (this.CompareTag ("Stone") && col.gameObject.CompareTag ("Life")) {
			Destroy (col.gameObject);
		}else if (this.CompareTag ("Egg") && col.gameObject.CompareTag ("Earth")) {
			Destroy (col.gameObject);
		}else if (this.CompareTag ("Lava") && col.gameObject.CompareTag ("Air")) {
			Destroy (col.gameObject);
			Instantiate (Fire, contact.point, Quaternion.identity);
		}else if (this.CompareTag ("Air") && col.gameObject.CompareTag ("Lava")) {
			Destroy (col.gameObject);
		}
		else if (this.CompareTag ("Water") && col.gameObject.CompareTag ("Lava")) {
			Destroy (col.gameObject);
			Instantiate (Stone, contact.point, Quaternion.identity);
		}
		else if (this.CompareTag ("Lava") && col.gameObject.CompareTag ("Water")) {
			Destroy (col.gameObject);
		}
		else if (this.CompareTag ("Life") && col.gameObject.CompareTag ("Water")) {
			Destroy (col.gameObject);
			Instantiate (Tree, contact.point, Quaternion.identity);
		}
		else if (this.CompareTag ("Water") && col.gameObject.CompareTag ("Life")) {
			Destroy (col.gameObject);
		}
	}
}
