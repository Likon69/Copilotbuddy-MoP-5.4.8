using System;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using GreenMagic.Native;

namespace GreenMagic
{
    public class Executor : IDisposable
    {
        public Executor(ExternalProcessMemory memory, IntPtr hookLocation, int copyBytes, bool veh)
        {
            _thisLock = new object();
            AssemblyLock = new object();
            _copyBytes = copyBytes;
            _veh = veh;
            _clearBytes = new byte[4096];
            if (memory == null || !memory.IsProcessOpen)
            {
                throw new ArgumentNullException("memory", "Memory object passed to Executor constructor was invalid");
            }
            if (hookLocation == IntPtr.Zero)
            {
                throw new ArgumentOutOfRangeException("hookLocation", "Hook location cannot be zero");
            }
            if (memory.Read<IntPtr>(hookLocation) == IntPtr.Zero)
            {
                throw new ArgumentException("hookLocation", "Hook location refers to an invalid location");
            }
            _mem = memory;
            memory.Asm.SetMemorySize(65536);
            _originalEndscene = hookLocation;
            byte[] seed = new byte[4];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetNonZeroBytes(seed);
            }
            _random = new Random(BitConverter.ToInt32(seed, 0));
            bool createdNew = false;
            string currentUserName = Environment.UserDomainName + "\\" + Environment.UserName;
            EventWaitHandleSecurity eventWaitHandleSecurity = new EventWaitHandleSecurity();
            EventWaitHandleAccessRule eventWaitHandleAccessRule = new EventWaitHandleAccessRule(currentUserName, EventWaitHandleRights.FullControl, AccessControlType.Allow);
            eventWaitHandleSecurity.AddAccessRule(eventWaitHandleAccessRule);
            _injectionWaitingEventName = "Global\\" + GetRandomGuidString();
            _injectionWaitingEvent = EventWaitHandleAcl.Create(false, EventResetMode.AutoReset, _injectionWaitingEventName, out createdNew, eventWaitHandleSecurity);
            if (!createdNew)
            {
                throw new Exception("You should never see this message but the event was opened instead of created! That's bad!");
            }
            _injectionWaitingCheckReadyEventName = "Global\\" + GetRandomGuidString();
            _injectionContinueEvent = EventWaitHandleAcl.Create(false, EventResetMode.AutoReset, _injectionWaitingCheckReadyEventName, out createdNew, eventWaitHandleSecurity);
            if (!createdNew)
            {
                throw new Exception("You should never see this message but the event was opened instead of created! That's bad!");
            }
            _injectionFinishedEventName = "Global\\" + GetRandomGuidString();
            _injectionFinishedEvent = EventWaitHandleAcl.Create(false, EventResetMode.AutoReset, _injectionFinishedEventName, out createdNew, eventWaitHandleSecurity);
            if (!createdNew)
            {
                throw new Exception("You should never see this message but the event was opened instead of created! That's bad!");
            }
            _frameDropWaitTimeXorKey = (uint)_random.Next();
            InitializeDetour();
            _mem.Asm.Clear();
            IsInitialized = true;
        }

        private void DisposeManagedResources()
        {
            if (!_disposed)
            {
                IsInitialized = false;
                if (_newBytes.SequenceEqual(_mem.ReadBytes(_originalEndscene, _newBytes.Length)))
                {
                    _mem.WriteBytes(_originalEndscene, _originalBytes);
                }
                _mem.FreeMemory(_endSceneDetour);
                _mem.FreeMemory(_injectedCode);
                _mem.FreeMemory(_dataPtr);
                _injectionWaitingEvent.Close();
                _injectionContinueEvent.Close();
                _injectionFinishedEvent.Close();
                _disposed = true;
            }
        }

        public void BeginExecute()
        {
            RaiseOnBeginExecute(this, EventArgs.Empty);
            lock (_thisLock)
            {
                if (_continuousExecution)
                {
                    EndExecute();
                }
                _continuousExecution = true;
                _firstExecution = true;
            }
        }

        public void EndExecute()
        {
            RaiseOnEndExecute(this, EventArgs.Empty);
            lock (_thisLock)
            {
                _continuousExecution = false;
                _firstExecution = false;
                if (!_injectionWaitingEvent.SafeWaitHandle.IsClosed)
                {
                    _injectionWaitingEvent.Reset();
                }
                if (!_injectionContinueEvent.SafeWaitHandle.IsClosed)
                {
                    _injectionContinueEvent.Set();
                }
            }
        }

        public void Execute()
        {
            Execute(15000);
        }

