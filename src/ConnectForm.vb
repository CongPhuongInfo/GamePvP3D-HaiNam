Imports System
Imports System.Drawing
Imports System.Net
Imports System.Net.Sockets
Imports System.Windows.Forms

' =====================================================================
'  ConnectForm - man hinh chon che do choi truoc khi vao GamePvP3D:
'  - Solo: choi 1 minh nhu ban test cu, khong dung mang.
'  - Host: mo phong, cho toi da 3 nguoi khac vao (dung NetworkHub).
'  - Join: ket noi vao phong cua nguoi khac qua dia chi IP (NetworkPeer).
' =====================================================================
Public Class ConnectForm
    Inherits Form

    Public ResultMode As String = "solo"   ' "solo", "host", "join"
    Public ResultIp As String = "127.0.0.1"
    Public ResultPort As Integer = 27015
    Public ResultMapIndex As Integer = 0   ' xem MapNames trong GameMaps.vb

    Private rbSolo As New RadioButton()
    Private rbHost As New RadioButton()
    Private rbJoin As New RadioButton()
    Private txtIp As New TextBox()
    Private txtPort As New TextBox()
    Private lblIp As New Label()
    Private lblPort As New Label()
    Private lblLocalIp As New Label()
    Private lblMap As New Label()
    Private cboMap As New ComboBox()
    Private lblMapNote As New Label()
    Private btnStart As New Button()

    Public Sub New()
        Me.Text = "GamePvP 3D - Chon che do choi"
        Me.ClientSize = New Size(360, 340)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterScreen

        Dim titleLbl As New Label()
        titleLbl.Text = "Phieu Luu Hai Nam - Che do choi"
        titleLbl.Font = New Font("Segoe UI", 11, FontStyle.Bold)
        titleLbl.SetBounds(16, 14, 320, 24)
        Me.Controls.Add(titleLbl)

        rbSolo.Text = "Choi 1 minh (Solo)"
        rbSolo.SetBounds(20, 50, 300, 24)
        rbSolo.Checked = True
        AddHandler rbSolo.CheckedChanged, AddressOf ModeChanged
        Me.Controls.Add(rbSolo)

        rbHost.Text = "Tao phong (Host) - cho toi da 3 nguoi vao"
        rbHost.SetBounds(20, 78, 320, 24)
        AddHandler rbHost.CheckedChanged, AddressOf ModeChanged
        Me.Controls.Add(rbHost)

        rbJoin.Text = "Vao phong nguoi khac (Join)"
        rbJoin.SetBounds(20, 106, 320, 24)
        AddHandler rbJoin.CheckedChanged, AddressOf ModeChanged
        Me.Controls.Add(rbJoin)

        lblIp.Text = "Dia chi IP host:"
        lblIp.SetBounds(20, 144, 110, 22)
        Me.Controls.Add(lblIp)

        txtIp.Text = "127.0.0.1"
        txtIp.SetBounds(140, 141, 180, 22)
        txtIp.Enabled = False
        Me.Controls.Add(txtIp)

        lblPort.Text = "Port:"
        lblPort.SetBounds(20, 174, 110, 22)
        Me.Controls.Add(lblPort)

        txtPort.Text = "27015"
        txtPort.SetBounds(140, 171, 100, 22)
        Me.Controls.Add(txtPort)

        ' Danh sach map lay tu GameMaps.vb (MapNames la Shared nen dung duoc tu ConnectForm
        ' du 2 class khac nhau) - moi map moi them vao do se tu dong hien o day, khong can
        ' sua ConnectForm them.
        lblMap.Text = "Map:"
        lblMap.SetBounds(20, 204, 110, 22)
        Me.Controls.Add(lblMap)

        cboMap.DropDownStyle = ComboBoxStyle.DropDownList
        cboMap.SetBounds(140, 201, 180, 22)
        cboMap.Items.AddRange(Form1.MapNames)
        cboMap.SelectedIndex = 0
        Me.Controls.Add(cboMap)

        lblMapNote.Text = ""
        lblMapNote.ForeColor = Color.DimGray
        lblMapNote.SetBounds(20, 226, 320, 18)
        Me.Controls.Add(lblMapNote)

        lblLocalIp.Text = ""
        lblLocalIp.ForeColor = Color.DimGray
        lblLocalIp.SetBounds(20, 246, 320, 40)
        Me.Controls.Add(lblLocalIp)

        btnStart.Text = "Bat dau"
        btnStart.SetBounds(120, 292, 120, 32)
        AddHandler btnStart.Click, AddressOf BtnStart_Click
        Me.Controls.Add(btnStart)
        Me.AcceptButton = btnStart

        ModeChanged(Nothing, EventArgs.Empty)
    End Sub

    Private Sub ModeChanged(sender As Object, e As EventArgs)
        txtIp.Enabled = rbJoin.Checked
        If rbHost.Checked Then
            lblLocalIp.Text = "IP cua may nay (cho ban be nhap khi Join): " & GetLocalIPv4() &
                               Environment.NewLine & "Nho mo port trong Firewall/Router neu choi qua mang LAN/Internet."
        Else
            lblLocalIp.Text = ""
        End If

        ' Ca 2 vai tro deu THAY duoc danh sach map (yeu cau: "ca Host lan Join deu thay danh
        ' sach"), nhung chi Host/Solo la thuc su co hieu luc - Join luon dong bo THEO HOST
        ' ngay khi vao phong (xem WELCOME trong GameHub.vb), lua chon o day chi de xem truoc.
        If rbJoin.Checked Then
            lblMapNote.Text = "(Chi de xem - Host se quyet dinh map thuc te khi ban vao phong)"
        Else
            lblMapNote.Text = "(Se ap dung khi bam Bat dau)"
        End If
    End Sub

    Private Sub BtnStart_Click(sender As Object, e As EventArgs)
        Dim port As Integer
        If Not Integer.TryParse(txtPort.Text.Trim(), port) OrElse port <= 0 OrElse port > 65535 Then
            MessageBox.Show("Port khong hop le (1-65535).", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If rbSolo.Checked Then
            ResultMode = "solo"
        ElseIf rbHost.Checked Then
            ResultMode = "host"
        Else
            ResultMode = "join"
            If String.IsNullOrWhiteSpace(txtIp.Text) Then
                MessageBox.Show("Vui long nhap dia chi IP cua host.", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            ResultIp = txtIp.Text.Trim()
        End If
        ResultPort = port
        ResultMapIndex = Math.Max(0, cboMap.SelectedIndex)

        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Function GetLocalIPv4() As String
        Try
            Dim host As IPHostEntry = Dns.GetHostEntry(Dns.GetHostName())
            For Each addr As IPAddress In host.AddressList
                If addr.AddressFamily = AddressFamily.InterNetwork Then
                    Return addr.ToString()
                End If
            Next
        Catch ex As Exception
        End Try
        Return "(khong xac dinh duoc, kiem tra bang ipconfig)"
    End Function

End Class
