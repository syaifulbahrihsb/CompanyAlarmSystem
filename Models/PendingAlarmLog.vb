Imports System

<Serializable>
Public Class PendingAlarmLog
    Public Property ScheduleId As Integer
    Public Property ScheduledAt As DateTime?
    Public Property TriggeredAt As DateTime
    Public Property Status As String
    Public Property AudioFile As String
    Public Property Message As String

    Public Sub New()
        TriggeredAt = DateTime.Now
        Status = "SUCCESS"
        AudioFile = ""
        Message = ""
    End Sub
End Class