        public void Execute(int timeout)
        {
            ExternalProcessMemory mem = _mem;
            if (mem == null || !mem.IsProcessOpen || !mem.IsThreadOpen || !IsInitialized)
            {
                throw new Exception("Cannot execute code while process is not opened and/or Executor is not initialized!");
            }
            RaiseOnExecute(this, EventArgs.Empty);
            lock (_thisLock)
            {
                if (_continuousExecution)
                {
                    if (_firstExecution)
                    {
                        _firstExecution = false;
                        _mem.Asm.Inject(_injectedCode);
                        _injectionContinueEvent.Reset();
                        _injectionWaitingEvent.Set();
                        WaitForInjection(timeout);
                    }
                    else
                    {
                        _mem.Asm.Inject(_injectedCode);
                        _injectionWaitingEvent.Set();
                        _injectionContinueEvent.Set();
                        WaitForInjection(timeout);
                    }
                }
                else
                {
                    _mem.Asm.Inject(_injectedCode);
                    _injectionWaitingEvent.Set();
                    try
                    {
                        WaitForInjection(timeout);
                    }
                    finally
                    {
                        _injectionWaitingEvent.Reset();
                        _injectionContinueEvent.Set();
                    }
                }
            }
        }

        public void Execute(byte[] bytes)
        {
            _mem.Asm.Clear();
            _mem.WriteBytes(_injectedCode, bytes);
            Execute(15000);
        }

        public void GrabFrame()
        {
            lock (AssemblyLock)
            {
                _mem.Asm.Clear();
                _mem.Asm.AddLine("retn");
                Execute(15000);
            }
        }

        public void Clear()
        {
            _mem.Asm.Clear();
        }

        public void AddLine(string fmt, params object[] args)
        {
            _mem.Asm.AddLine(fmt, args);
        }

        public void AddRandomLine(int numRandomLines, string fmt, params object[] args)
        {
            numRandomLines = (numRandomLines <= 1) ? 2 : numRandomLines;
            int numLinesBefore = (int)((double)((float)numRandomLines) * 0.5);
            RandomLine(numLinesBefore);
            _mem.Asm.AddLine(fmt, args);
            RandomLine(numRandomLines % 2 + numLinesBefore);
        }

        public void AddRandomLine(string fmt, params object[] args)
        {
            AddRandomLine(_random.Next(2, 5), fmt, args);
        }

        public ExternalProcessMemory Memory
        {
            get { return _mem; }
        }

        public IntPtr DataPointer
        {
            get { return _dataPtr; }
        }

        public IntPtr ReturnPointer
        {
            get { return _returnedDataPtr; }
        }

        public IntPtr InjectCodePointer
        {
            get { return _injectedCode; }
        }

        public bool IsExecutingContinuously
        {
            get { return _continuousExecution; }
        }

        public uint FrameCount
        {
            get { return _mem.Read<uint>(_frameCountPtr); }
        }

        public uint FrameDropWaitTime
        {
            get { return _mem.Read<uint>(_frameDropWaitTimePtr) ^ _frameDropWaitTimeXorKey; }
            set { _mem.Write<uint>(_frameDropWaitTimePtr, _frameDropWaitTimeXorKey ^ value); }
        }

        public bool IsInsideHook
        {
            get { return _mem.Read<uint>(_inHookPtr) != 0U; }
        }

        internal event EventHandler<EventArgs> OnBeginExecute;

        protected void RaiseOnBeginExecute(object sender, EventArgs e)
        {
            OnBeginExecute?.Invoke(sender, e);
        }

        internal event EventHandler<EventArgs> OnEndExecute;

        protected void RaiseOnEndExecute(object sender, EventArgs e)
        {
            OnEndExecute?.Invoke(sender, e);
        }

        internal event EventHandler<EventArgs> OnExecute;

        protected void RaiseOnExecute(object sender, EventArgs e)
        {
            OnExecute?.Invoke(sender, e);
        }

        protected string GetRandomR32()
        {
            switch (_random.Next(1, 7))
            {
                case 1:
                    return "eax";
                case 2:
                    return "ebx";
                case 3:
                    return "ecx";
                case 4:
                    return "edx";
                case 5:
                    return "edi";
                case 6:
                    return "esi";
                default:
                    return "eax";
            }
        }

        protected string GetRandomR16()
        {
            switch (_random.Next(1, 7))
            {
                case 1:
                    return "ax";
                case 2:
                    return "bx";
                case 3:
                    return "cx";
                case 4:
                    return "dx";
                case 5:
                    return "di";
                case 6:
                    return "si";
                default:
                    return "ax";
            }
        }

