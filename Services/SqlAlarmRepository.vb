Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Data
Imports System.Data.SqlClient
Imports System.Globalization
Imports System.Linq

Public Class SqlAlarmRepository
    Private ReadOnly _connectionString As String

    Public Sub New()
        Dim settings = ConfigurationManager.ConnectionStrings("SMB2")
        If settings Is Nothing OrElse String.IsNullOrWhiteSpace(settings.ConnectionString) Then
            Throw New ConfigurationErrorsException("Connection string 'SMB2' tidak ditemukan di App.config.")
        End If
        _connectionString = settings.ConnectionString
    End Sub

    Public Function TestConnection(ByRef errorMessage As String) As Boolean
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                errorMessage = ""
                Return True
            End Using
        Catch ex As Exception
            errorMessage = ex.Message
            Return False
        End Try
    End Function

    Public Function LoadRuntimeData() As RuntimeData
        Dim data As New RuntimeData()
        Dim map As New Dictionary(Of Integer, AlarmSchedule)()

        Using conn As New SqlConnection(_connectionString)
            Using cmd As New SqlCommand("dbo.sp_AS_GetRuntimeData", conn)
                cmd.CommandType = CommandType.StoredProcedure
                cmd.CommandTimeout = 10
                conn.Open()

                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim item As New AlarmSchedule With {
                            .ScheduleId = Convert.ToInt32(reader("ScheduleId")),
                            .ScheduleCode = Convert.ToString(reader("ScheduleCode")),
                            .ScheduleName = Convert.ToString(reader("ScheduleName")),
                            .AlarmTime = CType(reader("AlarmTime"), TimeSpan).ToString("hh\:mm", CultureInfo.InvariantCulture),
                            .SoundFile = If(reader("AudioFile") Is DBNull.Value, "", Convert.ToString(reader("AudioFile"))),
                            .RepeatCount = Convert.ToInt32(reader("RepeatCount")),
                            .RepeatIntervalSeconds = Convert.ToInt32(reader("RepeatIntervalSeconds")),
                            .SkipHoliday = Convert.ToBoolean(reader("SkipHoliday")),
                            .Enabled = Convert.ToBoolean(reader("IsActive")),
                            .Days = New List(Of Integer)()
                        }
                        data.Schedules.Add(item)
                        map(item.ScheduleId) = item
                    End While

                    If reader.NextResult() Then
                        While reader.Read()
                            Dim scheduleId = Convert.ToInt32(reader("ScheduleId"))
                            Dim dayNo = Convert.ToInt32(reader("DayNo"))
                            If map.ContainsKey(scheduleId) AndAlso Not map(scheduleId).Days.Contains(dayNo) Then
                                map(scheduleId).Days.Add(dayNo)
                            End If
                        End While
                    End If

                    If reader.NextResult() Then
                        While reader.Read()
                            data.Holidays.Add(New HolidayInfo With {
                                .HolidayId = Convert.ToInt32(reader("HolidayId")),
                                .HolidayDate = Convert.ToDateTime(reader("HolidayDate")),
                                .HolidayName = Convert.ToString(reader("HolidayName")),
                                .IsActive = Convert.ToBoolean(reader("IsActive"))
                            })
                        End While
                    End If
                End Using
            End Using
        End Using

        For Each item In data.Schedules
            item.Days.Sort()
        Next

        Return data
    End Function

    Public Function SaveSchedule(item As AlarmSchedule) As Integer
        Using conn As New SqlConnection(_connectionString)
            Using cmd As New SqlCommand("dbo.sp_AS_UpsertSchedule", conn)
                cmd.CommandType = CommandType.StoredProcedure
                cmd.CommandTimeout = 10

                cmd.Parameters.Add("@ScheduleId", SqlDbType.Int).Value = If(item.ScheduleId > 0, CType(item.ScheduleId, Object), DBNull.Value)
                cmd.Parameters.Add("@ScheduleCode", SqlDbType.VarChar, 40).Value = item.ScheduleCode
                cmd.Parameters.Add("@ScheduleName", SqlDbType.NVarChar, 150).Value = item.ScheduleName

                Dim parsedTime = DateTime.ParseExact(item.AlarmTime, "HH:mm", CultureInfo.InvariantCulture)
                cmd.Parameters.Add("@AlarmTime", SqlDbType.Time).Value = parsedTime.TimeOfDay
                cmd.Parameters.Add("@AudioFilePath", SqlDbType.NVarChar, 500).Value = item.SoundFile
                cmd.Parameters.Add("@RepeatCount", SqlDbType.TinyInt).Value = item.RepeatCount
                cmd.Parameters.Add("@RepeatIntervalSeconds", SqlDbType.SmallInt).Value = item.RepeatIntervalSeconds
                cmd.Parameters.Add("@SkipHoliday", SqlDbType.Bit).Value = item.SkipHoliday
                cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = item.Enabled
                cmd.Parameters.Add("@DayList", SqlDbType.VarChar, 30).Value = String.Join(",", item.Days.OrderBy(Function(x) x))
                cmd.Parameters.Add("@Actor", SqlDbType.NVarChar, 100).Value = Environment.UserName

                conn.Open()
                Return Convert.ToInt32(cmd.ExecuteScalar())
            End Using
        End Using
    End Function

    Public Sub DeleteSchedule(scheduleId As Integer)
        Using conn As New SqlConnection(_connectionString)
            Using cmd As New SqlCommand("dbo.sp_AS_DeleteSchedule", conn)
                cmd.CommandType = CommandType.StoredProcedure
                cmd.Parameters.Add("@ScheduleId", SqlDbType.Int).Value = scheduleId
                cmd.Parameters.Add("@Actor", SqlDbType.NVarChar, 100).Value = Environment.UserName
                conn.Open()
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    Public Function SaveHoliday(item As HolidayInfo) As Integer
        Using conn As New SqlConnection(_connectionString)
            Using cmd As New SqlCommand("dbo.sp_AS_UpsertHoliday", conn)
                cmd.CommandType = CommandType.StoredProcedure
                cmd.Parameters.Add("@HolidayId", SqlDbType.Int).Value = If(item.HolidayId > 0, CType(item.HolidayId, Object), DBNull.Value)
                cmd.Parameters.Add("@HolidayDate", SqlDbType.Date).Value = item.HolidayDate.Date
                cmd.Parameters.Add("@HolidayName", SqlDbType.NVarChar, 150).Value = item.HolidayName
                cmd.Parameters.Add("@Actor", SqlDbType.NVarChar, 100).Value = Environment.UserName
                conn.Open()
                Return Convert.ToInt32(cmd.ExecuteScalar())
            End Using
        End Using
    End Function

    Public Sub DeleteHoliday(holidayId As Integer)
        Using conn As New SqlConnection(_connectionString)
            Using cmd As New SqlCommand("dbo.sp_AS_DeleteHoliday", conn)
                cmd.CommandType = CommandType.StoredProcedure
                cmd.Parameters.Add("@HolidayId", SqlDbType.Int).Value = holidayId
                conn.Open()
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    Public Sub LogAlarm(item As PendingAlarmLog)
        Using conn As New SqlConnection(_connectionString)
            Using cmd As New SqlCommand("dbo.sp_AS_LogAlarm", conn)
                cmd.CommandType = CommandType.StoredProcedure
                cmd.CommandTimeout = 5
                cmd.Parameters.Add("@ScheduleId", SqlDbType.Int).Value = If(item.ScheduleId > 0, CType(item.ScheduleId, Object), DBNull.Value)
                cmd.Parameters.Add("@ScheduledAt", SqlDbType.DateTime2).Value = If(item.ScheduledAt.HasValue, CType(item.ScheduledAt.Value, Object), DBNull.Value)
                cmd.Parameters.Add("@TriggeredAt", SqlDbType.DateTime2).Value = item.TriggeredAt
                cmd.Parameters.Add("@Status", SqlDbType.VarChar, 20).Value = item.Status
                cmd.Parameters.Add("@AudioFile", SqlDbType.NVarChar, 500).Value = If(String.IsNullOrWhiteSpace(item.AudioFile), CType(DBNull.Value, Object), item.AudioFile)
                cmd.Parameters.Add("@MachineName", SqlDbType.NVarChar, 100).Value = Environment.MachineName
                cmd.Parameters.Add("@WindowsUser", SqlDbType.NVarChar, 100).Value = Environment.UserName
                cmd.Parameters.Add("@Message", SqlDbType.NVarChar, 500).Value = If(String.IsNullOrWhiteSpace(item.Message), CType(DBNull.Value, Object), item.Message)
                conn.Open()
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    Public Function FlushPendingLogs() As Integer
        Dim pending = LocalCacheStore.LoadPendingLogs()
        If pending.Count = 0 Then Return 0

        Dim remaining As New List(Of PendingAlarmLog)()
        Dim sent As Integer = 0

        For Each item In pending
            Try
                LogAlarm(item)
                sent += 1
            Catch
                remaining.Add(item)
            End Try
        Next

        LocalCacheStore.SavePendingLogs(remaining)
        Return sent
    End Function
End Class
