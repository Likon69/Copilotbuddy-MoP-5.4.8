using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using GreenMagic;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.Helpers;
using Styx.WoWInternals.WoWObjects;
using Tripper.Tools.Math;

namespace Styx.WoWInternals
{
    public static class WoWMovement
    {
        public static void Pulse()
        {
            DateTime now = DateTime.Now;
            MovementDirection movementDirection = MovementDirection.None;
            for (int i = _movementTimers.Count - 1; i >= 0; i--)
            {
                if (!(_movementTimers[i].End >= now))
                {
                    Logging.WriteDiagnostic("Flushing timed movement. Direction: " + _movementTimers[i].Direction);
                    movementDirection |= _movementTimers[i].Direction;
                    _movementTimers.RemoveAt(i);
                }
            }

            if (movementDirection != MovementDirection.None)
            {
                MoveStop(movementDirection);
            }
        }

        public static ClickToMoveInfoStruct ClickToMoveInfo
        {
            get { return StyxWoW.Memory.Read<ClickToMoveInfoStruct>(StyxWoW.Offsets.Statics.ClickToMove_Base); }
        }

        public static ulong ActiveMoverGuid
        {
            get { return StyxWoW.Memory.Read<ulong>(StyxWoW.Offsets.Statics.ActiveMover); }
        }

        public static WoWUnit ActiveMover
        {
            get { return ObjectManager.GetObjectByGuid<WoWUnit>(ActiveMoverGuid); }
        }

        public static bool IsFacing
        {
            get { return ClickToMoveInfo.Type == ClickToMoveType.ConstantFace; }
        }

        private static IntPtr ActiveInputControlPtr
        {
            get { return StyxWoW.Memory.Read<IntPtr>(StyxWoW.Offsets.Statics.ActiveInputControl); }
        }

        public static InputControl ActiveInputControl
        {
            get { return StyxWoW.Memory.Read<InputControl>(ActiveInputControlPtr); }
        }

        public static WoWPoint CalculatePointFrom(WoWPoint target, float distance)
        {
            return WoWMathHelper.CalculatePointFrom(StyxWoW.Me.Location, target, distance);
        }

        public static void ClickToMove(float x, float y, float z)
        {
            ClickToMove(new WoWPoint(x, y, z));
        }

        public static void ClickToMove(WoWPoint point, float distance)
        {
            WoWPoint newPoint = CalculatePointFrom(point, distance);
            ClickToMove(newPoint);
        }

        public static void ClickToMove(WoWPoint point)
        {
            ClickToMoveInfoStruct info = ClickToMoveInfo;
            if (info.IsClickMoving && point.DistanceSqr(info.ClickPos) <= 0.05 && StyxWoW.Me.IsMoving)
            {
                return;
            }

            ClickToMove(ClickToMoveType.Move, 0UL, point, 0f);
        }

        public static void ClickToMove(ulong guid, WoWPoint loc, ClickToMoveType type)
        {
            ClickToMove(type, guid, loc, 0f);
        }

        public static void ClickToMove(ulong guid, ClickToMoveType type)
        {
            ClickToMove(type, guid, WoWPoint.Empty, 0f);
        }

        public static void ClickToMove(ulong guid, WoWPoint loc, float facing, ClickToMoveType type)
        {
            ClickToMove(type, guid, loc, facing);
        }

        public static void GetHeadingDiff(double currentHeading, double destHeading, out double headingDiff, out int directionCoeff)
        {
            headingDiff = currentHeading - destHeading;
            directionCoeff = (int)(headingDiff / Math.Abs(headingDiff));
            headingDiff = Math.Abs(headingDiff);
            if (headingDiff > Math.PI)
            {
                headingDiff = 2.0 * Math.PI - headingDiff;
                directionCoeff *= -1;
            }
        }

        private static void Face(ulong guid, bool stop)
        {
            if (stop)
            {
                WoWObject obj = ObjectManager.GetObjectByGuid<WoWObject>(guid);
                if (obj != null)
                {
                    StyxWoW.Me.SetFacing(obj.Location);
                    return;
                }
            }

            ClickToMove(guid, !stop ? ClickToMoveType.LeftClick : ClickToMoveType.StopThrowsException);
        }

        public static void Face(ulong guid)
        {
            if (StyxWoW.Me != null)
            {
                Face(guid, false);
            }
        }

