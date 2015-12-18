/**
 * 
 *  You can not modify and use this source freely
 *  only for the development of application related OutLookAR.
 * 
 * (c) OutLookAR All rights reserved.
 * by Toshiki Imagawa
**/
using System.Collections;
using UnityEngine;
using OpenCVForUnity;
namespace OutLookAR
{
    public class CaptureManager : SingletonMonoBehaviour<CaptureManager>
    {
        [SerializeField]
        bool AutoStart;

        /// <summary>
        /// The web cam texture.
        /// </summary>
        WebCamTexture webCamTexture;

        /// <summary>
        /// The web cam device.
        /// </summary>
        WebCamDevice webCamDevice;

        /// <summary>
        /// The colors.
        /// </summary>
        Color32[] colors;

        /// <summary>
        /// The width.
        /// </summary>
        [SerializeField]
        int width = 640;

        /// <summary>
        /// The height.
        /// </summary>
        [SerializeField]
        int height = 480;

        /// <summary>
        /// The rgba mat.
        /// </summary>
        Mat rgbaMat;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The init done.
        /// </summary>
        bool initDone = false;

        public delegate void UpdateTextur(Texture2D texture);
        public delegate void UpdateMat(Mat mat);

        public int Width { get { return width; } }
        public int Height { get { return height; } }
        
        public event UpdateTextur OnTextur;
        public event UpdateMat OnMat;

        void Change()
        {
            if (!initDone)
                return;

#if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
			if (webCamTexture.width > 16 && webCamTexture.height > 16) {
#else
            if (webCamTexture.didUpdateThisFrame)
            {
#endif

                OpenCVForUnity.Utils.webCamTextureToMat(webCamTexture, rgbaMat, colors);
                if (webCamTexture.videoVerticallyMirrored)
                {
                    if (webCamDevice.isFrontFacing)
                    {
                        if (webCamTexture.videoRotationAngle == 0)
                        {
                            Core.flip(rgbaMat, rgbaMat, 1);
                        }
                        else if (webCamTexture.videoRotationAngle == 90)
                        {
                            Core.flip(rgbaMat, rgbaMat, 0);
                        }
                        else if (webCamTexture.videoRotationAngle == 270)
                        {
                            Core.flip(rgbaMat, rgbaMat, 1);
                        }
                    }
                    else
                    {
                        if (webCamTexture.videoRotationAngle == 90)
                        {

                        }
                        else if (webCamTexture.videoRotationAngle == 270)
                        {
                            Core.flip(rgbaMat, rgbaMat, -1);
                        }
                    }
                }
                else
                {
                    if (webCamDevice.isFrontFacing)
                    {
                        if (webCamTexture.videoRotationAngle == 0)
                        {
                            Core.flip(rgbaMat, rgbaMat, 1);
                        }
                        else if (webCamTexture.videoRotationAngle == 90)
                        {
                            Core.flip(rgbaMat, rgbaMat, 0);
                        }
                        else if (webCamTexture.videoRotationAngle == 270)
                        {
                            Core.flip(rgbaMat, rgbaMat, 1);
                        }
                    }
                    else
                    {
                        if (webCamTexture.videoRotationAngle == 90)
                        {

                        }
                        else if (webCamTexture.videoRotationAngle == 270)
                        {
                            Core.flip(rgbaMat, rgbaMat, -1);
                        }
                    }
                }
                if (OnMat != null)
                    OnMat(rgbaMat);
                if (OnTextur != null)
                {
                    OpenCVForUnity.Utils.matToTexture2D(rgbaMat, texture, colors);
                    OnTextur(texture);
                }
            }
        }

        /// <summary>
        /// Init this instance.
        /// 初期化
        /// </summary>
        IEnumerator init()
        {
            Stop();
            webCamDevice = WebCamTexture.devices[0];
            webCamTexture = new WebCamTexture(webCamDevice.name, width, height);
            // Starts the camera
            webCamTexture.Play();

            while (true)
            {
                //If you want to use webcamTexture.width and webcamTexture.height on iOS, you have to wait until webcamTexture.didUpdateThisFrame == 1, otherwise these two values will be equal to 16. (http://forum.unity3d.com/threads/webcamtexture-and-error-0x0502.123922/)
#if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
				if (webCamTexture.width > 16 && webCamTexture.height > 16) {
#else
                if (webCamTexture.didUpdateThisFrame)
                {
#endif
                    colors = new Color32[webCamTexture.width * webCamTexture.height];

                    rgbaMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);

                    texture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);

                    initDone = true;

                    break;
                }
                else
                {
                    yield return 0;
                }
            }
        }

        /// <summary>
        /// Stop the capture.
        /// 停止
        /// </summary>
        public void Stop()
        {
            if (webCamTexture != null)
            {
                webCamTexture.Stop();
                initDone = false;
                if (rgbaMat != null)
                    rgbaMat.Dispose();
            }
        }
        /// <summary>
        /// Play the capture.
        /// 再生
        /// </summary>
        public void Play()
        {
            StartCoroutine(init());
        }

        // Use this for initialization
        protected virtual void Start()
        {
            if (AutoStart)
            {
                StartCoroutine(init());
            }
        }

        // Update is called once per frame
        protected virtual void Update()
        {
            Change();
        }
        void OnDisable()
        {
            webCamTexture.Stop();
        }
    }
}