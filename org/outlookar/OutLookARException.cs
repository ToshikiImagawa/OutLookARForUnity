/**
 * 
 *  You can not modify and use this source freely
 *  only for the development of application related OutLookAR.
 * 
 * (c) OutLookAR All rights reserved.
 * by Toshiki Imagawa
**/
using System;
namespace OutLookAR{
	public class OutLookARException:Exception
	{
		public OutLookARException ()
		{
		}
		public OutLookARException(string message)
			: base(message)
		{
		}

		public OutLookARException(string message, Exception inner)
			: base(message, inner)
		{
		}
	}
}

