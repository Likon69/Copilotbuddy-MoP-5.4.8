using System;
using System.Runtime.InteropServices;
using Styx.Helpers;
using Tripper.Tools.Math;

namespace Styx.WoWInternals
{
    public class WoWMovementInfo : WoWSimpleMovementInfo
    {
        private CMovementData Struct
        {
            get
            {
                if (_structCache == null)
                {
                    _structCache = new PerFrameCachedValue<CMovementData>(() => StyxWoW.Memory.Read<CMovementData>(BaseAddress));
                }

                return _structCache;
            }
        }

        public WoWMovementInfo(IntPtr ptr)
            : base(ptr)
        {
        }

        public float DirectionCosX
        {
            get { return Struct.Base.DirectionCosX; }
        }

        public float DirectionSinX
        {
            get { return Struct.Base.DirectionSinX; }
        }

        public float DirectionCosY
        {
            get { return Struct.Base.DirectionCosY; }
        }

        public float DirectionSinY
        {
            get { return Struct.Base.DirectionSinY; }
        }

        public uint FallTime
        {
            get { return Struct.Base.FallTime; }
        }

        public float FallStartHeight
        {
            get { return Struct.Base.FallStartHeight; }
        }

        public float LastFallHeight
        {
            get { return Struct.Base.LastFallHeight; }
        }

        public float CurrentSpeed
        {
            get { return Struct.Base.CurrentSpeed; }
        }

        public float BackwardSpeed
        {
            get { return Struct.Base.BackwardSpeed; }
        }

        public float ForwardSpeed
        {
            get { return Struct.Base.ForwardSpeed; }
        }

        public float RunSpeed
        {
            get { return Struct.Base.RunForwardSpeed; }
        }

        public float SwimmingForwardSpeed
        {
            get { return Struct.Base.SwimmingForwardSpeed; }
        }

        public float SwimmingBackwardSpeed
        {
            get { return Struct.Base.SwimmingBackwardSpeed; }
        }

        public float FlyingForwardSpeed
        {
            get { return Struct.Base.FlyingForwardSpeed; }
        }

        public float FlyingBackwardSpeed
        {
            get { return Struct.Base.FlyingBackwardSpeed; }
        }

        public float HoverHeight
        {
            get { return Struct.Base.HoverHeight; }
        }

        public uint TimeMoved
        {
            get { return StyxWoW.Memory.Read<uint>(BaseAddress + 0x54); }
        }

        private uint MovementFlags
        {
            get { return Struct.Base.Flags; }
        }

        public bool IsFalling
        {
            get { return GetFlag(MoveFlags.Falling); }
        }

        public bool MovingForward
        {
            get { return GetFlag(MoveFlags.Forward); }
        }

        public bool MovingBackward
        {
            get { return GetFlag(MoveFlags.Backward); }
        }

        public bool MovingStrafeLeft
        {
            get { return GetFlag(MoveFlags.StrafeLeft); }
        }

        public bool MovingStrafeRight
        {
            get { return GetFlag(MoveFlags.StrafeRight); }
        }

        public bool MovingTurnLeft
        {
            get { return GetFlag(MoveFlags.Left); }
        }

        public bool MovingTurnRight
        {
            get { return GetFlag(MoveFlags.Right); }
        }

        public bool IsMoving
        {
            get { return GetFlag(MoveFlags.MotionMask); }
        }

        public bool IsStrafing
        {
            get { return GetFlag(MoveFlags.StrafeMask); }
        }

        public bool IsSwimming
        {
            get { return GetFlag(MoveFlags.Swimming); }
        }

        public bool IsFlying
        {
            get { return GetFlag(MoveFlags.Flying); }
        }

        public bool CanFly
        {
            get { return GetFlag(MoveFlags.CanFly); }
        }

        public bool IsAscending
        {
            get { return GetFlag(MoveFlags.Ascending); }
        }

        public bool IsDescending
        {
            get { return GetFlag(MoveFlags.Descending); }
        }

        public bool JumpingOrShortFalling
        {
            get { return GetFlag(MoveFlags.Falling); }
        }

        public bool FallingFar
        {
            get { return GetFlag(MoveFlags.FallMask) || GetFlag(MoveFlags.FallingFar); }
        }

        public WoWPoint GroundNormal
        {
            get { return Struct.GroundNormal; }
        }

        private bool GetFlag(MoveFlags flag)
        {
            return (MovementFlags & (uint)flag) != 0U;
        }