        protected void RandomLine(int numRandomLines)
        {
            while (numRandomLines-- > 0)
            {
                string randomR32 = GetRandomR32();
                string randomR16 = GetRandomR16();
                switch (_random.Next(1, 11))
                {
                    case 1:
                        for (int index = _random.Next(1, 4); index > 0; index--)
                        {
                            _mem.Asm.AddLine("nop");
                        }
                        break;
                    case 2:
                        _mem.Asm.AddLine("mov {0}, {0}", randomR32);
                        break;
                    case 3:
                        _mem.Asm.AddLine("mov {0}, {0}", randomR16);
                        break;
                    case 4:
                        _mem.Asm.AddLine("push {0}", randomR16);
                        _mem.Asm.AddLine("pop {0}", randomR16);
                        break;
                    case 5:
                        _mem.Asm.AddLine("push {0}", randomR32);
                        _mem.Asm.AddLine("pop {0}", randomR32);
                        break;
                    case 6:
                        _mem.Asm.AddLine("or {0}, {0}", randomR32);
                        _mem.Asm.AddLine("and {0}, {0}", randomR32);
                        break;
                    case 7:
                        _mem.Asm.AddLine("lea {0}, [{0}]", randomR32);
                        break;
                    case 8:
                        _mem.Asm.AddLine("sub {0}, 0", randomR32);
                        break;
                    case 9:
                        _mem.Asm.AddLine("add {0}, 0", randomR32);
                        break;
                    case 10:
                        _mem.Asm.AddLine("test {0}, {0}", randomR32);
                        break;
                }
            }
        }

        protected void InitializeDetour()
        {
            ExternalProcessMemory mem = _mem;
            if (mem == null || !mem.IsProcessOpen || !mem.IsThreadOpen)
            {
                throw new Exception("Process is not open for memory manipulation");
            }
            int pageSize = _clearBytes.Length + _random.Next(0, 16) * 256;
            _waitForSingleObject = _mem.GetProcAddress("kernel32.dll", "WaitForSingleObject");
            _createEventA = _mem.GetProcAddress("kernel32.dll", "CreateEventA");
            _resetEvent = _mem.GetProcAddress("kernel32.dll", "ResetEvent");
            _setEvent = _mem.GetProcAddress("kernel32.dll", "SetEvent");
            _tlsAlloc = _mem.GetProcAddress("kernel32.dll", "TlsAlloc");
            _tlsFree = _mem.GetProcAddress("kernel32.dll", "TlsFree");
            _tlsGetValue = _mem.GetProcAddress("kernel32.dll", "TlsGetValue");
            _tlsSetValue = _mem.GetProcAddress("kernel32.dll", "TlsSetValue");
            _addVectoredExceptionHandler = _mem.GetProcAddress("kernel32.dll", "AddVectoredExceptionHandler");
            _removeVectoredExceptionHandler = _mem.GetProcAddress("kernel32.dll", "RemoveVectoredExceptionHandler");
            IntPtr eventStub = _mem.AllocateMemory(pageSize, (uint)MemoryAllocationType.MEM_COMMIT, (uint)MemoryProtectionType.PAGE_EXECUTE_READ);
            if (eventStub == IntPtr.Zero)
            {
                throw new Exception("Could not allocate memory in the target process for event stub");
            }
            IntPtr eventNamesMemory = _mem.AllocateMemory(pageSize, (uint)MemoryAllocationType.MEM_COMMIT, (uint)MemoryProtectionType.PAGE_READWRITE);
            if (eventNamesMemory == IntPtr.Zero)
            {
                throw new Exception("Could not allocate memory in the target process for event stub");
            }
            IntPtr injectionContinueNamePtr = eventNamesMemory + _injectionWaitingEventName.Length + 4;
            IntPtr injectionFinishedNamePtr = injectionContinueNamePtr + _injectionWaitingCheckReadyEventName.Length + 4;
            if (!_mem.WriteString(eventNamesMemory, _injectionWaitingEventName, Encoding.UTF8) || !_mem.WriteString(injectionContinueNamePtr, _injectionWaitingCheckReadyEventName, Encoding.UTF8) || !_mem.WriteString(injectionFinishedNamePtr, _injectionFinishedEventName, Encoding.UTF8))
            {
                throw new Exception("Could not write event names to memory!");
            }
            _endSceneDetour = _mem.AllocateMemory(pageSize, (uint)MemoryAllocationType.MEM_COMMIT, (uint)MemoryProtectionType.PAGE_EXECUTE_READ);
            if (_endSceneDetour == IntPtr.Zero)
            {
                throw new Exception("Could not allocate memory in target process for detour");
            }
            _injectedCode = _mem.AllocateMemory(pageSize, (uint)MemoryAllocationType.MEM_COMMIT, (uint)(MemoryProtectionType.PAGE_EXECUTE_READWRITE | MemoryProtectionType.PAGE_NOCACHE));
            if (_injectedCode == IntPtr.Zero)
            {
                throw new Exception("Could not allocate memory in target process for injected code");
            }
            _dataPtr = _mem.AllocateMemory(pageSize, (uint)MemoryAllocationType.MEM_COMMIT, (uint)MemoryProtectionType.PAGE_READWRITE);
            if (_dataPtr == IntPtr.Zero)
            {
                throw new Exception("Could not allocate memory in target process for injected data");
            }
            _injectionWaitingHandlePtr = _dataPtr;
            _injectionContinueHandlePtr = _dataPtr + 4;
            _injectionFinishedHandlePtr = _dataPtr + 8;
            _returnedDataPtr = _dataPtr + 12;
            _frameCountPtr = _dataPtr + 16;
            _inHookPtr = _dataPtr + 20;
            _statusPtr = _dataPtr + 24;
            _tlsPtr = _dataPtr + 28;
            _vehPtr = _dataPtr + 32;
            _frameDropWaitTimePtr = _dataPtr + 36;
            if (_veh)
            {
                _mem.Write<uint>(_tlsPtr, uint.MaxValue);
            }
            FrameDropWaitTime = 1000U;
            if (!InjectEventStub(eventStub, eventNamesMemory, injectionContinueNamePtr, injectionFinishedNamePtr))
            {
                throw new Exception("Could not successfully set up_synchronization_events!");
            }
            InjectDetour();
            _mem.FreeMemory(eventStub);
            _mem.FreeMemory(eventNamesMemory);
        }

