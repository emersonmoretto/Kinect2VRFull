using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using System.IO;


public class OpenNI2Interface : DepthSensorInterface 
{

	private static class Constants
	{
		public const int SkeletonCount = 6;
		public const int JointCouunt = 15;
		public const float SmoothingFactor = 0.7f;
	}
	
	public enum SkeletonJoint
	{
		HEAD = 0,
		NECK = 1,

		LEFT_SHOULDER = 2,
		RIGHT_SHOULDER = 3,
		LEFT_ELBOW = 4,
		RIGHT_ELBOW = 5,
		LEFT_HAND = 6,
		RIGHT_HAND = 7,
		
		HIPS = 8,
		
		LEFT_HIP = 9,
		RIGHT_HIP = 10,
		LEFT_KNEE = 11,
		RIGHT_KNEE = 12,
		LEFT_FOOT = 13,
		RIGHT_FOOT = 14,
		
		COUNT 
	};
	
	// Struct to store the joint's poision.
	public struct SkeletonJointPosition
	{
		public float x, y, z;
	}
	
	// Struct that will hold the joints orientation.
	public struct SkeletonJointOrientation
	{
		public float x, y, z, w;
	}
	
	// Struct that combines the previous two and makes the transform.
	public struct SkeletonJointTransformation
	{
		public SkeletonJoint jointType;
		public SkeletonJointPosition position;
		public float positionConfidence;
		public SkeletonJointOrientation orientation;
		public float orientationConfidence;
	}

	private short[] oniUsers = new short[Constants.SkeletonCount];
	private short[] oniStates = new short[Constants.SkeletonCount];
	private Int32 oniUsersCount = 0;

	private List<uint> allUsers = new List<uint>();
	private SkeletonJointPosition jointPosition = new SkeletonJointPosition();

	private int usersMapSize = 0;
	private short[] usersLabelMap;
	private short[] usersDepthMap;

	private int usersClrSize = 0;
	private byte[] usersColorMap;

	private bool bBackgroundRemovalInited = false;

	[DllImport("UnityInterface2")]
	private static extern int GetDeviceCount(out int pCount);
	[DllImport("UnityInterface2", EntryPoint = "Init", SetLastError = true)]
	private static extern int InitNative(bool isInitDepthStream, bool isInitColorStream, bool isInitInfraredStream);
	[DllImport("UnityInterface2", EntryPoint = "Shutdown", SetLastError = true)]
	private static extern void ShutdownNative();
	[DllImport("UnityInterface2", EntryPoint = "Update", SetLastError = true)]
	private static extern int UpdateNative([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = Constants.SkeletonCount, ArraySubType = UnmanagedType.U2)] short[] pUsers,
	                                	   [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = Constants.SkeletonCount, ArraySubType = UnmanagedType.U2)] short[] pStates, ref int pUsersCount);
	
	[DllImport("UnityInterface2")]
	private static extern IntPtr GetLastErrorString();
	[DllImport("UnityInterface2", SetLastError = true)]
	private static extern int GetDepthWidth();
	[DllImport("UnityInterface2", SetLastError = true)]
	private static extern int GetDepthHeight();
	[DllImport("UnityInterface2", SetLastError = true)]
	private static extern int GetInfraredWidth();
	[DllImport("UnityInterface2", SetLastError = true)]
	private static extern int GetInfraredHeight();
	[DllImport("UnityInterface2", SetLastError = true)]
	private static extern int GetColorWidth();
	[DllImport("UnityInterface2", SetLastError = true)]
	private static extern int GetColorHeight();
	[DllImport("UnityInterface2", SetLastError = true)]
	private static extern IntPtr GetUsersLabelMap();
	[DllImport("UnityInterface2", SetLastError = true)]
	private static extern IntPtr GetUsersDepthMap();
	[DllImport("UnityInterface2", SetLastError = true)]
	private static extern IntPtr GetUsersInfraredMap();
	[DllImport("UnityInterface2", SetLastError = true)]
	private static extern IntPtr GetUsersColorMap();
	
	[DllImport("UnityInterface2", SetLastError = true)]
	private static extern void SetSkeletonSmoothing(float factor);
	
	[DllImport("UnityInterface2", SetLastError = true)]
	private static extern bool GetJointPosition(uint userID, int joint, ref SkeletonJointPosition pTransformation);
