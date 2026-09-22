using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Buddy.Overlay.Controls;
using Buddy.Overlay.Notifications;

namespace Buddy.Overlay.Internal
{
    internal sealed class ToastOverlayWindow : OverlayWindowBase, IToastHost, IWidgetHost
    {
        public ToastOverlayWindow(Process attachedProcess, ToastSettings toastSettings)
            : base(attachedProcess, 32)
        {
            Grid grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Brushes.Transparent
            };
            grid.RowDefinitions.Add(new RowDefinition
            {
                Height = new GridLength(0.4, GridUnitType.Star)
            });
            grid.RowDefinitions.Add(new RowDefinition
            {
                Height = new GridLength(0.6, GridUnitType.Star)
            });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1.0, GridUnitType.Star)
            });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1.0, GridUnitType.Auto)
            });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1.0, GridUnitType.Star)
            });
            this.toastStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(this.toastStack, 1);
            grid.Children.Add(this.toastStack);
            base.Grid.Children.Add(grid);
            this.ApplyToastSettings(toastSettings);
        }

        private void ApplyToastSettings(ToastSettings toastSettings)
        {
            VerticalAlignment verticalAlignment = toastSettings.DisplayTopToBottom ? VerticalAlignment.Top : VerticalAlignment.Bottom;
            this.toastStack.VerticalAlignment = verticalAlignment;
        }

        public bool AddWidget(OverlayControl control)
        {
            if (base.Canvas.Children.Contains(control))
            {
                return false;
            }
            base.Canvas.Children.Add(control);
            return true;
        }

        public void RemoveWidget(OverlayControl control)
        {
            base.Canvas.Children.Remove(control);
        }

        public bool AddToastElement(FrameworkElement element)
        {
            if (this.toastStack.Children.Contains(element))
            {
                return false;
            }
            this.toastStack.Children.Add(element);
            return true;
        }

        public void RemoveToastElement(FrameworkElement element)
        {
            this.toastStack.Children.Remove(element);
        }

        public void RemoveToastElementAt(int index)
        {
            this.toastStack.Children.RemoveAt(index);
        }

        private readonly StackPanel toastStack;
    }
}
