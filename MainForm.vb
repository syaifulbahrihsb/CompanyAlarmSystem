Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Threading.Tasks
Imports System.Windows.Forms

Public Class MainForm
    Inherits Form

    Private ReadOnly repository As New SqlAlarmRepository()
    Private ReadOnly schedulerTimer As New Timer()
    Private ReadOnly serverRefreshTimer As New Timer()
    Private ReadOnly audioPlayer As New System.Media.SoundPlayer()
    Private ReadOnly lblClock As New Label()
    Private ReadOnly lblDate As New Label()
    Private ReadOnly lblSystemStatus As New Label()
    Private ReadOnly lblDbStatus As New Label()
    Private ReadOnly lblNextAlarm As New Label()
    Private ReadOnly grid As New DataGridView()
    Private ReadOnly logList As New ListBox()
    Private ReadOnly btnAdd As New Button()
    Private ReadOnly btnEdit As New Button()
    Private ReadOnly btnDelete As New Button()
    Private ReadOnly btnTest As New Button()
    Private ReadOnly btnStop As New Button()
    Private ReadOnly btnHoliday As New Button()
    Private ReadOnly btnRefresh As New Button()

    Private schedules As New List(Of AlarmSchedule)()
    Private holidays As New List(Of HolidayInfo)()
    Private ReadOnly triggeredKeys As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
    Private isDbConnected As Boolean = False
    Private serverRefreshBusy As Boolean = False
    Private playbackCts As System.Threading.CancellationTokenSource

    Public Sub New()
        BuildUi()
        AddHandler Load, AddressOf MainForm_Load
        AddHandler FormClosing, AddressOf MainForm_FormClosing
    End Sub

    Private Sub BuildUi()
        Text = "Company Alarm System V2 - SMB-2"
        Me.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        StartPosition = FormStartPosition.CenterScreen
        MinimumSize = New Size(1100, 680)
        Size = New Size(1260, 790)
        Font = New Font("Segoe UI", 9.5F)
        BackColor = Color.FromArgb(248, 250, 252)

        Dim root As New TableLayoutPanel With {.Dock = DockStyle.Fill, .Padding = New Padding(20), .ColumnCount = 1, .RowCount = 4}
        root.RowStyles.Add(New RowStyle(SizeType.Absolute, 145))
        root.RowStyles.Add(New RowStyle(SizeType.Absolute, 66))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 70))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 30))
        root.Controls.Add(BuildHeader(), 0, 0)
        root.Controls.Add(BuildToolbar(), 0, 1)
        root.Controls.Add(BuildScheduleCard(), 0, 2)
        root.Controls.Add(BuildLogCard(), 0, 3)
        Controls.Add(root)
    End Sub

    Private Function BuildHeader() As Control
        Dim panel As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.White,
            .Padding = New Padding(18),
            .ColumnCount = 4,
            .RowCount = 2,
            .Margin = New Padding(0, 0, 0, 12)
        }
        panel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 29))
        panel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 22))
        panel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 22))
        panel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 27))
        panel.RowStyles.Add(New RowStyle(SizeType.Percent, 65))
        panel.RowStyles.Add(New RowStyle(SizeType.Percent, 35))

        lblClock.Text = "00:00:00"
        lblClock.Font = New Font("Segoe UI Semibold", 31.0F, FontStyle.Bold)
        lblClock.ForeColor = Color.FromArgb(15, 23, 42)
        lblClock.Dock = DockStyle.Fill
        lblClock.TextAlign = ContentAlignment.MiddleLeft
        panel.Controls.Add(lblClock, 0, 0)

        lblSystemStatus.Text = "● SYSTEM ACTIVE"
        lblSystemStatus.Font = New Font("Segoe UI Semibold", 12.0F, FontStyle.Bold)
        lblSystemStatus.ForeColor = Color.FromArgb(22, 163, 74)
        lblSystemStatus.Dock = DockStyle.Fill
        lblSystemStatus.TextAlign = ContentAlignment.MiddleCenter
        panel.Controls.Add(lblSystemStatus, 1, 0)

        lblDbStatus.Text = "● DATABASE CHECKING"
        lblDbStatus.Font = New Font("Segoe UI Semibold", 11.0F, FontStyle.Bold)
        lblDbStatus.ForeColor = Color.FromArgb(100, 116, 139)
        lblDbStatus.Dock = DockStyle.Fill
        lblDbStatus.TextAlign = ContentAlignment.MiddleCenter
        panel.Controls.Add(lblDbStatus, 2, 0)

        lblNextAlarm.Text = "Next Bell: -"
        lblNextAlarm.Font = New Font("Segoe UI Semibold", 11.0F, FontStyle.Bold)
        lblNextAlarm.ForeColor = Color.FromArgb(37, 99, 235)
        lblNextAlarm.Dock = DockStyle.Fill
        lblNextAlarm.TextAlign = ContentAlignment.MiddleRight
        panel.Controls.Add(lblNextAlarm, 3, 0)

        lblDate.Text = "-"
        lblDate.ForeColor = Color.DimGray
        lblDate.Dock = DockStyle.Fill
        lblDate.TextAlign = ContentAlignment.TopLeft
        panel.Controls.Add(lblDate, 0, 1)

        Dim info As New Label With {
            .Text = "SQL Server SMB-2 • 10.8.159.24:1435 • Cache Fail-safe • Speaker PC",
            .ForeColor = Color.DimGray,
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.TopRight
        }
        panel.Controls.Add(info, 1, 1)
        panel.SetColumnSpan(info, 3)
        Return panel
    End Function

    Private Function BuildToolbar() As Control
        Dim panel As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.LeftToRight, .WrapContents = False, .Padding = New Padding(0, 8, 0, 8)}

        ConfigureButton(btnAdd, "+ Tambah", Color.FromArgb(22, 163, 74), Color.White)
        ConfigureButton(btnEdit, "Edit", Color.FromArgb(37, 99, 235), Color.White)
        ConfigureButton(btnDelete, "Hapus", Color.FromArgb(220, 38, 38), Color.White)
        ConfigureButton(btnTest, "Test Bell", Color.FromArgb(15, 23, 42), Color.White)
        ConfigureButton(btnStop, "Stop Bell", Color.FromArgb(100, 116, 139), Color.White)
        ConfigureButton(btnHoliday, "Hari Libur", Color.FromArgb(124, 58, 237), Color.White)
        ConfigureButton(btnRefresh, "Refresh DB", Color.White, Color.FromArgb(15, 23, 42))

        AddHandler btnAdd.Click, AddressOf AddSchedule
        AddHandler btnEdit.Click, AddressOf EditSchedule
        AddHandler btnDelete.Click, AddressOf DeleteSchedule
        AddHandler btnTest.Click, AddressOf TestSelected
        AddHandler btnStop.Click, AddressOf StopBell
        AddHandler btnHoliday.Click, AddressOf OpenHoliday
        AddHandler btnRefresh.Click, AddressOf RefreshData

        panel.Controls.AddRange(New Control() {btnAdd, btnEdit, btnDelete, btnTest, btnStop, btnHoliday, btnRefresh})
        Return panel
    End Function

    Private Sub ConfigureButton(button As Button, textValue As String, back As Color, fore As Color)
        button.Text = textValue
        button.AutoSize = False
        button.Width = 112
        button.Height = 38
        button.BackColor = back
        button.ForeColor = fore
        button.FlatStyle = FlatStyle.Flat
        button.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225)
        button.Margin = New Padding(0, 0, 9, 0)
        button.Cursor = Cursors.Hand
    End Sub

    Private Function BuildScheduleCard() As Control
        Dim card As New Panel With {.Dock = DockStyle.Fill, .BackColor = Color.White, .Padding = New Padding(12), .Margin = New Padding(0, 0, 0, 12)}
        grid.Dock = DockStyle.Fill
        grid.AllowUserToAddRows = False
        grid.AllowUserToDeleteRows = False
        grid.AllowUserToResizeRows = False
        grid.ReadOnly = True
        grid.MultiSelect = False
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        grid.RowHeadersVisible = False
        grid.BackgroundColor = Color.White
        grid.BorderStyle = BorderStyle.None
        grid.EnableHeadersVisualStyles = False
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249)
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42)
        grid.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.5F, FontStyle.Bold)
        grid.ColumnHeadersHeight = 38
        grid.RowTemplate.Height = 34
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254)
        grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42)
        AddHandler grid.CellDoubleClick, AddressOf GridDoubleClick
        card.Controls.Add(grid)
        Return card
    End Function

    Private Function BuildLogCard() As Control
        Dim card As New Panel With {.Dock = DockStyle.Fill, .BackColor = Color.White, .Padding = New Padding(12)}
        Dim title As New Label With {.Text = "ACTIVITY LOG", .Dock = DockStyle.Top, .Height = 28, .Font = New Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), .ForeColor = Color.FromArgb(51, 65, 85)}
        logList.Dock = DockStyle.Fill
        logList.BorderStyle = BorderStyle.None
        logList.Font = New Font("Consolas", 9.0F)
        card.Controls.Add(logList)
        card.Controls.Add(title)
        Return card
    End Function

    Private Sub MainForm_Load(sender As Object, e As EventArgs)
        LoadDataFromServerOrCache()

        schedulerTimer.Interval = 1000
        AddHandler schedulerTimer.Tick, AddressOf SchedulerTick
        schedulerTimer.Start()

        serverRefreshTimer.Interval = 60000
        AddHandler serverRefreshTimer.Tick, AddressOf ServerRefreshTick
        serverRefreshTimer.Start()

        UpdateClockAndNext()
    End Sub

    Private Sub MainForm_FormClosing(sender As Object, e As FormClosingEventArgs)
        Try
            schedulerTimer.Stop()
            serverRefreshTimer.Stop()
            If playbackCts IsNot Nothing Then playbackCts.Cancel()
            audioPlayer.Stop()
        Catch
        End Try
    End Sub

    Private Sub LoadDataFromServerOrCache()
        Try
            Dim data = repository.LoadRuntimeData()
            schedules = data.Schedules
            holidays = data.Holidays
            LocalCacheStore.SaveRuntime(data)
            SetDatabaseStatus(True, "DATABASE ONLINE")
            Dim flushed = repository.FlushPendingLogs()
            LogUi("DB ONLINE | Runtime data loaded" & If(flushed > 0, " | pending log synced: " & flushed.ToString(), ""))
        Catch ex As Exception
            Dim data = LocalCacheStore.LoadRuntime()
            schedules = data.Schedules
            holidays = data.Holidays
            SetDatabaseStatus(False, "DATABASE OFFLINE")
            LogUi("DB OFFLINE | cache digunakan | " & ex.Message)
            LocalCacheStore.AppendTextLog("DB OFFLINE | " & ex.Message)
        End Try

        RefreshGrid()
    End Sub

    Private Async Sub ServerRefreshTick(sender As Object, e As EventArgs)
        If serverRefreshBusy Then Return
        serverRefreshBusy = True
        Try
            Dim data = Await Task.Run(Function() repository.LoadRuntimeData())
            schedules = data.Schedules
            holidays = data.Holidays
            LocalCacheStore.SaveRuntime(data)
            SetDatabaseStatus(True, "DATABASE ONLINE")
            RefreshGrid()
            Await Task.Run(Function() repository.FlushPendingLogs())
        Catch
            SetDatabaseStatus(False, "DATABASE OFFLINE")
        Finally
            serverRefreshBusy = False
        End Try
    End Sub

    Private Sub SetDatabaseStatus(connected As Boolean, textValue As String)
        isDbConnected = connected
        lblDbStatus.Text = "● " & textValue
        lblDbStatus.ForeColor = If(connected, Color.FromArgb(22, 163, 74), Color.FromArgb(220, 38, 38))
        btnAdd.Enabled = connected
        btnEdit.Enabled = connected
        btnDelete.Enabled = connected
        btnHoliday.Enabled = connected
    End Sub

    Private Sub SchedulerTick(sender As Object, e As EventArgs)
        UpdateClockAndNext()
        CheckSchedules(DateTime.Now)
        If triggeredKeys.Count > 500 Then triggeredKeys.Clear()
    End Sub

    Private Sub UpdateClockAndNext()
        Dim now = DateTime.Now
        lblClock.Text = now.ToString("HH:mm:ss")
        lblDate.Text = ScheduleHelpers.DayName(ScheduleHelpers.ToDayNo(now.DayOfWeek)) & ", " & now.ToString("dd MMMM yyyy", New CultureInfo("id-ID"))

        Dim nextInfo = FindNextAlarm(now)
        If nextInfo Is Nothing Then
            lblNextAlarm.Text = "Next Bell: -"
        Else
            Dim span = nextInfo.Occurrence - now
            Dim countdown As String
            If span.TotalDays >= 1 Then
                countdown = CInt(Math.Floor(span.TotalDays)).ToString() & " hari " & span.Hours.ToString("00") & " jam"
            Else
                countdown = Math.Max(0, CInt(Math.Floor(span.TotalHours))).ToString("00") & ":" & span.Minutes.ToString("00") & ":" & span.Seconds.ToString("00")
            End If
            lblNextAlarm.Text = "Next: " & nextInfo.Occurrence.ToString("HH:mm") & " • " & nextInfo.Schedule.ScheduleName & " • " & countdown
        End If
    End Sub

    Private Sub CheckSchedules(now As DateTime)
        Dim currentTime = now.ToString("HH:mm")
        Dim currentDay = ScheduleHelpers.ToDayNo(now.DayOfWeek)

        For Each item In schedules
            If Not item.Enabled Then Continue For
            If item.Days Is Nothing OrElse Not item.Days.Contains(currentDay) Then Continue For
            If Not String.Equals(item.AlarmTime, currentTime, StringComparison.Ordinal) Then Continue For

            Dim key = now.ToString("yyyyMMdd") & "|" & item.ScheduleCode & "|" & item.AlarmTime
            If triggeredKeys.Contains(key) Then Continue For
            triggeredKeys.Add(key)

            If item.SkipHoliday AndAlso IsHoliday(now.Date) Then
                Dim holidayName = GetHolidayName(now.Date)
                LogUi("SKIP | " & item.ScheduleName & " | hari libur: " & holidayName)
                WriteAlarmLog(item, "SKIPPED", now.Date.Add(ParseAlarmTime(item.AlarmTime)), "Hari libur: " & holidayName)
                Continue For
            End If

            PlaySchedule(item, False, now.Date.Add(ParseAlarmTime(item.AlarmTime)))
        Next
    End Sub

    Private Async Sub PlaySchedule(item As AlarmSchedule, isTest As Boolean, scheduledAt As DateTime)
        Dim path = ResolveSoundPath(item.SoundFile)
        If Not File.Exists(path) Then
            SetSystemStatus("AUDIO NOT FOUND", Color.FromArgb(220, 38, 38))
            LogUi("ERROR | File tidak ditemukan: " & path)
            WriteAlarmLog(item, "FAILED", scheduledAt, "Audio tidak ditemukan: " & path)
            Return
        End If

        Try
            If playbackCts IsNot Nothing Then playbackCts.Cancel()
            playbackCts = New System.Threading.CancellationTokenSource()
            Dim token = playbackCts.Token

            audioPlayer.Stop()
            audioPlayer.SoundLocation = path
            audioPlayer.Load()
            SetSystemStatus(If(isTest, "TEST BELL", "BELL ACTIVE"), Color.FromArgb(37, 99, 235))

            Dim repeatTotal = Math.Max(1, item.RepeatCount)
            Dim intervalMs = Math.Max(1, item.RepeatIntervalSeconds) * 1000
            For i As Integer = 1 To repeatTotal
                token.ThrowIfCancellationRequested()
                audioPlayer.Play()
                If i < repeatTotal Then Await Task.Delay(intervalMs, token)
            Next

            Dim status = If(isTest, "TEST", "SUCCESS")
            LogUi(status & " | " & item.AlarmTime & " | " & item.ScheduleName)
            WriteAlarmLog(item, status, scheduledAt, If(isTest, "Manual test bell", "Alarm triggered by scheduler"))
            Await Task.Delay(1500, token)
            SetSystemStatus("SYSTEM ACTIVE", Color.FromArgb(22, 163, 74))
        Catch ex As OperationCanceledException
            SetSystemStatus("SYSTEM ACTIVE", Color.FromArgb(22, 163, 74))
        Catch ex As Exception
            SetSystemStatus("AUDIO ERROR", Color.FromArgb(220, 38, 38))
            LogUi("ERROR | " & ex.Message)
            WriteAlarmLog(item, "FAILED", scheduledAt, ex.Message)
        End Try
    End Sub

    Private Sub WriteAlarmLog(item As AlarmSchedule, status As String, scheduledAt As DateTime, message As String)
        Dim logItem As New PendingAlarmLog With {
            .ScheduleId = item.ScheduleId,
            .ScheduledAt = scheduledAt,
            .TriggeredAt = DateTime.Now,
            .Status = status,
            .AudioFile = item.SoundFile,
            .Message = message
        }

        Try
            If Not isDbConnected Then Throw New InvalidOperationException("Database offline")
            repository.LogAlarm(logItem)
        Catch
            LocalCacheStore.QueuePendingLog(logItem)
        End Try
        LocalCacheStore.AppendTextLog(status & " | " & item.ScheduleName & " | " & message)
    End Sub

    Private Function ResolveSoundPath(value As String) As String
        If String.IsNullOrWhiteSpace(value) Then Return ""
        If Path.IsPathRooted(value) Then Return value
        Return Path.GetFullPath(Path.Combine(Application.StartupPath, value))
    End Function

    Private Sub SetSystemStatus(textValue As String, fore As Color)
        lblSystemStatus.Text = "● " & textValue
        lblSystemStatus.ForeColor = fore
    End Sub

    Private Function IsHoliday(dateValue As DateTime) As Boolean
        Return holidays.Any(Function(x) x.IsActive AndAlso x.HolidayDate.Date = dateValue.Date)
    End Function

    Private Function GetHolidayName(dateValue As DateTime) As String
        Dim item = holidays.FirstOrDefault(Function(x) x.IsActive AndAlso x.HolidayDate.Date = dateValue.Date)
        Return If(item Is Nothing, "Hari Libur", item.HolidayName)
    End Function

    Private Sub AddSchedule(sender As Object, e As EventArgs)
        If Not RequireDatabase() Then Return
        Using editor As New AlarmEditForm(New AlarmSchedule())
            If editor.ShowDialog(Me) = DialogResult.OK Then SaveScheduleToDatabase(editor.ResultSchedule)
        End Using
    End Sub

    Private Sub EditSchedule(sender As Object, e As EventArgs)
        If Not RequireDatabase() Then Return
        Dim selected = GetSelectedSchedule()
        If selected Is Nothing Then
            MessageBox.Show(Me, "Pilih jadwal yang ingin diedit.", "Alarm System", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        Using editor As New AlarmEditForm(selected)
            If editor.ShowDialog(Me) = DialogResult.OK Then SaveScheduleToDatabase(editor.ResultSchedule)
        End Using
    End Sub

    Private Sub SaveScheduleToDatabase(item As AlarmSchedule)
        Try
            repository.SaveSchedule(item)
            LogUi("SAVE | " & item.ScheduleName)
            LoadDataFromServerOrCache()
        Catch ex As Exception
            MessageBox.Show(Me, ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            LogUi("DB ERROR | Save schedule | " & ex.Message)
        End Try
    End Sub

    Private Sub DeleteSchedule(sender As Object, e As EventArgs)
        If Not RequireDatabase() Then Return
        Dim selected = GetSelectedSchedule()
        If selected Is Nothing OrElse selected.ScheduleId <= 0 Then Return
        If MessageBox.Show(Me, "Hapus jadwal '" & selected.ScheduleName & "'?", "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return

        Try
            repository.DeleteSchedule(selected.ScheduleId)
            LogUi("DELETE | " & selected.ScheduleName)
            LoadDataFromServerOrCache()
        Catch ex As Exception
            MessageBox.Show(Me, ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub TestSelected(sender As Object, e As EventArgs)
        Dim selected = GetSelectedSchedule()
        If selected Is Nothing Then
            MessageBox.Show(Me, "Pilih satu jadwal terlebih dahulu.", "Alarm System", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        PlaySchedule(selected, True, DateTime.Now)
    End Sub

    Private Sub StopBell(sender As Object, e As EventArgs)
        Try
            If playbackCts IsNot Nothing Then playbackCts.Cancel()
            audioPlayer.Stop()
            SetSystemStatus("SYSTEM ACTIVE", Color.FromArgb(22, 163, 74))
            LogUi("STOP | Audio dihentikan manual")
            LocalCacheStore.AppendTextLog("STOP | Audio dihentikan manual")
        Catch ex As Exception
            LogUi("ERROR | " & ex.Message)
        End Try
    End Sub

    Private Sub OpenHoliday(sender As Object, e As EventArgs)
        If Not RequireDatabase() Then Return
        Using form As New HolidayForm(repository, holidays)
            form.ShowDialog(Me)
        End Using
        LoadDataFromServerOrCache()
    End Sub

    Private Sub RefreshData(sender As Object, e As EventArgs)
        LoadDataFromServerOrCache()
    End Sub

    Private Function RequireDatabase() As Boolean
        If isDbConnected Then Return True
        MessageBox.Show(Me, "Database SMB-2 sedang offline. Perubahan data dikunci agar cache lokal tidak konflik. Alarm tetap berjalan dari cache terakhir.", "Database Offline", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        Return False
    End Function

    Private Sub GridDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex >= 0 Then EditSchedule(sender, EventArgs.Empty)
    End Sub

    Private Sub RefreshGrid()
        Dim selectedCode As String = Nothing
        Dim old = GetSelectedSchedule()
        If old IsNot Nothing Then selectedCode = old.ScheduleCode

        grid.DataSource = Nothing
        Dim rows = schedules.OrderBy(Function(x) x.AlarmTime).ThenBy(Function(x) x.ScheduleName).Select(Function(x) New With {
            .ScheduleId = x.ScheduleId,
            .Kode = x.ScheduleCode,
            .Hari = ScheduleHelpers.DaysDisplay(x.Days),
            .Jam = x.AlarmTime,
            .Keterangan = x.ScheduleName,
            .File_Suara = x.SoundFile,
            .Repeat = x.RepeatCount,
            .Libur = If(x.SkipHoliday, "SKIP", "TETAP BUNYI"),
            .Aktif = If(x.Enabled, "YA", "TIDAK")
        }).ToList()
        grid.DataSource = rows

        If grid.Columns.Contains("ScheduleId") Then grid.Columns("ScheduleId").Visible = False
        If grid.Columns.Contains("Kode") Then grid.Columns("Kode").FillWeight = 75
        If grid.Columns.Contains("Hari") Then grid.Columns("Hari").FillWeight = 130
        If grid.Columns.Contains("Jam") Then grid.Columns("Jam").FillWeight = 45
        If grid.Columns.Contains("Keterangan") Then grid.Columns("Keterangan").FillWeight = 100
        If grid.Columns.Contains("File_Suara") Then
            grid.Columns("File_Suara").HeaderText = "File Suara"
            grid.Columns("File_Suara").FillWeight = 130
        End If
        If grid.Columns.Contains("Repeat") Then grid.Columns("Repeat").FillWeight = 45
        If grid.Columns.Contains("Libur") Then grid.Columns("Libur").FillWeight = 65
        If grid.Columns.Contains("Aktif") Then grid.Columns("Aktif").FillWeight = 45

        If selectedCode IsNot Nothing Then
            For Each row As DataGridViewRow In grid.Rows
                If Convert.ToString(row.Cells("Kode").Value) = selectedCode Then
                    row.Selected = True
                    grid.CurrentCell = row.Cells("Kode")
                    Exit For
                End If
            Next
        End If
        UpdateClockAndNext()
    End Sub

    Private Function GetSelectedSchedule() As AlarmSchedule
        If grid.CurrentRow Is Nothing OrElse Not grid.Columns.Contains("Kode") Then Return Nothing
        Dim code = Convert.ToString(grid.CurrentRow.Cells("Kode").Value)
        Return schedules.FirstOrDefault(Function(x) String.Equals(x.ScheduleCode, code, StringComparison.OrdinalIgnoreCase))
    End Function

    Private Sub LogUi(message As String)
        Dim textValue = DateTime.Now.ToString("HH:mm:ss") & "  " & message
        logList.Items.Insert(0, textValue)
        While logList.Items.Count > 250
            logList.Items.RemoveAt(logList.Items.Count - 1)
        End While
    End Sub

    Private Function FindNextAlarm(now As DateTime) As NextAlarmInfo
        Dim candidates As New List(Of NextAlarmInfo)()
        For Each item In schedules.Where(Function(x) x.Enabled)
            Dim alarmTime As TimeSpan
            If Not TryParseAlarmTime(item.AlarmTime, alarmTime) Then Continue For

            For dayOffset As Integer = 0 To 14
                Dim dateCandidate = now.Date.AddDays(dayOffset)
                Dim dayNo = ScheduleHelpers.ToDayNo(dateCandidate.DayOfWeek)
                If item.Days Is Nothing OrElse Not item.Days.Contains(dayNo) Then Continue For
                If item.SkipHoliday AndAlso IsHoliday(dateCandidate) Then Continue For
                Dim occurrence = dateCandidate.Add(alarmTime)
                If occurrence >= now Then
                    candidates.Add(New NextAlarmInfo With {.Schedule = item, .Occurrence = occurrence})
                    Exit For
                End If
            Next
        Next
        Return candidates.OrderBy(Function(x) x.Occurrence).FirstOrDefault()
    End Function

    Private Function ParseAlarmTime(value As String) As TimeSpan
        Dim result As TimeSpan
        If TryParseAlarmTime(value, result) Then Return result
        Return TimeSpan.Zero
    End Function

    Private Function TryParseAlarmTime(value As String, ByRef result As TimeSpan) As Boolean
        Dim parsed As DateTime
        If DateTime.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, parsed) Then
            result = parsed.TimeOfDay
            Return True
        End If
        result = TimeSpan.Zero
        Return False
    End Function

    Private Class NextAlarmInfo
        Public Property Schedule As AlarmSchedule
        Public Property Occurrence As DateTime
    End Class
End Class
