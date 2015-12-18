/**
 * 
 *  You can not modify and use this source freely
 *  only for the development of application related OutLookAR.
 * 
 * (c) OutLookAR All rights reserved.
 * by Toshiki Imagawa
**/
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;
using System.Collections;
using System;

namespace OutLookAR
{
    /// <summary>
    /// EKF_SLAM.
    /// μ = Quaternion:{q1,q2,q3,q4}
    /// z = Point2f:{x,y} 
    /// </summary>
	public class EKF
    {
        Vector3 RotationError;
        readonly int Type = CvType.CV_32FC1;
        readonly double Infinite = 99999999;
        Mat _varianceCovarianceMatrix;
        Mat _stateMatrix;
        MatOfFloat _stateDescriptors;

        bool _updateStatus = false;
        public bool UpdateStatus { get { return _updateStatus; } }
        public delegate void UpdateEKF();
        public event UpdateEKF OnUpdate;

        // マップの基本姿勢
        Quaternion BaseRotation = Quaternion.identity;
        /// <summary>
        /// Gets the state matrix.
		/// 状態行列
        /// </summary>
        /// <value>The state matrix.</value>
        public Mat StateMatrix { get { return _stateMatrix; } }
        /// <summary>
        /// Gets the variance covariance matrix.
		/// 共分散行列
        /// </summary>
        /// <value>The variance covariance matrix.</value>
        public Mat VarianceCovarianceMatrix { get { return _varianceCovarianceMatrix; } }
        /// <summary>
        /// Gets the state descriptors.
		/// ディスクリプター
        /// </summary>
        /// <value>The state descriptors.</value>
        public MatOfFloat StateDescriptors { get { return _stateDescriptors; } }
        /// <summary>
        /// Gets the state rotation.
		/// 回転姿勢
        /// </summary>
        /// <value>The state rotation.</value>
        public Quaternion StateRotation { get { return new Quaternion((float)StateMatrix.get(0, 0)[0], (float)StateMatrix.get(1, 0)[0], (float)StateMatrix.get(2, 0)[0], (float)StateMatrix.get(3, 0)[0]); } }
        /// <summary>
        /// Gets the state key vectors.
		/// 特徴ベクトル
        /// </summary>
        /// <value>The state key vectors.</value>
        public List<Vector3> StateKeyVectors
        {
            get
            {
                List<Vector3> KeyVectors = new List<Vector3>();
                for (int i = 4; i < StateMatrix.rows(); i += 2)
                {
                    KeyVectors.Add(new Vector3((float)StateMatrix.get(i, 0)[0], (float)StateMatrix.get(i + 1, 0)[0]));
                }
                return KeyVectors;
            }
        }

        Mat Fx(int n)
        {
            if (n < 0)
            {
                throw new OutLookARException("引数が不正です。引数は0以上の整数である必要が有ります。");
            }
            Mat zeros = Mat.zeros(4, 4 + 3 * n, Type);
            zeros.put(0, 0, 1);
            zeros.put(1, 1, 1);
            zeros.put(2, 2, 1);
            zeros.put(3, 3, 1);
            return zeros;
        }
        Mat FxT(int n)
        {
            if (n < 0)
            {
                throw new OutLookARException("引数が不正です。引数は0以上の整数である必要が有ります。");
            }
            return Fx(n).t();
        }
        Mat FxJ(int j, int n)
        {
            if (j < 0 & n >= j)
            {
                throw new OutLookARException("引数が不正です。引数は0以上の整数である必要が有ります。またn>j。");
            }
            Mat mj = Mat.zeros(7, 4 + 3 * n, Type);
            mj.put(0, 0, 1);
            mj.put(1, 1, 1);
            mj.put(2, 2, 1);
            mj.put(3, 3, 1);

            mj.put(4, 3 * j + 1, 1);
            mj.put(5, 3 * j + 2, 1);
            mj.put(6, 3 * j + 3, 1);
            return mj;
        }

