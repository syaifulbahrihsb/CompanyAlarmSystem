Imports System
Imports System.Collections.Generic
Imports System.Linq

Public NotInheritable Class ScheduleHelpers
    Private Sub New()
    End Sub

    Public Shared Function ToDayNo(day As DayOfWeek) As Integer
        Select Case day
            Case DayOfWeek.Monday : Return 1
            Case DayOfWeek.Tuesday : Return 2
            Case DayOfWeek.Wednesday : Return 3
            Case DayOfWeek.Thursday : Return 4
            Case DayOfWeek.Friday : Return 5
            Case DayOfWeek.Saturday : Return 6
            Case DayOfWeek.Sunday : Return 7
            Case Else : Return 1
        End Select
    End Function

    Public Shared Function DayName(dayNo As Integer) As String
        Select Case dayNo
            Case 1 : Return "Senin"
            Case 2 : Return "Selasa"
            Case 3 : Return "Rabu"
            Case 4 : Return "Kamis"
            Case 5 : Return "Jumat"
            Case 6 : Return "Sabtu"
            Case 7 : Return "Minggu"
            Case Else : Return "-"
        End Select
    End Function

    Public Shared Function DaysDisplay(days As IEnumerable(Of Integer)) As String
        If days Is Nothing Then Return "-"
        Return String.Join(", ", days.Distinct().OrderBy(Function(x) x).Select(Function(x) DayName(x)))
    End Function
End Class
