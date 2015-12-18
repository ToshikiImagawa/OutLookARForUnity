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
    public class EKFCameraTracker
    {
        bool initFlag = false;
        public bool Init { get { return initFlag; } }
        public IEnumerator mStartCoroutine;

        List<Vector3> MapKeyVector = new List<Vector3>();
        MatOfFloat MapDescriptors;
        List<float> MapError = new List<float>();

        EKF ekf = new EKF(new Vector3(0.5f, 0.5f, 0.5f));
        public IEnumerator mEKFStartCoroutine;

        bool updateMapFlag = false;
        public bool UpdateMapFlag { get { return updateMapFlag; } }
        public void PointFeature(Mat src, out IList<Vector3> KeyVector, MatOfFloat Descriptors)
        {
            using (MatOfKeyPoint points = new MatOfKeyPoint())
            {
                src.ORBPointFeature(points, Descriptors);
                KeyPoint[] pointL = points.toArray();
                KeyVector = new Vector3[pointL.Length];
                for (int i = 0; i < pointL.Length; i++)
                {
                    KeyVector[i] = ARManager.Instance.ToPoint(pointL[i]);
                }
            }
        }
        public IEnumerator UpdateMap(Mat src)
        {
            IList<Vector3> KeyVector;
            MatOfFloat Descriptors = new MatOfFloat();
            PointFeature(src, out KeyVector, Descriptors);
            mStartCoroutine = UpdateMap(KeyVector, Descriptors);
            Debug.Log("Updat Map");
            while (mStartCoroutine != null)
            {
                bool aStatus = mStartCoroutine.MoveNext();
                if (!aStatus)
                {
                    Debug.Log("StartCoroutine done.");
                    mStartCoroutine = null;
                }
                yield return null;
            }
        }

        public IEnumerator AddEKF(Quaternion q, float error, IList<Vector3> KeyVector, MatOfFloat descriptors)
        {
            ekf.AddEnumerator(q, error, KeyVector, descriptors);
            yield return null;
        }

        public IEnumerator UpdateMap(IList<Vector3> KeyVector, MatOfFloat Descriptors)
        {
            float MinError = 500f;
            if (UpdateMapFlag)
            {
                throw new OutLookARException("Map更新中です。");
            }
            if (!initFlag)
            {
                throw new OutLookARException("初期化されていません");
            }
            if (KeyVector.Count != Descriptors.rows() || Descriptors.rows() <= 0)
            {
                throw new OutLookARException("KeyVectorとDescriptorsの数が合いません");
            }
            updateMapFlag = true;
            Debug.Log("Update Start");
            IList<DMatch> matches;
            matches = Utils.CrossMatcher(MapDescriptors, Descriptors).LowPassFilter();
            if (matches.Count <= 0)
            {
                updateMapFlag = false;
                throw new OutLookARException("マッチしませんでした");
            }
            Debug.Log("Updating <1>");
            yield return null;
            List<int> IndexL = new List<int>();
            List<Vector3> FromPointL = new List<Vector3>();
            List<Vector3> ToPointL = new List<Vector3>();
            for (int i = 0; i < KeyVector.Count; i++)
            {
                IndexL.Add(i);
            }
            Debug.Log("Updating <2>");
            foreach (DMatch match in matches)
            {
                IndexL.Remove(match.trainIdx);
                FromPointL.Add(MapKeyVector[match.queryIdx]);
                ToPointL.Add(KeyVector[match.trainIdx]);
            }
            yield return null;
            Debug.Log("Updating <3>");
            float minError;
            Quaternion q = LMedS(FromPointL, ToPointL, out minError);
            Debug.Log(minError);
            if (minError < 10f)
            {
                foreach (int id in IndexL)
                {
                    MapDescriptors.Add(Descriptors.row(id));
                    MapKeyVector.Add(new Quaternion(-q.x, -q.y, -q.z, q.w) * KeyVector[id]);
                    MapError.Add(minError + 10f);
                }
            }
            yield return null;
            Debug.Log("Updating <4>");
            SortedList<int, int> RemoveIDL = new SortedList<int, int>();
            foreach (DMatch match in matches)
            {
                Vector3 vec = q * MapKeyVector[match.queryIdx];
                float distanse = Mathf.Sqrt(Mathf.Pow((vec.x - KeyVector[match.trainIdx].x), 2) + Mathf.Pow((vec.y - KeyVector[match.trainIdx].y), 2) + Mathf.Pow((vec.z - KeyVector[match.trainIdx].z), 2));
                if (distanse > MinError)
                {
                    try
                    {
                        if (MapError[match.queryIdx] > minError)
                        {
                            RemoveIDL.Add(MapKeyVector.Count - match.queryIdx, match.queryIdx);
                        }
                        else if (MapError[match.queryIdx] < minError)
                        {
                            MapKeyVector[match.queryIdx] = (new Quaternion(-q.x, -q.y, -q.z, q.w) * KeyVector[match.trainIdx]);
                            MapError[match.queryIdx] = minError;
                        }
                    }
                    catch (InvalidCastException e)
                    {
                        Debug.Log(e);
                    }
                }
            }
            yield return null;
            MatOfFloat MapDescriptorsClone = MapDescriptors.clone() as MatOfFloat;
            foreach (int id in RemoveIDL.Values)
            {
                try
                {
                    MapDescriptorsClone.RemoveAt(id);
                    MapKeyVector.RemoveAt(id);
                    MapError.RemoveAt(id);
                }
                catch (InvalidCastException e)
                {
                    Debug.Log(e);
                }
            }
            MapDescriptorsClone.copyTo(MapDescriptors);
            updateMapFlag = false;
            Debug.Log("Update Comp");
        }

        public void SetMap(Mat src)
        {
            IList<Vector3> KeyVector;
            MatOfFloat Descriptors = new MatOfFloat();
            PointFeature(src, out KeyVector, Descriptors);
            SetMap(KeyVector, Descriptors);
        }
        public void SetMap(IList<Vector3> KeyVector, MatOfFloat Descriptors)
        {
            if (KeyVector.Count == Descriptors.rows() && Descriptors.rows() > 0)
            {
                MapKeyVector.AddRange(KeyVector);
                MapDescriptors = Descriptors;
                for (int i = 0; i < KeyVector.Count; i++)
                {
                    MapError.Add(0);
                }
                initFlag = true;
            }
        }
        public Quaternion RotationEstimation(Mat src)
        {
            IList<Vector3> KeyVector;
            MatOfFloat Descriptors = new MatOfFloat();
            PointFeature(src, out KeyVector, Descriptors);
            return RotationEstimation(KeyVector, Descriptors);
        }
        public Quaternion RotationEstimation(IList<Vector3> KeyVector, MatOfFloat Descriptors)
        {
            if (!initFlag)
            {
                throw new OutLookARException("初期化されていません");
            }
            if (KeyVector.Count != Descriptors.rows() || Descriptors.rows() <= 0)
            {
                throw new OutLookARException("KeyVectorとDescriptorsの数が合いません");
            }
            IList<DMatch> matches;
            matches = Utils.CrossMatcher(MapDescriptors, Descriptors).LowPassFilter();
            if (matches.Count <= 0)
            {
                throw new OutLookARException("マッチしませんでした");
            }
            List<Vector3> FromPointL = new List<Vector3>();
            List<Vector3> ToPointL = new List<Vector3>();
            foreach (DMatch match in matches)
            {
                FromPointL.Add(MapKeyVector[match.queryIdx]);
                ToPointL.Add(KeyVector[match.trainIdx]);
            }
            return LMedS(FromPointL, ToPointL);
        }

        public Quaternion RotationEstimation(Mat src, out float MinError)
        {
            IList<Vector3> KeyVector;
            MatOfFloat Descriptors = new MatOfFloat();
            PointFeature(src, out KeyVector, Descriptors);
            return RotationEstimation(KeyVector, Descriptors, out MinError);
        }
        public Quaternion RotationEstimation(IList<Vector3> KeyVector, MatOfFloat Descriptors, out float MinError)
        {
            if (!initFlag)
            {
                throw new OutLookARException("初期化されていません");
            }
            if (KeyVector.Count != Descriptors.rows() || Descriptors.rows() <= 0)
            {
                throw new OutLookARException("KeyVectorとDescriptorsの数が合いません");
            }
            IList<DMatch> matches;
            matches = Utils.CrossMatcher(MapDescriptors, Descriptors).LowPassFilter();
            if (matches.Count <= 0)
            {
                throw new OutLookARException("マッチしませんでした");
            }
            List<Vector3> FromPointL = new List<Vector3>();
            List<Vector3> ToPointL = new List<Vector3>();
            foreach (DMatch match in matches)
            {
                FromPointL.Add(MapKeyVector[match.queryIdx]);
                ToPointL.Add(KeyVector[match.trainIdx]);
            }
            return LMedS(FromPointL, ToPointL, out MinError);
        }
        Quaternion LMedS(IList<Vector3> FromPointL, IList<Vector3> ToPointL, float confidence = 0.99f, float outlier = 0.1f)
        {
            System.Random cRand = new System.Random();
            Quaternion MinQ = Quaternion.identity;
            float MinError = 1000000f;
            double nBuff = Math.Log(1 - confidence, (1 - (1 - outlier) * (1 - outlier)));
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
                        MinQ = q;
                    }
                    errorL.Clear();
                    i++;
                }
            }
            return MinQ;
        }
        Quaternion LMedS(IList<Vector3> FromPointL, IList<Vector3> ToPointL, out float MinError, float confidence = 0.99f, float outlier = 0.1f)
        {
            System.Random cRand = new System.Random();
            Quaternion MinQ = Quaternion.identity;
            MinError = 1000000f;
            double nBuff = Math.Log(1 - confidence, (1 - (1 - outlier) * (1 - outlier)));
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
                        MinQ = q;
                    }
                    errorL.Clear();
                    i++;
                }
            }
            return MinQ;
        }
