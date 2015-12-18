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
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
namespace OutLookAR{
	using OpenCVForUnity;
	public class MapPoint : DisposableOpenCVObject{
		Point3 _point;
		Mat _descriptor;
		public Point3 Point{ get { return _point; } }
		public Mat Descriptor{ get { return _descriptor; } }
		protected override void Dispose (bool disposing)
		{
			#if UNITY_PRO_LICENSE || ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR) || UNITY_5

			try {
				_point = null;
				_descriptor.Dispose();
				_descriptor = null;
			} finally {
				base.Dispose (disposing);
			}
			#else

			#endif
		}
		public override string ToString ()
		{
			return "{" + _point.x + ", " + _point.y + ", " + _point.z + "}\n" + _descriptor;
		}
		public MapPoint(Point3 point ,Mat descriptor){
			_point = point;
			_descriptor = descriptor;
		}
	}
}