        protected void InjectDetour()
        {
            _mem.Asm.Clear();
            AddRandomLine(_random.Next(2, 5), "pushad");
            if (_veh)
            {
                AddRandomLine(_random.Next(2, 5), "jmp @Continue");
                AddRandomLine(_random.Next(2, 5), "@ExceptionHandler:");
                AddRandomLine(_random.Next(2, 5), "push ebp");
                AddRandomLine(_random.Next(2, 5), "mov ebp, esp");
                AddRandomLine(_random.Next(2, 5), "push dword [{0}]", _tlsPtr);
                AddRandomLine(_random.Next(2, 5), "call {0}", _tlsGetValue);
                AddRandomLine(_random.Next(2, 5), "test eax, eax");
                AddRandomLine(_random.Next(2, 5), "jz @EHContinueSearch");
                AddRandomLine(_random.Next(2, 5), "mov ecx, eax");
                AddRandomLine(_random.Next(2, 5), "mov eax, [ebp+0x8]");
                AddRandomLine(_random.Next(2, 5), "mov eax, [eax]");
                AddRandomLine(_random.Next(2, 5), "mov eax, [eax]");
                AddRandomLine(_random.Next(2, 5), "mov [{0}], eax", _returnedDataPtr);
                AddRandomLine(_random.Next(2, 5), "mov eax, [ebp+0x8]");
                AddRandomLine(_random.Next(2, 5), "mov eax, [eax+0x4]");
                AddRandomLine(_random.Next(2, 5), "mov [eax+0xC4], ecx");
                AddRandomLine(_random.Next(2, 5), "mov dword [eax+0xB8], @CallGateInterject");
                AddRandomLine(_random.Next(2, 5), "mov eax, 0xFFFFFFFF");
                AddRandomLine(_random.Next(2, 5), "jmp @EHReturn");
                AddRandomLine(_random.Next(2, 5), "@EHContinueSearch:");
                AddRandomLine(_random.Next(2, 5), "mov eax, 0");
                AddRandomLine(_random.Next(2, 5), "@EHReturn:");
                AddRandomLine(_random.Next(2, 5), "leave");
                AddRandomLine(_random.Next(2, 5), "retn");
                AddRandomLine(_random.Next(2, 5), "@SetUpGate:");
                AddRandomLine(_random.Next(2, 5), "push ebp");
                AddRandomLine(_random.Next(2, 5), "mov ebp, esp");
                AddRandomLine(_random.Next(2, 5), "call {0}", _tlsAlloc);
                AddRandomLine(_random.Next(2, 5), "mov [{0}], eax", _tlsPtr);
                AddRandomLine(_random.Next(2, 5), "cmp eax, 0xFFFFFFFF");
                AddRandomLine(_random.Next(2, 5), "jnz @AddVEH");
                AddRandomLine(_random.Next(2, 5), "mov eax, 1");
                AddRandomLine(_random.Next(2, 5), "jmp @SetUpGateReturn");
                AddRandomLine(_random.Next(2, 5), "@AddVEH:");
                AddRandomLine(_random.Next(2, 5), "push @ExceptionHandler");
                AddRandomLine(_random.Next(2, 5), "push 1");
                AddRandomLine(_random.Next(2, 5), "call {0}", _addVectoredExceptionHandler);
                AddRandomLine(_random.Next(2, 5), "mov [{0}], eax", _vehPtr);
                AddRandomLine(_random.Next(2, 5), "test eax, eax");
                AddRandomLine(_random.Next(2, 5), "jnz @SetUpGateSucceeded");
                AddRandomLine(_random.Next(2, 5), "mov eax, 2");
                AddRandomLine(_random.Next(2, 5), "jmp @SetUpGateReturn");
                AddRandomLine(_random.Next(2, 5), "@SetUpGateSucceeded:");
                AddRandomLine(_random.Next(2, 5), "xor eax, eax");
                AddRandomLine(_random.Next(2, 5), "@SetUpGateReturn:");
                AddRandomLine(_random.Next(2, 5), "leave");
                AddRandomLine(_random.Next(2, 5), "retn");
                AddRandomLine(_random.Next(2, 5), "@TearDownGate:");
                AddRandomLine(_random.Next(2, 5), "push ebp");
                AddRandomLine(_random.Next(2, 5), "mov ebp, esp");
                AddRandomLine(_random.Next(2, 5), "mov eax, dword [{0}]", _vehPtr);
                AddRandomLine(_random.Next(2, 5), "test eax, eax");
                AddRandomLine(_random.Next(2, 5), "jz @RemoveTLS");
                AddRandomLine(_random.Next(2, 5), "push eax");
                AddRandomLine(_random.Next(2, 5), "call {0}", _removeVectoredExceptionHandler);
                AddRandomLine(_random.Next(2, 5), "mov dword [{0}], 0", _vehPtr);
                AddRandomLine(_random.Next(2, 5), "@RemoveTLS:");
                AddRandomLine(_random.Next(2, 5), "mov eax, dword [{0}]", _tlsPtr);
                AddRandomLine(_random.Next(2, 5), "cmp eax, 0xFFFFFFFF");
                AddRandomLine(_random.Next(2, 5), "jz @TearDownSucceeded");
                AddRandomLine(_random.Next(2, 5), "push eax");
                AddRandomLine(_random.Next(2, 5), "call {0}", _tlsFree);
                AddRandomLine(_random.Next(2, 5), "mov dword [{0}], 0xFFFFFFFF", _tlsPtr);
                AddRandomLine(_random.Next(2, 5), "@TearDownSucceeded:");
                AddRandomLine(_random.Next(2, 5), "xor eax, eax");
                AddRandomLine(_random.Next(2, 5), "leave");
                AddRandomLine(_random.Next(2, 5), "retn");
                AddRandomLine(_random.Next(2, 5), "@CallGate:");
                AddRandomLine(_random.Next(2, 5), "push ebp");
                AddRandomLine(_random.Next(2, 5), "mov ebp, esp");
                AddRandomLine(_random.Next(2, 5), "push esp");
                AddRandomLine(_random.Next(2, 5), "push dword [{0}]", _tlsPtr);
                AddRandomLine(_random.Next(2, 5), "call {0}", _tlsSetValue);
                AddRandomLine(_random.Next(2, 5), "test eax, eax");
                AddRandomLine(_random.Next(2, 5), "jnz @CallGateCallInjectedCode");
                AddRandomLine(_random.Next(2, 5), "mov eax, 3");
                AddRandomLine(_random.Next(2, 5), "jmp @CallGateReturn");
                AddRandomLine(_random.Next(2, 5), "@CallGateCallInjectedCode:");
                AddRandomLine(_random.Next(2, 5), "call {0}", _injectedCode);
                AddRandomLine(_random.Next(2, 5), "mov dword [{0}], eax", _returnedDataPtr);
                AddRandomLine(_random.Next(2, 5), "xor eax, eax");
                AddRandomLine(_random.Next(2, 5), "jmp @CallGateReturn");
                AddRandomLine(_random.Next(2, 5), "@CallGateInterject:");
                AddRandomLine(_random.Next(2, 5), "mov eax, 4");
                AddRandomLine(_random.Next(2, 5), "@CallGateReturn:");
                AddRandomLine(_random.Next(2, 5), "leave");
                AddRandomLine(_random.Next(2, 5), "retn");
                AddRandomLine(_random.Next(2, 5), "@Continue:");
            }
            AddRandomLine(_random.Next(2, 5), "inc dword [{0}]", _frameCountPtr);
            AddRandomLine(_random.Next(2, 5), "mov dword [{0}], 1", _inHookPtr);
            AddRandomLine(_random.Next(2, 5), "@CheckInjection:");
            AddRandomLine(_random.Next(2, 5), "mov eax, [{0}]", _injectionWaitingHandlePtr);
            AddRandomLine(_random.Next(2, 5), "push 0");
            AddRandomLine(_random.Next(2, 5), "push eax");
            AddRandomLine(_random.Next(2, 5), "call {0}", _waitForSingleObject);
            AddRandomLine(_random.Next(2, 5), "test eax, eax");
            AddRandomLine(_random.Next(2, 5), "jnz @NoInjection");
            if (_veh)
            {
                AddRandomLine(_random.Next(2, 5), "cmp dword [{0}], 0", _vehPtr);
                AddRandomLine(_random.Next(2, 5), "jnz @DoCall");
                AddRandomLine(_random.Next(2, 5), "call @SetUpGate");
                AddRandomLine(_random.Next(2, 5), "test eax, eax");
                AddRandomLine(_random.Next(2, 5), "jz @DoCall");
                AddRandomLine(_random.Next(2, 5), "jmp @StoreEaxAsStatus");
                AddRandomLine(_random.Next(2, 5), "@DoCall:");
                AddRandomLine(_random.Next(2, 5), "call @CallGate");
                AddRandomLine(_random.Next(2, 5), "@StoreEaxAsStatus:");
                AddRandomLine(_random.Next(2, 5), "mov [{0}], eax", _statusPtr);
            }
            else
            {
                AddRandomLine(_random.Next(2, 5), "call {0}", _injectedCode);
                AddRandomLine(_random.Next(2, 5), "mov [{0}], eax", _returnedDataPtr);
            }
            AddRandomLine(_random.Next(2, 5), "mov eax, [{0}]", _injectionFinishedHandlePtr);
            AddRandomLine(_random.Next(2, 5), "push eax");
            AddRandomLine(_random.Next(2, 5), "call {0}", _setEvent);
            AddRandomLine(_random.Next(2, 5), "mov eax, [{0}]", _frameDropWaitTimePtr);
            AddRandomLine(_random.Next(2, 5), "xor eax, {0}", _frameDropWaitTimeXorKey);
            AddRandomLine(_random.Next(2, 5), "push eax");
            AddRandomLine(_random.Next(2, 5), "mov eax, [{0}]", _injectionContinueHandlePtr);
            AddRandomLine(_random.Next(2, 5), "push eax");
            AddRandomLine(_random.Next(2, 5), "call {0}", _waitForSingleObject);
            AddRandomLine(_random.Next(2, 5), "test eax, eax");
            AddRandomLine(_random.Next(2, 5), "jz @CheckInjection");
            AddRandomLine(_random.Next(2, 5), "@NoInjection:");
            AddRandomLine(_random.Next(2, 5), "mov dword [{0}], 0", _inHookPtr);
            if (_veh)
            {
                AddRandomLine(_random.Next(2, 5), "call @TearDownGate");
            }
            AddRandomLine(_random.Next(2, 5), "popad");
            _originalBytes = _mem.ReadBytes(_originalEndscene, 5);
            int copyByteCount = _copyBytes;
            if (copyByteCount == 0)
            {
                copyByteCount = HookableFuncs.Max(hookType => hookType.IdentifierBytes.Length);
                byte[] identifierBytes = _mem.ReadBytes(_originalEndscene, copyByteCount);
                HookTypeInfo matchingHookType = HookableFuncs.FirstOrDefault(hookType => hookType.IdentifierBytesMatch(identifierBytes));
                if (matchingHookType == null)
                {
                    throw new Exception("Could not setup Executor; unknown func passed to hook: " + BitConverter.ToString(_mem.ReadBytes(_originalEndscene, 16)));
                }
                identifierBytes = identifierBytes.Take(matchingHookType.CopyAmount).ToArray();
                if (matchingHookType.CopiedBytesTransformer != null)
                {
                    _mem.Asm.InsertLine(string.Format("org 0x{0}", _endSceneDetour.ToString("X8")), 0);
                    byte[] detourBytes = _mem.Asm.Assemble();
                    IntPtr addrOfFirstInjectedByte = _endSceneDetour + detourBytes.Length;
                    identifierBytes = matchingHookType.CopiedBytesTransformer(_originalEndscene, identifierBytes, addrOfFirstInjectedByte);
                }
                for (int index = 0; index < matchingHookType.CopyAmount; index++)
                {
                    _mem.Asm.AddLine("db 0x{0:X}", identifierBytes[index]);
                }
                AddRandomLine(_random.Next(2, 5), "jmp {0}", _originalEndscene + matchingHookType.CopyAmount);
            }
            else
            {
                byte[] copiedBytes = _mem.ReadBytes(_originalEndscene, copyByteCount);
                for (int index = 0; index < _copyBytes; index++)
                {
                    _mem.Asm.AddLine("db 0x{0:X}", copiedBytes[index]);
                }
                AddRandomLine(_random.Next(2, 5), "jmp {0}", _originalEndscene + _copyBytes);
            }
            if (!_mem.Asm.Inject(_endSceneDetour))
            {
                throw new Exception("Could not assemble and inject trampoline");
            }
            _mem.Asm.Clear();
            _mem.Asm.AddLine("jmp {0}", _endSceneDetour);
            if (!_mem.Asm.Inject(_originalEndscene))
            {
                throw new Exception("Could not assemble and inject detour");
            }
            _newBytes = _mem.ReadBytes(_originalEndscene, 5);
        }

