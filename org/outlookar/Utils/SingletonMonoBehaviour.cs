/**
 * 
 *  You can not modify and use this source freely
 *  only for the development of application related OutLookAR.
 * 
 * (c) OutLookAR All rights reserved.
 * by Toshiki Imagawa
**/
using UnityEngine;
namespace OutLookAR{
    public class SingletonMonoBehaviour<T> : MonoBehaviour where T : SingletonMonoBehaviour<T>
	{
		protected static T instance;
		/// <summary>
		/// Gets the instance.
		/// </summary>
		/// <value>The instance.</value>
		public static T Instance {
			get {
				if (instance == null) {
					instance = (T)FindObjectOfType (typeof(T));

					if (instance == null) {
						Debug.LogWarning (typeof(T) + "is nothing");
					}
				}

				return instance;
			}
		}
		/// <summary>
		/// Determines if has instance.
		/// </summary>
		/// <returns><c>true</c> if has instance; otherwise, <c>false</c>.</returns>
		public static bool HasInstance(){
			if (instance == null) {
				instance = (T)FindObjectOfType (typeof(T));
				if (instance == null) {
					return false;
				}
			}
			return true;
		}

		protected virtual void Awake()
		{
			CheckInstance();
		}

		protected bool CheckInstance()
		{
			if( instance == null)
			{
				instance = (T)this;
				return true;
			}else if( Instance == this )
			{
				return true;
			}

			Destroy(this);
			return false;
		}
	}
}