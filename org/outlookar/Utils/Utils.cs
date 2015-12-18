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
namespace OutLookAR
{
    public static class Utils
    {
        static DescriptorMatcher matcher = DescriptorMatcher.create(DescriptorMatcher.BRUTEFORCE_HAMMINGLUT);

        /// <summary>
        /// Crosses the matcher.
        /// </summary>
        /// <returns>The matcher.</returns>
        /// <param name="queryDescriptors">Query descriptors.</param>
        /// <param name="trainDescriptors">Train descriptors.</param>
        public static IList<DMatch> CrossMatcher(MatOfFloat queryDescriptors, MatOfFloat trainDescriptors)
        {
            MatOfDMatch matchQT = new MatOfDMatch(), matchTQ = new MatOfDMatch();
            List<DMatch> bmatch = new List<DMatch>();
            DMatch[] dmatch;
            if (trainDescriptors.cols() <= 0)
                throw new ApplicationException("CrossMatcherの引数trainDescriptorsがありません。");
            matcher.match(queryDescriptors, trainDescriptors, matchQT);
            if (queryDescriptors.cols() <= 0)
                throw new ApplicationException("CrossMatcherの引数queryDescriptorsがありません。");
            matcher.match(trainDescriptors, queryDescriptors, matchTQ);
            for (int i = 0; i < matchQT.rows(); i++)
            {
                DMatch forward = matchQT.toList()[i];
                DMatch backward = matchTQ.toList()[forward.trainIdx];
                if (backward.trainIdx == forward.queryIdx)
                    bmatch.Add(forward);
            }
            dmatch = bmatch.ToArray();
            bmatch.Clear();
            return dmatch;
        }

        public static int ORBMatcher(Mat queryMat, Mat trainMat, MatOfKeyPoint queryKeypoints, MatOfKeyPoint trainKeypoints, out IList<DMatch> matches)
        {
            using (MatOfFloat queryDescriptors = new MatOfFloat())
            using (MatOfFloat trainDescriptors = new MatOfFloat())
            {
                queryMat.ORBPointFeature(queryKeypoints, queryDescriptors);
                trainMat.ORBPointFeature(trainKeypoints, trainDescriptors);
                if (queryDescriptors.type() == CvType.CV_8U && trainDescriptors.type() == CvType.CV_8U)
                {
                    matches = Utils.CrossMatcher(queryDescriptors, trainDescriptors);
                    if (matches.Count > 0)
                    {
                        return 0;
                    }
                    else
                    {
                        return -1;
                    }
                }
                else
                {
                    matches = null;
                    return -1;
                }
            }
        }
        public static void ORBPointFeature(Mat src, out IList<Vector3> KeyVector, MatOfFloat Descriptors)
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

        /// <summary>
        /// Vectors the length.
        /// ベクトルの長さを計算する
        /// </summary>
        /// <returns>The length.</returns>
        /// <param name="V">V.</param>
        public static float VectorLength(Vector2 V)
        {
            return (float)Math.Sqrt(V.x * V.x + V.y * V.y);
        }
        /// <summary>
        /// Dots the product.
        /// ベクトル内積
        /// </summary>
        /// <returns>The product.</returns>
        /// <param name="vl">Vl.</param>
        /// <param name="vr">Vr.</param>
        public static float DotProduct(Vector2 vl, Vector2 vr)
        {
            return vl.x * vr.x + vl.y * vr.y;
        }
        /// <summary>
        /// Crosses the product.
        /// ベクトル外積
        /// </summary>
        /// <returns>The product.</returns>
        /// <param name="vl">Vl.</param>
        /// <param name="vr">Vr.</param>
        public static float CrossProduct(Vector2 vl, Vector2 vr)
        {
            return vl.x * vr.y - vl.y * vr.x;
        }

        /// <summary>
        /// Froms to look rotation.
        /// </summary>
        /// <returns>The to look rotation.</returns>
        /// <param name="fromDirections">From directions.</param>
        /// <param name="toDirections">To directions.</param>
        public static Quaternion FromToLookRotation(IList<Vector3> fromDirections, IList<Vector3> toDirections)
        {
            if (fromDirections.Count != 2 || toDirections.Count != 2)
            {
                throw new ApplicationException("FromToLookRotationの引数が2つずつではありません。");
            }
            var forDirections = new Vector3[2];

            forDirections[0] = toDirections[0].normalized - fromDirections[0].normalized;
            forDirections[1] = toDirections[1].normalized - fromDirections[1].normalized;
            var fromDirectionM = fromDirections[0].normalized + (fromDirections[1].normalized - fromDirections[0].normalized) / 2;
            var toDirectionM = toDirections[0].normalized + (toDirections[1].normalized - toDirections[0].normalized) / 2;

            var axis = Vector3.Cross(forDirections[0].normalized, forDirections[1].normalized);
            var orthogonal = Vector3.Project(fromDirectionM.normalized, axis.normalized);
            var fromDirectionMO = fromDirectionM.normalized - orthogonal;
            var toDirectionMO = toDirectionM.normalized - orthogonal;

            axis = Vector3.Cross(fromDirectionMO.normalized, toDirectionMO.normalized);
            float theta = Vector3.Angle(fromDirectionMO.normalized, toDirectionMO.normalized);

            var q = Quaternion.AngleAxis(theta, axis.normalized);
            return q;
        }
    }
}