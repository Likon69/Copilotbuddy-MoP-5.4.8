using Styx.WoWInternals;

namespace Styx.CommonBot.Frames
{
    public class TrainerFrame : Frame
    {
        public static readonly TrainerFrame Instance = new TrainerFrame();

        public TrainerFrame() : base("ClassTrainerFrame") { }

        public new bool IsVisible => Lua.GetReturnVal<bool>("return ClassTrainerFrame and ClassTrainerFrame:IsVisible()", 0U);
        public int NumTrainerServices => Lua.GetReturnVal<int>("return GetNumTrainerServices()", 0U);
        public bool IsTradeskillTrainer => Lua.GetReturnVal<bool>("return IsTradeskillTrainer()", 0U);
        public int Selected => Lua.GetReturnVal<int>("return GetTrainerSelectionIndex()", 0U);

        public void SetServiceFilter(TrainerServiceFilter filter, bool show)
        {
            Lua.DoString("SetTrainerServiceTypeFilter('{0}',{1})", filter.ToString().ToLower(), show ? 1 : 0);
        }

        public int GetServiceCost(int index) => Lua.GetReturnVal<int>(string.Format("return GetTrainerServiceCost({0})", index + 1), 0U);
        public void Close() => Lua.DoString("CloseTrainer()", "WoW.lua");
        public void Buy(int index) => Lua.DoString("BuyTrainerService({0})", index + 1);
        public bool BuyAll() => Lua.GetReturnVal<bool>("BuyTrainerService(0); return GetNumTrainerServices();", 0U);
    }
}
