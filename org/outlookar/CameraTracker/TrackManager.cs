/**
 * 
 *  You can not modify and use this source freely
 *  only for the development of application related OutLookAR.
 * 
 * (c) OutLookAR All rights reserved.
 * by Toshiki Imagawa
**/
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;
namespace OutLookAR
{
    public class TrackManager : SingletonMonoBehaviour<TrackManager>
    {
        [SerializeField]
        Vector3 ekfError = new Vector3(0.5f, 0.5f, 0.5f);
        [SerializeField]
        float confidence = 0.99f;
        [SerializeField]
        float outlier = 0.1f;

        EKF _ekfModel;
        EKF EKFModel { get { return _ekfModel ?? (_ekfModel = new EKF(ekfError)); } }
        Tracker _trackerModel;
        Tracker TrackerModel { get { return _trackerModel ?? (_trackerModel = new Tracker(confidence, outlier)); } }

        bool MapUpdateFlag = false;
        enum TrackerType
        {
            Enable,
            StandBy,
            Disable,
        }
        TrackerType _state;
        TrackerType State { get { return _state; } }

        MatOfFloat _mapDescriptors;
        Vector3[] _mapKeyVectors;
        MatOfFloat MapDescriptors { get { return _mapDescriptors; } }
        Vector3[] MapKeyVectors { get { return _mapKeyVectors; } }

        public void Play() { }
        public void Stop() { }
        public void Reset() { }

        public delegate void UpdateMap();
        public delegate void InitMap();

        IEnumerator Init(IList<Vector3> baseKeyVectors, MatOfFloat baseDescriptors, Quaternion baseRotation)
        {
            _ekfModel = new EKF(ekfError);
            _trackerModel = new Tracker(confidence, outlier);
            EKFModel.OnUpdate += UpdateEKF;
            yield return StartCoroutine(EKFModel.AddEnumerator(baseRotation, 0.01f, baseKeyVectors, baseDescriptors));
            _state = TrackerType.StandBy;
        }

        void UpdateMat(Mat mat)
        {
            if (State == TrackerType.StandBy)
            {
                _state = TrackerType.Enable;
            }
            if (State == TrackerType.Enable)
            {
                try
                {
                    IList<Vector3> KeyVectors;
                    MatOfFloat Descriptors = new MatOfFloat();
                    Utils.ORBPointFeature(mat, out KeyVectors, Descriptors);
                    if (MapDescriptors == null || MapKeyVectors == null)
                    {
                        _state = TrackerType.StandBy;
                        throw new OutLookARException("MapDescriptors & MapKeyVectors :初期化されていません。");
                    }
                    IList<DMatch> matches;
                    Macher(KeyVectors, Descriptors, out matches);
                    List<Vector3> FromPointL = new List<Vector3>();
                    List<Vector3> ToPointL = new List<Vector3>();
                    foreach (DMatch match in matches)
                    {
                        FromPointL.Add(MapKeyVectors[match.queryIdx]);
                        ToPointL.Add(KeyVectors[match.trainIdx]);
                    }
                }
                catch (OutLookARException e)
                {
                    Debug.Log(e);
                }
            }
        }

        void Macher(IList<Vector3> KeyVector, MatOfFloat Descriptors, out IList<DMatch> matches)
        {
            if (KeyVector.Count != Descriptors.rows() || Descriptors.rows() <= 0)
            {
                _state = TrackerType.Disable;
                throw new OutLookARException("KeyVectorとDescriptorsの数が合いません");
            }
            matches = Utils.CrossMatcher(MapDescriptors, Descriptors).LowPassFilter();
            if (matches.Count <= 0)
            {
                throw new OutLookARException("マッチしませんでした");
            }
        }

        void UpdateEKF()
        {
            _mapDescriptors = EKFModel.StateDescriptors;
            _mapKeyVectors = EKFModel.StateKeyVectors.ToArray();
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
    public class Tracker
    {
        float _confidence, _outlier;
        public void Track(IList<Vector3> FromPointL, IList<Vector3> ToPointL, out Quaternion Rotation, out float MinError)
        {
            if (FromPointL.Count != ToPointL.Count)
                throw new OutLookARException("FromPointL & ToPointL: 引数が不正です. 同じ大きさのである必要があります.");
            System.Random cRand = new System.Random();
            Rotation = Quaternion.identity;
            MinError = 1000000f;
            double nBuff = Math.Log(1 - _confidence, (1 - (1 - _outlier) * (1 - _outlier)));
            int n = Math.Abs(nBuff - (int)nBuff) > 0 ? (int)nBuff + 1 : (int)nBuff;
            int i = 0;
            while (i < n)
            {
                int pt1ID = cRand.Next(FromPointL.Count);
                int pt2ID = cRand.Next(FromPointL.Count);
                if (pt1ID != pt2ID)
                {
                    Vector3[] FromPoints = { FromPointL[pt1ID], FromPointL[pt2ID] };
                    Vector3[] ToPoints = { ToPointL[pt1ID], ToPointL[pt2ID] };
                    Quaternion q = Utils.FromToLookRotation(FromPoints, ToPoints);
                    List<float> errorL = new List<float>();
                    for (int j = 0; j < FromPointL.Count; j++)
                    {
                        if (j != pt1ID && j != pt2ID)
                        {
                            Vector3 Estimation = q * FromPointL[j];
                            errorL.Add(Mathf.Sqrt((Mathf.Pow(Estimation.x - ToPointL[j].x, 2f) + Mathf.Pow(Estimation.y - ToPointL[j].y, 2f) + Mathf.Pow(Estimation.z - ToPointL[j].z, 2))));
                        }
                    }
                    if (errorL.Median() < MinError)
                    {
                        MinError = errorL.Median();
                        Rotation = q;
                    }
                    errorL.Clear();
                    i++;
                }
            }
        }
        public Tracker(float confidence = 0.99f, float outlier = 0.1f)
        {
            _confidence = confidence;
            _outlier = outlier;
        }
    }
}