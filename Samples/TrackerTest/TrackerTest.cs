/**
 * 
 *  You can not modify and use this source freely
 *  only for the development of application related OutLookAR.
 * 
 * (c) OutLookAR All rights reserved.
 * by Toshiki Imagawa
**/
using System;
using UnityEngine;
using OpenCVForUnity;
namespace OutLookAR.Test
{
    public class TrackerTest : MonoBehaviour
    {
        public float Error = 100f;
        GameObject cameraObject;
        GameObject CameraObject { get { return cameraObject ?? (cameraObject = GameObject.Find("ARCamera")); } }

        Mat CaptureMat = new Mat();
        CameraTracker _Tracker = new CameraTracker();
        // Use this for initialization
        void UpdateMap()
        {
            StartCoroutine(_Tracker.UpdateMap(CaptureMat));
        }

        bool init = true;
        void UpdateMat(Mat mat)
        {
            mat.copyTo(CaptureMat);
            if (init)
            {
                _Tracker.SetMap(CaptureMat);
                init = false;
            }
            Debug_Tracker (CaptureMat);
        }
        
        void Debug_Tracker(Mat src){
			if(!ARManager.Instance.Initialize){
				throw new OutLookARException ("ARManagerが初期化されていません");
			}
			try {
				float MinError;
				Quaternion newrotation = _Tracker.RotationEstimation (src,out MinError);
				if(MinError>Error)
					return;
				CameraObject.transform.localRotation = new Quaternion (-newrotation.x, -newrotation.y, -newrotation.z, newrotation.w);
				Debug.Log(CameraObject.transform.localRotation.eulerAngles);
			} catch (ApplicationException e) {
				Debug.Log(e);
			}
		}
		void OnGUI(){
			GUI.Box(new UnityEngine.Rect(10,10,100,90), "AR Menu");
			
			// Make the first button. If it is pressed, Application.Loadlevel (1) will be executed
			if(GUI.Button(new UnityEngine.Rect(20,40,80,20), "Update Map")) {
				if(!_Tracker.UpdateMapFlag){
					try{
						StartCoroutine(_Tracker.UpdateMap(CaptureMat));
					}catch(ApplicationException e){
						Debug.Log(e);
					}
				}else{
					Debug.Log("Updating Map");
				}
			}
			if(GUI.Button(new UnityEngine.Rect(20,70,80,20), "Reset Camera")) {
				_Tracker.SetMap (CaptureMat);
			}
		}
        
        void OnEnable()
        {
            CaptureManager.Instance.OnMat += UpdateMat;
        }
        void OnDisable()
        {
            CaptureManager.Instance.OnMat -= UpdateMat;
        }
    }
}