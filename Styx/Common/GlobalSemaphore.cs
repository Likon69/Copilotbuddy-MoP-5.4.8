using System;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace Styx.Common
{
	internal sealed class GlobalSemaphore : IDisposable
	{
		private Semaphore semaphore_0;

		private GlobalSemaphore(Semaphore sem)
		{
			this.semaphore_0 = sem;
		}

		public void Dispose()
		{
			if (this.semaphore_0 == null)
			{
				return;
			}
			this.semaphore_0.Release();
			this.semaphore_0.Dispose();
			this.semaphore_0 = null;
		}

		internal static GlobalSemaphore Acquire(string name)
		{
			return GlobalSemaphore.TryAcquire(name, -1);
		}

		internal static GlobalSemaphore TryAcquire(string name, int timeoutMs)
		{
			SemaphoreAccessRule rule = new SemaphoreAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), SemaphoreRights.FullControl, AccessControlType.Allow);
			SemaphoreSecurity semaphoreSecurity = new SemaphoreSecurity();
			semaphoreSecurity.AddAccessRule(rule);
			bool flag = false;
			Semaphore semaphore = null;
			try
			{
				bool flag2;
				semaphore = SemaphoreAcl.Create(1, 1, string.Format("Global\\{0}", name), out flag2, semaphoreSecurity);
				flag = semaphore.WaitOne(timeoutMs, false);
			}
			finally
			{
				if (!flag)
				{
					if (semaphore != null)
					{
						semaphore.Dispose();
					}
					semaphore = null;
				}
			}
			if (semaphore != null)
			{
				return new GlobalSemaphore(semaphore);
			}
			return null;
		}
	}
}