//	[DllImport("UnityInterface2", SetLastError = true)]
//	private static extern bool GetJointOrientation(uint userID, int joint, ref SkeletonJointOrientation pTransformation);
	[DllImport("UnityInterface2", SetLastError = true)]
	private static extern float GetJointPositionConfidence(uint userID, int joint);
//	[DllImport("UnityInterface2", SetLastError = true)]
//	private static extern float GetJointOrientationConfidence(uint userID, int joint);
	
	[DllImport("UnityInterface2", SetLastError = true)]
	private static extern void StartLookingForUsers(IntPtr NewUser, IntPtr CalibrationStarted, IntPtr CalibrationFailed, IntPtr CalibrationSuccess, IntPtr UserLost);
	[DllImport("UnityInterface2", SetLastError = true)]
	private static extern void StopLookingForUsers();
	
	public delegate void UserDelegate(uint userId);
	
	private static void StartLookingForUsers(UserDelegate NewUser, UserDelegate CalibrationStarted, UserDelegate CalibrationFailed, UserDelegate CalibrationSuccess, UserDelegate UserLost)
	{
		StartLookingForUsers(
			Marshal.GetFunctionPointerForDelegate(NewUser),
			Marshal.GetFunctionPointerForDelegate(CalibrationStarted),
			Marshal.GetFunctionPointerForDelegate(CalibrationFailed),
			Marshal.GetFunctionPointerForDelegate(CalibrationSuccess),
			Marshal.GetFunctionPointerForDelegate(UserLost));
	}
	

	public KinectInterop.DepthSensorPlatform GetSensorPlatform ()
	{
		return KinectInterop.DepthSensorPlatform.OpenNIv2;
	}

	public bool InitSensorInterface (bool bCopyLibs, ref bool bNeedRestart)
	{
//		bool bOneCopied = false, bAllCopied = true;
//		string sTargetPath = KinectInterop.GetTargetDllPath(".", KinectInterop.Is64bitArchitecture()) + "/";
//		//string sTargetPath = "./";
//		
//		if(!bCopyLibs)
//		{
//			// check if the native library is there
//			string sTargetLib = sTargetPath + "UnityInterface2.dll";
//			bNeedRestart = false;
//			
//			string sZipFileName = !KinectInterop.Is64bitArchitecture() ? "OpenNI2UnityInterface.x86.zip" : "OpenNI2UnityInterface.x64.zip";
//			long iTargetSize = KinectInterop.GetUnzippedEntrySize(sZipFileName, "UnityInterface2.dll");
//			
//			FileInfo targetFile = new FileInfo(sTargetLib);
//			return targetFile.Exists && targetFile.Length == iTargetSize;
//		}
//		
//		// check openni directory and resources
//		string sOpenNIPath = System.Environment.GetEnvironmentVariable("OPENNI2_REDIST");
//		if(sOpenNIPath == String.Empty || !Directory.Exists(sOpenNIPath))
//		{
//			Debug.LogError("OpenNI2-folder not found. Please check if the OPENNI2_REDIST environment variable exists.");
//			return false;
//		}
//		
//		sOpenNIPath = sOpenNIPath.Replace('\\', '/');
//		if(sOpenNIPath.EndsWith("/"))
//		{
//			sOpenNIPath = sOpenNIPath.Substring(0, sOpenNIPath.Length - 1);
//		}
//		
//		// check nite directory and resources
//		string sNiTEPath = System.Environment.GetEnvironmentVariable("NITE2_REDIST");
//		if(sNiTEPath == String.Empty || !Directory.Exists(sNiTEPath))
//		{
//			Debug.LogError("NiTE2-folder not found. Please check the NITE2_REDIST environment variable exists.");
//			return false;
//		}
//		
//		sNiTEPath = sNiTEPath.Replace('\\', '/');
//		if(sNiTEPath.EndsWith("/"))
//		{
//			sNiTEPath = sNiTEPath.Substring(0, sNiTEPath.Length - 1);
//		}
//		
//		if(!KinectInterop.Is64bitArchitecture())
//		{
//			Dictionary<string, string> dictFilesToUnzip = new Dictionary<string, string>();
//			
//			dictFilesToUnzip["UnityInterface2.dll"] = sTargetPath + "UnityInterface2.dll";
//			dictFilesToUnzip["OpenNI2.dll"] = sTargetPath + "OpenNI2.dll";
//			dictFilesToUnzip["NiTE2.dll"] = sTargetPath + "NiTE2.dll";
//			dictFilesToUnzip["OpenNI.ini"] = sTargetPath + "OpenNI.ini";
//			dictFilesToUnzip["NiTE.ini"] = sTargetPath + "NiTE.ini";
//
//			dictFilesToUnzip["msvcp90d.dll"] = sTargetPath + "msvcp90d.dll";
//			dictFilesToUnzip["msvcr90d.dll"] = sTargetPath + "msvcr90d.dll";
//			
//			KinectInterop.UnzipResourceFiles(dictFilesToUnzip, "OpenNI2UnityInterface.x86.zip", ref bOneCopied, ref bAllCopied);
//		}
//		else
//		{
//			Dictionary<string, string> dictFilesToUnzip = new Dictionary<string, string>();
//			
//			dictFilesToUnzip["UnityInterface2.dll"] = sTargetPath + "UnityInterface2.dll";
//			dictFilesToUnzip["OpenNI2.dll"] = sTargetPath + "OpenNI2.dll";
//			dictFilesToUnzip["NiTE2.dll"] = sTargetPath + "NiTE2.dll";
//			dictFilesToUnzip["OpenNI.ini"] = sTargetPath + "OpenNI.ini";
//			dictFilesToUnzip["NiTE.ini"] = sTargetPath + "NiTE.ini";
//
//			dictFilesToUnzip["msvcp90d.dll"] = sTargetPath + "msvcp90d.dll";
//			dictFilesToUnzip["msvcr90d.dll"] = sTargetPath + "msvcr90d.dll";
//			
//			KinectInterop.UnzipResourceFiles(dictFilesToUnzip, "OpenNI2UnityInterface.x64.zip", ref bOneCopied, ref bAllCopied);
//		}
//
//		if(File.Exists(sTargetPath + "OpenNI.ini"))
//	   	{
//			string sFileContent = File.ReadAllText(sTargetPath + "OpenNI.ini");
//			sFileContent = sFileContent.Replace("%OPENNI_REDIST_DIR%", sOpenNIPath);
//			File.WriteAllText(sTargetPath + "OpenNI.ini", sFileContent);
//		}
//
//		if(File.Exists(sTargetPath + "NiTE.ini"))
//		{
//			string sFileContent = File.ReadAllText(sTargetPath + "NiTE.ini");
//			sFileContent = sFileContent.Replace("%NITE_REDIST_DIR%", sNiTEPath);
//			File.WriteAllText(sTargetPath + "NiTE.ini", sFileContent);
//		}
//		
//		bNeedRestart = (bOneCopied && bAllCopied);
//		
//		return true;
		return false;
	}

	public void FreeSensorInterface (bool bDeleteLibs)
	{
//		if(bDeleteLibs)
//		{
//			KinectInterop.DeleteNativeLib("UnityInterface2.dll", true);
//			KinectInterop.DeleteNativeLib("OpenNI2.dll", false);
//			KinectInterop.DeleteNativeLib("NiTE2.dll", false);
//			KinectInterop.DeleteNativeLib("OpenNI.ini", false);
//			KinectInterop.DeleteNativeLib("NiTE.ini", false);
//			KinectInterop.DeleteNativeLib("msvcp90d.dll", false);
//			KinectInterop.DeleteNativeLib("msvcr90d.dll", false);
//		}
	}

	public bool IsSensorAvailable ()
	{
		bool bAvailable = GetSensorsCount() > 0;
		return bAvailable;
	}

	public int GetSensorsCount ()
	{
		int iSensorCount = 0;
		int hr = GetDeviceCount(out iSensorCount);
		
		return (hr == 0 ? iSensorCount : 0);
	}

	public KinectInterop.SensorData OpenDefaultSensor (KinectInterop.FrameSource dwFlags, float sensorAngle, bool bUseMultiSource)
	{
		bool bColor = false, bDepth = false, bInfrared = false;
		
		if((dwFlags & KinectInterop.FrameSource.TypeColor) != 0)
		{
			bColor = true;
		}
		
		if((dwFlags & KinectInterop.FrameSource.TypeDepth) != 0)
		{
			bDepth = true;
		}
		
		if((dwFlags & KinectInterop.FrameSource.TypeBodyIndex) != 0)
		{
			bInfrared = true;
		}

		int hr = InitNative(bDepth, bColor, bInfrared);

		if(hr == 0)
		{
			KinectInterop.SensorData sensorData = new KinectInterop.SensorData();
			
			sensorData.bodyCount = Constants.SkeletonCount;
			sensorData.jointCount = Constants.JointCouunt;
			
			sensorData.depthCameraFOV = 46.6f;
			sensorData.colorCameraFOV = 48.6f;
			sensorData.depthCameraOffset = 0.02f;

			sensorData.colorImageWidth = GetColorWidth();
			sensorData.colorImageHeight = GetColorHeight();

			sensorData.depthImageWidth = GetDepthWidth();
			sensorData.depthImageHeight = GetDepthHeight();

			usersClrSize = sensorData.colorImageWidth * sensorData.colorImageHeight;
			usersColorMap = new byte[usersClrSize * 3];

			usersMapSize = sensorData.depthImageWidth * sensorData.depthImageHeight;
			usersLabelMap = new short[usersMapSize];
			usersDepthMap = new short[usersMapSize];

			if((dwFlags & KinectInterop.FrameSource.TypeColor) != 0)
			{
				sensorData.colorImage = new byte[sensorData.colorImageWidth * sensorData.colorImageHeight * 4];
			}
			
			if((dwFlags & KinectInterop.FrameSource.TypeDepth) != 0)
			{
				sensorData.depthImage = new ushort[sensorData.depthImageWidth * sensorData.depthImageHeight];
			}
			
			if((dwFlags & KinectInterop.FrameSource.TypeBodyIndex) != 0)
			{
				sensorData.bodyIndexImage = new byte[sensorData.depthImageWidth * sensorData.depthImageHeight];
			}
			
			if((dwFlags & KinectInterop.FrameSource.TypeInfrared) != 0)
			{
				sensorData.infraredImage = new ushort[sensorData.colorImageWidth * sensorData.colorImageHeight];
			}
			
			if((dwFlags & KinectInterop.FrameSource.TypeBody) != 0)
			{
				StartLookingForUsers(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
				SetSkeletonSmoothing(Constants.SmoothingFactor);
			}
			
			return sensorData;
		}
		else
		{
			Debug.LogError("InitKinectSensor failed: " + Marshal.PtrToStringAnsi(GetLastErrorString()));
		}
		
		return null;
	}

	public void CloseSensor (KinectInterop.SensorData sensorData)
	{
		StopLookingForUsers();
		ShutdownNative();
	}

	public bool UpdateSensorData (KinectInterop.SensorData sensorData)
	{
		// Update to the next frame.
		oniUsersCount = oniUsers.Length;
		UpdateNative(oniUsers, oniStates, ref oniUsersCount);

		return true;
	}

	public bool GetMultiSourceFrame (KinectInterop.SensorData sensorData)
	{
		return false;
	}

	public void FreeMultiSourceFrame (KinectInterop.SensorData sensorData)
	{
	}

	public bool PollBodyFrame (KinectInterop.SensorData sensorData, ref KinectInterop.BodyFrameData bodyFrame, ref Matrix4x4 kinectToWorld)
	{
		bool newSkeleton = (oniUsersCount > 0);

		for(int i = 0; i < oniUsersCount; i++)
		{
			uint userId = (uint)oniUsers[i];
			short userState = oniStates[i];
			
			switch(userState)
			{
			case 1: // new user
				Debug.Log(String.Format("New user: {0}", userId));
				break;
				
			case 2: // calibration started
				Debug.Log(String.Format("Calibration started for user: {0}", userId));
				break;
				
			case 3: // calibration succeeded
				Debug.Log(String.Format("Calibration succeeded for user: {0}", userId));

				if(!allUsers.Contains(userId))
				{
					allUsers.Add(userId);
				}
				break;
				
			case 4: // calibration failed
				Debug.Log(String.Format("Calibration failed for user: {0}", userId));
				break;
				
			case 5: // user lost
				Debug.Log(String.Format("User lost: {0}", userId));

				if(allUsers.Contains(userId))
				{
					allUsers.Remove(userId);
				}
				break;
			}
		}

		// fill in joint info for the tracked skeletons
		for(int i = 0; i < allUsers.Count; i++)
		{
			uint userId = allUsers[i];

			bodyFrame.bodyData[i].liTrackingID = (long)userId;
			bodyFrame.bodyData[i].bIsTracked = 1;

			for(int j = 0; j < sensorData.jointCount; j++)
			{
				KinectInterop.JointData jointData = bodyFrame.bodyData[i].joint[j];
				
				jointData.jointType = GetJointAtIndex(j);

				float fConfidence = GetJointPositionConfidence(userId, j);
				jointData.trackingState = (fConfidence > 0.7f ? KinectInterop.TrackingState.Tracked :
					(fConfidence > 0.3f ? KinectInterop.TrackingState.Inferred : KinectInterop.TrackingState.NotTracked));
				
				if(jointData.trackingState != KinectInterop.TrackingState.NotTracked)
				{
					if(GetJointPosition(userId, j, ref jointPosition))
					{
						jointData.kinectPos = new Vector3(jointPosition.x * 0.001f, jointPosition.y * 0.001f, jointPosition.z * 0.001f);;
						jointData.position = kinectToWorld.MultiplyPoint3x4(jointData.kinectPos);
					}
				}
				
				jointData.orientation = Quaternion.identity;

				if(j == (int)SkeletonJoint.HIPS)
				{
					bodyFrame.bodyData[i].position = jointData.position;
					bodyFrame.bodyData[i].orientation = jointData.orientation;
				}
				
				bodyFrame.bodyData[i].joint[j] = jointData;
			}
		}

		// set the rest of the skeletons as not tracked
		for(int i = allUsers.Count; i < sensorData.bodyCount; i++)
		{
			bodyFrame.bodyData[i].liTrackingID = 0;
			bodyFrame.bodyData[i].bIsTracked = 0;
		}

		return newSkeleton;
	}

	public bool PollColorFrame (KinectInterop.SensorData sensorData)
	{
		IntPtr pColorMap = GetUsersColorMap();
		if(pColorMap == IntPtr.Zero)
			return false;
		
		// copy over the map
		Marshal.Copy(pColorMap, usersColorMap, 0, usersColorMap.Length);
		
		// Create the actual users texture based on label map and depth histogram
		for (int i = 0, srcI = 0, dstI = 0; i < usersClrSize; i++)
		{
			sensorData.colorImage[dstI] = usersColorMap[srcI];
			sensorData.colorImage[dstI + 1] = usersColorMap[srcI + 1];
			sensorData.colorImage[dstI + 2] = usersColorMap[srcI + 2];
			sensorData.colorImage[dstI + 3] = 255;
			
			srcI += 3;
			dstI += 4;
		}
		
		return true;
	}

	public bool PollDepthFrame (KinectInterop.SensorData sensorData)
	{
		IntPtr pLabelMap = GetUsersLabelMap();
		IntPtr pDepthMap = GetUsersDepthMap();
		
		if(pLabelMap == IntPtr.Zero || pDepthMap == IntPtr.Zero)
			return false;
		
		// copy over the maps
		Marshal.Copy(pLabelMap, usersLabelMap, 0, usersLabelMap.Length);
		Marshal.Copy(pDepthMap, usersDepthMap, 0, usersDepthMap.Length);

		for (int i = 0; i < usersMapSize; i++)
		{
			int userIndex = usersLabelMap[i];

			sensorData.bodyIndexImage[i] = userIndex != 0 ? (byte)(userIndex - 1) : (byte)255;
			sensorData.depthImage[i] = (ushort)usersDepthMap[i];
		}

		return true;
		
	}

	public bool PollInfraredFrame (KinectInterop.SensorData sensorData)
	{
		throw new System.NotImplementedException ();
	}

	public void FixJointOrientations (KinectInterop.SensorData sensorData, ref KinectInterop.BodyData bodyData)
	{
	}

	private void NuiTransformSkeletonToDepthImage(Vector3 vPoint, out float pfDepthX, out float pfDepthY, out float pfDepthZ)
	{
		if (vPoint.z > float.Epsilon)
		{
			pfDepthX = 0.5f + ((vPoint.x * 285.63f) / (vPoint.z * 320f));
			pfDepthY = 0.5f - ((vPoint.y * 285.63f) / (vPoint.z * 240f));
			pfDepthZ = vPoint.z * 1000f;
		}
		else
		{
			pfDepthX = 0f;
			pfDepthY = 0f;
			pfDepthZ = 0f;
		}
	}
	
	public Vector2 MapSpacePointToDepthCoords (KinectInterop.SensorData sensorData, Vector3 spacePos)
	{
		float fDepthX, fDepthY, fDepthZ;
		NuiTransformSkeletonToDepthImage(spacePos, out fDepthX, out fDepthY, out fDepthZ);
		
		fDepthX = Mathf.RoundToInt(fDepthX * sensorData.depthImageWidth);
		fDepthY = Mathf.RoundToInt(fDepthY * sensorData.depthImageHeight);
		fDepthZ = Mathf.RoundToInt(fDepthZ);
		
		Vector3 point = new Vector3(fDepthX, fDepthY, fDepthZ);
		
		return point;
	}
	
	private Vector3 NuiTransformDepthImageToSkeleton(float fDepthX, float fDepthY, int depthValue)
	{
		Vector3 point = Vector3.zero;
		
		if (depthValue > 0)
		{
			float fSpaceZ = ((float)depthValue) / 1000f;
			float fSpaceX = ((fDepthX - 0.5f) * (0.003501f * fSpaceZ)) * 320f;
			float fSpaceY = ((0.5f - fDepthY) * (0.003501f * fSpaceZ)) * 240f;
			
			point = new Vector3(fSpaceX, fSpaceY, fSpaceZ);
		}
		
		return point;
	}
	
	public Vector3 MapDepthPointToSpaceCoords (KinectInterop.SensorData sensorData, Vector2 depthPos, ushort depthVal)
	{
		float fDepthX = depthPos.x / sensorData.depthImageWidth;
		float fDepthY = depthPos.y / sensorData.depthImageHeight;
		
		Vector3 point = NuiTransformDepthImageToSkeleton(fDepthX, fDepthY, depthVal);
		
		return point;
	}
	
	public Vector2 MapDepthPointToColorCoords (KinectInterop.SensorData sensorData, Vector2 depthPos, ushort depthVal)
	{
		Vector2 colorPos = depthPos;
		return colorPos;
	}

	public bool MapDepthFrameToColorCoords (KinectInterop.SensorData sensorData, ref Vector2[] vColorCoords)
	{
		return false;
	}

	public bool MapColorFrameToDepthCoords (KinectInterop.SensorData sensorData, ref Vector2[] vDepthCoords)
	{
		return false;
	}

	public int GetJointIndex (KinectInterop.JointType joint)
	{
		switch(joint)
		{
		case KinectInterop.JointType.SpineBase:
			return (int)SkeletonJoint.HIPS;
		case KinectInterop.JointType.SpineShoulder:
		case KinectInterop.JointType.Neck:
			return (int)SkeletonJoint.NECK;
		case KinectInterop.JointType.Head:
			return (int)SkeletonJoint.HEAD;
			
		case KinectInterop.JointType.ShoulderLeft:
			return (int)SkeletonJoint.LEFT_SHOULDER;
		case KinectInterop.JointType.ElbowLeft:
			return (int)SkeletonJoint.LEFT_ELBOW;
		case KinectInterop.JointType.WristLeft:
		case KinectInterop.JointType.HandLeft:
			return (int)SkeletonJoint.LEFT_HAND;
			
		case KinectInterop.JointType.ShoulderRight:
			return (int)SkeletonJoint.RIGHT_SHOULDER;
		case KinectInterop.JointType.ElbowRight:
			return (int)SkeletonJoint.RIGHT_ELBOW;
		case KinectInterop.JointType.WristRight:
		case KinectInterop.JointType.HandRight:
			return (int)SkeletonJoint.RIGHT_HAND;
			
		case KinectInterop.JointType.HipLeft:
			return (int)SkeletonJoint.LEFT_HIP;
		case KinectInterop.JointType.KneeLeft:
			return (int)SkeletonJoint.LEFT_KNEE;
		case KinectInterop.JointType.AnkleLeft:
		case KinectInterop.JointType.FootLeft:
			return (int)SkeletonJoint.LEFT_FOOT;
			
		case KinectInterop.JointType.HipRight:
			return (int)SkeletonJoint.RIGHT_HIP;
		case KinectInterop.JointType.KneeRight:
			return (int)SkeletonJoint.RIGHT_KNEE;
		case KinectInterop.JointType.AnkleRight:
		case KinectInterop.JointType.FootRight:
			return (int)SkeletonJoint.RIGHT_FOOT;
		}
		
		return -1;
	}

	public KinectInterop.JointType GetJointAtIndex (int index)
	{
		switch(index)
		{
			case (int)SkeletonJoint.HIPS:
				return KinectInterop.JointType.SpineBase;
			case (int)SkeletonJoint.NECK:
				return KinectInterop.JointType.Neck;
			case (int)SkeletonJoint.HEAD:
				return KinectInterop.JointType.Head;
				
			case (int)SkeletonJoint.LEFT_SHOULDER:
				return KinectInterop.JointType.ShoulderLeft;
			case (int)SkeletonJoint.LEFT_ELBOW:
				return KinectInterop.JointType.ElbowLeft;
			case (int)SkeletonJoint.LEFT_HAND:
				return KinectInterop.JointType.WristLeft;
				
			case (int)SkeletonJoint.RIGHT_SHOULDER:
				return KinectInterop.JointType.ShoulderRight;
			case (int)SkeletonJoint.RIGHT_ELBOW:
				return KinectInterop.JointType.ElbowRight;
			case (int)SkeletonJoint.RIGHT_HAND:
				return KinectInterop.JointType.WristRight;
				
			case (int)SkeletonJoint.LEFT_HIP:
				return KinectInterop.JointType.HipLeft;
			case (int)SkeletonJoint.LEFT_KNEE:
				return KinectInterop.JointType.KneeLeft;
			case (int)SkeletonJoint.LEFT_FOOT:
				return KinectInterop.JointType.AnkleLeft;
				
			case (int)SkeletonJoint.RIGHT_HIP:
				return KinectInterop.JointType.HipRight;
			case (int)SkeletonJoint.RIGHT_KNEE:
				return KinectInterop.JointType.KneeRight;
			case (int)SkeletonJoint.RIGHT_FOOT:
				return KinectInterop.JointType.AnkleRight;
		}
		
		return (KinectInterop.JointType)(-1);
	}

	public KinectInterop.JointType GetParentJoint (KinectInterop.JointType joint)
	{
		switch(joint)
		{
			case KinectInterop.JointType.Neck:
				return KinectInterop.JointType.SpineBase;
			case KinectInterop.JointType.Head:
				return KinectInterop.JointType.Neck;
				
			case  KinectInterop.JointType.ShoulderLeft:
				return KinectInterop.JointType.Neck;
			case KinectInterop.JointType.ElbowLeft:
				return KinectInterop.JointType.ShoulderLeft;
			case KinectInterop.JointType.WristLeft:
				return KinectInterop.JointType.ElbowLeft;
				
			case KinectInterop.JointType.ShoulderRight:
				return KinectInterop.JointType.Neck;
			case KinectInterop.JointType.ElbowRight:
				return KinectInterop.JointType.ShoulderRight;
			case KinectInterop.JointType.WristRight:
				return KinectInterop.JointType.ElbowRight;
				
			case KinectInterop.JointType.HipLeft:
				return KinectInterop.JointType.SpineBase;
			case KinectInterop.JointType.KneeLeft:
				return KinectInterop.JointType.HipLeft;
			case KinectInterop.JointType.AnkleLeft:
				return KinectInterop.JointType.KneeLeft;
				
			case KinectInterop.JointType.HipRight:
				return KinectInterop.JointType.SpineBase;
			case KinectInterop.JointType.KneeRight:
				return KinectInterop.JointType.HipRight;
			case KinectInterop.JointType.AnkleRight:
				return KinectInterop.JointType.KneeRight;
		}
		
		return joint;
	}

	public KinectInterop.JointType GetNextJoint (KinectInterop.JointType joint)
	{
		switch(joint)
		{
			case KinectInterop.JointType.SpineBase:
				return KinectInterop.JointType.Neck;
			case KinectInterop.JointType.Neck:
				return KinectInterop.JointType.Head;
				
			case KinectInterop.JointType.ShoulderLeft:
				return KinectInterop.JointType.ElbowLeft;
			case KinectInterop.JointType.ElbowLeft:
				return KinectInterop.JointType.WristLeft;
				
			case KinectInterop.JointType.ShoulderRight:
				return KinectInterop.JointType.ElbowRight;
			case KinectInterop.JointType.ElbowRight:
				return KinectInterop.JointType.WristRight;
				
			case KinectInterop.JointType.HipLeft:
				return KinectInterop.JointType.KneeLeft;
			case KinectInterop.JointType.KneeLeft:
				return KinectInterop.JointType.AnkleLeft;
				
			case KinectInterop.JointType.HipRight:
				return KinectInterop.JointType.KneeRight;
			case KinectInterop.JointType.KneeRight:
				return KinectInterop.JointType.AnkleRight;
		}
		
		return joint;  // end joint
	}

	public bool IsFaceTrackingAvailable (ref bool bNeedRestart)
	{
		bNeedRestart = false;
		return false;
	}

	public bool InitFaceTracking (bool bUseFaceModel, bool bDrawFaceRect)
	{
		return false;
	}

	public void FinishFaceTracking ()
	{
	}

	public bool UpdateFaceTracking ()
	{
		return false;
	}

	public bool IsFaceTrackingActive ()
	{
		return false;
	}

	public bool IsDrawFaceRect ()
	{
		return false;
	}

	public bool IsFaceTracked (long userId)
	{
		return false;
	}

	public bool GetFaceRect (long userId, ref Rect faceRect)
	{
		return false;
	}

	public void VisualizeFaceTrackerOnColorTex (Texture2D texColor)
	{
	}

	public bool GetHeadPosition (long userId, ref Vector3 headPos)
	{
		return false;
	}

	public bool GetHeadRotation (long userId, ref Quaternion headRot)
	{
		return false;
	}

	public bool GetAnimUnits (long userId, ref System.Collections.Generic.Dictionary<KinectInterop.FaceShapeAnimations, float> afAU)
	{
		return false;
	}

	public bool GetShapeUnits (long userId, ref System.Collections.Generic.Dictionary<KinectInterop.FaceShapeDeformations, float> afSU)
	{
		return false;
	}

	public int GetFaceModelVerticesCount (long userId)
	{
		return 0;
	}

	public bool GetFaceModelVertices (long userId, ref Vector3[] avVertices)
	{
		return false;
	}

	public int GetFaceModelTrianglesCount ()
	{
		return 0;
	}

	public bool GetFaceModelTriangles (bool bMirrored, ref int[] avTriangles)
	{
		return false;
	}

	public bool IsSpeechRecognitionAvailable (ref bool bNeedRestart)
	{
		bNeedRestart = false;
		return false;
	}

	public int InitSpeechRecognition (string sRecoCriteria, bool bUseKinect, bool bAdaptationOff)
	{
		return 0;
	}

	public void FinishSpeechRecognition ()
	{
	}

	public int UpdateSpeechRecognition ()
	{
		return 0;
	}

	public int LoadSpeechGrammar (string sFileName, short iLangCode)
	{
		return 0;
	}

	public void SetSpeechConfidence (float fConfidence)
	{
	}

	public bool IsSpeechStarted ()
	{
		return false;
	}

	public bool IsSpeechEnded ()
	{
		return false;
	}

	public bool IsPhraseRecognized ()
	{
		return false;
	}

	public string GetRecognizedPhraseTag ()
	{
		return string.Empty;
	}

	public void ClearRecognizedPhrase ()
	{
	}

	public bool IsBackgroundRemovalAvailable(ref bool bNeedRestart)
	{
		bBackgroundRemovalInited = KinectInterop.IsOpenCvAvailable(ref bNeedRestart);
		return bBackgroundRemovalInited;
	}
	
	public bool InitBackgroundRemoval(KinectInterop.SensorData sensorData, bool isHiResPrefered)
	{
		return KinectInterop.InitBackgroundRemoval(sensorData, isHiResPrefered);
	}
	
	public void FinishBackgroundRemoval(KinectInterop.SensorData sensorData)
	{
		KinectInterop.FinishBackgroundRemoval(sensorData);
		bBackgroundRemovalInited = false;
	}
	
	public bool UpdateBackgroundRemoval(KinectInterop.SensorData sensorData, bool isHiResPrefered, Color32 defaultColor)
	{
		return KinectInterop.UpdateBackgroundRemoval(sensorData, isHiResPrefered, defaultColor);
	}
	
	public bool IsBackgroundRemovalActive()
	{
		return bBackgroundRemovalInited;
	}
	
	public Rect GetForegroundFrameRect(KinectInterop.SensorData sensorData, bool isHiResPrefered)
	{
		return KinectInterop.GetForegroundFrameRect(sensorData, isHiResPrefered);
	}
	
	public int GetForegroundFrameLength(KinectInterop.SensorData sensorData, bool isHiResPrefered)
	{
		return KinectInterop.GetForegroundFrameLength(sensorData, isHiResPrefered);
	}
	
	public bool PollForegroundFrame(KinectInterop.SensorData sensorData, bool isHiResPrefered, Color32 defaultColor, ref byte[] foregroundImage)
	{
		return KinectInterop.PollForegroundFrame(sensorData, isHiResPrefered, defaultColor, ref foregroundImage);
	}
	
}
