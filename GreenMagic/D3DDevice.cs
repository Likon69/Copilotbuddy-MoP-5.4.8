using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using GreenMagic.Native;

namespace GreenMagic
{
    internal abstract class D3DDevice : IDisposable
    {
        protected readonly IntPtr D3DDevicePtr;
        protected readonly IntPtr WindowHandle;
        private readonly string _d3DDllName;
        private readonly List<IntPtr> _loadedLibraries = new List<IntPtr>();

        private IntPtr _myD3DDll;

        protected D3DDevice(IntPtr hWnd, string d3DDllName)
        {
            WindowHandle = hWnd;
            _d3DDllName = d3DDllName;
            LoadDll();
            InitD3D(out D3DDevicePtr);
        }

        public string D3DModulePath { get; private set; }

        public abstract int BeginSceneVtableIndex { get; }
        public abstract int EndSceneVtableIndex { get; }
        public abstract int PresentVtableIndex { get; }

        protected abstract void InitD3D(out IntPtr d3DDevicePtr);

        protected abstract void CleanD3D();

        protected IntPtr LoadLibrary(string library)
        {
            IntPtr ret = Imports.GetModuleHandleW(library);
            if (ret == IntPtr.Zero)
            {
                ret = Imports.LoadLibrary(library);
                _loadedLibraries.Add(ret);
            }
            return ret;
        }

        private void LoadDll()
        {
            _myD3DDll = LoadLibrary(_d3DDllName);
            if (_myD3DDll == IntPtr.Zero)
            {
                throw new Exception(string.Format("Could not load {0}", _d3DDllName));
            }
            D3DModulePath = GetModulePath(_myD3DDll);
        }

        protected static string GetModulePath(IntPtr moduleHandle)
        {
            foreach (ProcessModule processModule in Process.GetCurrentProcess().Modules)
            {
                if (processModule.BaseAddress == moduleHandle)
                {
                    return processModule.FileName;
                }
            }
            return null;
        }

        protected unsafe IntPtr GetVTableFuncAddress(IntPtr obj, int funcIndex)
        {
            IntPtr pointer = *(IntPtr*)((void*)obj);
            return *(IntPtr*)((void*)((int)pointer + funcIndex * 4));
        }

        public unsafe IntPtr GetDeviceVTableFuncRelativeAddress(int funcIndex)
        {
            IntPtr pointer = *(IntPtr*)((void*)D3DDevicePtr);
            pointer = *(IntPtr*)((void*)((int)pointer + funcIndex * 4));
            return IntPtr.Subtract(pointer, _myD3DDll.ToInt32());
        }

        protected T GetDelegate<T>(IntPtr address) where T : class
        {
            return Marshal.GetDelegateForFunctionPointer(address, typeof(T)) as T;
        }

        #region IDisposable Pattern Implementation

        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    CleanD3D();
                    foreach (IntPtr loadedLibrary in _loadedLibraries)
                    {
                        Imports.FreeLibrary(loadedLibrary);
                    }
                }
                _disposed = true;
            }
        }

        ~D3DDevice()
        {
            Dispose(false);
        }

        #endregion

        #region Nested type: VTableFuncDelegate

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        protected delegate void VTableFuncDelegate(IntPtr instance);

        #endregion
    }
}