        Mat H(Vector3 v, Quaternion q)
        {
            Mat h = Mat.zeros(3, 7, Type);

            //{{2 (qy Y + qz Z)}, {2 (qy X - 2 qx Y + qw Z)}, {2 qz X - 2 qw Y - 4 qx Z}}
            h.put(0, 0, (double)(2 * (q.y * v.y - q.z * v.z)));
            h.put(1, 0, (double)(2 * (q.y * v.x - 2 * q.x * v.y + q.w * v.z)));
            h.put(2, 0, (double)(2 * (q.z * v.x - q.w * v.y - 2 * q.x * v.z)));

            //{{-4 qy X + 2 qx Y - 2 qw Z}, {2 (qx X + qz Z)}, {-2 qw X + 2 qz Y - 4 qy Z}}
            h.put(0, 1, (double)(2 * (-2 * q.y * v.x + q.x * v.y - q.w * v.z)));
            h.put(1, 1, (double)(2 * (q.x * v.x + q.z * v.z)));
            h.put(2, 1, (double)(2 * (-q.w * v.x + q.z * v.y - 2 * q.y * v.z)));

            //{{2 (-2 qz X + qw Y + qx Z)}, {-2 qw X - 4 qz Y + 2 qy Z}, {2 (qx X + qy Y)}}
            h.put(0, 2, (double)(2 * (-2 * q.z * v.x + q.w * v.y + q.x * v.z)));
            h.put(1, 2, (double)(2 * (-q.w * v.x - 2 * q.z * v.y + q.y * v.z)));
            h.put(2, 2, (double)(2 * (q.x * v.x + q.y * v.y)));

            //{{2 qz Y - 2 qy Z}, {-2 qz X + 2 qx Z}, {-2 (qy X + qx Y)}}
            h.put(0, 3, (double)(2 * (q.z * v.y - q.y * v.z)));
            h.put(1, 3, (double)(2 * (-q.z * v.x + q.x * v.z)));
            h.put(2, 3, (double)(-2 * (q.y * v.x + q.x * v.y)));

            //{{1 - 2 qy^2 - 2 qz^2}, {2 qx qy - 2 qz qw}, {2 qx qz - 2 qy qw}}
            h.put(0, 4, (double)(1 - 2 * Mathf.Pow(q.y, 2f) - 2 * Mathf.Pow(q.z, 2f)));
            h.put(1, 4, (double)(2 * (q.x * q.y - q.z * q.w)));
            h.put(2, 4, (double)(2 * (q.x * q.z - q.y * q.w)));

            //{{2 (qx qy + qz qw)}, {1 - 2 qx^2 - 2 qz^2}, {2 qy qz - 2 qx qw}}
            h.put(0, 5, (double)(2 * (q.x * q.y - q.z * q.w)));
            h.put(1, 5, (double)(1 - 2 * Mathf.Pow(q.x, 2f) - 2 * Mathf.Pow(q.z, 2f)));
            h.put(2, 5, (double)(2 * (q.y * q.z - q.x * q.w)));

            //{{2 qx qz - 2 qy qw}, {2 (qy qz + qx qw)}, {1 - 2 qx^2 - 2 qy^2}}
            h.put(0, 6, (double)(2 * (q.x * q.z - q.y * q.w)));
            h.put(1, 6, (double)(2 * (q.y * q.z + q.x * q.w)));
            h.put(2, 6, (double)(1 - 2 * Mathf.Pow(q.x, 2f) - 2 * Mathf.Pow(q.y, 2f)));

            return h;
        }

        Mat q;
        Mat Q
        {
            get
            {
                if (q == null)
                {
                    q = Mat.zeros(3, 3, Type);
                    q.put(0, 0, (double)Mathf.Pow(RotationError.x, 2));
                    q.put(1, 1, (double)Mathf.Pow(RotationError.y, 2));
                    q.put(2, 2, (double)Mathf.Pow(RotationError.z, 2));
                }
                return q;
            }
        }

