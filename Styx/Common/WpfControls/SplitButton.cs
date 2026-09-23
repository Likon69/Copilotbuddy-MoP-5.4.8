using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Styx.Common.WpfControls
{
    [TemplatePart(Name = "SplitElement", Type = typeof(UIElement))]
    public class SplitButton : Button
    {
        public bool AlwaysOpenMenu
        {
            get
            {
                return (bool)base.GetValue(SplitButton.AlwaysOpenMenuProperty);
            }
            set
            {
                base.SetValue(SplitButton.AlwaysOpenMenuProperty, value);
            }
        }

        public Collection<object> ButtonMenuItemsSource
        {
            get
            {
                return this._buttonMenuItemsSource;
            }
        }

        protected bool IsMouseOverSplitElement { get; private set; }

        public SplitButton()
        {
            base.DefaultStyleKey = typeof(SplitButton);
        }

        public override void OnApplyTemplate()
        {
            if (this._splitElement != null)
            {
                this._splitElement.MouseEnter -= this.SplitElement_MouseEnter;
                this._splitElement.MouseLeave -= this.SplitElement_MouseLeave;
                this._splitElement = null;
            }
            if (this._contextMenu != null)
            {
                this._contextMenu.Opened -= this.ContextMenu_Opened;
                this._contextMenu.Closed -= this.ContextMenu_Closed;
                this._contextMenu = null;
            }
            if (this._logicalChild != null)
            {
                base.RemoveLogicalChild(this._logicalChild);
                this._logicalChild = null;
            }
            base.OnApplyTemplate();
            this._splitElement = base.GetTemplateChild("SplitElement") as UIElement;
            if (this._splitElement != null)
            {
                this._splitElement.MouseEnter += this.SplitElement_MouseEnter;
                this._splitElement.MouseLeave += this.SplitElement_MouseLeave;
                this._contextMenu = ContextMenuService.GetContextMenu(this._splitElement);
                if (this._contextMenu != null)
                {
                    this._contextMenu.IsOpen = true;
                    DependencyObject dependencyObject = this._contextMenu;
                    do
                    {
                        this._logicalChild = dependencyObject;
                        dependencyObject = LogicalTreeHelper.GetParent(dependencyObject);
                    }
                    while (dependencyObject != null);
                    this._contextMenu.IsOpen = false;
                    base.AddLogicalChild(this._logicalChild);
                    this._contextMenu.Opened += this.ContextMenu_Opened;
                    this._contextMenu.Closed += this.ContextMenu_Closed;
                }
            }
        }

        protected override void OnClick()
        {
            if (!this.AlwaysOpenMenu && !this.IsMouseOverSplitElement)
            {
                base.OnClick();
                return;
            }
            this.OpenButtonMenu();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }
            if (Key.Down != e.Key)
            {
                if (Key.Up != e.Key)
                {
                    base.OnKeyDown(e);
                    return;
                }
            }
            base.Dispatcher.BeginInvoke(new Action(this.OpenButtonMenu), new object[0]);
        }

        protected void OpenButtonMenu()
        {
            if (this._buttonMenuItemsSource.Any<object>() && this._contextMenu != null)
            {
                this._contextMenu.HorizontalOffset = 0.0;
                this._contextMenu.VerticalOffset = 0.0;
                this._contextMenu.IsOpen = true;
            }
        }

        private void SplitElement_MouseEnter(object sender, MouseEventArgs e)
        {
            this.IsMouseOverSplitElement = true;
        }

        private void SplitElement_MouseLeave(object sender, MouseEventArgs e)
        {
            this.IsMouseOverSplitElement = false;
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            this._contextMenuInitialOffset = base.TranslatePoint(new Point(0.0, base.ActualHeight), this._contextMenu);
            this.UpdateContextMenuOffsets();
            base.LayoutUpdated += this.SplitButton_LayoutUpdated;
        }

        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            base.LayoutUpdated -= this.SplitButton_LayoutUpdated;
            base.Focus();
        }

        private void SplitButton_LayoutUpdated(object sender, EventArgs e)
        {
            this.UpdateContextMenuOffsets();
        }

        private void UpdateContextMenuOffsets()
        {
            Point point = default(Point);
            Point contextMenuInitialOffset = this._contextMenuInitialOffset;
            this._contextMenu.HorizontalOffset = contextMenuInitialOffset.X - point.X;
            this._contextMenu.VerticalOffset = contextMenuInitialOffset.Y - point.Y;
            if (FlowDirection.RightToLeft == base.FlowDirection)
            {
                this._contextMenu.HorizontalOffset *= -1.0;
            }
        }

        private const string SplitElementName = "SplitElement";

        public static readonly DependencyProperty AlwaysOpenMenuProperty = DependencyProperty.Register("AllowButton", typeof(bool), typeof(SplitButton), new PropertyMetadata(false));

        private UIElement _splitElement;

        private ContextMenu _contextMenu;

        private DependencyObject _logicalChild;

        private Point _contextMenuInitialOffset;

        private readonly ObservableCollection<object> _buttonMenuItemsSource = new ObservableCollection<object>();
    }
}