/*
        public IEnumerator RotationEstimationEKF(Mat src,out float MinError,Action<Quaternion> rotation)
        {
            IList<Vector3> KeyVector;
            MatOfFloat Descriptors = new MatOfFloat();
            PointFeature(src, out KeyVector, Descriptors);
            return RotationEstimationEKF(KeyVector, Descriptors, out MinError);
        }*/
        public IEnumerator RotationEstimationEKF(IList<Vector3> KeyVector, MatOfFloat Descriptors,Action <float,Quaternion> callback)
        {
            if (!initFlag)
            {
                throw new OutLookARException("初期化されていません");
            }
            if (KeyVector.Count != Descriptors.rows() || Descriptors.rows() <= 0)
            {
                throw new OutLookARException("KeyVectorとDescriptorsの数が合いません");
            }
            IList<DMatch> matches;
            matches = Utils.CrossMatcher(MapDescriptors, Descriptors).LowPassFilter();
            if (matches.Count <= 0)
            {
                throw new OutLookARException("マッチしませんでした");
            }
            List<Vector3> FromPointL = new List<Vector3>();
            List<Vector3> ToPointL = new List<Vector3>();
            Dictionary<int,Vector3> points = new Dictionary<int,Vector3>();
            foreach (DMatch match in matches)
            {
                FromPointL.Add(MapKeyVector[match.queryIdx]);
                ToPointL.Add(KeyVector[match.trainIdx]);
            }
            float MinError;
            Quaternion rotation = LMedS(FromPointL, ToPointL, out MinError);
            callback(MinError,rotation);
            
            mEKFStartCoroutine = ekf.AddEnumerator(Quaternion.identity, 0, KeyVector, Descriptors);
            while (mStartCoroutine != null)
            {
                bool aStatus = mEKFStartCoroutine.MoveNext();
                if (!aStatus)
                {
                    Debug.Log("StartCoroutine done.");
                    mStartCoroutine = null;
                }
                yield return null;
            }
        }
        public IEnumerator InitEKF(Mat src)
        {
            IList<Vector3> KeyVector;
            MatOfFloat Descriptors = new MatOfFloat();
            PointFeature(src, out KeyVector, Descriptors);
            if (KeyVector.Count == Descriptors.rows() && Descriptors.rows() > 0)
            {
                MapKeyVector.AddRange(KeyVector);
                MapDescriptors = Descriptors;
                for (int i = 0; i < KeyVector.Count; i++)
                {
                    MapError.Add(0);
                }
                mEKFStartCoroutine = ekf.AddEnumerator(Quaternion.identity, 0, KeyVector, Descriptors);
                while (mStartCoroutine != null)
                {
                    bool aStatus = mEKFStartCoroutine.MoveNext();
                    if (!aStatus)
                    {
                        Debug.Log("StartCoroutine done.");
                        mStartCoroutine = null;
                    }
                    yield return null;
                }
                initFlag = true;
            }
        }
    }
}