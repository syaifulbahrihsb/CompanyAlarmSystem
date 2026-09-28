Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms

Public Class HolidayForm
    Inherits Form

    Private ReadOnly repository As SqlAlarmRepository
    Private holidays As List(Of HolidayInfo)
    Private ReadOnly grid As New DataGridView()
    Private ReadOnly datePicker As New DateTimePicker()
    Private ReadOnly txtName As New TextBox()
    Private ReadOnly btnSave As New Button()
    Private ReadOnly btnDelete As New Button()
    Private ReadOnly btnClose As New Button()

    Public Sub New(repo As SqlAlarmRepository, source As List(Of HolidayInfo))
        repository = repo
        holidays = New List(Of HolidayInfo)(source)
        BuildUi()
        RefreshGrid()
    End Sub

    Private Sub BuildUi()
        Text = "Hari Libur Alarm System"
        Me.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        StartPosition = FormStartPosition.CenterParent
        Size = New Size(720, 500)
        Font = New Font("Segoe UI", 9.5F)
        BackColor = Color.White

        Dim root As New TableLayoutPanel With {.Dock = DockStyle.Fill, .Padding = New Padding(16), .ColumnCount = 1, .RowCount = 3}
        root.RowStyles.Add(New RowStyle(SizeType.Absolute, 88))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        root.RowStyles.Add(New RowStyle(SizeType.Absolute, 52))

        Dim editor As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 4, .RowCount = 2}
        editor.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 90))
        editor.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 170))
        editor.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 90))
        editor.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        editor.Controls.Add(New Label With {.Text = "Tanggal", .Anchor = AnchorStyles.Left, .AutoSize = True}, 0, 0)
        datePicker.Format = DateTimePickerFormat.Custom
        datePicker.CustomFormat = "dd MMM yyyy"
        datePicker.Dock = DockStyle.Fill
        editor.Controls.Add(datePicker, 1, 0)
        editor.Controls.Add(New Label With {.Text = "Keterangan", .Anchor = AnchorStyles.Left, .AutoSize = True}, 2, 0)
        txtName.Dock = DockStyle.Fill
        editor.Controls.Add(txtName, 3, 0)

        Dim buttonsTop As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.LeftToRight}
        btnSave.Text = "Tambah / Update"
        btnSave.Width = 130
        btnDelete.Text = "Nonaktifkan"
        btnDelete.Width = 110
        AddHandler btnSave.Click, AddressOf SaveHoliday
        AddHandler btnDelete.Click, AddressOf DeleteHoliday
        buttonsTop.Controls.Add(btnSave)
        buttonsTop.Controls.Add(btnDelete)
        editor.Controls.Add(buttonsTop, 1, 1)
        editor.SetColumnSpan(buttonsTop, 3)
        root.Controls.Add(editor, 0, 0)

        grid.Dock = DockStyle.Fill
        grid.ReadOnly = True
        grid.AllowUserToAddRows = False
        grid.AllowUserToDeleteRows = False
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        grid.MultiSelect = False
        grid.RowHeadersVisible = False
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        AddHandler grid.SelectionChanged, AddressOf GridSelectionChanged
        root.Controls.Add(grid, 0, 1)

        Dim bottom As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.RightToLeft}
        btnClose.Text = "Tutup"
        btnClose.Width = 100
        AddHandler btnClose.Click, Sub() Close()
        bottom.Controls.Add(btnClose)
        root.Controls.Add(bottom, 0, 2)

        Controls.Add(root)
    End Sub

    Private Sub RefreshGrid()
        grid.DataSource = Nothing
        grid.DataSource = holidays.OrderBy(Function(x) x.HolidayDate).Select(Function(x) New With {
            .HolidayId = x.HolidayId,
            .Tanggal = x.HolidayDate.ToString("dd-MM-yyyy"),
            .Keterangan = x.HolidayName
        }).ToList()
        If grid.Columns.Contains("HolidayId") Then grid.Columns("HolidayId").Visible = False
    End Sub

    Private Function SelectedHoliday() As HolidayInfo
        If grid.CurrentRow Is Nothing OrElse Not grid.Columns.Contains("HolidayId") Then Return Nothing
        Dim id = Convert.ToInt32(grid.CurrentRow.Cells("HolidayId").Value)
        Return holidays.FirstOrDefault(Function(x) x.HolidayId = id)
    End Function

    Private Sub GridSelectionChanged(sender As Object, e As EventArgs)
        Dim item = SelectedHoliday()
        If item Is Nothing Then Return
        datePicker.Value = item.HolidayDate
        txtName.Text = item.HolidayName
    End Sub

    Private Sub SaveHoliday(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(txtName.Text) Then
            MessageBox.Show(Me, "Keterangan hari libur belum diisi.", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim selected = SelectedHoliday()
        Dim item As New HolidayInfo With {
            .HolidayId = If(selected Is Nothing, 0, selected.HolidayId),
            .HolidayDate = datePicker.Value.Date,
            .HolidayName = txtName.Text.Trim(),
            .IsActive = True
        }

        Try
            repository.SaveHoliday(item)
            ReloadFromServer()
            txtName.Clear()
        Catch ex As Exception
            MessageBox.Show(Me, ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub DeleteHoliday(sender As Object, e As EventArgs)
        Dim item = SelectedHoliday()
        If item Is Nothing Then Return
        If MessageBox.Show(Me, "Nonaktifkan hari libur '" & item.HolidayName & "'?", "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return

        Try
            repository.DeleteHoliday(item.HolidayId)
            ReloadFromServer()
        Catch ex As Exception
            MessageBox.Show(Me, ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ReloadFromServer()
        holidays = repository.LoadRuntimeData().Holidays
        RefreshGrid()
    End Sub
End Class
