Imports System
Imports System.Collections.Generic

<Serializable>
Public Class RuntimeData
    Public Property Schedules As List(Of AlarmSchedule)
    Public Property Holidays As List(Of HolidayInfo)

    Public Sub New()
        Schedules = New List(Of AlarmSchedule)()
        Holidays = New List(Of HolidayInfo)()
    End Sub
End Class
