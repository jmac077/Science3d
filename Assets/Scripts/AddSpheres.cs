using UnityEngine;
using System.Collections;
public class AddSpheres : MonoBehaviour {

	public Transform LavaSphere;
	public Transform AirSphere;
	public Transform EarthSphere;
	public Transform WaterSphere;
	public Transform StoneSphere;
	public Transform MudSphere;
	public float y;

	public void Water(float x, float z){
		Vector3 spherePos;
		spherePos.x = x;
		spherePos.z = z;
		//set y to height in texture plus 2
		spherePos.y = y;
		Instantiate (WaterSphere, spherePos, Quaternion.identity);
	}

	public void Lava(float x, float z){
		Vector3 spherePos;
		spherePos.x = x;
		spherePos.z = z;
		//set y to height in texture plus 2
		spherePos.y = y;
		Instantiate (LavaSphere, spherePos, Quaternion.identity);
	}

	public void Earth(float x, float z){
		Vector3 spherePos;
		spherePos.x = x;
		spherePos.z = z;
		//set y to height in texture plus 2
		spherePos.y = y;
		Instantiate (EarthSphere, spherePos, Quaternion.identity);
	}

	public void Air(float x, float y, float z){
		Vector3 spherePos;
		spherePos.x = x;
		spherePos.z = z;
		//set y to height in texture plus 2
		spherePos.y = y;
		Instantiate (AirSphere, spherePos, Quaternion.identity);
	}

	public void Stone(float x, float z){
		Vector3 spherePos;
		spherePos.x = x;
		spherePos.z = z;
		//set y to height in texture plus 2
		spherePos.y = y;
		Instantiate (StoneSphere, spherePos, Quaternion.identity);
	}

	public void Mud(float x, float z){
		Vector3 spherePos;
		spherePos.x = x;
		spherePos.z = z;
		//set y to height in texture plus 2
		spherePos.y = y;
		Instantiate (MudSphere, spherePos, Quaternion.identity);
	}
}