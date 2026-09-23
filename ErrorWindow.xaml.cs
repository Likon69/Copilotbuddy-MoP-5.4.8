using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls;

namespace CopilotBuddy
{
    public partial class ErrorWindow : MetroWindow
    {
        private bool? Result { get; set; }

        public ErrorWindow()
        {
            InitializeComponent();
        }

        public static MessageBoxResult Show(string message, string caption, MessageBoxButton button, Icon icon)
        {
            return System.Windows.Application.Current.Dispatcher.Invoke(delegate
            {
                ErrorWindow errorWindow = new ErrorWindow
                {
                    Title = caption
                };
                errorWindow.MessageBox.AppendText(message);
                errorWindow.ErrorWindowIcon.Source = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                switch (button)
                {
                case MessageBoxButton.OK:
                    errorWindow.Button1.Content = "OK";
                    errorWindow.ButtonPanel.Children.RemoveRange(1, 2);
                    break;
                case MessageBoxButton.OKCancel:
                    errorWindow.Button1.Content = "OK";
                    errorWindow.ButtonPanel.Children.RemoveAt(1);
                    errorWindow.Button3.Content = "Cancel";
                    break;
                default:
                    throw new ArgumentOutOfRangeException("button");
                case MessageBoxButton.YesNoCancel:
                    errorWindow.Button1.Content = "Yes";
                    errorWindow.Button2.Content = "No";
                    errorWindow.Button3.Content = "Cancel";
                    break;
                case MessageBoxButton.YesNo:
                    errorWindow.Button1.Content = "Yes";
                    errorWindow.Button2.Content = "No";
                    errorWindow.ButtonPanel.Children.RemoveAt(2);
                    break;
                }
                errorWindow.ShowDialog();
                if (errorWindow.Result.HasValue)
                {
                    switch (button)
                    {
                    case MessageBoxButton.OK:
                    case MessageBoxButton.OKCancel:
                        if (!errorWindow.Result.Value)
                        {
                            return MessageBoxResult.Cancel;
                        }
                        return MessageBoxResult.OK;
                    default:
                        throw new ArgumentOutOfRangeException("button");
                    case MessageBoxButton.YesNoCancel:
                    case MessageBoxButton.YesNo:
                        if (!errorWindow.Result.Value)
                        {
                            return MessageBoxResult.No;
                        }
                        return MessageBoxResult.Yes;
                    }
                }
                switch (button)
                {
                case MessageBoxButton.OK:
                    return MessageBoxResult.OK;
                default:
                    throw new ArgumentOutOfRangeException("button");
                case MessageBoxButton.OKCancel:
                case MessageBoxButton.YesNoCancel:
                    return MessageBoxResult.Cancel;
                case MessageBoxButton.YesNo:
                    return MessageBoxResult.No;
                }
            });
        }

        private void MessageBox_OnLinkClicked(object sender, System.Windows.Forms.LinkClickedEventArgs e)
        {
            Process.Start(e.LinkText);
        }

        private void Button1_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Result = true;
            Close();
        }

        private void Button2_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Result = false;
            Close();
        }

        private void Button3_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Close();
        }
    }
}
