using System;

namespace Tripper.RecastManaged.Detour
{
	public struct Status
	{
		public Status(uint status)
		{
			this.Id = status;
		}

		public override string ToString()
		{
			StatusDetailFlag id = (StatusDetailFlag)this.Id;
			return ((StatusDetailFlag)id).ToString();
		}

		public bool Failed
		{
			get
			{
				return (this.Id & (uint)StatusDetailFlag.Failure) != 0;
			}
		}

		public bool Succeeded
		{
			get
			{
				return (this.Id & (uint)StatusDetailFlag.Success) != 0;
			}
		}

		public bool InProgress
		{
			get
			{
				return (this.Id & (uint)StatusDetailFlag.InProgress) != 0;
			}
		}

		public StatusDetailFlag Flags
		{
			get
			{
				return (StatusDetailFlag)this.Id;
			}
		}

		public bool HasFlag(StatusDetailFlag flag)
		{
			return (this.Id & (uint)flag) != 0;
		}

		static Status()
		{
			Status status;
			status.Id = 2147483648U;
			Status.Failure = status;
			Status status2;
			status2.Id = 1073741824U;
			Status.Success = status2;
		}

		public uint Id;

		public static readonly Status Failure;

		public static readonly Status Success;
	}
}
