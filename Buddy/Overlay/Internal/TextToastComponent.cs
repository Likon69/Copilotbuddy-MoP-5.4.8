using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using Buddy.Overlay.Notifications;

namespace Buddy.Overlay.Internal
{
    internal class TextToastComponent : ToastUIComponent
    {
        public TextToastComponent(Func<string> textProducer, TimeSpan duration, Color color, Color shadowColor, FontFamily fontFamily, FontWeight fontWeight, double fontSize)
        {
            this.TextProducer = textProducer;
            base.DisplayDuration = duration;
            this.TextColor = color;
            this.ShadowColor = shadowColor;
            this.FontFamily = fontFamily;
            this.FontWeight = fontWeight;
            this.FontSize = fontSize;
        }

        public Func<string> TextProducer { get; private set; }

        public Color TextColor { get; private set; }

        public Color ShadowColor { get; private set; }

        public FontFamily FontFamily { get; private set; }

        public double FontSize { get; private set; }

        public FontWeight FontWeight { get; private set; }

        public override FrameworkElement GuiElement
        {
            get
            {
                return this.TextBlock;
            }
        }

        private TextBlock TextBlock
        {
            get
            {
                TextBlock textBlock;
                if ((textBlock = this.textBlock) == null)
                {
                    textBlock = (this.textBlock = this.CreateTextBlock());
                }
                return textBlock;
            }
        }

        private TextBlock CreateTextBlock()
        {
            SolidColorBrush solidColorBrush = new SolidColorBrush(this.TextColor);
            solidColorBrush.Freeze();
            TextBlock textBlock = new TextBlock();
            textBlock.Text = this.TextProducer();
            textBlock.FontFamily = this.FontFamily;
            textBlock.FontSize = this.FontSize;
            textBlock.FontWeight = this.FontWeight;
            textBlock.Foreground = solidColorBrush;
            textBlock.RenderTransformOrigin = new Point(0.5, 0.5);
            textBlock.IsHitTestVisible = false;
            textBlock.TextAlignment = TextAlignment.Center;
            textBlock.Effect = new DropShadowEffect
            {
                Color = this.ShadowColor,
                BlurRadius = 15.0,
                ShadowDepth = 0.0
            };
            ScaleTransform scaleTransform = new ScaleTransform(1.0, 1.0);
            textBlock.RenderTransform = scaleTransform;
            DoubleAnimation doubleAnimation = new DoubleAnimation(2.5, 1.0, new Duration(TimeSpan.FromMilliseconds(400.0)))
            {
                EasingFunction = new CircleEase()
            };
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, doubleAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, doubleAnimation);
            return textBlock;
        }

        protected internal override void Update()
        {
            this.TextBlock.Text = this.TextProducer();
        }

        private TextBlock textBlock;
    }
}
