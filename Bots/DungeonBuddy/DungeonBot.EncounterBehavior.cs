using Styx.Common;
using Styx.TreeSharp;
using Bots.DungeonBuddy.Dynamics;

namespace Bots.DungeonBuddy
{
    public partial class DungeonBot
    {
        private Composite CreateEncounterBehavior()
        {
            return  new PrioritySelector(
                    new Decorator(
                        ret => DungeonManager.SelectedDungeon != null,
                        CompositeBuilder.GetDungeonComposite(DungeonManager.SelectedDungeon)
                    ));
        }
    }
}
