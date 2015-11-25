#pragma strict


var lerp = 0;
var duration = 0.5;

function Start () {

}

function Update () {

	 if(Input.GetKeyDown(KeyCode.W)){
	 	Debug.Log("aeaeae");
	 	
	 	var targetPos = transform.position;
		
		targetPos.z += 1;
			
			
		lerp += Time.deltaTime / duration;
			
	 	transform.position = 
				Vector3.Lerp (transform.position, targetPos, lerp);
 	}
				
	/*
	Rotation!!!! 	
	transform.rotation = Quaternion.Lerp (startRot, endRot, lerp);	
	*/
	
	
}