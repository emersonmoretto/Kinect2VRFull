#pragma strict
var characters : GameObject[];
var currentModel: GameObject;
private var i:int=0;

function Start () {
	currentModel = Instantiate(characters[i], transform.position, transform.rotation);
	currentModel.transform.parent = transform;
}

function Update () {

}

function LoadNextModel(){
	if (characters[i]){
		Destroy(currentModel);
	}
	i++;
	if (i>=characters.length){
		i=0;
	}
	transform.eulerAngles.y=120;
	currentModel = Instantiate(characters[i], transform.position, transform.rotation);
	currentModel.transform.parent = transform;
}