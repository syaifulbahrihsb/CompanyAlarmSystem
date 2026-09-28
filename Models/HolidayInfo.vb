Imports System

<Serializable>
Public Class HolidayInfo
    Public Property HolidayId As Integer
    Public Property HolidayDate As DateTime
    Public Property HolidayName As String
    Public Property IsActive As Boolean

    Public Sub New()
        HolidayId = 0
        HolidayDate = DateTime.Today
        HolidayName = "Hari Libur"
        IsActive = True
    End Sub
End Class
