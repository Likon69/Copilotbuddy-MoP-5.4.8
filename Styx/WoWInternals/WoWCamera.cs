using System;
using System.Runtime.InteropServices;
using Tripper.Tools.Math;

namespace Styx.WoWInternals
{
    public class WoWCamera
    {
        internal WoWCamera()
        {
        }

        public Vector3 Position
        {
            get
            {
                return this.ReadCameraData().Position;
            }
        }

        public float FieldOfView
        {
            get
            {
                return this.ReadCameraData().FieldOfView;
            }
        }

        public float NearZ
        {
            get
            {
                return this.ReadCameraData().NearZ;
            }
        }

        public float FarZ
        {
            get
            {
                return this.ReadCameraData().FarZ;
            }
        }

        public float Aspect
        {
            get
            {
                return this.ReadCameraData().Aspect;
            }
        }

        public Matrix Projection
        {
            get
            {
                return Matrix.CreatePerspectiveFieldOfView(this.FieldOfView * 0.6f, this.Aspect, this.NearZ, this.FarZ);
            }
        }

        public Matrix World
        {
            get
            {
                return Matrix.Identity;
            }
        }

        public Matrix View
        {
            get
            {
                Vector3 position = this.Position;
                Vector3 target = position + this.Forward;
                return Matrix.CreateLookAt(position, target, new Vector3(0f, 0f, 1f));
            }
        }

        public Vector3 Forward
        {
            get
            {
                Matrix matrix = this.Matrix;
                return new Vector3(matrix.M11, matrix.M12, matrix.M13);
            }
        }

        public Vector3 Right
        {
            get
            {
                Matrix matrix = this.Matrix;
                return new Vector3(matrix.M21, matrix.M22, matrix.M23);
            }
        }

        public Vector3 Up
        {
            get
            {
                Matrix matrix = this.Matrix;
                return new Vector3(matrix.M31, matrix.M32, matrix.M33);
            }
        }

        public Matrix Matrix
        {
            get
            {
                WoWCamera.CameraMatrix cameraMatrix = this.ReadCameraData().Matrix;
                Matrix matrix = default(Matrix);
                matrix.M11 = cameraMatrix.M11;
                matrix.M12 = cameraMatrix.M12;
                matrix.M13 = cameraMatrix.M13;
                matrix.M21 = cameraMatrix.M21;
                matrix.M22 = cameraMatrix.M22;
                matrix.M23 = cameraMatrix.M23;
                matrix.M31 = cameraMatrix.M31;
                matrix.M32 = cameraMatrix.M32;
                matrix.M33 = cameraMatrix.M33;
                matrix.M44 = 1f;
                return matrix;
            }
        }

        internal WoWCamera.CameraData ReadCameraData()
        {
            return StyxWoW.Memory.Read<WoWCamera.CameraData>(StyxWoW.CallInjected<IntPtr>(StyxWoW.Offsets.Functions.CGWorldFrame__GetActiveCamera, CallingConvention.Cdecl, new object[0]));
        }

        [StructLayout(LayoutKind.Sequential, Size = 36)]
        internal struct CameraMatrix
        {
            public override string ToString()
            {
                return string.Format("<M11: {0}, M12: {1}, M13: {2}, M21: {3}, M22: {4}, M23: {5}, M31: {6}, M32: {7}, M33: {8}>", new object[] { this.M11, this.M12, this.M13, this.M21, this.M22, this.M23, this.M31, this.M32, this.M33 });
            }

            public float M11;

            public float M12;

            public float M13;

            public float M21;

            public float M22;

            public float M23;

            public float M31;

            public float M32;

            public float M33;
        }

        internal struct CameraData
        {
            public override string ToString()
            {
                return string.Format("VTable: {0:X}, Dword4: {1}, Position: {2}, Matrix: {3}, FieldOfView: {4}, Model: {5:X}, Timestamp: {6}, NearZ: {7}, FarZ: {8}, Aspect: {9}", new object[] { this.VTable, this.Dword4, this.Position, this.Matrix, this.FieldOfView, this.Model, this.Timestamp, this.NearZ, this.FarZ, this.Aspect });
            }

            public IntPtr VTable;

            public int Dword4;

            public Vector3 Position;

            public WoWCamera.CameraMatrix Matrix;

            public float FieldOfView;

            public IntPtr Model;

            public int Timestamp;

            public float NearZ;

            public float FarZ;

            public float Aspect;
        }
    }
}
