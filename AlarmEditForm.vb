Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms

Public Class AlarmEditForm
    Inherits Form

    Private ReadOnly txtCode As New TextBox()
    Private ReadOnly txtName As New TextBox()
    Private ReadOnly daysList As New CheckedListBox()
    Private ReadOnly timePicker As New DateTimePicker()
    Private ReadOnly txtSound As New TextBox()
    Private ReadOnly numRepeat As New NumericUpDown()
    Private ReadOnly numInterval As New NumericUpDown()
    Private ReadOnly chkSkipHoliday As New CheckBox()
    Private ReadOnly chkEnabled As New CheckBox()
    Private ReadOnly btnBrowse As New Button()
    Private ReadOnly btnOk As New Button()
    Private ReadOnly btnCancel As New Button()
    Private ReadOnly workingItem As AlarmSchedule

    Public ReadOnly Property ResultSchedule As AlarmSchedule
        Get
            Return workingItem
        End Get
    End Property

    Public Sub New(item As AlarmSchedule)
        workingItem = item.CloneSchedule()
        BuildUi()
        LoadItem()
    End Sub

    Private Sub BuildUi()
        Text = "Tambah / Edit Jadwal Alarm"
        Me.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        StartPosition = FormStartPosition.CenterParent
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        ClientSize = New Size(610, 515)
        Font = New Font("Segoe UI", 9.5F)
        BackColor = Color.White

        Dim table As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .Padding = New Padding(18),
            .ColumnCount = 3,
            .RowCount = 11
        }
        table.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 145))
        table.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        table.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 95))

        AddLabel(table, "Kode Jadwal", 0)
        txtCode.Dock = DockStyle.Fill
        table.Controls.Add(txtCode, 1, 0)
        table.SetColumnSpan(txtCode, 2)

        AddLabel(table, "Nama / Keterangan", 1)
        txtName.Dock = DockStyle.Fill
        table.Controls.Add(txtName, 1, 1)
        table.SetColumnSpan(txtName, 2)

        AddLabel(table, "Hari", 2)
        daysList.Items.AddRange(New Object() {"Senin", "Selasa", "Rabu", "Kamis", "Jumat", "Sabtu", "Minggu"})
        daysList.CheckOnClick = True
        daysList.Height = 105
        daysList.Dock = DockStyle.Fill
        table.Controls.Add(daysList, 1, 2)
        table.SetColumnSpan(daysList, 2)

        AddLabel(table, "Jam", 3)
        timePicker.Format = DateTimePickerFormat.Custom
        timePicker.CustomFormat = "HH:mm"
        timePicker.ShowUpDown = True
        timePicker.Width = 120
        table.Controls.Add(timePicker, 1, 3)

        AddLabel(table, "File WAV", 4)
        txtSound.Dock = DockStyle.Fill
        table.Controls.Add(txtSound, 1, 4)
        btnBrowse.Text = "Browse..."
        btnBrowse.Dock = DockStyle.Fill
        AddHandler btnBrowse.Click, AddressOf BrowseSound
        table.Controls.Add(btnBrowse, 2, 4)

        AddLabel(table, "Jumlah Bunyi", 5)
        numRepeat.Minimum = 1
        numRepeat.Maximum = 10
        numRepeat.Width = 90
        table.Controls.Add(numRepeat, 1, 5)

        AddLabel(table, "Interval Ulang (detik)", 6)
        numInterval.Minimum = 1
        numInterval.Maximum = 300
        numInterval.Width = 90
        table.Controls.Add(numInterval, 1, 6)

        chkSkipHoliday.Text = "Lewati alarm pada hari libur"
        chkSkipHoliday.AutoSize = True
        table.Controls.Add(chkSkipHoliday, 1, 7)
        table.SetColumnSpan(chkSkipHoliday, 2)

        chkEnabled.Text = "Jadwal aktif"
        chkEnabled.AutoSize = True
        table.Controls.Add(chkEnabled, 1, 8)
        table.SetColumnSpan(chkEnabled, 2)

        Dim info As New Label With {
            .Text = "File relatif seperti Sounds\bell_masuk.wav dicari dari folder aplikasi. File absolut juga didukung pada PC alarm.",
            .ForeColor = Color.DimGray,
            .AutoSize = True,
            .MaximumSize = New Size(400, 0)
        }
        table.Controls.Add(info, 1, 9)
        table.SetColumnSpan(info, 2)

        Dim buttons As New FlowLayoutPanel With {.FlowDirection = FlowDirection.RightToLeft, .Dock = DockStyle.Fill}
        btnOk.Text = "Simpan"
        btnOk.Width = 95
        btnOk.BackColor = Color.FromArgb(22, 163, 74)
        btnOk.ForeColor = Color.White
        btnOk.FlatStyle = FlatStyle.Flat
        btnOk.FlatAppearance.BorderSize = 0
        AddHandler btnOk.Click, AddressOf SaveAndClose

        btnCancel.Text = "Batal"
        btnCancel.Width = 95
        btnCancel.DialogResult = DialogResult.Cancel

        buttons.Controls.Add(btnOk)
        buttons.Controls.Add(btnCancel)
        table.Controls.Add(buttons, 1, 10)
        table.SetColumnSpan(buttons, 2)

        Controls.Add(table)
        AcceptButton = btnOk
        CancelButton = btnCancel
    End Sub

    Private Sub AddLabel(table As TableLayoutPanel, textValue As String, row As Integer)
        table.Controls.Add(New Label With {.Text = textValue, .AutoSize = True, .Anchor = AnchorStyles.Left}, 0, row)
    End Sub

    Private Sub LoadItem()
        txtCode.Text = workingItem.ScheduleCode
        txtName.Text = workingItem.ScheduleName

        For i As Integer = 0 To daysList.Items.Count - 1
            daysList.SetItemChecked(i, workingItem.Days IsNot Nothing AndAlso workingItem.Days.Contains(i + 1))
        Next

        Dim parsed As DateTime
        If DateTime.TryParseExact(workingItem.AlarmTime, "HH:mm", Globalization.CultureInfo.InvariantCulture, Globalization.DateTimeStyles.None, parsed) Then
            timePicker.Value = DateTime.Today.Add(parsed.TimeOfDay)
        End If

        txtSound.Text = workingItem.SoundFile
        numRepeat.Value = Math.Max(numRepeat.Minimum, Math.Min(numRepeat.Maximum, workingItem.RepeatCount))
        numInterval.Value = Math.Max(numInterval.Minimum, Math.Min(numInterval.Maximum, workingItem.RepeatIntervalSeconds))
        chkSkipHoliday.Checked = workingItem.SkipHoliday
        chkEnabled.Checked = workingItem.Enabled
    End Sub

    Private Sub BrowseSound(sender As Object, e As EventArgs)
        Using dialog As New OpenFileDialog()
            dialog.Filter = "WAV Audio (*.wav)|*.wav|All files (*.*)|*.*"
            dialog.Title = "Pilih file suara"
            If dialog.ShowDialog(Me) = DialogResult.OK Then txtSound.Text = dialog.FileName
        End Using
    End Sub

    Private Sub SaveAndClose(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(txtCode.Text) Then
            MessageBox.Show(Me, "Kode jadwal belum diisi.", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtCode.Focus()
            Return
        End If

        If String.IsNullOrWhiteSpace(txtName.Text) Then
            MessageBox.Show(Me, "Nama atau keterangan alarm belum diisi.", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtName.Focus()
            Return
        End If

        Dim selectedDays As New List(Of Integer)()
        For i As Integer = 0 To daysList.Items.Count - 1
            If daysList.GetItemChecked(i) Then selectedDays.Add(i + 1)
        Next
        If selectedDays.Count = 0 Then
            MessageBox.Show(Me, "Pilih minimal satu hari.", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If String.IsNullOrWhiteSpace(txtSound.Text) Then
            MessageBox.Show(Me, "File suara belum dipilih.", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtSound.Focus()
            Return
        End If

        workingItem.ScheduleCode = txtCode.Text.Trim().ToUpperInvariant()
        workingItem.ScheduleName = txtName.Text.Trim()
        workingItem.Days = selectedDays
        workingItem.AlarmTime = timePicker.Value.ToString("HH:mm")
        workingItem.SoundFile = txtSound.Text.Trim()
        workingItem.RepeatCount = Convert.ToInt32(numRepeat.Value)
        workingItem.RepeatIntervalSeconds = Convert.ToInt32(numInterval.Value)
        workingItem.SkipHoliday = chkSkipHoliday.Checked
        workingItem.Enabled = chkEnabled.Checked

        DialogResult = DialogResult.OK
        Close()
    End Sub
End Class
