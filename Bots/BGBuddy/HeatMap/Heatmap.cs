using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using Bots.BGBuddy.Logic.Battlegrounds;
using Styx;
using Styx.Common;
using Styx.WoWInternals.WoWObjects;

namespace Bots.BGBuddy.HeatMap
{
    public class Heatmap
    {
        // Enemy players should be less "hot" than friendly players.
        // We typically want to give the advantage to the other team (more kills)
        // We want *at most* 3 friendlies on a flag.xzz
        public static float EnemyPlayerHeat = 0.10f;

        public static float FriendlyPlayerHeat = -0.13f;

        public static float ObjectiveHeat = 0.6f;


        internal static readonly List<HeatPoint> HeatPoints = new List<HeatPoint>();

        public static void Clear()
        {
            HeatPoints.Clear();
        }


        public static void Add(Vector2 location, float radius, float intensity)
        {
            HeatPoints.Add(new HeatPoint(location.X, location.Y, radius, intensity));
        }

        public static void AddObjective(Vector2 location, float radius, float intensity)
        {
            HeatPoints.Add(new ObjectiveHeatpoint(location.X, location.Y, radius, intensity));
        }

        public static void Add(WoWPlayer player, float radius, float intensity)
        {
            try
            {
                Add(new Vector2(player.X, player.Y), radius, intensity);
            }
            catch{}
        }

        public static void Add(WoWGameObject obj, float radius, float intensity)
        {
            try
            {
                AddObjective(new Vector2(obj.X, obj.Y), radius, intensity);
            }
            catch
            {
            }
        }

        public static void Add(LandmarkInfo landmark, float radius, float intensity)
        {
            AddObjective(new Vector2(landmark.Box.Center.X, landmark.Box.Center.Y), radius, intensity);
        }

        #region Debug Stuff

        internal static Vector2 GetRelative(Vector2 myLoc, Vector2 theirLoc, int width, int height)
        {
            // Flip these so we can display them the proper way on the map.
            myLoc = new Vector2(myLoc.Y, myLoc.X);
            theirLoc = new Vector2(theirLoc.Y, theirLoc.X);

            // Center = myLoc
            Vector2 center = new Vector2(width / 2f, height / 2f);

            // Offset from center...
            Vector2 offset = myLoc - theirLoc;

            var ret = offset + center;

            // If it's off the screen, don't draw it. (I think we can ignore this maybe? Idk if it attempts to draw off-screen if it doesn't have space to)
            if (ret.X > width || ret.X < 0 || ret.Y < 0 || ret.Y > height)
                return Vector2.Zero;

            return ret;
        }
        static List<HeatPoint> MakeRelativeHeatpoints(List<HeatPoint> points, int width, int height)
        {
            List<HeatPoint> ret = new List<HeatPoint>();
            var myLoc = StyxWoW.Me.Location;
            var myLoc2 = new Vector2(myLoc.X, myLoc.Y);

            foreach (var p in points)
            {
                var rel =GetRelative(myLoc2, p, width, height);
                ret.Add(new HeatPoint(rel.X, rel.Y, p.Radius, p.Intensity));
            }
            return ret;
        }
        internal static Bitmap CreateIntensityMask(Bitmap surface, List<HeatPoint> heatPoints)
        {
            // Create new graphics surface from memory bitmap
            Graphics drawSurface = Graphics.FromImage(surface);

            // Set background color to white so that pixels can be correctly colorized
            drawSurface.Clear(Color.White);

            // Traverse heat heatPoint data and draw masks for each heat heatPoint
            foreach (HeatPoint dataPoint in OrderByIntensity(MakeRelativeHeatpoints(heatPoints, surface.Width, surface.Height)))
            {
                if (dataPoint == Vector2.Zero)
                    continue;

                // Render current heat heatPoint on draw surface
                DrawHeatPoint(drawSurface, dataPoint);
            }

            return surface;
        }

        private static IEnumerable<HeatPoint> OrderByIntensity(IEnumerable<HeatPoint> points)
        {
            return points.OrderByDescending(p => p.Intensity).ToList();
        }