        public static void Face()
        {
            if (StyxWoW.Me == null)
            {
                return;
            }

            WoWUnit currentTarget = StyxWoW.Me.CurrentTarget;
            if (currentTarget != null && !StyxWoW.Me.IsMoving)
            {
                ConstantFace(currentTarget.Guid);
            }
        }

        public static void StopFace()
        {
            if (StyxWoW.Me == null)
            {
                return;
            }

            WoWUnit currentTarget = StyxWoW.Me.CurrentTarget;
            if (currentTarget != null && !StyxWoW.Me.IsMoving)
            {
                ConstantFaceStop(currentTarget.Guid);
            }
        }

        public static void ConstantFace(ulong guid)
        {
            if (StyxWoW.Me == null)
            {
                return;
            }

            Face(guid, false);
        }

        public static void ConstantFaceStop(ulong guid)
        {
            if (StyxWoW.Me == null)
            {
                return;
            }

            Face(guid, true);
        }

        internal static event MovementEventHandler OnMovementFlagsChanged
        {
            add
            {
                MovementEventHandler handler = _onMovementFlagsChanged;
                MovementEventHandler comparand;
                do
                {
                    comparand = handler;
                    MovementEventHandler combined = (MovementEventHandler)Delegate.Combine(comparand, value);
                    handler = Interlocked.CompareExchange(ref _onMovementFlagsChanged, combined, comparand);
                }
                while (handler != comparand);
            }
            remove
            {
                MovementEventHandler handler = _onMovementFlagsChanged;
                MovementEventHandler comparand;
                do
                {
                    comparand = handler;
                    MovementEventHandler combined = (MovementEventHandler)Delegate.Remove(comparand, value);
                    handler = Interlocked.CompareExchange(ref _onMovementFlagsChanged, combined, comparand);
                }
                while (handler != comparand);
            }
        }

        public static void Move(MovementDirection dir, TimeSpan time)
        {
            if (ActiveInputControl.Flags.HasFlag(dir))
            {
                return;
            }

            TimedMovementAction action = new TimedMovementAction
            {
                Direction = dir,
                End = DateTime.Now + time
            };
            _movementTimers.Add(action);
            Move(dir);
        }

        public static void Move(MovementDirection direction)
        {
            if (ActiveInputControl.Flags.HasFlag(direction))
            {
                return;
            }

            ToggleControlBits(direction, false);
        }

        public static void MoveStop(MovementDirection direction)
        {
            direction &= ActiveInputControl.Flags;
            if (direction == MovementDirection.None)
            {
                return;
            }

            ToggleControlBits(direction, true);
        }

        public static void MoveStop()
        {
            MoveStop(MovementDirection.AllAllowed);
            if (ActiveInputControl.Flags.HasFlag(MovementDirection.IsCTMing))
            {
                ClickToMoveStop();
            }
        }

        private static void ToggleControlBits(MovementDirection dir, bool stop)
        {
            if (!ControlBitTimer.IsFinished && stop)
            {
                _movementTimers.Add(new TimedMovementAction
                {
                    Direction = dir,
                    End = ControlBitTimer.EndTime + TimeSpan.FromMilliseconds(1.0)
                });
                return;
            }

            ControlBitTimer.Reset();
            StyxWoW.ResetAfk();
            Guard.AgainstInvalidExecutor();
            Executor executor = StyxWoW.Memory.Executor;
            lock (executor.AssemblyLock)
            {
                IntPtr ptr1 = (IntPtr)0;
                try
                {
                    executor.Clear();
                    if (dir != MovementDirection.JumpAscend)
                    {
                        AddToggleControlBitASM(ActiveInputControlPtr, (uint)dir, stop ? 0U : 1U);
                    }

                    if ((dir & MovementDirection.JumpAscend) != MovementDirection.None)
                    {
                        Lua.AddDoStringASM(!stop ? "JumpOrAscendStart()" : "AscendStop()", "WoW.lua", out ptr1);
                    }

                    executor.AddLine("retn");
                    executor.Execute();
                }
                finally
                {
                    if (ptr1 != (IntPtr)0)
                    {
                        executor.Memory.FreeMemory(ptr1);
                    }
                }
            }

            if (_onMovementFlagsChanged != null)
            {
                _onMovementFlagsChanged(new MovementEventArgs(dir, stop));
            }
        }

