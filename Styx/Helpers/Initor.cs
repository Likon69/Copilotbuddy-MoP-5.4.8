using System.Collections.Generic;
using System.Threading.Tasks;
using Styx.CommonBot;
using Styx.CommonBot.CharacterManagement;
using Styx.CommonBot.Inventory;
using Styx.CommonBot.Routines;
using Styx.Pathing;
using Styx.Plugins;
using Styx.Resources;
using Styx.WoWInternals;

namespace Styx.Helpers
{
    internal static class Initor
    {
        internal static bool Initialized { get; private set; }

        internal static void OnStart()
        {
        }

        internal static void OnAuthSuccess()
        {
            ObjectManager.Update();
            List<Task> initializationTasks = new List<Task>();
            TreeRoot.StatusText = StyxResources.Loading_memory_management___tables;
            TreeRoot.StatusText = StyxResources.Loading_Spells;
            DbHelper.Initialize();
            TreeRoot.StatusText = StyxResources.Initializing_Spell_Manager;
            SpellManager.Init();
            TreeRoot.StatusText = StyxResources.Initializing_Navigator;
            Navigator.Init();
            CharacterManager.Initialize();
            LootPredictor.Initialize();
            BlackspotManager.Initialize();
            TreeRoot.StatusText = StyxResources.Initializing_Flight_Paths;
            FlightPaths.Initialize();
            EquipmentManager.Initialize();
            LootRoll.Initialize();
            InactivityDetector.Initialize();
            TreeRoot.StatusText = StyxResources.Initializing_Routines;
            initializationTasks.Add(Task.Factory.StartNew(RoutineManager.Initialize));
            TreeRoot.StatusText = StyxResources.Initializing_Plugins;
            initializationTasks.Add(Task.Factory.StartNew(delegate
            {
                PluginManager.Initialize(CharacterSettings.Instance.EnabledPlugins);
            }));
            Task.WaitAll(initializationTasks.ToArray());
            Initialized = true;
        }

        internal static void OnShutdown()
        {
            CharacterManager.Shutdown();
        }
    }
}
