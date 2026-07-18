Option Strict On
Option Explicit On

Imports System.Net
Imports System.Net.Sockets
Imports System.IO
Imports System.Text
Imports System.Threading
Imports System.Windows.Forms
Imports System.Collections.Generic

' NetworkHub: dung o phia HOST khi choi 3-4 nguoi.
' Host la player slot 0 luon. Hub chap nhan toi da 3 khach (slot 1,2,3)
' va relay du lieu qua lai. Client (khach) van dung NetworkPeer nhu cu,
' chi ket noi voi Host - khong ket noi truc tiep voi nhau.
Public Class NetworkHub

    Private Class ClientSlot
        Public SlotIndex As Integer
        Public Client As TcpClient
        Public Stream As NetworkStream
        Public Reader As StreamReader
        Public Writer As StreamWriter
        Public ReadThread As Thread
    End Class

    ' Su kien: (slotIndex, line) khi nhan duoc du lieu tu 1 khach
    Public Event ClientLineReceived(slotIndex As Integer, line As String)
    Public Event ClientConnected(slotIndex As Integer)
    Public Event ClientDisconnected(slotIndex As Integer)

    Private listener As TcpListener
    Private acceptThread As Thread
    Private uiControl As Control
    Private isListening As Boolean = False
    Private Const MAX_GUESTS As Integer = 3   ' toi da 3 khach + 1 host = 4 nguoi

    Private slots As New List(Of ClientSlot)()
    Private slotsLock As New Object()

    Public Sub New(uiOwner As Control)
        uiControl = uiOwner
    End Sub

    Public ReadOnly Property ConnectedCount As Integer
        Get
            SyncLock slotsLock
                Return slots.Count
            End SyncLock
        End Get
    End Property

    Public Sub StartListening(port As Integer)
        listener = New TcpListener(IPAddress.Any, port)
        listener.Start()
        isListening = True
        acceptThread = New Thread(New ThreadStart(AddressOf AcceptLoop))
        acceptThread.IsBackground = True
        acceptThread.Start()
    End Sub

    Private Sub AcceptLoop()
        Try
            Do While isListening
                SyncLock slotsLock
                    If slots.Count >= MAX_GUESTS Then
                        ' Da du 4 nguoi (host + 3 khach), khong nhan them.
                        ' Van phai Accept de tranh backlog treo, roi dong ngay ket noi thua.
                    End If
                End SyncLock

                Dim tc As TcpClient = listener.AcceptTcpClient()

                Dim full As Boolean
                SyncLock slotsLock
                    full = slots.Count >= MAX_GUESTS
                End SyncLock

                If full Then
                    Try
                        tc.Close()
                    Catch
                    End Try
                    Continue Do
                End If

                Dim newSlot As New ClientSlot()
                SyncLock slotsLock
                    ' Slot 0 la host, nen khach dau tien la slot 1, tiep la 2, 3
                    newSlot.SlotIndex = slots.Count + 1
                    newSlot.Client = tc
                    newSlot.Stream = tc.GetStream()
                    newSlot.Reader = New StreamReader(newSlot.Stream, Encoding.UTF8)
                    newSlot.Writer = New StreamWriter(newSlot.Stream, Encoding.UTF8)
                    newSlot.Writer.AutoFlush = True
                    slots.Add(newSlot)
                End SyncLock

                newSlot.ReadThread = New Thread(Sub() ClientReadLoop(newSlot))
                newSlot.ReadThread.IsBackground = True
                newSlot.ReadThread.Start()

                RaiseClientConnected(newSlot.SlotIndex)
            Loop
        Catch ex As Exception
            ' Listener bi dong (StopListening) hoac loi mang - thoat vong lap binh thuong
        End Try
    End Sub

    Private Sub ClientReadLoop(slot As ClientSlot)
        Try
            Do While isListening
                Dim line As String = slot.Reader.ReadLine()
                If line Is Nothing Then Exit Do
                RaiseClientLine(slot.SlotIndex, line)
            Loop
        Catch ex As Exception
            ' Mat ket noi
        End Try

        SyncLock slotsLock
            slots.Remove(slot)
        End SyncLock
        RaiseClientDisconnected(slot.SlotIndex)
    End Sub

    ' Gui 1 dong du lieu den TAT CA khach dang ket noi (khong gom host, vi host tu xu ly local)
    Public Sub Broadcast(line As String)
        Dim snapshot As List(Of ClientSlot)
        SyncLock slotsLock
            snapshot = New List(Of ClientSlot)(slots)
        End SyncLock
        Dim s As ClientSlot
        For Each s In snapshot
            SendToSlotInternal(s, line)
        Next s
    End Sub

    ' Gui den 1 khach cu the theo slotIndex (1,2,3)
    Public Sub SendTo(slotIndex As Integer, line As String)
        Dim target As ClientSlot = Nothing
        SyncLock slotsLock
            Dim s As ClientSlot
            For Each s In slots
                If s.SlotIndex = slotIndex Then target = s : Exit For
            Next s
        End SyncLock
        If target IsNot Nothing Then SendToSlotInternal(target, line)
    End Sub

    Private Sub SendToSlotInternal(slot As ClientSlot, line As String)
        Try
            slot.Writer.WriteLine(line)
        Catch ex As Exception
            ' Bo qua, ClientReadLoop se phat hien mat ket noi va don dep
        End Try
    End Sub

    ' Gui den tat ca khach TRU 1 slot (dung khi relay chat, tranh nhan lai tin nhan cua chinh minh)
    Public Sub BroadcastExcept(excludeSlotIndex As Integer, line As String)
        Dim snapshot As List(Of ClientSlot)
        SyncLock slotsLock
            snapshot = New List(Of ClientSlot)(slots)
        End SyncLock
        Dim s As ClientSlot
        For Each s In snapshot
            If s.SlotIndex <> excludeSlotIndex Then SendToSlotInternal(s, line)
        Next s
    End Sub

    Public Sub StopListening()
        isListening = False
        Try
            If listener IsNot Nothing Then listener.Stop()
        Catch ex As Exception
        End Try
        SyncLock slotsLock
            Dim s As ClientSlot
            For Each s In slots
                Try
                    s.Client.Close()
                Catch ex As Exception
                End Try
            Next s
            slots.Clear()
        End SyncLock
    End Sub

    Private Sub RaiseClientLine(slotIndex As Integer, line As String)
        If uiControl.InvokeRequired Then
            uiControl.Invoke(Sub() RaiseEvent ClientLineReceived(slotIndex, line))
        Else
            RaiseEvent ClientLineReceived(slotIndex, line)
        End If
    End Sub

    Private Sub RaiseClientConnected(slotIndex As Integer)
        If uiControl.InvokeRequired Then
            uiControl.Invoke(Sub() RaiseEvent ClientConnected(slotIndex))
        Else
            RaiseEvent ClientConnected(slotIndex)
        End If
    End Sub

    Private Sub RaiseClientDisconnected(slotIndex As Integer)
        If uiControl.InvokeRequired Then
            uiControl.BeginInvoke(New Action(Sub() RaiseEvent ClientDisconnected(slotIndex)))
        Else
            RaiseEvent ClientDisconnected(slotIndex)
        End If
    End Sub

End Class
