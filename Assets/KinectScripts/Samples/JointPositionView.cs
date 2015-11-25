using UnityEngine;
using System.Collections;
//using Windows.Kinect;


public class JointPositionView : MonoBehaviour 
{
	public KinectInterop.JointType trackedJoint = KinectInterop.JointType.SpineBase;
	public bool mirroredView = false;
	public bool displayKinectPos = false;
	public bool moveTransform = false;
	public float smoothFactor = 5f;
	public GUIText debugText;
	
	private Vector3 initialPosition = Vector3.zero;
	private long calibratedUserId = 0;
	private Vector3 initialUserOffset = Vector3.zero;


	void Start()
	{
		initialPosition = transform.position;
	}
	
	void Update () 
	{
		KinectManager manager = KinectManager.Instance;
		
		if(manager && manager.IsInitialized())
		{
			int iJointIndex = (int)trackedJoint;

			if(manager.IsUserDetected())
			{
				long userId = manager.GetPrimaryUserID();
				
				if(manager.IsJointTracked(userId, iJointIndex))
				{
					Vector3 vPosJoint = manager.GetJointPosition(userId, iJointIndex);
					vPosJoint.z = !mirroredView ? -vPosJoint.z : vPosJoint.z;

					if(userId != calibratedUserId)
					{
						calibratedUserId = userId;
						initialUserOffset = vPosJoint;
					}

					Vector3 vPosObject = !displayKinectPos ? (vPosJoint - initialUserOffset) : vPosJoint;
					vPosObject = initialPosition + vPosObject;
					
					if(debugText)
					{
						debugText.GetComponent<GUIText>().text = string.Format("{0} - ({1:F3}, {2:F3}, {3:F3})", trackedJoint, 
						                                                       vPosObject.x, vPosObject.y, vPosObject.z);
					}

					if(moveTransform)
					{
						if(smoothFactor != 0f)
							transform.position = Vector3.Lerp(transform.position, vPosObject, smoothFactor * Time.deltaTime);
						else
							transform.position = vPosObject;
					}
				}
				
			}
			
		}
	}
}