        protected bool InjectEventStub(IntPtr eventStub, IntPtr injectionWaitingNamePtr, IntPtr injectionContinueNamePtr, IntPtr injectionFinishedNamePtr)
        {
            _mem.Asm.Clear();
            AddRandomLine(_random.Next(2, 5), "push {0}", injectionWaitingNamePtr);
            AddRandomLine(_random.Next(2, 5), "push 0");
            AddRandomLine(_random.Next(2, 5), "push 0");
            AddRandomLine(_random.Next(2, 5), "push 0");
            AddRandomLine(_random.Next(2, 5), "call {0}", _createEventA);
            AddRandomLine(_random.Next(2, 5), "test eax, eax");
            AddRandomLine(_random.Next(2, 5), "jz @ReturnFalse");
            AddRandomLine(_random.Next(2, 5), "mov [{0}], eax", _injectionWaitingHandlePtr);
            AddRandomLine(_random.Next(2, 5), "push {0}", injectionContinueNamePtr);
            AddRandomLine(_random.Next(2, 5), "push 0");
            AddRandomLine(_random.Next(2, 5), "push 0");
            AddRandomLine(_random.Next(2, 5), "push 0");
            AddRandomLine(_random.Next(2, 5), "call {0}", _createEventA);
            AddRandomLine(_random.Next(2, 5), "test eax, eax");
            AddRandomLine(_random.Next(2, 5), "jz @ReturnFalse");
            AddRandomLine(_random.Next(2, 5), "mov [{0}], eax", _injectionContinueHandlePtr);
            AddRandomLine(_random.Next(2, 5), "push {0}", injectionFinishedNamePtr);
            AddRandomLine(_random.Next(2, 5), "push 0");
            AddRandomLine(_random.Next(2, 5), "push 0");
            AddRandomLine(_random.Next(2, 5), "push 0");
            AddRandomLine(_random.Next(2, 5), "call {0}", _createEventA);
            AddRandomLine(_random.Next(2, 5), "test eax, eax");
            AddRandomLine(_random.Next(2, 5), "jz @ReturnFalse");
            AddRandomLine(_random.Next(2, 5), "mov [{0}], eax", _injectionFinishedHandlePtr);
            AddRandomLine(_random.Next(2, 5), "mov eax, 1");
            AddRandomLine(_random.Next(2, 5), "retn");
            AddRandomLine(_random.Next(2, 5), "@ReturnFalse:");
            AddRandomLine(_random.Next(2, 5), "xor eax, eax");
            AddRandomLine(_random.Next(2, 5), "retn");
            return _mem.Asm.InjectAndExecute(eventStub) == 1;
        }

