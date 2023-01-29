using System;

namespace MangaDownloader;

public enum NotificationType
{
    None,
    FinishedTask,
    Success,
    Error,
}

public class NotificationError : Exception
{
    public string Notification { get; private set; }
    public NotificationType Type { get; private set; }

    public NotificationError(string notificationMsg, NotificationType notificationType = NotificationType.None)
    {
        Notification = notificationMsg;
        Type = notificationType;
    }
}
