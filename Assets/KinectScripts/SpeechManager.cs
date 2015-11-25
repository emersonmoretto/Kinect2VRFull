using UnityEngine;
using System;
using System.Collections;
using System.IO;

public class SpeechManager : MonoBehaviour 
{
	// Grammar XML file name
	public string grammarFileName = "SpeechGrammar.grxml";
	
	// Grammar language (English by default)
	public int languageCode = 1033;
	
	// Required confidence
	public float requiredConfidence = 0f;
	
	// GUI Text to show messages.
	public GUIText debugText;

	// Is currently listening
	private bool isListening;
	
	// Current phrase recognized
	private bool isPhraseRecognized;
	private string phraseTagRecognized;
	
	// primary sensor data structure
	private KinectInterop.SensorData sensorData = null;
	
	// Bool to keep track of whether Kinect and SAPI have been initialized
	private bool sapiInitialized = false;
	
	// The single instance of SpeechManager
	private static SpeechManager instance;
	
	
	// returns the single SpeechManager instance
    public static SpeechManager Instance
    {
        get
        {
            return instance;
        }
    }
	
	// returns true if SAPI is successfully initialized, false otherwise
	public bool IsSapiInitialized()
	{
		return sapiInitialized;
	}
	
	// returns true if the speech recognizer is listening at the moment
	public bool IsListening()
	{
		return isListening;
	}
	
	// returns true if the speech recognizer has recognized a phrase
	public bool IsPhraseRecognized()
	{
		return isPhraseRecognized;
	}
	
	// returns the tag of the recognized phrase
	public string GetPhraseTagRecognized()
	{
		return phraseTagRecognized;
	}
	
	// clears the recognized phrase
	public void ClearPhraseRecognized()
	{
		isPhraseRecognized = false;
		phraseTagRecognized = String.Empty;
	}
	
	//----------------------------------- end of public functions --------------------------------------//
	
	void Start() 
	{
		try 
		{
			// get sensor data
			KinectManager kinectManager = KinectManager.Instance;
			if(kinectManager && kinectManager.IsInitialized())
			{
				sensorData = kinectManager.GetSensorData();
			}
			
			if(sensorData == null || sensorData.sensorInterface == null)
			{
				throw new Exception("Speech recognition cannot be started, because KinectManager is missing or not initialized.");
			}
			
			if(debugText != null)
			{
				debugText.GetComponent<GUIText>().text = "Please, wait...";
			}
			
			// ensure the needed dlls are in place and speech recognition is available for this interface
			bool bNeedRestart = false;
			if(sensorData.sensorInterface.IsSpeechRecognitionAvailable(ref bNeedRestart))
			{
				if(bNeedRestart)
				{
					KinectInterop.RestartLevel(gameObject, "SM");
					return;
				}
			}
			else
			{
				string sInterfaceName = sensorData.sensorInterface.GetType().Name;
				throw new Exception(sInterfaceName + ": Speech recognition is not supported!");
			}
			
			// Initialize the speech recognizer
			string sCriteria = String.Format("Language={0:X};Kinect=True", languageCode);
			int rc = sensorData.sensorInterface.InitSpeechRecognition(sCriteria, true, false);
	        if (rc < 0)
	        {
				string sErrorMessage = (new SpeechErrorHandler()).GetSapiErrorMessage(rc);
				throw new Exception(String.Format("Error initializing Kinect/SAPI: " + sErrorMessage));
	        }
			
			if(requiredConfidence > 0)
			{
				sensorData.sensorInterface.SetSpeechConfidence(requiredConfidence);
			}
			
			if(grammarFileName != string.Empty)
			{
				// copy the grammar file from Resources, if available
				if(!File.Exists(grammarFileName))
				{
					TextAsset textRes = Resources.Load(grammarFileName, typeof(TextAsset)) as TextAsset;
					
					if(textRes != null)
					{
						string sResText = textRes.text;
						File.WriteAllText(grammarFileName, sResText);
					}
				}

				// load the grammar file
				rc = sensorData.sensorInterface.LoadSpeechGrammar(grammarFileName, (short)languageCode);
		        if (rc < 0)
		        {
					string sErrorMessage = (new SpeechErrorHandler()).GetSapiErrorMessage(rc);
					throw new Exception("Error loading grammar file " + grammarFileName + ": " + sErrorMessage);
		        }
			}
			
			instance = this;
			sapiInitialized = true;
			
			//DontDestroyOnLoad(gameObject);

			if(debugText != null)
			{
				debugText.GetComponent<GUIText>().text = "Ready.";
			}
		} 
		catch(DllNotFoundException ex)
		{
			Debug.LogError(ex.ToString());
			if(debugText != null)
				debugText.GetComponent<GUIText>().text = "Please check the Kinect and SAPI installations.";
		}
		catch (Exception ex) 
		{
			Debug.LogError(ex.ToString());
			if(debugText != null)
				debugText.GetComponent<GUIText>().text = ex.Message;
		}
	}

	void OnDestroy()
	{
		if(sapiInitialized && sensorData != null && sensorData.sensorInterface != null)
		{
			// finish speech recognition
			sensorData.sensorInterface.FinishSpeechRecognition();
		}
		
		sapiInitialized = false;
		instance = null;
	}
	
	void Update () 
	{
		// start Kinect speech recognizer as needed
//		if(!sapiInitialized)
//		{
//			StartRecognizer();
//			
//			if(!sapiInitialized)
//			{
//				Application.Quit();
//				return;
//			}
//		}
		
		if(sapiInitialized)
		{
			// update the speech recognizer
			int rc = sensorData.sensorInterface.UpdateSpeechRecognition();
			
			if(rc >= 0)
			{
				// estimate the listening state
				if(sensorData.sensorInterface.IsSpeechStarted())
				{
					isListening = true;
				}
				else if(sensorData.sensorInterface.IsSpeechEnded())
				{
					isListening = false;
				}

				// check if a grammar phrase has been recognized
				if(sensorData.sensorInterface.IsPhraseRecognized())
				{
					isPhraseRecognized = true;
					
					phraseTagRecognized = sensorData.sensorInterface.GetRecognizedPhraseTag();
					sensorData.sensorInterface.ClearRecognizedPhrase();
					
					//Debug.Log(phraseTagRecognized);
				}
			}
		}
	}
	
	void OnGUI()
	{
		if(sapiInitialized)
		{
			if(debugText != null)
			{
				if(isPhraseRecognized)
				{
					debugText.GetComponent<GUIText>().text = phraseTagRecognized;
				}
				else if(isListening)
				{
					debugText.GetComponent<GUIText>().text = "Listening...";
				}
			}
		}
	}
	
	
}
