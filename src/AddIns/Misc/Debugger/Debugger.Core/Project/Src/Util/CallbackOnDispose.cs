﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Diagnostics;
using System.Threading;

namespace Debugger
{
	/// <summary>
	/// Invokes a callback when this class is disposed.
	/// </summary>
	sealed class CallbackOnDispose : IDisposable
	{
		Action callback;
		
		public CallbackOnDispose(Action callback)
		{
			if (callback == null)
				throw new ArgumentNullException("callback");
			this.callback = callback;
		}
		
		public void Dispose()
		{
			Action action = Interlocked.Exchange(ref callback, null);
			if (action != null) {
				action();
				#if DEBUG
				GC.SuppressFinalize(this);
				#endif
			}
		}
		
		#if DEBUG
		~CallbackOnDispose()
		{
			Debug.Fail("CallbackOnDispose was finalized without being disposed.");
		}
		#endif
	}
}