        protected string GetRandomString(int length)
        {
            return GetRandomString(length, length);
        }

        protected string GetRandomString(int minLength, int maxLength)
        {
            char[] charPool = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
            StringBuilder stringBuilder = (minLength < maxLength) ? new StringBuilder(_random.Next(minLength, maxLength)) : new StringBuilder(minLength);
            for (int index = 0; index < stringBuilder.Capacity; index++)
            {
                stringBuilder.Append(charPool[_random.Next(charPool.Length)]);
            }
            return stringBuilder.ToString();
        }

        protected string GetRandomGuidString()
        {
            char[] hexPool = "0123456789abcdef".ToCharArray();
            StringBuilder stringBuilder = new StringBuilder(38);
            int[] groupLengths = new int[] { 8, 4, 4, 4, 12 };
            stringBuilder.Append('{');
            for (int groupIndex = 0; groupIndex < groupLengths.Length; groupIndex++)
            {
                if (groupIndex > 0)
                {
                    stringBuilder.Append('-');
                }
                for (int charIndex = 0; charIndex < groupLengths[groupIndex]; charIndex++)
                {
                    stringBuilder.Append(hexPool[_random.Next(hexPool.Length)]);
                }
            }
            stringBuilder.Append('}');
            return stringBuilder.ToString();
        }

