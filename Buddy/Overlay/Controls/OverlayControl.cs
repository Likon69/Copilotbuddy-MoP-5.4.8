using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Buddy.Overlay.Commands;
using Buddy.Overlay.Internal;

namespace Buddy.Overlay.Controls
{
    public class OverlayControl : ContentControl
    {
        public bool AllowMoving
        {
            get
            {
                return (bool)base.GetValue(OverlayControl.AllowMovingProperty);
            }
            set
            {
                base.SetValue(OverlayControl.AllowMovingProperty, value);
            }
        }

        public bool AllowResizing
        {
            get
            {
                return (bool)base.GetValue(OverlayControl.AllowResizingProperty);
            }
            set
            {
                base.SetValue(OverlayControl.AllowResizingProperty, value);
            }
        }

        public double X
        {
            get
            {
                return (double)base.GetValue(OverlayControl.XProperty);
            }
            set
            {
                base.SetValue(OverlayControl.XProperty, value);
            }
        }

        public double Y
        {
            get
            {
                return (double)base.GetValue(OverlayControl.YProperty);
            }
            set
            {
                base.SetValue(OverlayControl.YProperty, value);
            }
        }

        private static void OnXChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            Canvas.SetLeft((OverlayControl)dependencyObject, (double)e.NewValue);
        }

