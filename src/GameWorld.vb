Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

' =====================================================================
'  Form1 - World: spawn nam/item/trang tri, vong lap game, nhat do vat
'  (Mot phan cua Form1 - xem README.md muc 'Cau truc code' de biet
'  toan bo cac file partial class va vai tro cua tung file)
' =====================================================================

Partial Public Class Form1

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        gameTimer.Start()
        EngageMouseLook()
    End Sub

    ' ---------------------------------------------------------------
    '  Nap texture tu thu muc Assets\ (cung cap bang, resize san 128x128).
    '  Neu thieu file thi dung mau phang thay the de game khong bi crash.
    ' ---------------------------------------------------------------
    Private Sub SpawnMushrooms(count As Integer)
        Dim placed As Integer = 0
        Dim attempts As Integer = 0
        While placed < count AndAlso attempts < 1000
            attempts += 1
            Dim mx As Integer = rng.Next(1, MAP_W - 1)
            Dim my As Integer = rng.Next(1, MAP_H - 1)
            If mapData(my, mx) = 0 Then
                If Math.Abs(mx + 0.5 - playerX) > 1.5 OrElse Math.Abs(my + 0.5 - playerY) > 1.5 Then
                    mushrooms.Add(New MushroomItem() With {.Id = nextMushroomId, .Pos = New PointF(mx + 0.5F, my + 0.5F)})
                    nextMushroomId += 1
                    placed += 1
                End If
            End If
        End While
    End Sub

    ' Vat pham (dao/kiem/cung/thuoc...) nam ngoai the gioi, loai ngau nhien tu
    ' itemCatalogKeys. Chi Host (hoac Solo) goi ham nay; Client nhan danh sach
    ' qua ITEMSYNC tu Host, khong tu sinh rieng (tranh lech du lieu).
    Private Sub SpawnWorldItems(count As Integer)
        If itemCatalogKeys.Count = 0 Then Return
        Dim placed As Integer = 0
        Dim attempts As Integer = 0
        While placed < count AndAlso attempts < 1000
            attempts += 1
            Dim wx As Integer = rng.Next(1, MAP_W - 1)
            Dim wy As Integer = rng.Next(1, MAP_H - 1)
            If mapData(wy, wx) = 0 Then
                If Math.Abs(wx + 0.5 - playerX) > 1.5 OrElse Math.Abs(wy + 0.5 - playerY) > 1.5 Then
                    Dim pickedItemId As String = itemCatalogKeys(rng.Next(itemCatalogKeys.Count))
                    worldItems.Add(New WorldItemSpawn() With {.Id = nextWorldItemId, .Pos = New PointF(wx + 0.5F, wy + 0.5F), .ItemId = pickedItemId})
                    nextWorldItemId += 1
                    placed += 1
                End If
            End If
        End While
    End Sub

    ' Bui co / cay hoa: chi de trang tri, khong nhat duoc, khong can va cham.
    ' Dung Random voi seed CO DINH (khong phai rng dung chung) de moi may trong PvP
    ' tu sinh ra dung 1 lop canh giong het nhau, khong can gui du lieu qua mang.
    Private Sub SpawnDecorations()
        decorations.Clear()
        Dim decorRng As New Random(20260716)
        For y As Integer = 1 To MAP_H - 2
            For x As Integer = 1 To MAP_W - 2
                If mapData(y, x) = 0 Then
                    If decorRng.NextDouble() < DECORATION_DENSITY Then
                        Dim count As Integer = decorRng.Next(1, 3)
                        For c As Integer = 0 To count - 1
                            Dim px As Single = x + 0.15F + CSng(decorRng.NextDouble()) * 0.7F
                            Dim py As Single = y + 0.15F + CSng(decorRng.NextDouble()) * 0.7F
                            Dim kindRoll As Double = decorRng.NextDouble()
                            Dim kind As Integer = If(kindRoll < 0.6, 0, If(kindRoll < 0.85, 1, 2)) ' 60% co, 25% hoa, 15% cay nen
                            decorations.Add(New DecorationItem() With {
                                .Pos = New PointF(px, py),
                                .Kind = kind,
                                .Scale = 0.7F + CSng(decorRng.NextDouble()) * 0.6F,
                                .HueSeed = CSng(decorRng.NextDouble())
                            })
                        Next
                    End If
                End If
            Next
        Next
    End Sub

    Private Sub gameTimer_Tick(sender As Object, e As EventArgs) Handles gameTimer.Tick
        Dim now As DateTime = DateTime.Now
        Dim dt As Double = (now - lastTick).TotalSeconds
        lastTick = now
        If dt > 0.1 Then dt = 0.1

        If isDrawingBow AndAlso playerHealth <= 0 Then CancelBowDraw() ' guc giua luc dang keo: huy, khong ban
        HandleInput(dt)
        CheckPickup()
        CheckItemPickup()
        UpdateProjectiles(dt)
        If attackSwingTime > 0.0 Then attackSwingTime = Math.Max(0.0, attackSwingTime - dt)
        If pickupAnimTime > 0.0 Then pickupAnimTime = Math.Max(0.0, pickupAnimTime - dt)
        UpdateRemoteAnimations(dt)
        NetworkTick()
        Me.Invalidate()
    End Sub
    Private Sub CheckPickup()
        If curNetMode = NetMode.Client Then
            CheckPickupClientRequest()
            Return
        End If

        Dim i As Integer = mushrooms.Count - 1
        While i >= 0
            Dim m As MushroomItem = mushrooms(i)
            Dim ddx As Double = m.Pos.X - playerX
            Dim ddy As Double = m.Pos.Y - playerY
            If (ddx * ddx + ddy * ddy) < 0.16 Then
                pickupAnimTime = PICKUP_ANIM_DURATION
                If curNetMode = NetMode.Host Then
                    ApplyPickup(0, m.Id)
                Else
                    ' Solo: khong can dong bo mang, xu ly cuc bo nhu cu
                    mushrooms.RemoveAt(i)
                    score += 10
                    mushroomsThisLevel += 1
                    If mushroomsThisLevel >= MUSHROOMS_PER_LEVEL Then
                        mushroomsThisLevel = 0
                        level += 1
                        speedMultiplier += 0.15
                    End If
                    If mushrooms.Count < 5 Then SpawnMushrooms(10)
                End If
            End If
            i -= 1
        End While
    End Sub

    ' Client: khong tu xoa nam cuc bo (tranh lech du lieu voi Host), chi gui yeu cau
    ' va cho "PICKOK" xac nhan. Dung pendingPickupRequests de khong spam yeu cau lien tuc.
    Private Sub CheckPickupClientRequest()
        For Each m As MushroomItem In mushrooms
            Dim ddx As Double = m.Pos.X - playerX
            Dim ddy As Double = m.Pos.Y - playerY
            If (ddx * ddx + ddy * ddy) < 0.16 AndAlso Not pendingPickupRequests.Contains(m.Id) Then
                pendingPickupRequests.Add(m.Id)
                pickupAnimTime = PICKUP_ANIM_DURATION
                If peer IsNot Nothing AndAlso peer.IsConnected Then
                    peer.SendLine("PICKREQ|" & localSlot & "|" & m.Id)
                End If
            End If
        Next
    End Sub

    ' ---- Nhat vat pham (dao/kiem/cung/thuoc...) vao tui - cung co che voi nam ----
    Private Sub CheckItemPickup()
        If curNetMode = NetMode.Client Then
            CheckItemPickupClientRequest()
            Return
        End If

        Dim i As Integer = worldItems.Count - 1
        While i >= 0
            Dim w As WorldItemSpawn = worldItems(i)
            Dim ddx As Double = w.Pos.X - playerX
            Dim ddy As Double = w.Pos.Y - playerY
            If (ddx * ddx + ddy * ddy) < 0.16 AndAlso HasEmptySlot() Then
                If curNetMode = NetMode.Host Then
                    ApplyItemPickup(0, w.Id)
                Else
                    ' Solo: khong can dong bo mang, xu ly cuc bo
                    worldItems.RemoveAt(i)
                    AddItemToLocalInventory(w.ItemId)
                End If
            End If
            i -= 1
        End While
    End Sub

    Private Sub CheckItemPickupClientRequest()
        If Not HasEmptySlot() Then Return ' tui day thi khong can xin nhat lam gi
        For Each w As WorldItemSpawn In worldItems
            Dim ddx As Double = w.Pos.X - playerX
            Dim ddy As Double = w.Pos.Y - playerY
            If (ddx * ddx + ddy * ddy) < 0.16 AndAlso Not pendingItemPickupRequests.Contains(w.Id) Then
                pendingItemPickupRequests.Add(w.Id)
                If peer IsNot Nothing AndAlso peer.IsConnected Then
                    peer.SendLine("ITEMPICKREQ|" & localSlot & "|" & w.Id)
                End If
            End If
        Next
    End Sub

    ' ---------------------------------------------------------------
    '  Raycasting: ve tuong + san nha co texture that tren pixelBuf
    '  (do phan giai noi bo RES_W x RES_H)
    ' ---------------------------------------------------------------

End Class
