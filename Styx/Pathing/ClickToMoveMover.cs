using Styx.WoWInternals;

namespace Styx.Pathing
{
    public class ClickToMoveMover : IPlayerMover
    {
        public void Move(WoWMovement.MovementDirection direction) => WoWMovement.Move(direction);
        public void MoveTowards(WoWPoint location) => WoWMovement.ClickToMove(location);
        public void MoveStop() => WoWMovement.MoveStop();
    }
}
