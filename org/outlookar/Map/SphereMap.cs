/**
 * 
 *  You can not modify and use this source freely
 *  only for the development of application related OutLookAR.
 * 
 * (c) OutLookAR All rights reserved.
 * by Toshiki Imagawa
**/

using UnityEngine;
using System;
using System.Collections.Generic;
using OpenCVForUnity;
namespace OutLookAR
{
    public class SphereMap : ICloneable
    {
        public const int SURF = 0;
        public const int SIFT = 1;
        public const int BRIEF = 2;
        public const int BRISK = 3;
        public const int ORB = 4;
        public const int FREAK = 5;

        int _type;
        float _radius;
        List<MatOfFloat> _descriptorL = new List<MatOfFloat>();
        List<Vector3> _pointL = new List<Vector3>();
        List<MatOfFloat> DescriptorL { get { return _descriptorL; } }
        List<Vector3> PointL { get { return _pointL; } }

        /// <summary>
        /// マップのタイプを取得
        /// </summary>
        /// <value>The type.</value>
        public int Type { get { return _type; } }
        /// <summary>
        /// マップの半径を取得
        /// </summary>
        /// <value>The radius.</value>
        public float Radius { get { return _radius; } }
        /// <summary>
        /// 指定したインデックスにあるディスクリプターのコピーを取得します。
        /// </summary>
        /// <param name="index">Index.</param>
        public MatOfFloat Descriptor(int index)
        {
			MatOfFloat m = new MatOfFloat();
            _descriptorL[index].copyTo(m);
            return m;
        }
        /// <summary>
        /// 指定したインデックスにあるポイントを取得します。
        /// </summary>
        /// <param name="index">Index.</param>
        public Vector3 Point(int index)
        {
            return _pointL[index];
        }
        /// <summary>
        /// ディスクリプターを新しい配列にコピーします。
        /// </summary>
        public IEnumerable<Mat> DescriptorToArray()
        {
            return _descriptorL.ToArray();
        }
        /// <summary>
        /// ポイントを新しい配列にコピーします。
        /// </summary>
        public IEnumerable<Vector3> PointToArray()
        {
            return _pointL.ToArray();
        }

        /// <summary>
        /// クラスの新しいインスタンスを初期化します。 <see cref="AROpenCV.SphereMap"/>
        /// </summary>
        /// <param name="radius">Radius.</param>
        /// <param name="type">Type.</param>
        public SphereMap(float radius = 1000f, int type = SURF)
        {
            _radius = radius;
            if (type >= SURF && type <= FREAK)
                _type = type;
        }
        /// <summary>
        /// クラスの新しいインスタンスを初期化します。 <see cref="AROpenCV.SphereMap"/>
        /// </summary>
        /// <param name="other">Other.</param>
        public SphereMap(SphereMap other)
        {
            _radius = other.Radius;
            _type = other.Type;
            _descriptorL = new List<MatOfFloat>(other.DescriptorL);
            _pointL = new List<Vector3>(other.PointL);
        }
        /// <summary>
        /// 現在のインスタンスのコピーである新しいオブジェクトを作成します。
        /// </summary>
        /// <returns>このインスタンスのコピーである新しいオブジェクト。</returns>
        /// <filterpriority>2</filterpriority>
        public virtual object Clone()
        {
            return new SphereMap(this);
        }
        /// <summary>
        /// マップの末尾にオブジェクトを追加します。
        /// </summary>
        /// <param name="point">Point.</param>
        /// <param name="descriptor">Descriptor.</param>
        public void Add(Vector3 point, MatOfFloat descriptor)
        {
            _pointL.Add(point);
            _descriptorL.Add(descriptor);
        }
        /// <summary>
        /// マップからすべての要素を削除します。
        /// </summary>
        public void Clear()
        {
            _pointL.Clear();
            _descriptorL.Clear();
        }
        /// <summary>
        /// マップに格納されている要素の数を取得します。
        /// </summary>
        /// <returns>このマップに格納されている要素の数(不正の場合 -1)。</returns>
        /// <value>The count.</value>
        public int Count
        {
            get
            {
                if (_pointL.Count != _descriptorL.Count)
                {
                    throw new OutLookARException("マップに格納されている要素が不正です。");
                }
                return _pointL.Count;
            }
        }
        /// <summary>
        /// 指定したインデックスにある要素を変更します。
        /// </summary>
        /// <param name="index">Index.</param>
        public void Clear(int index)
        {
            _pointL.RemoveAt(index);
            _descriptorL.RemoveAt(index);
        }
        /// <summary>
        /// 指定したインデックスにある要素を変更します。
        /// </summary>
        /// <param name="index">Index.</param>
        /// <param name="point">Point.</param>
        /// <param name="descriptor">Descriptor.</param>
        public void ChangeAt(int index, Vector3 point, MatOfFloat descriptor)
        {
            ChangeAt(index, point);
            ChangeAt(index, descriptor);
        }
        /// <summary>
        /// 指定したインデックスにある要素を変更します。
        /// </summary>
        /// <param name="index">Index.</param>
        /// <param name="descriptor">Descriptor.</param>
        public void ChangeAt(int index, MatOfFloat descriptor)
        {
            _descriptorL[index] = descriptor;
        }
        /// <summary>
        /// 指定したインデックスにある要素を変更します。
        /// </summary>
        /// <param name="index">Index.</param>
        /// <param name="point">Point.</param>
        public void ChangeAt(int index, Vector3 point)
        {
            _pointL[index] = point;
        }
    }
}