        internal static void SetFacing(float facingInRadians)
        {
            StyxWoW.ResetAfk();
            ClickToMove(ClickToMoveType.Face, 0UL, WoWPoint.Empty, facingInRadians + 1E-06f);
        }

        private static void AddToggleControlBitASM(IntPtr @this, uint flag, uint enable)
        {
            Executor executor = StyxWoW.Memory.Executor;
            lock (executor.AssemblyLock)
            {
                executor.AddRandomLine("push {0}", 1);
                executor.AddRandomLine("push {0}", 0);
                executor.AddRandomLine("call {0}", StyxWoW.Offsets.Functions.PerformanceCounter);
                executor.AddRandomLine("push eax");
                executor.AddRandomLine("push {0}", enable);
                executor.AddRandomLine("push {0}", flag);
                executor.AddRandomLine("mov ecx, {0}", @this);
                executor.AddRandomLine("test ecx, ecx");
                executor.AddRandomLine("jz @OUT");
                executor.AddRandomLine("call {0}", StyxWoW.Offsets.Functions.CGInputControl__ToggleControlBit);
                executor.AddRandomLine("@OUT:");
            }
        }

        private static void ClickToMove(ClickToMoveType clickToMoveType, ulong guid, WoWPoint clickPos, float facing)
        {
            if (float.IsNaN(clickPos.X) || float.IsNaN(clickPos.Y) || float.IsNaN(clickPos.Z))
            {
                throw new ArgumentException("A component of the position is NaN", "clickPos");
            }

            if (float.IsNaN(facing))
            {
                throw new ArgumentException("Facing cannot be NaN", "facing");
            }

            StyxWoW.ResetAfk();
            Guard.AgainstInvalidExecutor();
            Executor executor = StyxWoW.Memory.Executor;
            WoWUnit activeMover = ActiveMover;
            if (activeMover != null && activeMover.IsValid)
            {
                WoWObject transport = activeMover.Transport;
                if (transport != null)
                {
                    Matrix worldMatrix = transport.GetWorldMatrix();
                    Matrix.Invert(ref worldMatrix, out worldMatrix);
                    Vector3 vector = clickPos;
                    Vector3 vector2;
                    Vector3.Transform(ref vector, ref worldMatrix, out vector2);
                    clickPos = vector2;
                }

                using (AllocatedMemory allocatedMemory = StyxWoW.Memory.CreateAllocatedMemory(20))
                {
                    allocatedMemory.Write<WoWPoint>(0, clickPos);
                    allocatedMemory.Write<ulong>(12, guid);
                    lock (executor.AssemblyLock)
                    {
                        executor.Clear();
                        executor.AddRandomLine("call {0}", StyxWoW.Offsets.Functions.ClntObjMgrGetActivePlayerObj);
                        executor.AddRandomLine("test eax, eax");
                        executor.AddRandomLine("jz @DONE");
                        executor.AddRandomLine("push {0}", AsmHelper.GetFloatUInt(facing));
                        executor.AddRandomLine("push {0}", allocatedMemory.GetAddress(0));
                        executor.AddRandomLine("push {0}", allocatedMemory.GetAddress(12));
                        executor.AddRandomLine("push {0}", (uint)clickToMoveType);
                        ulong activeMoverGuid = ActiveMoverGuid;
                        executor.AddRandomLine("push 8");
                        executor.AddRandomLine("push {0}", (uint)(activeMoverGuid >> 32));
                        executor.AddRandomLine("push {0}", (uint)activeMoverGuid);
                        executor.AddRandomLine("call {0}", StyxWoW.Offsets.Functions.ClntObjMgrObjectPtr);
                        executor.AddRandomLine("add esp, 12");
                        executor.AddRandomLine("test eax, eax");
                        executor.AddRandomLine("jz @DONE");
                        executor.AddRandomLine("mov ecx, eax");
                        executor.AddRandomLine("call {0}", StyxWoW.Offsets.Functions.CGPlayer_C__ClickToMove);
                        executor.AddRandomLine("@DONE:");
                        executor.AddRandomLine("retn");
                        executor.Execute();
                    }
                }

                if (_onMovementFlagsChanged != null)
                {
                    _onMovementFlagsChanged(new MovementEventArgs(MovementDirection.ClickToMove, false));
                }
            }
        }

