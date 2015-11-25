using UnityEngine;
using System.Collections;

public class OverlayController : MonoBehaviour 
{
	public GUITexture backgroundImage;
	public Camera backgroundCamera;
	public Camera foregroundCamera;

	public GUIText debugText;


	void Start () 
	{
		KinectManager manager = KinectManager.Instance;
		
		if(manager && manager.IsInitialized())
		{
			KinectInterop.SensorData sensorData = manager.GetSensorData();

			if(foregroundCamera != null && sensorData != null && sensorData.sensorInterface != null)
			{
				foregroundCamera.transform.position = new Vector3(sensorData.depthCameraOffset, manager.sensorHeight, 0f);
				foregroundCamera.transform.rotation = Quaternion.Euler(-manager.sensorAngle, 0f, 0f);

				foregroundCamera.fieldOfView = sensorData.colorCameraFOV;
			}

			if(backgroundCamera != null && sensorData != null && sensorData.sensorInterface != null)
			{
				backgroundCamera.transform.position = new Vector3(0f, manager.sensorHeight, 0f);
				backgroundCamera.transform.rotation = Quaternion.Euler(-manager.sensorAngle, 0f, 0f);
			}

			if(debugText != null)
			{
				debugText.GetComponent<GUIText>().text = "Please stand in T-pose for calibration.";
			}
		}
		else
		{
			string sMessage = "KinectManager is missing or not initialized";
			Debug.LogError(sMessage);

			if(debugText != null)
			{
				debugText.GetComponent<GUIText>().text = sMessage;
			}
		}
	}
	
	void Update () 
	{
		KinectManager manager = KinectManager.Instance;
		
		if(manager && manager.IsInitialized())
		{
			if(manager.autoHeightAngle == KinectManager.AutoHeightAngle.AutoUpdate || 
			   manager.autoHeightAngle == KinectManager.AutoHeightAngle.AutoUpdateAndShowInfo)
			{
				// update the cameras automatically, according to the current sensor height and angle
				KinectInterop.SensorData sensorData = manager.GetSensorData();

				if(foregroundCamera != null && sensorData != null)
				{
					foregroundCamera.transform.position = new Vector3(sensorData.depthCameraOffset, manager.sensorHeight, 0f);
					foregroundCamera.transform.rotation = Quaternion.Euler(-manager.sensorAngle, 0f, 0f);
				}
				
				if(backgroundCamera != null && sensorData != null)
				{
					backgroundCamera.transform.position = new Vector3(0f, manager.sensorHeight, 0f);
					backgroundCamera.transform.rotation = Quaternion.Euler(-manager.sensorAngle, 0f, 0f);
				}
				
			}
			
			if(backgroundImage && (backgroundImage.texture == null))
			{
				backgroundImage.texture = manager.GetUsersClrTex();
			}

			MonoBehaviour[] monoScripts = FindObjectsOfType(typeof(MonoBehaviour)) as MonoBehaviour[];

			foreach(MonoBehaviour monoScript in monoScripts)
			{
				if(typeof(AvatarScaler).IsAssignableFrom(monoScript.GetType()))
				{
					AvatarScaler scaler = (AvatarScaler)monoScript;

					int userIndex = scaler.playerIndex;
					long userId = manager.GetUserIdByIndex(userIndex);

					if(userId != scaler.currentUserId)
					{
						scaler.currentUserId = userId;
					
						if(userId != 0)
						{
							scaler.GetUserBodySize(true, true, true);
							scaler.FixJointsBeforeScale();
							scaler.ScaleAvatar(0f);
						}
					}
				}
			}

			if(!manager.IsUserDetected())
			{
				if(debugText != null)
				{
					debugText.GetComponent<GUIText>().text = "Please stand in T-pose for calibration.";
				}
			}

		}

	}

}