        private static void OnYChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            Canvas.SetTop((OverlayControl)dependencyObject, (double)e.NewValue);
        }

        static OverlayControl()
        {
            FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(typeof(OverlayControl), new FrameworkPropertyMetadata(typeof(OverlayControl)));
        }

        public OverlayControl()
        {
            base.MouseMove += this.OverlayControl_MouseMove;
            base.MouseLeftButtonUp += this.OverlayControl_MouseLeftButtonUp;
            base.LostMouseCapture += this.OverlayControl_LostMouseCapture;
        }

        public ICommand DragMoveCommand
        {
            get
            {
                ICommand command;
                if ((command = this.dragMoveCommand) == null)
                {
                    command = (this.dragMoveCommand = new RelayCommand(new Action<object>(this.ExecuteDragMove), new Predicate<object>(this.CanDragMove)));
                }
                return command;
            }
        }

        public ICommand DragResizeCommand
        {
            get
            {
                ICommand command;
                if ((command = this.dragResizeCommand) == null)
                {
                    command = (this.dragResizeCommand = new RelayCommand(new Action<object>(this.ExecuteDragResize), new Predicate<object>(this.CanDragResize)));
                }
                return command;
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }
            this.canvas = OverlayControl.FindVisualParent<Canvas>(this);
            if (this.canvas == null)
            {
                throw new InvalidOperationException("OverlayControl must have a canvas parent");
            }
            this.overlayWindow = OverlayControl.FindVisualParent<OverlayWindowBase>(this);
            if (this.overlayWindow == null)
            {
                throw new InvalidOperationException("OverlayControl can only be used in an OverlayWindow");
            }
            double num = 1.0 / this.overlayWindow.ScaleX;
            double num2 = 1.0 / this.overlayWindow.ScaleY;
            this.X = OverlayControl.Clamp(this.X, 0.0, this.overlayWindow.ActualWidth * num - base.ActualWidth);
            this.Y = OverlayControl.Clamp(this.Y, 0.0, this.overlayWindow.ActualHeight * num2 - base.ActualHeight);
        }

        public void DragMove()
        {
            if (!this.AllowMoving)
            {
                return;
            }
            if (Mouse.LeftButton == MouseButtonState.Pressed && !this.isMoving && !this.isResizing)
            {
                this.isMoving = true;
                this.moveOrigin = new Point(this.X, this.Y);
                this.BeginDrag();
                return;
            }
        }

        private void BeginDrag()
        {
            this.lastMousePosition = (this.dragStart = Mouse.GetPosition(null));
            base.CaptureMouse();
        }

        public bool CancelMove()
        {
            if (!this.isMoving)
            {
                return false;
            }
            if (base.IsMouseCaptured)
            {
                base.ReleaseMouseCapture();
            }
            this.isMoving = false;
            return true;
        }

        private void OverlayControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (!this.isMoving && !this.isResizing)
            {
                return;
            }
            if (e.MouseDevice.LeftButton == MouseButtonState.Pressed)
            {
                Point position = e.GetPosition(null);
                if (position != this.lastMousePosition)
                {
                    this.lastMousePosition = position;
                    e.Handled = true;
                    double num = 1.0 / this.overlayWindow.ScaleX;
                    double num2 = 1.0 / this.overlayWindow.ScaleY;
                    double num3 = (position.X - this.dragStart.X) * num;
                    double num4 = (position.Y - this.dragStart.Y) * num2;
                    if (this.isMoving)
                    {
                        double num5 = this.overlayWindow.ActualWidth * num - base.ActualWidth;
                        double num6 = this.overlayWindow.ActualHeight * num2 - base.ActualHeight;
                        this.ApplyMove(num3, num4, num5, num6);
                        return;
                    }
                    if (this.isResizing)
                    {
                        this.ApplyResize(num3, num4);
                        return;
                    }
                }
            }
            else
            {
                this.CancelResize();
                this.CancelMove();
            }
        }

        private void ApplyMove(double deltaX, double deltaY, double maxX, double maxY)
        {
            double num = this.moveOrigin.X + deltaX;
            double num2 = this.moveOrigin.Y + deltaY;
            this.X = OverlayControl.Clamp(num, 0.0, maxX);
            this.Y = OverlayControl.Clamp(num2, 0.0, maxY);
        }

        private void OverlayControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.CancelResize() || this.CancelMove())
            {
                e.Handled = true;
            }
        }

        private void OverlayControl_LostMouseCapture(object sender, MouseEventArgs e)
        {
            if (this.CancelResize() || this.CancelMove())
            {
                e.Handled = true;
            }
        }

        public void DragResize(HorizontalAlignment horizontalDirection = HorizontalAlignment.Right, VerticalAlignment verticalDirection = VerticalAlignment.Bottom)
        {
            if (!this.AllowResizing)
            {
                return;
            }
            if (Mouse.LeftButton == MouseButtonState.Pressed && !this.isMoving && !this.isResizing)
            {
                this.isResizing = true;
                this.horizontalDirection = ((horizontalDirection == HorizontalAlignment.Right) ? 1 : ((horizontalDirection == HorizontalAlignment.Left) ? -1 : 0));
                this.verticalDirection = ((verticalDirection == VerticalAlignment.Bottom) ? 1 : ((verticalDirection == VerticalAlignment.Top) ? -1 : 0));
                this.resizeStartWidth = base.ActualWidth;
                this.resizeStartHeight = base.ActualHeight;
                this.BeginDrag();
                return;
            }
        }

        private void ApplyResize(double deltaX, double deltaY)
        {
            double num = this.resizeStartWidth + deltaX * (double)this.horizontalDirection;
            double num2 = this.resizeStartHeight + deltaY * (double)this.verticalDirection;
            base.Width = OverlayControl.Clamp(num, base.MinWidth, base.MaxWidth);
            base.Height = OverlayControl.Clamp(num2, base.MinHeight, base.MaxHeight);
        }

        public bool CancelResize()
        {
            if (!this.isResizing)
            {
                return false;
            }
            if (base.IsMouseCaptured)
            {
                base.ReleaseMouseCapture();
            }
            this.isResizing = false;
            return true;
        }

        private static T FindVisualParent<T>(FrameworkElement frameworkElement) where T : FrameworkElement
        {
            if (frameworkElement == null)
            {
                return default(T);
            }
            FrameworkElement frameworkElement2 = frameworkElement.Parent as FrameworkElement;
            if (frameworkElement2 is T)
            {
                return (T)((object)frameworkElement2);
            }
            FrameworkElement frameworkElement3 = frameworkElement.TemplatedParent as FrameworkElement;
            if (frameworkElement3 is T)
            {
                return (T)((object)frameworkElement3);
            }
            return OverlayControl.FindVisualParent<T>(frameworkElement2 ?? frameworkElement3);
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
            {
                return min;
            }
            if (value > max)
            {
                return max;
            }
            return value;
        }

        private void ExecuteDragMove(object parameter)
        {
            this.DragMove();
        }

        private bool CanDragMove(object parameter)
        {
            return this.AllowMoving;
        }

        private void ExecuteDragResize(object parameter)
        {
            this.DragResize(HorizontalAlignment.Right, VerticalAlignment.Bottom);
        }

        private bool CanDragResize(object parameter)
        {
            return this.AllowResizing;
        }

        public static readonly DependencyProperty AllowMovingProperty = DependencyProperty.Register("AllowMoving", typeof(bool), typeof(OverlayControl));

        public static readonly DependencyProperty AllowResizingProperty = DependencyProperty.Register("AllowResizing", typeof(bool), typeof(OverlayControl));

        public static readonly DependencyProperty XProperty = DependencyProperty.Register("X", typeof(double), typeof(OverlayControl), new PropertyMetadata(new PropertyChangedCallback(OverlayControl.OnXChanged)));

        public static readonly DependencyProperty YProperty = DependencyProperty.Register("Y", typeof(double), typeof(OverlayControl), new PropertyMetadata(new PropertyChangedCallback(OverlayControl.OnYChanged)));

        private Canvas canvas;

        private OverlayWindowBase overlayWindow;

        private ICommand dragMoveCommand;

        private ICommand dragResizeCommand;

        private bool isMoving;

        private Point moveOrigin;

        private Point dragStart;

        private int horizontalDirection;

        private bool isResizing;

        private Point lastMousePosition;

        private int verticalDirection;

        private double resizeStartWidth;

        private double resizeStartHeight;
    }
}