        Mat r;
        Mat R(float error)
        {
            if (r == null)
            {
                r = Mat.eye(4, 4, Type);
            }
            r.put(0, 0, error);
            r.put(1, 1, error);
            r.put(2, 2, error);
            r.put(3, 3, error);
            return r;
        }

        Mat g(Mat mu, Quaternion q)
        {
            /*
			using (mu) {
				if (mu.rows () < 4 || mu.cols () != 1) {
					throw new OutLookARException ("Mat mu is rows >= 4 & cols() = 1 .");
				}
				int n = (mu.rows () - 4) / 3;
				Mat buffG;
				using (Mat qm = new Mat (4, 1, Type))
				using(Mat fxT = FxT (n)){
					qm.put (0, 0, (double)q.x);
					qm.put (1, 0, (double)q.y);
					qm.put (2, 0, (double)q.z);
					qm.put (3, 0, (double)q.w);
					buffG = mu + fxT * qm;
					qm.Dispose ();
					fxT.Dispose ();
				}
				return buffG;
			}
			*/
            using (mu)
            {
                if (mu.rows() < 4 || mu.cols() != 1)
                {
                    throw new OutLookARException("Mat mu is rows >= 4 & cols() = 1 .");
                }
                //Quaternion bq = new Quaternion ((float)mu.get (0, 0) [0], (float)mu.get (1, 0) [0], (float)mu.get (2, 0) [0], (float)mu.get (3, 0) [0]);
                Quaternion bq = BaseRotation;
                bq = bq * q;
                Mat buffG = new Mat();
                mu.copyTo(buffG);
                buffG.put(0, 0, (double)bq.x);
                buffG.put(1, 0, (double)bq.y);
                buffG.put(2, 0, (double)bq.z);
                buffG.put(3, 0, (double)bq.w);
                return buffG;
            }
        }

        Mat G(int n)
        {
            /*
			Mat I = Mat.eye (4 + 3 * n, 4 + 3 * n, Type);
			Mat zeros = Mat.zeros (4, 4, Type);
			return I + FxT (n) * zeros * Fx (n);
			*/
            return Mat.eye(4 + 3 * n, 4 + 3 * n, Type);
        }
        Mat GT(int n)
        {
            return G(n).t();
        }