        public override string ToString()
        {
            return string.Format("Position: {0}, Heading: {1}, TransportGuid: {2}, DirectionCosX: {3}, DirectionSinX: {4}, DirectionCosY: {5}, DirectionSinY: {6}, FallTime: {7}, FallStartHeight: {8}, LastFallHeight: {9}, CurrentSpeed: {10}, BackwardSpeed: {11}, ForwardSpeed: {12}, RunSpeed: {13}, SwimmingForwardSpeed: {14}, SwimmingBackwardSpeed: {15}, FlyingForwardSpeed: {16}, FlyingBackwardSpeed: {17}, HoverHeight: {18}, TimeMoved: {19}, MovementFlags: {20}, IsFalling: {21}, MovingForward: {22}, MovingBackward: {23}, MovingStrafeLeft: {24}, MovingStrafeRight: {25}, MovingTurnLeft: {26}, MovingTurnRight: {27}, IsMoving: {28}, IsStrafing: {29}, IsSwimming: {30}, IsFlying: {31}, CanFly: {32}, IsAscending: {33}, IsDescending: {34}, JumpingOrShortFalling: {35}, FallingFar: {36}",
                Position, Heading, TransportGuid, DirectionCosX, DirectionSinX, DirectionCosY, DirectionSinY, FallTime, FallStartHeight, LastFallHeight,
                CurrentSpeed, BackwardSpeed, ForwardSpeed, RunSpeed, SwimmingForwardSpeed, SwimmingBackwardSpeed, FlyingForwardSpeed, FlyingBackwardSpeed, HoverHeight, TimeMoved,
                MovementFlags, IsFalling, MovingForward, MovingBackward, MovingStrafeLeft, MovingStrafeRight, MovingTurnLeft, MovingTurnRight, IsMoving, IsStrafing,
                IsSwimming, IsFlying, CanFly, IsAscending, IsDescending, JumpingOrShortFalling, FallingFar);
        }

        private PerFrameCachedValue<CMovementData> _structCache;

        [Flags]
        public enum MoveFlags : uint
        {
            None = 0U,
            Forward = 1U,
            Backward = 2U,
            StrafeLeft = 4U,
            StrafeRight = 8U,
            Left = 16U,
            Right = 32U,
            PitchUp = 64U,
            PitchDown = 128U,
            Walk = 256U,
            Levitating = 512U,
            Root = 1024U,
            Falling = 2048U,
            FallingFar = 4096U,
            PendingStop = 8192U,
            PendingStrafeStop = 16384U,
            PendingForward = 32768U,
            PendingBackward = 65536U,
            PendingStrafeLeft = 131072U,
            PendingStrafeRight = 262144U,
            PendingRoot = 524288U,
            Swimming = 1048576U,
            Ascending = 2097152U,
            Descending = 4194304U,
            CanFly = 8388608U,
            Flying = 16777216U,
            SplineElevation = 33554432U,
            SplineEnabled = 67108864U,
            WaterWalking = 134217728U,
            SafeFall = 268435456U,
            Hover = 536870912U,
            PlayerFlag = 2147483648U,
            TurnMask = Left | Right,
            MoveMask = Forward | Backward | StrafeMask | TurnMask,
            StrafeMask = StrafeLeft | StrafeRight,
            FallMask = Falling | FallingFar,
            PitchMask = PitchUp | PitchDown,
            MotionMask = 6291711U,
            StoppedMask = 200719U
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct CMovementData
        {
            public CMovementShared Base;

            public Vector3 GroundNormal;

            public uint uintCC;

            public uint uintD0;

            public uint uintD4;

            public uint uintD8;

            public uint uintDC;

            public uint uintE0;

            public fixed byte fE4[8];

            public uint uintEC;

            public uint uintF0;

            public uint uintF4;

            public uint uintF8;

            public uint uintFC;

            public uint uint100;

            public uint uint104;

            public uint uint108;

            public uint uint10C;

            public uint uint110;

            public uint uint114;

            public uint uint118;

            public uint uint11C;

            public uint uint120;

            public uint uint124;

            public uint uint128;

            public uint uint12C;

            public uint uint130;

            public uint uint134;

            public uint uint138;

            public uint uint13C;

            public uint uint140;

            public uint uint144;

            public uint OwningUnit;
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct CMovementShared
        {
            public WoWSimpleMovementInfo.CSimpleMovementData Base;

            public uint dword30;

            public uint dword34;

            public uint Flags;

            public ushort word3C;

            public byte byte3E;

            public byte f3F;

            public Vector3 dword40;

            public Vector2 Direction;

            public fixed byte f54[4];

            public Vector3 Direction3;

            public float DirectionCosX;

            public float DirectionSinX;

            public float DirectionCosY;

            public float DirectionSinY;

            public uint FallTime;

            public float FallStartHeight;

            public float LastFallHeight;

            public float CurrentSpeed;

            public float BackwardSpeed;

            public float ForwardSpeed;

            public float RunForwardSpeed;

            public float SwimmingForwardSpeed;

            public float SwimmingBackwardSpeed;

            public float FlyingForwardSpeed;

            public float FlyingBackwardSpeed;

            public uint dwordA0;

            public uint dwordA4;

            public float HoverHeight;

            public uint dwordAC;

            public uint dwordB0;

            public uint dwordB4_PerfCounter;

            public fixed uint padding_end[2];
        }
    }
}
