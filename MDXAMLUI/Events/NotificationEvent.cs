using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace MDXAMLUI.Events;

/// <summary>
/// Represents the method that will handle the NotificationEventArgs event raised when an operation's status has changed.
/// </summary>
/// <param name="sender">The source of the event.</param>
/// <param name="e">A NotificationEventArgs that contains the event data.</param>
public delegate void NotificationRaisedEventHandler(object sender, NotificationEventArgs e);

/// <summary>
/// Contains state information and event data associated with a notification event.
/// </summary>
public class NotificationEventArgs : RoutedEventArgs
{
    public NotificationEventArgs(string message, NotificationType type)
    {
        this.Message = message;
        this.NotificationType = type;
        this.RoutedEvent = MainWindow.NotificationRoutedEvent;
    }

    public string Message { get; private set; }
    public NotificationType NotificationType { get; private set; }
}
