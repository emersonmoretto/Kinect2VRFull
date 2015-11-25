using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ModelFaceController : MonoBehaviour 
{
	public enum AxisEnum { X, Y, Z };
	
	// Head
	public Transform HeadTransform;

	// Public bool to determine whether the head rotation and movement should be mirrored or normal
	public bool mirroredHeadMovement = true;

	// the smooth factor
	public float smoothFactor = 5f;
	
	// Upper Lip Left
	public Transform UpperLipLeft;
	public AxisEnum UpperLipLeftAxis;
	public float UpperLipLeftUp;

	// Upper Lip Right
	public Transform UpperLipRight;
	public AxisEnum UpperLipRightAxis;
	public float UpperLipRightUp;

	// Jaw
	public Transform Jaw;
	public AxisEnum JawAxis;
	public float JawDown;
	
	// Lip Left
	public Transform LipLeft;
	public AxisEnum LipLeftAxis;
	public float LipLeftStretched;

	// Lip Right
	public Transform LipRight;
	public AxisEnum LipRightAxis;
	public float LipRightStretched;

	// Eyebrow Left
	public Transform EyebrowLeft;
	public AxisEnum EyebrowLeftAxis;
	public float EyebrowLeftLowered;

	// Eyebrow Right
	public Transform EyebrowRight;
	public AxisEnum EyebrowRightAxis;
	public float EyebrowRightLowered;
	
	// Lip Corner Left
	public Transform LipCornerLeft;
	public AxisEnum LipCornerLeftAxis;
	public float LipCornerLeftDepressed;

	// Lip Corner Right
	public Transform LipCornerRight;
	public AxisEnum LipCornerRightAxis;
	public float LipCornerRightDepressed;

	// Upper Eyelid Left
	public Transform UpperEyelidLeft;
	public AxisEnum UpperEyelidLeftAxis;
	public float UpperEyelidLeftLowered;

	// Upper Eyelid Right
	public Transform UpperEyelidRight;
	public AxisEnum UpperEyelidRightAxis;
	public float UpperEyelidRightLowered;
	
	// Lower Eyelid Left
	public Transform LowerEyelidLeft;
	public AxisEnum LowerEyelidLeftAxis;
	public float LowerEyelidLeftRaised;

	// Lower Eyelid Right
	public Transform LowerEyelidRight;
	public AxisEnum LowerEyelidRightAxis;
	public float LowerEyelidRightRaised;
	
	
	private FacetrackingManager manager;
	private KinectInterop.DepthSensorPlatform platform;
	
	private Vector3 HeadInitialPosition;
	private Quaternion HeadInitialRotation;
	
	private float UpperLipLeftNeutral;
	private float UpperLipRightNeutral;
	private float JawNeutral;
	private float LipLeftNeutral;
	private float LipRightNeutral;
	private float EyebrowLeftNeutral;
	private float EyebrowRightNeutral;
	private float LipCornerLeftNeutral;
	private float LipCornerRightNeutral;
	private float UpperEyelidLeftNeutral;
	private float UpperEyelidRightNeutral;
	private float LowerEyelidLeftNeutral;
	private float LowerEyelidRightNeutral;

	
	void Start()
	{
		if(HeadTransform != null)
		{
			HeadInitialPosition = HeadTransform.localPosition;
			//HeadInitialPosition.z = 0;
			HeadInitialRotation = HeadTransform.localRotation;
		}
		
		UpperLipLeftNeutral = GetJointRotation(UpperLipLeft, UpperLipLeftAxis);
		UpperLipRightNeutral = GetJointRotation(UpperLipRight, UpperLipRightAxis);
		
		JawNeutral = GetJointRotation(Jaw, JawAxis);
		
		LipLeftNeutral = GetJointRotation(LipLeft, LipLeftAxis);
		LipRightNeutral = GetJointRotation(LipRight, LipRightAxis);
		
		EyebrowLeftNeutral = GetJointRotation(EyebrowLeft, EyebrowLeftAxis);
		EyebrowRightNeutral = GetJointRotation(EyebrowRight, EyebrowRightAxis);
		
		LipCornerLeftNeutral = GetJointRotation(LipCornerLeft, LipCornerLeftAxis);
		LipCornerRightNeutral = GetJointRotation(LipCornerRight, LipCornerRightAxis);
		
		UpperEyelidLeftNeutral = GetJointRotation(UpperEyelidLeft, UpperEyelidLeftAxis);
		UpperEyelidRightNeutral = GetJointRotation(UpperEyelidRight, UpperEyelidRightAxis);

		LowerEyelidLeftNeutral = GetJointRotation(LowerEyelidLeft, LowerEyelidLeftAxis);
		LowerEyelidRightNeutral = GetJointRotation(LowerEyelidRight, LowerEyelidRightAxis);

		KinectManager kinectManager = KinectManager.Instance;
		if(kinectManager && kinectManager.IsInitialized())
		{
			platform = kinectManager.GetSensorPlatform();
		}
	}
	
	void Update() 
	{
		// get the face-tracking manager instance
		if(manager == null)
		{
			manager = FacetrackingManager.Instance;
		}

		if(manager && manager.IsTrackingFace())
		{
			// set head position & rotation
			if(HeadTransform != null)
			{
				// head position
				Vector3 newPosition = HeadInitialPosition + manager.GetHeadPosition(mirroredHeadMovement);

				if(smoothFactor != 0f)
					HeadTransform.localPosition = Vector3.Lerp(HeadTransform.localPosition, newPosition, smoothFactor * Time.deltaTime);
				else
					HeadTransform.localPosition = newPosition;

				// head rotation
				Quaternion newRotation = HeadInitialRotation * manager.GetHeadRotation(mirroredHeadMovement);

				if(smoothFactor != 0f)
					HeadTransform.localRotation = Quaternion.Slerp(HeadTransform.localRotation, newRotation, smoothFactor * Time.deltaTime);
				else
					HeadTransform.localRotation = newRotation;
			}
			
			// apply animation units

			// AU0 - Upper Lip Raiser
			// 0=neutral, covering teeth; 1=showing teeth fully; -1=maximal possible pushed down lip
			float fAU0 = manager.GetAnimUnit(KinectInterop.FaceShapeAnimations.LipPucker);
			SetJointRotation(UpperLipLeft, UpperLipLeftAxis, fAU0, UpperLipLeftNeutral, UpperLipLeftUp);
			SetJointRotation(UpperLipRight, UpperLipRightAxis, fAU0, UpperLipRightNeutral, UpperLipRightUp);
			
			// AU1 - Jaw Lowerer
			// 0=closed; 1=fully open; -1= closed, like 0
			float fAU1 = manager.GetAnimUnit(KinectInterop.FaceShapeAnimations.JawOpen);
			SetJointRotation(Jaw, JawAxis, fAU1, JawNeutral, JawDown);
			
			// AU2 – Lip Stretcher
			// 0=neutral; 1=fully stretched (joker’s smile); -1=fully rounded (kissing mouth)
			float fAU2_left = manager.GetAnimUnit(KinectInterop.FaceShapeAnimations.LipStretcherLeft);
			fAU2_left = (platform == KinectInterop.DepthSensorPlatform.KinectSDKv2) ? (fAU2_left * 2 - 1) : fAU2_left;
			SetJointRotation(LipLeft, LipLeftAxis, fAU2_left, LipLeftNeutral, LipLeftStretched);

			float fAU2_right = manager.GetAnimUnit(KinectInterop.FaceShapeAnimations.LipStretcherRight);
			fAU2_right = (platform == KinectInterop.DepthSensorPlatform.KinectSDKv2) ? (fAU2_right * 2 - 1) : fAU2_right;
			SetJointRotation(LipRight, LipRightAxis, fAU2_right, LipRightNeutral, LipRightStretched);
			
			// AU3 – Brow Lowerer
			// 0=neutral; -1=raised almost all the way; +1=fully lowered (to the limit of the eyes)
			float fAU3_left = manager.GetAnimUnit(KinectInterop.FaceShapeAnimations.LefteyebrowLowerer);
			fAU3_left = (platform == KinectInterop.DepthSensorPlatform.KinectSDKv2) ? (fAU3_left * 2 - 1) : fAU3_left;
			SetJointRotation(EyebrowLeft, EyebrowLeftAxis, fAU3_left, EyebrowLeftNeutral, EyebrowLeftLowered);

			float fAU3_right = manager.GetAnimUnit(KinectInterop.FaceShapeAnimations.RighteyebrowLowerer);
			fAU3_right = (platform == KinectInterop.DepthSensorPlatform.KinectSDKv2) ? (fAU3_right * 2 - 1) : fAU3_right;
			SetJointRotation(EyebrowRight, EyebrowRightAxis, fAU3_right, EyebrowRightNeutral, EyebrowRightLowered);
			
			// AU4 – Lip Corner Depressor
			// 0=neutral; -1=very happy smile; +1=very sad frown
			float fAU4_left = manager.GetAnimUnit(KinectInterop.FaceShapeAnimations.LipCornerDepressorLeft);
			fAU4_left = (platform == KinectInterop.DepthSensorPlatform.KinectSDKv2) ? (fAU4_left * 2) : fAU4_left;
			SetJointRotation(LipCornerLeft, LipCornerLeftAxis, fAU4_left, LipCornerLeftNeutral, LipCornerLeftDepressed);

			float fAU4_right = manager.GetAnimUnit(KinectInterop.FaceShapeAnimations.LipCornerDepressorRight);
			fAU4_right = (platform == KinectInterop.DepthSensorPlatform.KinectSDKv2) ? (fAU4_right * 2) : fAU4_right;
			SetJointRotation(LipCornerRight, LipCornerRightAxis, fAU4_right, LipCornerRightNeutral, LipCornerRightDepressed);

			// AU6, AU7 – Eyelid closed
			// 0=neutral; -1=raised; +1=fully lowered
			float fAU6_left = manager.GetAnimUnit(KinectInterop.FaceShapeAnimations.LefteyeClosed);
			fAU6_left = (platform == KinectInterop.DepthSensorPlatform.KinectSDKv2) ? (fAU6_left * 2 - 1) : fAU6_left;
			SetJointRotation(UpperEyelidLeft, UpperEyelidLeftAxis, fAU6_left, UpperEyelidLeftNeutral, UpperEyelidLeftLowered);
			SetJointRotation(LowerEyelidLeft, LowerEyelidLeftAxis, fAU6_left, LowerEyelidLeftNeutral, LowerEyelidLeftRaised);

			float fAU6_right = manager.GetAnimUnit(KinectInterop.FaceShapeAnimations.RighteyeClosed);
			fAU6_right = (platform == KinectInterop.DepthSensorPlatform.KinectSDKv2) ? (fAU6_right * 2 - 1) : fAU6_right;
			SetJointRotation(UpperEyelidRight, UpperEyelidRightAxis, fAU6_right, UpperEyelidRightNeutral, UpperEyelidRightLowered);
			SetJointRotation(LowerEyelidRight, LowerEyelidRightAxis, fAU6_right, LowerEyelidRightNeutral, LowerEyelidRightRaised);
		}
	}
	
	private float GetJointRotation(Transform joint, AxisEnum axis)
	{
		float fJointRot = 0.0f;
		
		if(joint == null)
			return fJointRot;
		
		Vector3 jointRot = joint.localRotation.eulerAngles;
		
		switch(axis)
		{
			case AxisEnum.X:
				fJointRot = jointRot.x;
				break;
			
			case AxisEnum.Y:
				fJointRot = jointRot.y;
				break;
			
			case AxisEnum.Z:
				fJointRot = jointRot.z;
				break;
		}
		
		return fJointRot;
	}
	
	private void SetJointRotation(Transform joint, AxisEnum axis, float fAU, float fMin, float fMax)
	{
		if(joint == null)
			return;
		
//		float fSign = 1.0f;
//		if(fMax < fMin)
//			fSign = -1.0f;
		
		// [-1, +1] -> [0, 1]
		//fAUnorm = (fAU + 1f) / 2f;
		float fValue = fMin + (fMax - fMin) * fAU;
		
		Vector3 jointRot = joint.localRotation.eulerAngles;
		
		switch(axis)
		{
			case AxisEnum.X:
				jointRot.x = fValue;
				break;
			
			case AxisEnum.Y:
				jointRot.y = fValue;
				break;
			
			case AxisEnum.Z:
				jointRot.z = fValue;
				break;
		}

		if(smoothFactor != 0f)
			joint.localRotation = Quaternion.Slerp(joint.localRotation, Quaternion.Euler(jointRot), smoothFactor * Time.deltaTime);
		else
			joint.localRotation = Quaternion.Euler(jointRot);
	}
	
	
}
