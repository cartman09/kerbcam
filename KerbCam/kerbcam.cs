using System;
using UnityEngine;
using KSP.IO;

// Plugin startup taken from:
// http://forum.kerbalspaceprogram.com/showthread.php/43027

namespace KerbCam {
	
	// Class purely for the purpose for injecting the plugin.
	public class Bootstrap : KSP.Testing.UnitTest
	{
		public Bootstrap()
		{
			var gameObject = new GameObject("KerbCam", typeof(KerbCam));
			UnityEngine.Object.DontDestroyOnLoad(gameObject);
		}
	}
	
	// Plugin behaviour class.
	public class KerbCam : MonoBehaviour
	{
		private bool isEnabled = false;

		private MainWindow mainWindow;
		
		private int keyIndex = 0;
		private AnimationCurve hdgCurve = null;
		private AnimationCurve pitchCurve = null;
		private AnimationCurve zoomCurve = null;
		
		private bool isRunning = false;
		private float curTime;
		
		public void OnLevelWasLoaded ()
		{
			isEnabled = (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
				&& HighLogic.LoadedScene == GameScenes.FLIGHT;

			if (mainWindow == null)
				mainWindow = new MainWindow();
			
			stopRunning();
			
			startCurve ();
		}
		
		public void Update()
		{
			if (!isEnabled)
				return;
			
			if (isRunning)
			{
				curTime += Time.deltaTime;
				if (curTime > (keyIndex-1))
				{
					stopRunning();
				}
				else
				{
					updateCamera();
				}
			}
		}
		
		private void startCurve()
		{
			keyIndex = 0;
			hdgCurve = new AnimationCurve();
			pitchCurve = new AnimationCurve();
			zoomCurve = new AnimationCurve();
		}
		
		private void updateCamera()
		{
			var cam = FlightCamera.fetch;
			cam.camHdg = hdgCurve.Evaluate(curTime);
			cam.camPitch = pitchCurve.Evaluate(curTime);
			cam.SetDistance(zoomCurve.Evaluate(curTime));
		}
		
		private void startRunning()
		{
			isRunning = true;
			curTime = 0F;
		}
		
		private void stopRunning()
		{
			isRunning = false;
		}
		
		public void OnGUI ()
		{
			if (!isEnabled)
				return;

			// Ignore input when running.
			// TODO: Allow the sequence to be stopped.
			if (isRunning)
				return;
			
			// TODO: Custom keybindings.
			if (Event.current.Equals(Event.KeyboardEvent("F4"))) {
				var cam = FlightCamera.fetch;
				// TODO: Proper GUI etc. so that timings can be tweaked.
				// For now, each key is one second apart.
				hdgCurve.AddKey (keyIndex, cam.camHdg);
				pitchCurve.AddKey (keyIndex, cam.camPitch);
				zoomCurve.AddKey (keyIndex, cam.Distance);
				keyIndex++;
			} else if (Event.current.Equals(Event.KeyboardEvent("F5"))) {
				startRunning ();
			} else if (Event.current.Equals(Event.KeyboardEvent("F6"))) {
				startCurve ();
			} else if (Event.current.Equals(Event.KeyboardEvent("F7"))) {
				// TODO: Find out if we can use these for other pathing techniques.
				Debug.Log(FlightCamera.fetch.transform.localPosition);
				Debug.Log(FlightCamera.fetch.transform.localRotation);
				Debug.Log(FlightCamera.fetch.transform.localScale);
				Debug.Log(FlightCamera.fetch.transform.right);
				Debug.Log(FlightCamera.fetch.transform.up);
				Debug.Log(FlightCamera.fetch.targetDirection);
				Debug.Log(FlightCamera.fetch.transform.forward);
				Debug.Log(FlightCamera.fetch.transform.rotation);
				Debug.Log(FlightCamera.fetch.camera);
			} else if (Event.current.Equals(Event.KeyboardEvent("F8"))) {
				mainWindow.ToggleWindow();
			}
		}
	}

	class MainWindow
	{
		private const int WINDOW_ID = 73469086; // xkcd/221 compliance.
		private bool isWindowOpen = false;
		private Rect windowPos;

		public MainWindow()
		{
			windowPos = new Rect(Screen.width / 2, Screen.height / 2, 100, 50);
		}

		public void ToggleWindow()
		{
			isWindowOpen = !isWindowOpen;
			if (isWindowOpen) {
				RenderingManager.AddToPostDrawQueue(3, new Callback(DrawGUI));
			} else {
				RenderingManager.RemoveFromPostDrawQueue(3, new Callback(DrawGUI));
			}
		}

		private void DrawGUI()
		{
			GUI.skin = HighLogic.Skin;
			windowPos = GUILayout.Window(
				WINDOW_ID, windowPos, DoGUI, "KerbCam",
				GUILayout.MinWidth(100),
				GUILayout.MinHeight(100));
		}

		private void DoGUI(int windowID)
		{
			GUILayout.BeginVertical();
			GUILayout.Button("DO SOMETHING");
			GUILayout.EndVertical();
			GUI.DragWindow();
		}
	}
}