        private void WaitForInjection(int timeout)
        {
            if (!_injectionFinishedEvent.WaitOne(timeout, false))
            {
                throw new Exception("Process must have frozen or gotten out of sync: InjectionFinishedEvent_was_never_fired");
            }
            if (_veh)
            {
                switch (_mem.Read<int>(_statusPtr))
                {
                    case 0:
                        break;
                    case 1:
                        throw new Exception("Injection cannot proceed because there are no TLS slots available");
                    case 2:
                        throw new Exception("Injection cannot proceed because VEH couldn't be registered");
                    case 3:
                        throw new Exception("Injection cannot proceed because TlsSet failed");
                    case 4:
                        throw new Exception("Injection resulted in a process exception with error code 0x" + _mem.Read<uint>(_returnedDataPtr).ToString("x8"));
                    default:
                        throw new Exception("Injection resulted in invalid status");
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeManagedResources();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public readonly object AssemblyLock;

        public bool IsInitialized;

        private static HookTypeInfo[] HookableFuncs = new HookTypeInfo[]
        {
            new HookTypeInfo("gclGamePlay_EndFrameCB/Neverwinter", 6, new int[] { 0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x40 }),
            new HookTypeInfo("gclGamePlay_EndFrameCB/Neverwinter 2", 6, new int[] { 0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x58 }),
            new HookTypeInfo("FF Hook", 6, new int[] { 0x53, 0x8B, 0xDC, 0x83, 0xEC, 0x08 }),
            new HookTypeInfo("FF Hook2", 7, new int[] { 0x55, 0x8B, 0xEC, 0x66, 0x8B, 0x41, 0x04 }),
            new HookTypeInfo("Standard D3D9", 5, new int[] { 0x8B, 0xFF, 0x55, 0x8B, 0xEC }),
            new HookTypeInfo("Standard D3D11", 5, new int[] { 0x89, 0xFF, 0x55, 0x89, 0xE5 }),
            new HookTypeInfo("Other hook", 5, new int[] { 0xE9, -1, -1, -1, -1 })
            {
                CopiedBytesTransformer = (addrOfFirstByte, bytes, addOfFirstInjectedByte) =>
                {
                    IntPtr jumpAddress = addrOfFirstByte + 5 + (int)BitConverter.ToUInt32(bytes, 1);
                    int addressInMemory = (int)(jumpAddress.ToInt64() - addOfFirstInjectedByte.ToInt64() - 5);
                    Buffer.BlockCopy(BitConverter.GetBytes(addressInMemory), 0, bytes, 1, 4);
                    return bytes;
                }
            },
            new HookTypeInfo("Nvidia coproc manager", 6, new int[] { 0x55, 0x8B, 0xEC, 0x8B, 0x45, 0x08 }),
            new HookTypeInfo("New D3D9 (Windows 8)", 7, new int[] { 0x6A, 0x14, 0xB8, -1, -1, -1, -1, 0xE8, -1, -1, -1, -1, 0x8B, 0x75, 0x08 }),
            new HookTypeInfo("New D3D9 (Windows 10)", 7, new int[] { 0x6A, 0x14, 0xB8, -1, -1, -1, -1, 0xE8, -1, -1, -1, -1, 0x8B, 0x7D, 0x08 })
        };

        private int _copyBytes;

        private object _thisLock;

        private ExternalProcessMemory _mem;

        private Random _random;

        private bool _firstExecution;

        private bool _continuousExecution;

        private bool _disposed;

        private bool _veh;

        private uint _frameDropWaitTimeXorKey;

        private byte[] _clearBytes;

        private byte[] _originalBytes;

        private byte[] _newBytes;

        private IntPtr _originalEndscene;

        private IntPtr _endSceneDetour;

        private IntPtr _injectedCode;

        private IntPtr _dataPtr;

        private IntPtr _statusPtr;

        private IntPtr _returnedDataPtr;

        private IntPtr _frameCountPtr;

        private IntPtr _inHookPtr;

        private IntPtr _tlsPtr;

        private IntPtr _vehPtr;

        private IntPtr _frameDropWaitTimePtr;

        private IntPtr _injectionWaitingHandlePtr;

        private string _injectionWaitingEventName;

        private EventWaitHandle _injectionWaitingEvent;

        private IntPtr _injectionContinueHandlePtr;

        private string _injectionWaitingCheckReadyEventName;

        private EventWaitHandle _injectionContinueEvent;

        private IntPtr _injectionFinishedHandlePtr;

        private string _injectionFinishedEventName;

        private EventWaitHandle _injectionFinishedEvent;

        private IntPtr _waitForSingleObject;

        private IntPtr _createEventA;

        private IntPtr _resetEvent;

        private IntPtr _setEvent;

        private IntPtr _tlsAlloc;

        private IntPtr _tlsFree;

        private IntPtr _tlsGetValue;

        private IntPtr _tlsSetValue;

        private IntPtr _addVectoredExceptionHandler;

        private IntPtr _removeVectoredExceptionHandler;
    }
}
