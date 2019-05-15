using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameStateManager : MonoBehaviour {

	public Text currentLiazards;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if(LizardMovement.numOfLizards == 5){
			currentLiazards.text = "Congratulations You Win!";
			  Time.timeScale = 0F;
		}
		currentLiazards.text = "Lizards: " + LizardMovement.numOfLizards.ToString() + "  Goal: 5";
	}
}
