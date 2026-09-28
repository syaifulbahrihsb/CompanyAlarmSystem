Imports System
Imports System.Collections.Generic
Imports System.Linq

<Serializable>
Public Class AlarmSchedule
    Public Property ScheduleId As Integer
    Public Property ScheduleCode As String
    Public Property ScheduleName As String
    Public Property AlarmTime As String
    Public Property SoundFile As String
    Public Property RepeatCount As Integer
    Public Property RepeatIntervalSeconds As Integer
    Public Property SkipHoliday As Boolean
    Public Property Enabled As Boolean
    Public Property Days As List(Of Integer)

    Public Sub New()
        ScheduleId = 0
        ScheduleCode = "AS-" & Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant()
        ScheduleName = "Alarm"
        AlarmTime = "08:00"
        SoundFile = "Sounds\bell_masuk.wav"
        RepeatCount = 1
        RepeatIntervalSeconds = 3
        SkipHoliday = True
        Enabled = True
        Days = New List(Of Integer) From {1, 2, 3, 4, 5}
    End Sub

    Public Function CloneSchedule() As AlarmSchedule
        Return New AlarmSchedule With {
            .ScheduleId = Me.ScheduleId,
            .ScheduleCode = Me.ScheduleCode,
            .ScheduleName = Me.ScheduleName,
            .AlarmTime = Me.AlarmTime,
            .SoundFile = Me.SoundFile,
            .RepeatCount = Me.RepeatCount,
            .RepeatIntervalSeconds = Me.RepeatIntervalSeconds,
            .SkipHoliday = Me.SkipHoliday,
            .Enabled = Me.Enabled,
            .Days = New List(Of Integer)(If(Me.Days, New List(Of Integer)()))
        }
    End Function
End Class
