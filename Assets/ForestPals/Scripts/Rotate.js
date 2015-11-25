#pragma strict

function Start () {

}

function Update () {
	transform.Rotate(Vector3(0,20,0) * Time.deltaTime);
}