        private static void DrawHeatPoint(Graphics graphics, HeatPoint heatPoint)
        {
            var circleList = new List<Point>();

            // Storing this so we can flip it if we need to.
            var drawIntensity = heatPoint.Intensity;

            // Loop through all angles of a circle
            // Define loop variable as a double to prevent casting in each iteration
            // Iterate through loop on 10 degree deltas, this can change to improve performance
            for (double i = 0; i <= 360; i += 10)
            {
                var pt = new Point
                    {
                        X = Convert.ToInt32(heatPoint.X + (heatPoint.Radius) * Math.Cos(ConvertDegreesToRadians(i))),
                        Y = Convert.ToInt32(heatPoint.Y + (heatPoint.Radius) * Math.Sin(ConvertDegreesToRadians(i)))
                    };

                // Add newly plotted circumference heatPoint to generic heatPoint list
                circleList.Add(pt);
            }

            var gradientBrush = new PathGradientBrush(circleList.ToArray());
            var colorBlend = new ColorBlend(3);


            // If intensity is < 0, then we need to "cool" the spot. Otherwise, heat it up.
            if (drawIntensity < 0)
            {
                // Flip the sign. Intensity still works on 0.0-1.0. Negative means we cool it. Flip the sign to keep the same "intensity"
                // but in the opposite direction.
                drawIntensity = -drawIntensity;

                // For "cooling" intensities (< 0.0) we want to push a white texture.
                // We should probably figure something out to place the white on top of the black
                // so we get the full cooling effect shown. Otherwise, the black will just overlay the white and we're basically screwed.
                // ^ Done

                // Define positions of gradient colors, use intesity to adjust the middle color to
                // show more mask or less mask
                colorBlend.Positions = new [] {0, drawIntensity, 1};
                // Define gradient colors and their alpha values, adjust alpha of gradient colors to match intensity
                // Since all intensities are clamped to -1 thru 1. Just *255 to get a "fraction of full alpha" to display.
                colorBlend.Colors = new []
                {
                    Color.FromArgb(0, Color.White),
                    Color.FromArgb((int) (drawIntensity * 255), Color.White),
                    Color.FromArgb((int) (drawIntensity * 255), Color.White)
                };
            }
            else
            {
                colorBlend.Positions = new[] {0, drawIntensity, 1};
                colorBlend.Colors = new[]
                {
                    Color.FromArgb(0, Color.White),
                    Color.FromArgb((int) (drawIntensity * 255), Color.Black),
                    Color.FromArgb((int) (drawIntensity * 255), Color.Black)
                };
            }

            // Pass off color blend to PathGradientBrush to instruct it how to generate the gradient
            gradientBrush.InterpolationColors = colorBlend;

            // Draw polygon (circle) using our heatPoint array and gradient brush
            graphics.FillPolygon(gradientBrush, circleList.ToArray());
        }

        private static double ConvertDegreesToRadians(double degrees)
        {
            double radians = (Math.PI / 180) * degrees;
            return (radians);
        }

        public static Bitmap Colorize(Bitmap mask, byte alpha)
        {
            // Create new bitmap to act as a work surface for the colorization process
            var output = new Bitmap(mask.Width, mask.Height, PixelFormat.Format32bppArgb);

            // Create a graphics object from our memory bitmap so we can draw on it and clear it's drawing surface
            Graphics surface = Graphics.FromImage(output);
            surface.Clear(Color.Transparent);

            // Build an array of color mappings to remap our greyscale mask to full color
            // Accept an alpha byte to specify the transparancy of the output image
            ColorMap[] colors = CreatePaletteIndex(alpha);

            // Create new image attributes class to handle the color remappings
            // Inject our color map array to instruct the image attributes class how to do the colorization
            var remapper = new ImageAttributes();
            remapper.SetRemapTable(colors);

            // Draw our mask onto our memory bitmap work surface using the new color mapping scheme
            surface.DrawImage(mask, new Rectangle(0, 0, mask.Width, mask.Height), 0, 0, mask.Width, mask.Height, GraphicsUnit.Pixel, remapper);

            // Send back newly colorized memory bitmap
            return output;
        }

        public static byte MinThreshold = 0;
        public static byte MaxThreshold = 255;

        private static Bitmap _palette = CreateHeatmapPaletteImage();
        static Bitmap CreateHeatmapPaletteImage()
        {
            // This function just creates a gradient for use in the heatmap colorization. This could be saved to a file (For a more accurate representation) but since
            // we're using this for approximations, this just saves some headaches with extra files. It's good enough :)
            Bitmap bmp = new Bitmap(256, 1);
            using (var gfx = Graphics.FromImage(bmp))
            {
                var br = new LinearGradientBrush(new Point(0, 0), new Point(255, 0), Color.Black, Color.Black);
                var blend = new ColorBlend(6)
                    {
                        Positions = new[] {0f, 1 / 6f, 2 / 6f, 3 / 6f, 4 / 6f, 5 / 6f, 1f},
                        Colors = new[] {Color.White, Color.Red, Color.Yellow, Color.Green, Color.Aqua, Color.Blue, Color.Black}
                    };
                br.InterpolationColors = blend;
                gfx.FillRectangle(br, 0, 0, 255, 1);
            }
            return bmp;
        }

        private static ColorMap[] CreatePaletteIndex(byte alpha)
        {
            ColorMap[] outputMap = new ColorMap[256];

            Bitmap palette = _palette;// (Bitmap)Image.FromFile(@"REDACTED_PATH");

            // Loop through each pixel and create a new color mapping
            for (int X = 0; X <= 255; X++)
            {
                outputMap[X] = new ColorMap();
                outputMap[X].OldColor = Color.FromArgb(X, X, X);

                if (X < MinThreshold || X > MaxThreshold)
                {
                    outputMap[X].NewColor = Color.FromArgb(0, Color.White);
                }
                else
                    outputMap[X].NewColor = Color.FromArgb(alpha, palette.GetPixel(X, 0));
            }

            return outputMap;
        }
        #endregion
    }
}
