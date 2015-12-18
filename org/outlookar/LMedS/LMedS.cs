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
    public class LMedS
    {
        double _outlier = 0.1;
        double _confidence = 0.99;

        public LMedS()
        {
        }
        public LMedS(double outlier)
        {
            _outlier = outlier;
        }
        public LMedS(double outlier, double confidence)
        {
            _outlier = outlier;
            _confidence = confidence;
        }
        public ARTransform findARTransform(IList<Point> FromPointL, IList<Point> ToPointL)
        {

            System.Random cRand = new System.Random();
            List<ARTransform> ARTransformL = new List<ARTransform>();
            double nBuff = Math.Log(1 - _confidence, (1 - (1 - _outlier) * (1 - _outlier)));
            int n = Math.Abs(nBuff - (int)nBuff) > 0 ? (int)nBuff + 1 : (int)nBuff;
            int i = 0;
            if (FromPointL.Count == ToPointL.Count && FromPointL.Count >= n)
            {
                while (i < n)
                {
                    int pt1ID = cRand.Next(FromPointL.Count);
                    int pt2ID = cRand.Next(FromPointL.Count);
                    if (pt1ID != pt2ID)
                    {
                        Point[] FromPoints = { FromPointL[pt1ID], FromPointL[pt2ID] };
                        Point[] ToPoints = { ToPointL[pt1ID], ToPointL[pt2ID] };
                        ARTransform arTrans = new ARTransform(FromPoints, ToPoints);
                        if (arTrans.Error >= 0)
                        {
                            List<double> errorL = new List<double>();
                            for (int j = 0; j < FromPointL.Count; j++)
                            {
                                if (j != pt1ID && j != pt2ID)
                                {
                                    Point Estimation = arTrans.Transform(FromPointL[j]);
                                    errorL.Add(Math.Sqrt((double)((Estimation.x - ToPointL[j].x) * (Estimation.x - ToPointL[j].x) + (Estimation.y - ToPointL[j].y) * (Estimation.y - ToPointL[j].y))));
                                }
                            }
                            arTrans.Error = (float)errorL.Median();
                            errorL.Clear();
                            ARTransformL.Add(arTrans);
                            i++;
                        }
                    }
                }
            }
            ARTransform MinARTransform = ARTransformL[0];
            foreach (ARTransform art in ARTransformL)
            {
                if (art.Error < MinARTransform.Error && art.Error >= 0)
                    MinARTransform = art;
            }
            ARTransformL.Clear();
            return MinARTransform;
        }
    }
    public struct ARTransform
    {

        public const float ERROR = -1f;

        public static Vector2 ScreenSize = new Vector2(512f, 512f);
        public static Vector2 RootMat { get { return new Vector2(ScreenSize.x / 2, ScreenSize.y / 2); } }

        float _tx, _ty, _radian;
        public float Tx { get { return _tx; } }
        public float Ty { get { return _ty; } }
        public float Radian { get { return _radian; } }
        public float Error { get; set; }

        public ARTransform(IList<Point> FromPoints, IList<Point> ToPoints)
        {
            Point[] FromRootPoints = {
                new Point (FromPoints [0].x - RootMat.x, FromPoints [0].y - RootMat.y),
                new Point (FromPoints [1].x - RootMat.x, FromPoints [1].y - RootMat.y)
            };
            Point[] ToRootPoints = {
                new Point (ToPoints [0].x - RootMat.x, ToPoints [0].y - RootMat.y),
                new Point (ToPoints [1].x - RootMat.x, ToPoints [1].y - RootMat.y)
            };

            double sum_xTo = 0, sum_yTo = 0, sum_xFrom = 0, sum_yFrom = 0;
            double c1 = 0, c2 = 0;

            double radian = 0, tx = 0, ty = 0;

            for (int i = 0; i < 2; i++)
            {
                sum_xTo += ToRootPoints[i].x;
                sum_yTo += ToRootPoints[i].y;
                sum_xFrom += FromRootPoints[i].x;
                sum_yFrom += FromRootPoints[i].y;
                c1 += ToRootPoints[i].x * FromRootPoints[i].y - ToRootPoints[i].y * FromRootPoints[i].x;
                c2 += ToRootPoints[i].x * FromRootPoints[i].y + ToRootPoints[i].y * FromRootPoints[i].y;
            }

            double tmpx = (double)(c2 * 2 - sum_xTo * sum_xFrom - sum_yTo * sum_yFrom);
            double tmpy = (double)(-c1 * 2 + sum_xTo * sum_yFrom - sum_yTo * sum_xFrom);

            if (Math.Abs(tmpx) + Math.Abs(tmpy) > 0)
            {
                radian = Math.Atan2(tmpy, tmpx);
                tx = ((double)sum_xTo - Math.Cos(radian) * (double)sum_xFrom + Math.Sin(radian) * (double)sum_yFrom) / 2;
                ty = ((double)sum_yTo - Math.Sin(radian) * (double)sum_xFrom - Math.Cos(radian) * (double)sum_yFrom) / 2;
                _tx = (float)tx;
                _ty = (float)ty;
                _radian = (float)radian;
                Error = 0;
            }
            else
            {
                _tx = 0;
                _ty = 0;
                _radian = 0;
                Error = ERROR;
            }
        }

        public override string ToString()
        {
            return "ARTransform: Tx = " + _tx + ", Ty = " + _ty + ", Radian = " + _radian + ", Error = " + Error;
        }

        public Point Transform(Point FromPoint)
        {
            if (Error >= 0)
            {
                Vector2 FromMat = new Vector2((float)FromPoint.x, (float)FromPoint.y);
                Matrix2x2 R = new Matrix2x2((float)Math.Cos((double)Radian), -(float)Math.Sin((double)Radian), (float)Math.Sin((double)Radian), (float)Math.Cos((double)Radian));
                Vector2 T = new Vector2(Tx, Ty);
                Vector2 ToMat = R * FromMat + RootMat + T - R * RootMat;
                return new Point(ToMat.x, ToMat.y);
            }
            else
            {
                return FromPoint;
            }
        }
    }
}