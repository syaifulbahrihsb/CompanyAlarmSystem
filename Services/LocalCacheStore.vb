Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Xml.Serialization

Public NotInheritable Class LocalCacheStore
    Private Sub New()
    End Sub

    Public Shared ReadOnly Property DataDirectory As String
        Get
            Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CompanyAlarmSystem")
        End Get
    End Property

    Public Shared ReadOnly Property CacheFile As String
        Get
            Return Path.Combine(DataDirectory, "runtime-cache.xml")
        End Get
    End Property

    Private Shared ReadOnly Property PendingLogFile As String
        Get
            Return Path.Combine(DataDirectory, "pending-logs.xml")
        End Get
    End Property

    Public Shared ReadOnly Property LogDirectory As String
        Get
            Return Path.Combine(DataDirectory, "Logs")
        End Get
    End Property

    Public Shared Sub SaveRuntime(data As RuntimeData)
        Directory.CreateDirectory(DataDirectory)
        Dim serializer As New XmlSerializer(GetType(RuntimeData))
        Using stream As New FileStream(CacheFile, FileMode.Create, FileAccess.Write, FileShare.None)
            serializer.Serialize(stream, data)
        End Using
    End Sub

    Public Shared Function LoadRuntime() As RuntimeData
        Directory.CreateDirectory(DataDirectory)
        If Not File.Exists(CacheFile) Then Return CreateDefaultRuntime()

        Try
            Dim serializer As New XmlSerializer(GetType(RuntimeData))
            Using stream As New FileStream(CacheFile, FileMode.Open, FileAccess.Read, FileShare.Read)
                Dim result = TryCast(serializer.Deserialize(stream), RuntimeData)
                If result IsNot Nothing Then Return result
            End Using
        Catch
        End Try

        Return CreateDefaultRuntime()
    End Function

    Public Shared Sub AppendTextLog(message As String)
        Directory.CreateDirectory(LogDirectory)
        Dim filePath = Path.Combine(LogDirectory, DateTime.Now.ToString("yyyy-MM-dd") & ".log")
        File.AppendAllText(filePath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & " | " & message & Environment.NewLine)
    End Sub

    Public Shared Function LoadPendingLogs() As List(Of PendingAlarmLog)
        Directory.CreateDirectory(DataDirectory)
        If Not File.Exists(PendingLogFile) Then Return New List(Of PendingAlarmLog)()

        Try
            Dim serializer As New XmlSerializer(GetType(List(Of PendingAlarmLog)))
            Using stream As New FileStream(PendingLogFile, FileMode.Open, FileAccess.Read, FileShare.Read)
                Dim result = TryCast(serializer.Deserialize(stream), List(Of PendingAlarmLog))
                If result IsNot Nothing Then Return result
            End Using
        Catch
        End Try

        Return New List(Of PendingAlarmLog)()
    End Function

    Public Shared Sub SavePendingLogs(items As List(Of PendingAlarmLog))
        Directory.CreateDirectory(DataDirectory)
        Dim serializer As New XmlSerializer(GetType(List(Of PendingAlarmLog)))
        Using stream As New FileStream(PendingLogFile, FileMode.Create, FileAccess.Write, FileShare.None)
            serializer.Serialize(stream, items)
        End Using
    End Sub

    Public Shared Sub QueuePendingLog(item As PendingAlarmLog)
        Dim items = LoadPendingLogs()
        items.Add(item)
        SavePendingLogs(items)
    End Sub

    Private Shared Function CreateDefaultRuntime() As RuntimeData
        Dim data As New RuntimeData()
        data.Schedules.Add(New AlarmSchedule With {.ScheduleCode = "AS-WORK-PREP", .ScheduleName = "Persiapan Masuk", .AlarmTime = "07:55", .SoundFile = "Sounds\bell_masuk.wav"})
        data.Schedules.Add(New AlarmSchedule With {.ScheduleCode = "AS-WORK-START", .ScheduleName = "Mulai Kerja", .AlarmTime = "08:00", .SoundFile = "Sounds\bell_masuk.wav"})
        data.Schedules.Add(New AlarmSchedule With {.ScheduleCode = "AS-BREAK", .ScheduleName = "Jam Istirahat", .AlarmTime = "12:00", .SoundFile = "Sounds\bell_istirahat.wav"})
        data.Schedules.Add(New AlarmSchedule With {.ScheduleCode = "AS-WORK-RETURN", .ScheduleName = "Masuk Kembali", .AlarmTime = "13:00", .SoundFile = "Sounds\bell_kembali.wav"})
        data.Schedules.Add(New AlarmSchedule With {.ScheduleCode = "AS-WORK-END", .ScheduleName = "Jam Pulang", .AlarmTime = "17:00", .SoundFile = "Sounds\bell_pulang.wav"})
        Return data
    End Function
End Class
