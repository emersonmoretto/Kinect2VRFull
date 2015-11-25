using UnityEngine;
using System.Collections;

public class SetBackgroundImage : MonoBehaviour 
{
	public GUITexture backgroundImage;
	public Camera mainCamera;


	void Start()
	{
		KinectManager manager = KinectManager.Instance;
		
		if(manager && manager.IsInitialized())
		{
			KinectInterop.SensorData sensorData = manager.GetSensorData();

			if(backgroundImage && (backgroundImage.texture == null))
			{
				backgroundImage.texture = manager.GetUsersClrTex();
			}

			if(mainCamera != null && sensorData != null && sensorData.sensorInterface != null)
			{
				mainCamera.fieldOfView = sensorData.colorCameraFOV;

				Vector3 mainCamPos = mainCamera.transform.position;
				mainCamPos.x += sensorData.depthCameraOffset;
				mainCamera.transform.position = mainCamPos;

			}
		}
	}

}
