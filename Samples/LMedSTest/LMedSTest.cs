/**
 * 
 *  You can not modify and use this source freely
 *  only for the development of application related OutLookAR.
 * 
 * (c) OutLookAR All rights reserved.
 * by Toshiki Imagawa
**/
using System;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;
namespace OutLookAR.Test
{
    public class LMedSTest : MonoBehaviour
    {
        [SerializeField]
        GameObject cameraObject;
        GameObject CameraObject { get { return cameraObject ?? (cameraObject = GameObject.Find("ARCamera")); } }

        // Rotation
        float radZ;
        float thetaX;
        float thetaY;
        float thetaZ { get { return radZ * 180f / (float)Math.PI; } }

        [SerializeField]
        double filter;
        [SerializeField]
        float HorizontalAngle;
        [SerializeField]
        float VerticalAngle;
        [SerializeField]
        float Width;
        [SerializeField]
        float Height;
        Mat buffMat;
        Mat BuffMat
        {
            get
            {
                if (buffMat == null)
                {
                    buffMat = new Mat();
                    CaptureMat.copyTo(buffMat);
                }
                return buffMat;
            }
        }
        Mat CaptureMat = new Mat();
        LMedS _lMedS = new LMedS();

        void Run(Mat src1, Mat src2)
        {
            IList<DMatch> matches;
            MatOfKeyPoint keypoints1 = new MatOfKeyPoint(), keypoints2 = new MatOfKeyPoint();
            if (OutLookAR.Utils.ORBMatcher(src1, src2, keypoints1, keypoints2, out matches) == 0)
            {
                IList<DMatch> lowPassFilterMatches = matches.LowPassFilter();
                List<Point> matchPoint1L = new List<Point>();
                List<Point> matchPoint2L = new List<Point>();
                foreach (DMatch match in lowPassFilterMatches)
                {
                    matchPoint1L.Add(keypoints1.toArray()[match.queryIdx].pt);
                    matchPoint2L.Add(keypoints2.toArray()[match.trainIdx].pt);
                }
                if (matchPoint1L.Count > 0)
                {
                    ARTransform ARtorans = _lMedS.findARTransform(matchPoint2L.ToArray(), matchPoint1L.ToArray());

                    Debug.Log(string.Format("matchPoint1L.Count = {0} ,ARtorans.Error = {1}", matchPoint1L.Count, ARtorans.Error));

                    if (ARtorans.Error < filter)
                    {
                        Vector2 TTheta = new Vector2(HorizontalAngle * ARtorans.Tx / Width, VerticalAngle * ARtorans.Ty / Height);
                        Matrix2x2 R = new Matrix2x2((float)Math.Cos(-(double)ARtorans.Radian), -(float)Math.Sin(-(double)ARtorans.Radian), (float)Math.Sin(-(double)ARtorans.Radian), (float)Math.Cos(-(double)ARtorans.Radian));
                        Vector2 TRTheta = R * TTheta;
                        Debug.Log(R + " * " + TTheta + " = " + TRTheta);
                        float LthetaX = TRTheta.x,
                        LthetaY = TRTheta.y;
                        CameraObject.transform.localRotation = Quaternion.Euler(-LthetaY, -LthetaX, ARtorans.Radian.Degree());
                        Debug.Log(ARtorans);
                    }
                }
            }
        }
        // bool init = false;
        void UpdateMat(Mat mat)
        {
            //if (!init)
            {
                mat.copyTo(CaptureMat);
                Run(CaptureMat, BuffMat);
                //CaptureMat.copyTo(buffMat);
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

        void OnGUI()
        {
            GUI.Box(new UnityEngine.Rect(10, 10, 100, 90), "AR Menu");

            // Make the first button. If it is pressed, Application.Loadlevel (1) will be executed
            if (GUI.Button(new UnityEngine.Rect(20, 40, 80, 20), "Refresh Mat"))
            {
                CaptureMat.copyTo(buffMat);
            }
            if (GUI.Button(new UnityEngine.Rect(20, 70, 80, 20), "Reset Camera"))
            {
                CameraObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
            }
        }
    }
}