        private static void ClickToMoveStop()
        {
            StyxWoW.ResetAfk();
            Guard.AgainstInvalidExecutor();
            Executor executor = StyxWoW.Memory.Executor;
            lock (executor.AssemblyLock)
            {
                executor.Clear();
                executor.AddRandomLine("call {0}", StyxWoW.Offsets.Functions.ClntObjMgrGetActivePlayerObj);
                executor.AddRandomLine("mov ecx, eax");
                executor.AddRandomLine("test ecx, ecx");
                executor.AddRandomLine("jz @OUT");
                executor.AddRandomLine("call {0}", StyxWoW.Offsets.Functions.CGPlayer_C__ClickToMoveStop);
                executor.AddRandomLine("@OUT:");
                executor.AddRandomLine("retn");
                executor.Execute();
            }

            if (_onMovementFlagsChanged != null)
            {
                _onMovementFlagsChanged(new MovementEventArgs(MovementDirection.ClickToMove, true));
            }
        }

        private static readonly List<TimedMovementAction> _movementTimers = new List<TimedMovementAction>();

        private static MovementEventHandler _onMovementFlagsChanged;

        private static readonly WaitTimer ControlBitTimer = new WaitTimer(TimeSpan.FromMilliseconds(100.0));

        private struct TimedMovementAction
        {
            public MovementDirection Direction;

            public DateTime End;
        }

        public class MovementEventArgs : EventArgs
        {
            public MovementEventArgs(MovementDirection direction, bool stop)
            {
                Direction = direction;
                Stop = stop;
            }

            public MovementDirection Direction;

            public bool Stop;
        }

        internal delegate void MovementEventHandler(MovementEventArgs args);

        public enum ClickToMoveType
        {
            LeftClick = 1,
            Face,
            StopThrowsException,
            Move,
            NpcInteract,
            Loot,
            ObjInteract,
            Skin = 9,
            AttackPosition,
            AttackGuid,
            ConstantFace,
            FaceOther = 8,
            None = 13
        }

        [Flags]
        public enum MovementDirection : uint
        {
            None = 0U,
            RMouse = 1U,
            LMouse = 2U,
            Forward = 16U,
            Backwards = 32U,
            StrafeLeft = 64U,
            StrafeRight = 128U,
            TurnLeft = 256U,
            TurnRight = 512U,
            PitchUp = 1024U,
            PitchDown = 2048U,
            AutoRun = 4096U,
            JumpAscend = 8192U,
            Descend = 16384U,
            ClickToMove = 4194304U,
            IsCTMing = 2097152U,
            ForwardBackMovement = 65536U,
            StrafeMovement = 131072U,
            TurnMovement = 262144U,
            StrafeMask = StrafeLeft | StrafeRight | StrafeMovement,
            TurnMask = TurnLeft | TurnRight | TurnMovement,
            MoveMask = Forward | Backwards | ForwardBackMovement | AutoRun,
            All = StrafeMask | TurnMask | MoveMask,
            AllAllowed = Forward | Backwards | StrafeLeft | StrafeRight | TurnLeft | TurnRight | JumpAscend | Descend | AutoRun
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ClickToMoveInfoStruct
        {
            public bool IsClickMoving
            {
                get { return Type == ClickToMoveType.Move; }
            }

            public bool IsUsing
            {
                get { return Type != ClickToMoveType.None; }
            }

            public override string ToString()
            {
                return string.Format("Velocity: {0}, InteractDistSqrd: {1}, InteractDist: {2}, FaceAngle: {3}, LastCtmUpdate: {4}, Type: {5}, InteractGUID: {6}, CurrentPos: {7}, ClickPos: {8}", Velocity, InteractDistSqrd, InteractDist, FaceAngle, LastCtmUpdate, Type, InteractGuid, CurrentPos, ClickPos);
            }

            private float _unk1;

            public float Velocity;

            public float InteractDistSqrd;

            public float InteractDist;

            private float _unk2;

            public float FaceAngle;

            public uint LastCtmUpdate;

            public ClickToMoveType Type;

            public ulong InteractGuid;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
            private uint[] _unk3;

            public ulong _unk;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            private uint[] _unk4;

            public WoWPoint CurrentPos;

            public WoWPoint ClickPos;

            private float _unk5;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct InputControl
        {
            public uint Time;

            [MarshalAs(UnmanagedType.U4)]
            public MovementDirection Flags;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            private uint[] _unk;

            public MovementControl Movement;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
            private uint[] _unk2;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MovementControl
        {
            private uint _vtable;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
            private uint[] _unk;
        }
    }
}
