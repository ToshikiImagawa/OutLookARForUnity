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
namespace OutLookAR.Test{
public class EKFTest : MonoBehaviour {

	// Use this for initialization
	void Start () {
		Mat m = Mat.eye (4, 4, CvType.CV_32FC1);
		m.put (0, 0, 2);
		Mat n = Mat.eye (4, 4, CvType.CV_32FC1);
		Mat o = m + n;
		o.put (0, 3, 2);
		Mat p = m * o;
		Debug.Log (m.ToStringMat () + " * " + o.ToStringMat () + " = " + p.ToStringMat ());

		p = p.resize (5);
		Debug.Log ("p.resize (5)" + p.ToStringMat ());
		p = p.resize(1,6);
		Debug.Log ("p.resize(1,6)" + p.ToStringMat ());

		Mat mu = Mat.zeros (10, 1, CvType.CV_32FC1);
		mu.put (4, 0, 5.599999);
		mu.put (5, 0, 6.599999);
		mu.put (7, 0, 7.59999999999);
		mu.put (8, 0, 8.599999999999999999999);

		// EKF Test
		Debug.Log("--------- EKF Test ---------");
		
		Debug.Log("-- EKF Setting --");
		OutLookAR.EKF ekf = new OutLookAR.EKF (new Vector3 (0.5f, 0.5f, 0.5f));
		Debug.Log(ekf.ToString());
		Debug.Log("-- EKF Setting --");
		
		Debug.Log("-- EKF Add Setting --");
		Quaternion q = Quaternion.AngleAxis( -30, -Vector3.forward );
		float error = 0f;
		List<Vector3> rl = new List<Vector3> ();
		rl.Add (new Vector3(5f,2f,3f).normalized);
		rl.Add (new Vector3(1f,2f,4f).normalized);
		Debug.Log(string.Format("Quaternion : {0}\n"+
			"Point : [{1},{2}]",q,new Vector3(5f,2f,3f).normalized,new Vector3(1f,2f,4f).normalized));
		Debug.Log("-- EKF Add Setting --");
		Debug.Log("-- EKF Add Running --");
		ekf.Add (q, error, rl);

		Debug.Log (string.Format("StateMatrix : {0}",ekf.StateMatrix.ToStringMat ()));
		Debug.Log (string.Format("VarianceCovarianceMatrix : {0}",ekf.VarianceCovarianceMatrix.ToStringMat ()));

		Debug.Log("-- EKF Add Running --");
		Debug.Log("-- EKF Add Setting --");
		q = Quaternion.Euler (Vector3.forward);
		error = 0.000001f;
		Dictionary<int,Vector3> poins = new Dictionary<int, Vector3> ();
		poins.Add (0, new Vector3 (1f, 2f, 1f).normalized);
		poins.Add (1, new Vector3 (1f, 1f, 3f).normalized);
		Debug.Log(string.Format("Quaternion : {0}\n"+
			"Point : [{1},{2}]",q,new Vector3(1f, 2f, 1f).normalized,new Vector3(1f, 1f, 3f).normalized));
		Debug.Log("-- EKF Add Setting --");
		Debug.Log("-- EKF Update Running --");
		ekf.Update (q,error,poins);

		Debug.Log (string.Format("StateMatrix : {0}",ekf.StateMatrix.ToStringMat ()));
		Debug.Log (string.Format("VarianceCovarianceMatrix : {0}",ekf.VarianceCovarianceMatrix.ToStringMat ()));
	
		Debug.Log("-- EKF Update Running --");
		Debug.Log("--------- EKF Test ---------");
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
}