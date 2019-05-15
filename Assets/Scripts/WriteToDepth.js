#pragma strict

function Start () {
	GetComponent.<Camera>().depthTextureMode |= DepthTextureMode.Depth;
}

function Update () {

}