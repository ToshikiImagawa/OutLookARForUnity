/**
 * 
 *  You can not modify and use this source freely
 *  only for the development of application related OutLookAR.
 * 
 * (c) OutLookAR All rights reserved.
 * by Toshiki Imagawa
**/
using System.Collections.Generic;
namespace OutLookAR
{
    using OpenCVForUnity;
    public class Map : DisposableOpenCVObject{
		List<MapPoint> _mapPoints = new List<MapPoint>();

		public Mat Descriptors{ 
			get {
				int rows = _mapPoints.Count;
				if (rows <= 0) {
					return null;
				}
				int cols = _mapPoints [0].Descriptor.cols ();
				Mat descriptors = new Mat (rows,cols,CvType.CV_64F);
				for (int r = 0; r < rows; r++) {
					Mat descriptor = _mapPoints [r].Descriptor;
					for (int c = 0; c < cols; c++) {
						descriptors.put (r, c, descriptor.get (0, c));
					}
				}
				return descriptors;
			}
		}

		protected override void Dispose (bool disposing)
		{
			#if UNITY_PRO_LICENSE || ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR) || UNITY_5

			try {
				_mapPoints.Clear();
				_mapPoints = null;

			} finally {
				base.Dispose (disposing);
			}

			#else

			#endif
		}

		public void Add(MapPoint point){
			_mapPoints.Add (point);
		}

		public void Clear(){
			_mapPoints.Clear ();
		}

		public void RemoveAt(int index){
			_mapPoints.RemoveAt (index);
		}

		public void Remove(MapPoint item){
			_mapPoints.Remove (item);
		}
	}
}