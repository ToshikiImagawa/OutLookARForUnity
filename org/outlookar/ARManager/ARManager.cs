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
namespace OutLookAR
{
    public class ARManager : SingletonMonoBehaviour<ARManager>
    {
        public const int NO = 0;
        public const int CAMERA = 1;
        public const int MAP = 2;
        public const int ALL = 3;
        [SerializeField]
        float _horizontalAngle = 67f;
        [SerializeField]
        float _verticalAngle = 48f;
        [SerializeField]
        int _width = 640;
        [SerializeField]
        int _height = 480;
        int initialize = 0;
        public int InitializeCount { get { return initialize; } }
        public bool Initialize { get { return initialize == ALL; } }
        SphereMap _mainMap;
        ARCamera _mainCamera;
        /// <summary>
        /// Set the specified mainMap.
        /// </summary>
        /// <param name="mainMap">Main map.</param>
        public void Set(SphereMap mainMap)
        {
            if (mainMap == null)
                throw new OutLookARException("MainMapが初期化されていません。");
            _mainMap = mainMap;
            if (initialize == NO)
                initialize = MAP;
            if (initialize == CAMERA)
                initialize = ALL;
        }
        /// <summary>
        /// Set the specified mainCamera.
        /// </summary>
        /// <param name="mainCamera">Main camera.</param>
        public void Set(ARCamera mainCamera)
        {
            if (mainCamera == null)
                throw new OutLookARException("MainCameraが初期化されていません。");
            _mainCamera = mainCamera;
            if (initialize == NO)
                initialize = CAMERA;
            if (initialize == MAP)
                initialize = ALL;
        }
        /// <summary>
        /// Set the specified mainCamera and mainMap.
        /// </summary>
        /// <param name="mainCamera">Main camera.</param>
        /// <param name="mainMap">Main map.</param>
        public void Set(ARCamera mainCamera, SphereMap mainMap)
        {
            Set(mainCamera);
            Set(mainMap);
        }
        public SphereMap MainMap { get { return _mainMap; } }
        public ARCamera MainCamera { get { return _mainCamera; } }

        /// <summary>
        /// カメラと特徴点よりポイントを作成.
        /// </summary>
        /// <returns>The point.</returns>
        /// <param name="src">Source.</param>
        /// <param name="camera">Camera.</param>
        public Vector3 ToPoint(Point src)
        {
            if (!Initialize)
            {
                throw new OutLookARException("ARManagerが初期化されていません。");
            }
            if (src.x > MainCamera.Width || src.x < 0 || src.y > MainCamera.Height || src.y < 0)
            {
                throw new OutLookARException(string.Format("ポイントが画面外です。 X : {0}, Y : {1}", src.x, src.y));
            }
            Vector3 dst;
            float PointL = (float)Math.Sqrt(Math.Pow(src.x, 2) + Math.Pow(src.y, 2) + Math.Pow(MainCamera.FocalLengthH, 2));
            Point pointP = new Point(src.x - MainCamera.Width / 2, MainCamera.Height / 2 - src.y);
            float h = MainCamera.HorizontalAngle * (float)pointP.x / MainCamera.Width;
            float v = MainCamera.VerticalAngle * (float)pointP.y / MainCamera.Height;
            dst.x = (float)pointP.x * MainMap.Radius / PointL;
            dst.y = (float)pointP.y * MainMap.Radius / PointL;
            dst.z = MainCamera.FocalLengthH * MainMap.Radius / PointL;
            return dst;
        }
        /// <summary>
        /// カメラと特徴点よりポイントを作成.
        /// </summary>
        /// <returns>The point.</returns>
        /// <param name="src">Source.</param>
        /// <param name="camera">Camera.</param>
        public Vector3 ToPoint(KeyPoint src)
        {
            return ToPoint(src.pt);
        }


        void OnEnable()
        {
            ARManager.Instance.Set(new SphereMap());
            ARManager.Instance.Set(new ARCamera(_width, _height, _horizontalAngle, _verticalAngle));
        }
    }
}