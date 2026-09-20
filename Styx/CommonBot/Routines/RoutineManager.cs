using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Styx.Common;
using Styx.Helpers;

namespace Styx.CommonBot.Routines
{
    public static class RoutineManager
    {
        private static readonly AssemblyLoader<ICombatRoutine> Loader;
        private static ICombatRoutine _current;
        private static bool _initialized;
        private static readonly CapabilityState[] _capabilityStates = Enumerable.Repeat(CapabilityState.DontCare, 32).ToArray();
        private static string _lastSelectedRoutineName;

        static RoutineManager()
        {
            if (!Directory.Exists(RoutineDirectory))
            {
                Directory.CreateDirectory(RoutineDirectory);
            }

            Loader = new AssemblyLoader<ICombatRoutine>(RoutineDirectory, GlobalSettings.Instance.ReloadRoutinesOnFileChange);
            Loader.Reloaded += (s, e) => ChangeRoutine(CommandLineRoutine ?? _lastSelectedRoutineName);
        }

        public static List<ICombatRoutine> Routines { get; private set; } = new List<ICombatRoutine>();

        public static List<ICombatRoutine> AllRoutines { get; set; }

        public static string RoutineDirectory
        {
            get
            {
                string path = GlobalSettings.Instance.CombatRoutinesPath;
                if (!Path.IsPathRooted(path))
                {
                    path = Path.Combine(Utilities.AssemblyDirectory, path);
                }
                return path;
            }
        }

        public static string CommandLineRoutine
        {
            get { return CommandLine.Arguments.Single("customclass"); }
        }

        public static ICombatRoutine Current
        {
            get
            {
                return _current;
            }
            set
            {
                if (_current != null && _current.Equals(value))
                {
                    return;
                }

                if (value != null)
                {
                    try
                    {
                        value.Initialize();
                    }
                    catch (Exception ex)
                    {
                        Logging.WriteException(ex);
                        RoutineSelectionForm routineSelectionForm = new RoutineSelectionForm(Routines);
                        if (routineSelectionForm.ShowDialog() == DialogResult.OK)
                        {
                            Current = routineSelectionForm.SelectedRoutine;
                        }
                        else
                        {
                            Current = new InvalidRoutineWrapper();
                        }
                        return;
                    }

                    if (_current != null)
                    {
                        _current.Dispose();
                    }
                    _current = value;
                    _lastSelectedRoutineName = _current.Name;
                    return;
                }

                if (_current != null)
                {
                    _current.Dispose();
                }
                _current = null;
            }
        }

        public static event EventHandler Reloaded;

        public static CapabilityState GetCapabilityState(CapabilityFlags capability)
        {
            if (capability == CapabilityFlags.None)
            {
                return CapabilityState.DontCare;
            }
            if (CountSetBits((uint)capability) > 1)
            {
                throw new ArgumentOutOfRangeException("capability");
            }
            int index = GetSetBitIndex((uint)capability);
            return _capabilityStates[index];
        }

        public static void SetCapabilityState(CapabilityFlags capability, CapabilityState state)
        {
            if (state < CapabilityState.Disallowed || state > CapabilityState.DontCare)
            {
                throw new ArgumentOutOfRangeException("state");
            }
            if (CountSetBits((uint)capability) > 1)
            {
                throw new ArgumentOutOfRangeException("capability");
            }
            if (capability == CapabilityFlags.None)
            {
                return;
            }
            int index = GetSetBitIndex((uint)capability);
            CapabilityState currentState = _capabilityStates[index];
            if (state == currentState)
            {
                return;
            }
            _capabilityStates[index] = state;
            Logging.WriteDiagnostic("Capability state flags changed. {0}: {1} -> {2}", capability, currentState, state);
        }

        private static int GetSetBitIndex(uint value)
        {
            int result = ((value > 65535U) ? 1 : 0) << 4;
            value >>= result;
            int shift = ((value > 255U) ? 1 : 0) << 3;
            value >>= shift;
            result |= shift;
            shift = ((value > 15U) ? 1 : 0) << 2;
            value >>= shift;
            result |= shift;
            shift = ((value > 3U) ? 1 : 0) << 1;
            value >>= shift;
            result |= shift;
            return result | (int)(value >> 1);
        }

        private static int CountSetBits(uint value)
        {
            int count = 0;
            while (value != 0U)
            {
                value &= value - 1U;
                count++;
            }
            return count;
        }

        public static void ChangeRoutine(string routineNamePartial = null, bool reloadAssemblies = false)
        {
            Current = null;

            if (reloadAssemblies)
            {
                ReloadRoutineAssemblies();
            }

            Reload();

            if (!string.IsNullOrWhiteSpace(routineNamePartial))
            {
                ICombatRoutine nameMatchedRoutine = Routines.FirstOrDefault(r => r.Name.ToLower().Contains(routineNamePartial.ToLower()));
                if (nameMatchedRoutine != null)
                {
                    Current = nameMatchedRoutine;
                    return;
                }
            }

            List<ICombatRoutine> routines = Routines;
            if (routines.Count == 0)
            {
                throw new Exception("Can not change the routine! No routines loaded for this class!");
            }
            if (routines.Count == 1)
            {
                Current = routines[0];
                return;
            }

            if (!string.IsNullOrWhiteSpace(_lastSelectedRoutineName))
            {
                ICombatRoutine nameMatchedRoutine = Routines.FirstOrDefault(r => r.Name.ToLower().Contains(_lastSelectedRoutineName.ToLower()));
                if (nameMatchedRoutine != null)
                {
                    Current = nameMatchedRoutine;
                    return;
                }
            }

            RoutineSelectionForm routineSelectionForm = new RoutineSelectionForm(Routines);
            if (routineSelectionForm.ShowDialog() == DialogResult.OK)
            {
                Current = routineSelectionForm.SelectedRoutine;
                return;
            }
            Current = new InvalidRoutineWrapper();
        }

        public static void SetCurrentCombatRoutine()
        {
            if (Current == null)
            {
                ChangeRoutine(CommandLineRoutine, false);
            }
        }

        internal static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            ReloadRoutineAssemblies();
            _initialized = true;
        }

        public static void Reload()
        {
            foreach (ICombatRoutine combatRoutine in Routines)
            {
                combatRoutine.Dispose();
            }
            Routines.Clear();

            if (!Directory.Exists(RoutineDirectory))
            {
                throw new Exception("Cannot load or reload combat routines. Cannot find routine folder.");
            }

            AllRoutines = Loader.Instances.ToList();
            Routines = AllRoutines.Where(r => r.Class == StyxWoW.Me.Class).ToList();

            Logging.WriteDiagnostic("[RoutineManager] Routines have been reloaded. Current class-specific list:");
            foreach (ICombatRoutine combatRoutine in Routines)
            {
                Logging.WriteDiagnostic(combatRoutine.Name + " v" + combatRoutine.GetType().Assembly.GetName().Version);
            }

            string commandLineRoutine = CommandLineRoutine;
            if (!string.IsNullOrWhiteSpace(commandLineRoutine))
            {
                Logging.WriteDiagnostic("[RoutineManager] Routine selected from command line: " + commandLineRoutine);
            }

            if (Reloaded != null)
            {
                Reloaded(null, EventArgs.Empty);
            }
        }

        public static void ReloadRoutineAssemblies()
        {
            Loader.Reload("[RoutineManager] ReloadRoutineAssemblies called.");
        }
    }
}