        /// <summary>
        /// Update the EKF.
        /// 更新
        /// </summary>
        /// <param name="q">Q.</param>
        /// <param name="error">Error.</param>
        /// <param name="points">Points.</param>
        public void Update(Quaternion q, float error, Dictionary<int, Vector3> points)
        {
            _updateStatus = true;
            int nowSize = StateMatrix.rows();
            int N = (nowSize - 4) / 3;
            Mat uStateMatrix = g(StateMatrix, q);
            Mat uVarianceCovarianceMatrix = G(N) * VarianceCovarianceMatrix * GT(N) + FxT(N) * R(error) * Fx(N);
            Mat I = Mat.eye(nowSize, nowSize, Type);
            Quaternion uStateMatrix_q = new Quaternion((float)uStateMatrix.get(0, 0)[0], (float)uStateMatrix.get(1, 0)[0], (float)uStateMatrix.get(2, 0)[0], (float)uStateMatrix.get(3, 0)[0]);

            foreach (int pointID in points.Keys)
            {
                Vector3 ZT = points[pointID];
                Vector3 uStateMatrix_j = new Vector3((float)uStateMatrix.get(4 + pointID * 3, 0)[0], (float)uStateMatrix.get(4 + pointID * 3 + 1, 0)[0], (float)uStateMatrix.get(4 + pointID * 3 + 2, 0)[0]);
                Quaternion qi = new Quaternion(-uStateMatrix_q.x, -uStateMatrix_q.y, -uStateMatrix_q.z, uStateMatrix_q.w);
                Vector3 ZTH = qi * uStateMatrix_j;
                Mat h = H(uStateMatrix_j, uStateMatrix_q) * FxJ(pointID, N);
                Mat K = uVarianceCovarianceMatrix * h.t() * (h * uVarianceCovarianceMatrix * h.t() + Q).inv();
                Mat deltaZ = Mat.zeros(3, 3, Type);
                deltaZ = deltaZ.resize(3, 1);
                deltaZ.put(0, 0, (double)(ZT.x - ZTH.x));
                deltaZ.put(1, 0, (double)(ZT.y - ZTH.y));
                deltaZ.put(2, 0, (double)(ZT.z - ZTH.z));
                uStateMatrix = uStateMatrix + K * deltaZ;
                uVarianceCovarianceMatrix = (I - K * h) * uVarianceCovarianceMatrix;
            }
            _stateMatrix = uStateMatrix;
            _varianceCovarianceMatrix = uVarianceCovarianceMatrix;
            _updateStatus = false;
        }
        /// <summary>
        /// Update the EKF.
        /// 更新
        /// </summary>
        /// <param name="q">Q.</param>
        /// <param name="error">Error.</param>
        /// <param name="points">Points.</param>
        /// <param name="callback">Callback.</param>
        public IEnumerator UpdateEnumerator(Quaternion q, float error, Dictionary<int, Vector3> points)
        {
            _updateStatus = true;
            int nowSize = StateMatrix.rows();
            int N = (nowSize - 4) / 3;
            Mat uStateMatrix = g(StateMatrix, q);
            Mat uVarianceCovarianceMatrix = G(N) * VarianceCovarianceMatrix * GT(N) + FxT(N) * R(error) * Fx(N);
            yield return null;
            Mat I = Mat.eye(nowSize, nowSize, Type);
            Quaternion uStateMatrix_q = new Quaternion((float)uStateMatrix.get(0, 0)[0], (float)uStateMatrix.get(1, 0)[0], (float)uStateMatrix.get(2, 0)[0], (float)uStateMatrix.get(3, 0)[0]);

            foreach (int pointID in points.Keys)
            {
                Vector3 ZT = points[pointID];
                Vector3 uStateMatrix_j = new Vector3((float)uStateMatrix.get(4 + pointID * 3, 0)[0], (float)uStateMatrix.get(4 + pointID * 3 + 1, 0)[0], (float)uStateMatrix.get(4 + pointID * 3 + 2, 0)[0]);
                Quaternion qi = new Quaternion(-uStateMatrix_q.x, -uStateMatrix_q.y, -uStateMatrix_q.z, uStateMatrix_q.w);
                Vector3 ZTH = qi * uStateMatrix_j;
                Mat h = H(uStateMatrix_j, uStateMatrix_q) * FxJ(pointID, N);
                Mat K = uVarianceCovarianceMatrix * h.t() * (h * uVarianceCovarianceMatrix * h.t() + Q).inv();
                Mat deltaZ = Mat.zeros(3, 3, Type);
                deltaZ = deltaZ.resize(3, 1);
                deltaZ.put(0, 0, (double)(ZT.x - ZTH.x));
                deltaZ.put(1, 0, (double)(ZT.y - ZTH.y));
                deltaZ.put(2, 0, (double)(ZT.z - ZTH.z));
                uStateMatrix = uStateMatrix + K * deltaZ;
                uVarianceCovarianceMatrix = (I - K * h) * uVarianceCovarianceMatrix;
                yield return null;
            }
            _stateMatrix = uStateMatrix;
            _varianceCovarianceMatrix = uVarianceCovarianceMatrix;
            _updateStatus = false;
        }
        /// <summary>
        /// Add the EKF.
        /// 追加
        /// </summary>
        /// <param name="q">Q.</param>
        /// <param name="error">Error.</param>
        /// <param name="points">Points.</param>
        public void Add(Quaternion q, float error, IList<Vector3> points)
        {
            _updateStatus = true;
            int nowSize = StateMatrix.rows();
            int N = (nowSize - 4) / 3;
            int pointsCount = points.Count;
            int newSize = nowSize + pointsCount * 3;

            Mat uStateMatrix = g(StateMatrix, q);
            uStateMatrix = uStateMatrix.resize(newSize);

            Mat uVarianceCovarianceMatrix = G(N) * VarianceCovarianceMatrix * GT(N) + FxT(N) * R(error) * Fx(N);
            uVarianceCovarianceMatrix = uVarianceCovarianceMatrix.resize(newSize, newSize);
            for (int i = 0; i < pointsCount; i++)
            {
                for (int c = 0; c < 3; c++)
                {
                    uVarianceCovarianceMatrix.put(nowSize + c + 3 * i, nowSize + c + 3 * i, Infinite);
                }
            }
            Mat I = Mat.eye(newSize, newSize, Type);
            Quaternion uStateMatrix_q = new Quaternion((float)uStateMatrix.get(0, 0)[0], (float)uStateMatrix.get(1, 0)[0], (float)uStateMatrix.get(2, 0)[0], (float)uStateMatrix.get(3, 0)[0]);
            int j = N + 1;
            foreach (Vector3 point in points)
            {
                Vector3 uStateMatrix_j = uStateMatrix_q * point;
                uStateMatrix.put(j * 3 + 1, 0, uStateMatrix_j.x);
                uStateMatrix.put(j * 3 + 2, 0, uStateMatrix_j.y);
                uStateMatrix.put(j * 3 + 3, 0, uStateMatrix_j.z);
                Mat h = H(uStateMatrix_j, uStateMatrix_q) * FxJ(j, N + pointsCount);
                Mat K = uVarianceCovarianceMatrix * h.t() * (h * uVarianceCovarianceMatrix * h.t() + Q).inv();
                uVarianceCovarianceMatrix = (I - K * h) * uVarianceCovarianceMatrix;
                j++;
            }
            _stateMatrix = uStateMatrix;
            _varianceCovarianceMatrix = uVarianceCovarianceMatrix;
            _updateStatus = false;
        }
        /// <summary>
        /// Add the EKF.
        /// 追加
        /// </summary>
        /// <param name="q">Q.</param>
        /// <param name="error">Error.</param>
        /// <param name="points">Points.</param>
        /// <param name="Descriptors">Descriptors.</param>
        public IEnumerator AddEnumerator(Quaternion q, float error, IList<Vector3> points, MatOfFloat Descriptors)
        {
            _updateStatus = true;
            if (points.Count != Descriptors.rows())
                throw new OutLookARException("KeyVectorとDescriptorsの数が合いません");
            int nowSize = StateMatrix.rows();
            int N = (nowSize - 4) / 3;
            int pointsCount = points.Count;
            int newSize = nowSize + pointsCount * 3;

            Mat uStateMatrix = g(StateMatrix, q);
            uStateMatrix = uStateMatrix.resize(newSize);
            yield return null;
            Mat uVarianceCovarianceMatrix = G(N) * VarianceCovarianceMatrix * GT(N) + FxT(N) * R(error) * Fx(N);
            uVarianceCovarianceMatrix = uVarianceCovarianceMatrix.resize(newSize, newSize);
            for (int i = 0; i < pointsCount; i++)
            {
                for (int c = 0; c < 3; c++)
                {
                    uVarianceCovarianceMatrix.put(nowSize + c + 3 * i, nowSize + c + 3 * i, Infinite);
                }
                yield return null;
            }
            Mat I = Mat.eye(newSize, newSize, Type);
            Quaternion uStateMatrix_q = new Quaternion((float)uStateMatrix.get(0, 0)[0], (float)uStateMatrix.get(1, 0)[0], (float)uStateMatrix.get(2, 0)[0], (float)uStateMatrix.get(3, 0)[0]);
            int j = N + 1;
            foreach (Vector3 point in points)
            {
                Vector3 uStateMatrix_j = uStateMatrix_q * point;
                uStateMatrix.put(j * 3 + 1, 0, uStateMatrix_j.x);
                uStateMatrix.put(j * 3 + 2, 0, uStateMatrix_j.y);
                uStateMatrix.put(j * 3 + 3, 0, uStateMatrix_j.z);
                Mat h = H(uStateMatrix_j, uStateMatrix_q) * FxJ(j, N + pointsCount);
                Mat K = uVarianceCovarianceMatrix * h.t() * (h * uVarianceCovarianceMatrix * h.t() + Q).inv();
                uVarianceCovarianceMatrix = (I - K * h) * uVarianceCovarianceMatrix;

                yield return null;
                j++;
            }
            _stateMatrix = uStateMatrix;
            _varianceCovarianceMatrix = uVarianceCovarianceMatrix;
            _stateDescriptors.Add(Descriptors);
            _updateStatus = false;
        }

