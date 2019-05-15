using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameStateManager2 : MonoBehaviour {
	private ErosionSim world;
	public Text currentLiazards;
	private float timer = 0f;
	private int count=0;
	private float next = 0;
	// Use this for initialization
	void Start () {
		world = GameObject.Find("ErosionSim").GetComponent<ErosionSim>();
	}
	
	// Update is called once per frame
	void Update () {
		timer += Time.deltaTime;

		if(LizardMovement.numOfLizards == 8){
			currentLiazards.text = "Congratulations You Win!";
			Time.timeScale = 0F;

		}
		currentLiazards.text = "Lizards: " + LizardMovement.numOfLizards.ToString() + "  Goal: 8";
		if(LizardMovement.numOfLizards == 4 && count<10 && timer>=next){
			world.Tsunami();
			count++;
			next = timer + 3;
		}
	}
}
