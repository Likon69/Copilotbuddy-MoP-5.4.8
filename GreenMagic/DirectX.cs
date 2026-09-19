using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace GreenMagic
{
    public class DirectX
    {
        public DirectX(MemoryBase mem, bool dx9)
        {
            _mem = mem;
            if (dx9)
            {
                string containingModulePath;
                IntPtr relativeAddress = GetD3D9EndScenePointer(out containingModulePath);
                if (relativeAddress == IntPtr.Zero)
                {
                    throw new Exception("Could not find EndScene function address!");
                }
                ProcessModule containingModule = FindModuleByPath(containingModulePath);
                if (containingModule == null)
                {
                    throw new Exception("Could not find DX9 in process!");
                }
                HookLocation = new IntPtr(containingModule.BaseAddress.ToInt64() + relativeAddress.ToInt64());
            }
            else
            {
                string containingModulePath;
                IntPtr relativeAddress = GetD3D11SwapChainPresentPointer(out containingModulePath);
                if (relativeAddress == IntPtr.Zero)
                {
                    throw new Exception("Could not find SwapChain::Present function address!");
                }
                ProcessModule containingModule = FindModuleByPath(containingModulePath);
                if (containingModule == null)
                {
                    throw new Exception("Could not find DX11 in process!");
                }
                HookLocation = new IntPtr(containingModule.BaseAddress.ToInt64() + relativeAddress.ToInt64());
            }
        }

        private ProcessModule FindModuleByName(string name)
        {
            name = name.ToLowerInvariant();
            foreach (ProcessModule module in _mem._proc.Modules)
            {
                if (module.ModuleName.ToLowerInvariant() == name)
                {
                    return module;
                }
            }
            return null;
        }

        private ProcessModule FindModuleByPath(string path)
        {
            path = path.ToLowerInvariant().TrimEnd('\\');
            foreach (ProcessModule module in _mem._proc.Modules)
            {
                if (module.FileName.ToLowerInvariant().TrimEnd('\\') == path)
                {
                    return module;
                }
            }
            return null;
        }

        private static IntPtr GetEndScene(IntPtr hWnd, out string containingModulePath)
        {
            using (D3D9Device device = new D3D9Device(hWnd))
            {
                containingModulePath = device.D3DModulePath;
                return device.GetDeviceVTableFuncRelativeAddress(device.EndSceneVtableIndex);
            }
        }

        private static IntPtr GetSwapChainPresent(IntPtr hWnd, out string containingModulePath)
        {
            using (D3D11Device device = new D3D11Device(hWnd))
            {
                containingModulePath = device.DxgiModulePath;
                return device.GetSwapVTableFuncRelativeAddress(device.PresentVtableIndex);
            }
        }

        private IntPtr GetD3D9EndScenePointer(out string containingModulePath)
        {
            using (Form form = new Form())
            {
                return GetEndScene(form.Handle, out containingModulePath);
            }
        }

        private IntPtr GetD3D11SwapChainPresentPointer(out string containingModulePath)
        {
            using (Form form = new Form())
            {
                return GetSwapChainPresent(form.Handle, out containingModulePath);
            }
        }

        public IntPtr HookLocation;

        private MemoryBase _mem;
    }
}