        public IEnumerator Run(Quaternion q, float error, IList<Vector3> points, MatOfFloat Descriptors)
        {
            if (points.Count != Descriptors.rows())
                throw new OutLookARException("KeyVectorとDescriptorsの数が合いません");
            int nowSize = StateMatrix.rows();
            int N = (nowSize - 4) / 3;
            int pointsCount = points.Count;
            int newSize = nowSize + pointsCount * 3;

            Mat uStateMatrix = g(StateMatrix, q);
            uStateMatrix = uStateMatrix.resize(newSize);
            yield return null;
            Mat uVarianceCovarianceMatrix = G(N) * VarianceCovarianceMatrix * GT(N) + FxT(N) * R(error) * Fx(N);
            uVarianceCovarianceMatrix = uVarianceCovarianceMatrix.resize(newSize, newSize);
            for (int i = 0; i < pointsCount; i++)
            {
                for (int c = 0; c < 3; c++)
                {
                    uVarianceCovarianceMatrix.put(nowSize + c + 3 * i, nowSize + c + 3 * i, Infinite);
                }
                yield return null;
            }
            Mat I = Mat.eye(newSize, newSize, Type);
            Quaternion uStateMatrix_q = new Quaternion((float)uStateMatrix.get(0, 0)[0], (float)uStateMatrix.get(1, 0)[0], (float)uStateMatrix.get(2, 0)[0], (float)uStateMatrix.get(3, 0)[0]);
            int j = N + 1;
            foreach (Vector3 point in points)
            {
                Vector3 uStateMatrix_j = uStateMatrix_q * point;
                uStateMatrix.put(j * 3 + 1, 0, uStateMatrix_j.x);
                uStateMatrix.put(j * 3 + 2, 0, uStateMatrix_j.y);
                uStateMatrix.put(j * 3 + 3, 0, uStateMatrix_j.z);
                Mat h = H(uStateMatrix_j, uStateMatrix_q) * FxJ(j, N + pointsCount);
                Mat K = uVarianceCovarianceMatrix * h.t() * (h * uVarianceCovarianceMatrix * h.t() + Q).inv();
                uVarianceCovarianceMatrix = (I - K * h) * uVarianceCovarianceMatrix;

                yield return null;
                j++;
            }
            _stateMatrix = uStateMatrix;
            _varianceCovarianceMatrix = uVarianceCovarianceMatrix;
            _stateDescriptors.Add(Descriptors);
        }

        public EKF(Vector3 error)
        {
            RotationError = error;
            // init
            _varianceCovarianceMatrix = Mat.zeros(4, 4, Type);
            _stateMatrix = new Mat(4, 1, Type);
            _stateMatrix.put(0, 0, 0);
            _stateMatrix.put(1, 0, 0);
            _stateMatrix.put(2, 0, 0);
            _stateMatrix.put(3, 0, 1);
            _stateDescriptors = new MatOfFloat(0, 0, Type);
        }

        public override string ToString()
        {
            return string.Format("RotationError : {0}", RotationError);
        }
    }
}