using Styx.WoWInternals;

namespace Styx.CommonBot.Frames
{
    public class Frame
    {
        #region Properties

        public string FrameName { get; private set; }

        public bool IsVisible { get { return Lua.GetReturnVal<int>(string.Format("if {0} and {0}:IsVisible() then return 1 else return 0 end", FrameName), 0) == 1; } }

        #endregion

        #region Constructors

        public Frame(string frameName)
        {
            FrameName = frameName;
        }

        #endregion

        #region Instance methods

        public virtual void Hide()
        {
            Lua.DoString(string.Format("{0}:Hide()", FrameName), "Economist.lua");
        }

        public virtual void Show()
        {
            Lua.DoString(string.Format("{0}:Show()", FrameName), "Economist.lua");
        }

        #endregion
    }
}
