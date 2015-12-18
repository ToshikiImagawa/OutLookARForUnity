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
namespace OutLookAR
{
    public class ARCamera : ICloneable
    {
        float _horizontalAngle;
        float _verticalAngle;
        int _width;
        int _height;

        public float HorizontalAngle { get { return _horizontalAngle; } }
        public float VerticalAngle { get { return _verticalAngle; } }
        public int Width { get { return _width; } }
        public int Height { get { return _height; } }
        public float FocalLengthH { get { return Width / 2f / Mathf.Atan((HorizontalAngle / 2f).Radian()); } }
        public float FocalLengthV { get { return Height / 2f / Mathf.Atan((VerticalAngle / 2f).Radian()); } }

        public ARCamera(int width, int height, float horizontalAngle, float verticalAngle)
        {
            _horizontalAngle = horizontalAngle;
            _verticalAngle = verticalAngle;
            _width = width;
            _height = height;
        }
        public ARCamera(ARCamera other)
        {
            _horizontalAngle = other.HorizontalAngle;
            _verticalAngle = other.VerticalAngle;
            _width = other.Width;
            _height = other.Height;
        }
        public virtual object Clone()
        {
            return new ARCamera(this);
        }
    }
}