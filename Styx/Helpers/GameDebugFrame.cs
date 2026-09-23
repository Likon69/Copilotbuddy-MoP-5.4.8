using System;
using System.Text;
using System.Threading;
using Styx;
using Styx.CommonBot;
using Styx.WoWInternals;

namespace Styx.Helpers
{
    internal static class GameDebugFrame
    {
        private static class GameDebugFrameRenderers
        {
            private static float _relativeSpeed;

            private static WoWPoint _lastRelativeCheck;

            private static DateTime _lastRelativeTime;

            static GameDebugFrameRenderers()
            {
                _relativeSpeed = 0f;
                OnFrameRender += GameDebugFrameRenderers_OnFrameRender;
            }

            public static void Init()
            {
            }

            private static void GameDebugFrameRenderers_OnFrameRender(StringBuilder sb)
            {
                RefreshRelativeSpeed();
                sb.AppendFormat("Time: {0}\\nSpeed: {1}\\nStuck: {2}\\nRelative: {3}", StyxWoW.Me.MovementInfo.TimeMoved, StyxWoW.Me.MovementInfo.CurrentSpeed, StyxWoW.Me.MovementInfo.TimeMoved == 0 && StyxWoW.Me.MovementInfo.CurrentSpeed != 0f, _relativeSpeed);
            }

            private static void RefreshRelativeSpeed()
            {
                if (_lastRelativeCheck == WoWPoint.Empty)
                {
                    _lastRelativeTime = DateTime.Now;
                    _lastRelativeCheck = StyxWoW.Me.Location;
                    _relativeSpeed = StyxWoW.Me.MovementInfo.CurrentSpeed;
                }
                else
                {
                    TimeSpan timeSpan = DateTime.Now.Subtract(_lastRelativeTime);
                    float distance = StyxWoW.Me.Location.Distance(_lastRelativeCheck);
                    _relativeSpeed = (float)((double)distance / timeSpan.TotalSeconds);
                    _lastRelativeTime = DateTime.Now;
                    _lastRelativeCheck = StyxWoW.Me.Location;
                }
            }
        }

        private static readonly string _frameName;

        private static GameDebugAddStringDelegate _onFrameRender;

        private static bool FrameIsVisible => Lua.GetReturnVal<bool>("return " + _frameName + " and " + _frameName + ":IsVisible()", 0u);

        public static event GameDebugAddStringDelegate OnFrameRender
        {
            add
            {
                GameDebugAddStringDelegate handler = _onFrameRender;
                GameDebugAddStringDelegate comparand;
                do
                {
                    comparand = handler;
                    GameDebugAddStringDelegate combined = (GameDebugAddStringDelegate)Delegate.Combine(comparand, value);
                    handler = Interlocked.CompareExchange(ref _onFrameRender, combined, comparand);
                }
                while ((object)handler != comparand);
            }
            remove
            {
                GameDebugAddStringDelegate handler = _onFrameRender;
                GameDebugAddStringDelegate comparand;
                do
                {
                    comparand = handler;
                    GameDebugAddStringDelegate combined = (GameDebugAddStringDelegate)Delegate.Remove(comparand, value);
                    handler = Interlocked.CompareExchange(ref _onFrameRender, combined, comparand);
                }
                while ((object)handler != comparand);
            }
        }

        static GameDebugFrame()
        {
            _frameName = "hbdebugframe";
            BotEvents.OnPulse += BotEvents_OnPulse;
            GameDebugFrameRenderers.Init();
        }

        private static void CreateFrame()
        {
            if (!FrameIsVisible)
            {
                Lua.DoString("hbdebugframe = CreateFrame('Frame', nil, UIParent) hbdebugframe:ClearAllPoints() hbdebugframe:SetWidth(128) hbdebugframe:SetHeight(64) hbdebugframe.text = hbdebugframe:CreateFontString(nil, 'BACKGROUND', 'GameFontNormal') hbdebugframe.text:SetAllPoints() hbdebugframe.text:SetText('Debug Frame Created!') hbdebugframe:SetPoint('CENTER', 0,0)hbdebugframe:Show()");
            }
        }

        private static void EnsureVisible()
        {
            if (!FrameIsVisible)
            {
                CreateFrame();
            }
        }

        private static void BotEvents_OnPulse(BotEvents.PulseEventArgs args)
        {
            StringBuilder stringBuilder = new StringBuilder();
            _onFrameRender?.Invoke(stringBuilder);
            string text = stringBuilder.ToString();
            if (string.IsNullOrEmpty(text))
            {
                text = "Empty Event Handler";
            }
            using (StyxWoW.Memory.AcquireFrame())
            {
                EnsureVisible();
                Lua.DoString("{0}.text:SetText(\"{1}\") {0}.text:SetPoint('TOPLEFT', {0}, \"TOPLEFT\", 5, -5) {0}.text:SetWidth({0}:GetRight() - {0}:GetLeft() - 10) {0}:SetHeight({0}.text:GetHeight() + 15)", _frameName, text);
            }
        }

        public static void Initialize()
        {
            CreateFrame();
        }
    }
}
