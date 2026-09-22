using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using Bots.BGBuddy.HeatMap;

using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Bots.BGBuddy
{
    public partial class HeatmapWindow : Form
    {
        public HeatmapWindow()
        {
            InitializeComponent();

            nudEnemy.Value = (decimal) Heatmap.EnemyPlayerHeat;
            nudFriendly.Value = (decimal) Heatmap.FriendlyPlayerHeat;
            nudObjective.Value = (decimal) Heatmap.ObjectiveHeat;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!StyxWoW.IsInGame)
                return;
            if (!TreeRoot.IsRunning)
                ObjectManager.Update();

            UpdateHeatmap();

            var width = pbHeatMap.Width;
            var height = pbHeatMap.Height;
            Bitmap bmp = new Bitmap(width, height);

            bmp = Heatmap.CreateIntensityMask(bmp, Heatmap.HeatPoints);
            pbHeatMap.Image = LabelHeats(Heatmap.Colorize(bmp, 255));
        }

        Image LabelHeats(Bitmap bmp)
        {
            using (var graphics = Graphics.FromImage(bmp))
            {
                var myLoc = new Vector2(StyxWoW.Me.X, StyxWoW.Me.Y);
                foreach (var hp in Heatmap.HeatPoints.OrderBy(p => myLoc.Distance(p)))
                {
                    if (hp is ObjectiveHeatpoint)
                    {
                        var rel = Heatmap.GetRelative(myLoc, hp, bmp.Width, bmp.Height);

                        graphics.DrawString(hp.GetHeatAverage().ToString(),
                            SystemFonts.DefaultFont,
                            Brushes.Violet,
                            rel.X,
                            rel.Y);
                    }
                }
            }
            return bmp;
        }

        void PrintClosest()
        {
            var myLoc = new Vector2(StyxWoW.Me.X, StyxWoW.Me.Y);

            foreach (var hp in Heatmap.HeatPoints.OrderBy(p => myLoc.Distance(p)))
            {
                if (hp is ObjectiveHeatpoint)
                {
                    Logging.WriteDiagnostic("HeatPoint at " + hp.X + ", " + hp.Y + " avg heat: " + hp.GetHeatAverage());
                    break;
                }
            }
        }

        void UpdateHeatmap()
        {
        }

        private void nudEnemy_ValueChanged(object sender, EventArgs e)
        {
            Heatmap.EnemyPlayerHeat = (float) nudEnemy.Value;
        }

        private void nudFriendly_ValueChanged(object sender, EventArgs e)
        {
            Heatmap.FriendlyPlayerHeat = (float) nudFriendly.Value;
        }

        private void nudObjective_ValueChanged(object sender, EventArgs e)
        {
            Heatmap.ObjectiveHeat = (float) nudObjective.Value;
        }
    }
}
