Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

' =====================================================================
'  Form1 - Network: dong bo Host/Client qua NetworkHub/NetworkPeer
'  (Mot phan cua Form1 - xem README.md muc 'Cau truc code' de biet
'  toan bo cac file partial class va vai tro cua tung file)
' =====================================================================

Partial Public Class Form1

    Private Sub StartNetworking(modeArg As String)
        Select Case modeArg
            Case "host"
                curNetMode = NetMode.Host
                localSlot = 0
                hub = New NetworkHub(Me)
                AddHandler hub.ClientConnected, AddressOf Hub_ClientConnected
                AddHandler hub.ClientDisconnected, AddressOf Hub_ClientDisconnected
                AddHandler hub.ClientLineReceived, AddressOf Hub_ClientLineReceived
                Try
                    hub.StartListening(netPort)
                    netStatusText = "Dang lam Host tren port " & netPort & " - cho nguoi choi vao..."
                Catch ex As Exception
                    netStatusText = "Loi mo port " & netPort & ": " & ex.Message
                End Try
            Case "join"
                curNetMode = NetMode.Client
                peer = New NetworkPeer(Me)
                AddHandler peer.Connected, AddressOf Peer_Connected
                AddHandler peer.Disconnected, AddressOf Peer_Disconnected
                AddHandler peer.LineReceived, AddressOf Peer_LineReceived
                netStatusText = "Dang ket noi toi " & netHostIp & ":" & netPort & "..."
                peer.ConnectToHost(netHostIp, netPort)
            Case Else
                curNetMode = NetMode.None
                netStatusText = ""
        End Select
    End Sub

    ' ---- Host: co khach moi vao ----
    Private Sub Hub_ClientConnected(slotIndex As Integer)
        netStatusText = "Nguoi choi #" & slotIndex & " da vao phong (" & (hub.ConnectedCount) & "/3 khach)."
        hub.SendTo(slotIndex, BuildWelcomeLine(slotIndex))
        hub.SendTo(slotIndex, BuildItemSyncLine("ITEMSYNC"))
        hub.SendTo(slotIndex, BuildPosLine(0, playerX, playerY, playerAngle, playerZ, crouchAmount, LocalHeldItemId()))
        For Each kv In remotePlayers
            hub.SendTo(slotIndex, BuildPosLine(kv.Key, kv.Value.X, kv.Value.Y, kv.Value.Angle, kv.Value.Z, kv.Value.Crouch, kv.Value.HeldItemId))
        Next
    End Sub

    Private Sub Hub_ClientDisconnected(slotIndex As Integer)
        remotePlayers.Remove(slotIndex)
        netStatusText = "Nguoi choi #" & slotIndex & " da roi phong."
        hub.Broadcast("LEAVE|" & slotIndex)
    End Sub

    Private Sub Hub_ClientLineReceived(slotIndex As Integer, line As String)
        If line.StartsWith("POS|") Then
            ApplyRemotePos(line)
            hub.BroadcastExcept(slotIndex, line)
        ElseIf line.StartsWith("PICKREQ|") Then
            Dim parts() As String = line.Split("|"c)
            If parts.Length = 3 Then
                Dim reqSlot As Integer, mid As Integer
                If Integer.TryParse(parts(1), reqSlot) AndAlso Integer.TryParse(parts(2), mid) Then
                    ApplyPickup(reqSlot, mid)
                End If
            End If
        ElseIf line.StartsWith("ITEMPICKREQ|") Then
            Dim parts() As String = line.Split("|"c)
            If parts.Length = 3 Then
                Dim reqSlot As Integer, wid As Integer
                If Integer.TryParse(parts(1), reqSlot) AndAlso Integer.TryParse(parts(2), wid) Then
                    ApplyItemPickup(reqSlot, wid)
                End If
            End If
        ElseIf line.StartsWith("ATKREQ|") Then
            Dim parts() As String = line.Split("|"c)
            If parts.Length = 4 Then
                Dim atkSlot As Integer, tgtSlot As Integer, dmg As Integer
                If Integer.TryParse(parts(1), atkSlot) AndAlso Integer.TryParse(parts(2), tgtSlot) AndAlso Integer.TryParse(parts(3), dmg) Then
                    ApplyDamage(atkSlot, tgtSlot, dmg)
                End If
            End If
        ElseIf line.StartsWith("SHOOTREQ|") Then
            Dim parts() As String = line.Split("|"c)
            If parts.Length = 6 Then
                Dim pid As Integer
                Dim px As Double, py As Double, pang As Double, pspeed As Double
                If Integer.TryParse(parts(1), pid) AndAlso
                   Double.TryParse(parts(2), NumberStyles.Float, CultureInfo.InvariantCulture, px) AndAlso
                   Double.TryParse(parts(3), NumberStyles.Float, CultureInfo.InvariantCulture, py) AndAlso
                   Double.TryParse(parts(4), NumberStyles.Float, CultureInfo.InvariantCulture, pang) AndAlso
                   Double.TryParse(parts(5), NumberStyles.Float, CultureInfo.InvariantCulture, pspeed) Then
                    ' Host tu them mui ten cua khach vao danh sach cuc bo de VE tren man hinh
                    ' cua Host, nhung KHONG tu kiem tra va cham (do la viec cua may so huu - slotIndex).
                    projectiles.Add(New Projectile() With {.Id = pid, .OwnerSlot = slotIndex, .X = px, .Y = py,
                                                            .Angle = pang, .Speed = pspeed, .Damage = 0, .Life = ARROW_LIFE_SECONDS})
                    hub.BroadcastExcept(slotIndex, "ARROW|" & slotIndex & "|" & pid & "|" & parts(2) & "|" & parts(3) & "|" & parts(4) & "|" & parts(5))
                End If
            End If
        ElseIf line.StartsWith("HPSELF|") Then
            Dim parts() As String = line.Split("|"c)
            If parts.Length = 3 Then
                Dim slot As Integer, hp As Integer
                If Integer.TryParse(parts(1), slot) AndAlso Integer.TryParse(parts(2), hp) Then
                    GetOrCreateRemote(slot).Health = hp
                    hub.BroadcastExcept(slotIndex, line)
                End If
            End If
        End If
    End Sub

    ' ---- Client: ket noi toi Host ----
    Private Sub Peer_Connected()
        netStatusText = "Da ket noi toi Host, dang cho phan cong slot..."
    End Sub

    Private Sub Peer_Disconnected()
        netStatusText = "Mat ket noi voi Host."
    End Sub

    Private Sub Peer_LineReceived(line As String)
        If line.StartsWith("WELCOME|") Then
            Dim parts() As String = line.Split("|"c)
            If parts.Length >= 2 Then Integer.TryParse(parts(1), localSlot)
            mushrooms = ParseMushroomList(parts, 2)
            pendingPickupRequests.Clear()
            netStatusText = "Da vao phong, ban la nguoi choi #" & localSlot
        ElseIf line.StartsWith("MSYNC") Then
            Dim parts() As String = line.Split("|"c)
            mushrooms = ParseMushroomList(parts, 1)
            pendingPickupRequests.Clear()
        ElseIf line.StartsWith("ITEMSYNC") Then
            Dim parts() As String = line.Split("|"c)
            worldItems = ParseWorldItemList(parts, 1)
            pendingItemPickupRequests.Clear()
        ElseIf line.StartsWith("POS|") Then
            ApplyRemotePos(line)
        ElseIf line.StartsWith("PICKOK|") Then
            ApplyPickOkLocally(line)
        ElseIf line.StartsWith("ITEMPICKOK|") Then
            ApplyItemPickOkLocally(line)
        ElseIf line.StartsWith("LEAVE|") Then
            Dim parts() As String = line.Split("|"c)
            Dim slot As Integer
            If parts.Length = 2 AndAlso Integer.TryParse(parts(1), slot) Then remotePlayers.Remove(slot)
        ElseIf line.StartsWith("DMG|") Then
            Dim parts() As String = line.Split("|"c)
            If parts.Length = 4 Then
                Dim slot As Integer, hp As Integer
                If Integer.TryParse(parts(1), slot) AndAlso Integer.TryParse(parts(2), hp) Then
                    If slot = localSlot Then
                        playerHealth = hp
                        lastDamageTakenTime = DateTime.Now
                    Else
                        GetOrCreateRemote(slot).Health = hp
                    End If
                End If
            End If
        ElseIf line.StartsWith("RESPAWN|") Then
            Dim parts() As String = line.Split("|"c)
            If parts.Length = 5 Then
                Dim slot As Integer, hp As Integer
                Dim x As Double, y As Double
                If Integer.TryParse(parts(1), slot) AndAlso
                   Double.TryParse(parts(2), NumberStyles.Float, CultureInfo.InvariantCulture, x) AndAlso
                   Double.TryParse(parts(3), NumberStyles.Float, CultureInfo.InvariantCulture, y) AndAlso
                   Integer.TryParse(parts(4), hp) Then
                    If slot = localSlot Then
                        playerX = x : playerY = y : playerZ = 0.0 : crouchAmount = 0.0 : playerHealth = hp
                    Else
                        Dim rp As RemotePlayerState = GetOrCreateRemote(slot)
                        rp.X = x : rp.Y = y : rp.Health = hp
                    End If
                End If
            End If
        ElseIf line.StartsWith("ARROW|") Then
            Dim parts() As String = line.Split("|"c)
            If parts.Length = 7 Then
                Dim ownerSlot As Integer, pid As Integer
                Dim px As Double, py As Double, pang As Double, pspeed As Double
                If Integer.TryParse(parts(1), ownerSlot) AndAlso Integer.TryParse(parts(2), pid) AndAlso
                   Double.TryParse(parts(3), NumberStyles.Float, CultureInfo.InvariantCulture, px) AndAlso
                   Double.TryParse(parts(4), NumberStyles.Float, CultureInfo.InvariantCulture, py) AndAlso
                   Double.TryParse(parts(5), NumberStyles.Float, CultureInfo.InvariantCulture, pang) AndAlso
                   Double.TryParse(parts(6), NumberStyles.Float, CultureInfo.InvariantCulture, pspeed) Then
                    projectiles.Add(New Projectile() With {.Id = pid, .OwnerSlot = ownerSlot, .X = px, .Y = py,
                                                            .Angle = pang, .Speed = pspeed, .Damage = 0, .Life = ARROW_LIFE_SECONDS})
                End If
            End If
        ElseIf line.StartsWith("HPSELF|") Then
            Dim parts() As String = line.Split("|"c)
            If parts.Length = 3 Then
                Dim slot As Integer, hp As Integer
                If Integer.TryParse(parts(1), slot) AndAlso Integer.TryParse(parts(2), hp) AndAlso slot <> localSlot Then
                    GetOrCreateRemote(slot).Health = hp
                End If
            End If
        End If
    End Sub

    ' ---- Xay dung / phan tich cac dong giao thuc ----
    Private Function LocalHeldItemId() As String
        Dim item As ItemDefinition = CurrentItem()
        Return If(item Is Nothing, "", item.Id)
    End Function

    Private Function BuildPosLine(slot As Integer, x As Double, y As Double, ang As Double, z As Double, crouch As Double, itemId As String) As String
        Return "POS|" & slot & "|" & x.ToString(CultureInfo.InvariantCulture) & "|" & y.ToString(CultureInfo.InvariantCulture) &
               "|" & ang.ToString(CultureInfo.InvariantCulture) & "|" & z.ToString(CultureInfo.InvariantCulture) &
               "|" & crouch.ToString(CultureInfo.InvariantCulture) & "|" & itemId
    End Function

    Private Function BuildMushroomSyncLine(prefix As String) As String
        Dim sb As New System.Text.StringBuilder(prefix)
        For Each m As MushroomItem In mushrooms
            sb.Append("|").Append(m.Id).Append(",").Append(m.Pos.X.ToString(CultureInfo.InvariantCulture)).
               Append(",").Append(m.Pos.Y.ToString(CultureInfo.InvariantCulture))
        Next
        Return sb.ToString()
    End Function

    Private Function BuildWelcomeLine(slotIndex As Integer) As String
        Return BuildMushroomSyncLine("WELCOME|" & slotIndex)
    End Function

    Private Function ParseMushroomList(parts() As String, startIndex As Integer) As List(Of MushroomItem)
        Dim result As New List(Of MushroomItem)
        For idx As Integer = startIndex To parts.Length - 1
            Dim fields() As String = parts(idx).Split(","c)
            If fields.Length = 3 Then
                Dim mid As Integer, mx As Double, my As Double
                If Integer.TryParse(fields(0), mid) AndAlso
                   Double.TryParse(fields(1), NumberStyles.Float, CultureInfo.InvariantCulture, mx) AndAlso
                   Double.TryParse(fields(2), NumberStyles.Float, CultureInfo.InvariantCulture, my) Then
                    result.Add(New MushroomItem() With {.Id = mid, .Pos = New PointF(CSng(mx), CSng(my))})
                End If
            End If
        Next
        Return result
    End Function

    Private Sub ApplyRemotePos(line As String)
        Dim parts() As String = line.Split("|"c)
        If parts.Length < 7 Then Return
        Dim slot As Integer
        If Not Integer.TryParse(parts(1), slot) Then Return
        If slot = localSlot AndAlso curNetMode <> NetMode.None Then Return
        Dim x As Double, y As Double, ang As Double, z As Double, crouch As Double
        If Double.TryParse(parts(2), NumberStyles.Float, CultureInfo.InvariantCulture, x) AndAlso
           Double.TryParse(parts(3), NumberStyles.Float, CultureInfo.InvariantCulture, y) AndAlso
           Double.TryParse(parts(4), NumberStyles.Float, CultureInfo.InvariantCulture, ang) AndAlso
           Double.TryParse(parts(5), NumberStyles.Float, CultureInfo.InvariantCulture, z) AndAlso
           Double.TryParse(parts(6), NumberStyles.Float, CultureInfo.InvariantCulture, crouch) Then
            Dim rp As RemotePlayerState = GetOrCreateRemote(slot)
            Dim now As DateTime = DateTime.Now
            rp.HeldItemId = If(parts.Length > 7, parts(7), "") ' "" = chua trang bi (xem LocalHeldItemId)

            ' Uoc luong toc do di chuyen tu khoang cach + thoi gian giua 2 goi POS gan nhat,
            ' dung de biet remote player "dang di bo" hay "dang dung yen" (UpdateRemoteAnimations
            ' se dung gia tri nay moi frame, vi goi POS chi den theo chu ky mang, khong phai moi frame).
            If rp.HasPrevPos Then
                Dim dtPos As Double = (now - rp.PrevPosTime).TotalSeconds
                If dtPos > 0.01 Then
                    Dim dist As Double = Math.Sqrt((x - rp.PrevX) ^ 2 + (y - rp.PrevY) ^ 2)
                    rp.MoveSpeedEst = dist / dtPos
                End If
            End If
            rp.PrevX = x : rp.PrevY = y : rp.PrevPosTime = now : rp.HasPrevPos = True

            rp.X = x : rp.Y = y : rp.Angle = ang : rp.Z = z : rp.Crouch = crouch
            rp.LastSeen = now
        End If
    End Sub

    ' Cap nhat hoat anh (di bo / tho) cho tat ca remote player, goi moi frame (khong phai moi
    ' khi nhan goi mang) de hoat anh muot du goi POS thua thot. Dua tren MoveSpeedEst uoc luong
    ' trong ApplyRemotePos: >= REMOTE_MOVE_SPEED_THRESHOLD thi coi la dang di bo.
    Private Sub UpdateRemoteAnimations(dt As Double)
        If remotePlayers.Count = 0 Then Return
        Dim now As DateTime = DateTime.Now
        For Each kv In remotePlayers
            Dim rp As RemotePlayerState = kv.Value
            ' Neu qua 0.5s khong nhan goi POS moi, coi nhu da dung lai (tranh ket "dang di bo"
            ' mai khi mat goi tin hoac doi thu ngat ket noi tam thoi).
            Dim recentlyUpdated As Boolean = (now - rp.LastSeen).TotalSeconds < 0.5
            Dim isMoving As Boolean = recentlyUpdated AndAlso rp.MoveSpeedEst >= REMOTE_MOVE_SPEED_THRESHOLD

            Dim bobTarget As Double = If(isMoving, 1.0, 0.0)
            If rp.BobAmount < bobTarget Then
                rp.BobAmount = Math.Min(bobTarget, rp.BobAmount + REMOTE_BOB_LERP_SPEED * dt)
            Else
                rp.BobAmount = Math.Max(bobTarget, rp.BobAmount - REMOTE_BOB_LERP_SPEED * dt)
            End If

            If isMoving Then rp.BobPhase += REMOTE_WALK_BOB_SPEED * dt
            rp.IdlePhase += REMOTE_IDLE_BREATH_SPEED * dt
        Next
    End Sub

    Private Function GetOrCreateRemote(slot As Integer) As RemotePlayerState
        If Not remotePlayers.ContainsKey(slot) Then remotePlayers(slot) = New RemotePlayerState()
        Return remotePlayers(slot)
    End Function

    ' Host-only: xu ly 1 luot nhat nam (ca cua host lan cua khach xin qua PICKREQ),
    ' vi Host la nguon du lieu goc nen day la noi DUY NHAT thuc su xoa nam khoi danh sach.
    Private Sub ApplyPickup(pickerSlot As Integer, mushroomId As Integer)
        Dim idx As Integer = mushrooms.FindIndex(Function(mm) mm.Id = mushroomId)
        If idx < 0 Then Return ' nam nay da bi nguoi khac nhat mat roi, bo qua

        mushrooms.RemoveAt(idx)
        Dim newScore As Integer, newLevel As Integer

        If pickerSlot = 0 Then
            score += 10
            mushroomsThisLevel += 1
            If mushroomsThisLevel >= MUSHROOMS_PER_LEVEL Then
                mushroomsThisLevel = 0 : level += 1 : speedMultiplier += 0.15
            End If
            newScore = score : newLevel = level
        Else
            Dim rp As RemotePlayerState = GetOrCreateRemote(pickerSlot)
            rp.Score += 10
            newScore = rp.Score : newLevel = 0
        End If

        hub.Broadcast("PICKOK|" & mushroomId & "|" & pickerSlot & "|" & newScore & "|" & newLevel)

        If mushrooms.Count < 5 Then
            SpawnMushrooms(10)
            hub.Broadcast(BuildMushroomSyncLine("MSYNC"))
        End If
    End Sub

    ' Client-only: khi nhan duoc xac nhan tu Host thi moi thuc su xoa nam khoi danh sach cua minh
    Private Sub ApplyPickOkLocally(line As String)
        Dim parts() As String = line.Split("|"c)
        If parts.Length <> 5 Then Return
        Dim mid As Integer, slot As Integer, sc As Integer, lvl As Integer
        If Not (Integer.TryParse(parts(1), mid) AndAlso Integer.TryParse(parts(2), slot) AndAlso
                Integer.TryParse(parts(3), sc) AndAlso Integer.TryParse(parts(4), lvl)) Then Return

        Dim idx As Integer = mushrooms.FindIndex(Function(mm) mm.Id = mid)
        If idx >= 0 Then mushrooms.RemoveAt(idx)
        pendingPickupRequests.Remove(mid)

        If slot = localSlot Then
            score = sc
            If lvl > level Then level = lvl : speedMultiplier += 0.15
        Else
            Dim rp As RemotePlayerState = GetOrCreateRemote(slot)
            rp.Score = sc
        End If
    End Sub

    ' ---- Dong bo vat pham the gioi (cung co che voi nam, nhung khong co diem/level -
    ' ket qua la item duoc them vao TUI RIENG cua nguoi nhat, khong phai du lieu chung) ----
    Private Function BuildItemSyncLine(prefix As String) As String
        Dim sb As New System.Text.StringBuilder(prefix)
        For Each w As WorldItemSpawn In worldItems
            sb.Append("|").Append(w.Id).Append(",").Append(w.Pos.X.ToString(CultureInfo.InvariantCulture)).
               Append(",").Append(w.Pos.Y.ToString(CultureInfo.InvariantCulture)).Append(",").Append(w.ItemId)
        Next
        Return sb.ToString()
    End Function

    Private Function ParseWorldItemList(parts() As String, startIndex As Integer) As List(Of WorldItemSpawn)
        Dim result As New List(Of WorldItemSpawn)
        For idx As Integer = startIndex To parts.Length - 1
            Dim fields() As String = parts(idx).Split(","c)
            If fields.Length = 4 Then
                Dim wid As Integer, wx As Double, wy As Double
                If Integer.TryParse(fields(0), wid) AndAlso
                   Double.TryParse(fields(1), NumberStyles.Float, CultureInfo.InvariantCulture, wx) AndAlso
                   Double.TryParse(fields(2), NumberStyles.Float, CultureInfo.InvariantCulture, wy) Then
                    result.Add(New WorldItemSpawn() With {.Id = wid, .Pos = New PointF(CSng(wx), CSng(wy)), .ItemId = fields(3)})
                End If
            End If
        Next
        Return result
    End Function

    ' Host-only: xu ly 1 luot nhat vat pham (ca cua host lan cua khach xin qua ITEMPICKREQ).
    Private Sub ApplyItemPickup(pickerSlot As Integer, worldItemId As Integer)
        Dim idx As Integer = worldItems.FindIndex(Function(ww) ww.Id = worldItemId)
        If idx < 0 Then Return ' vat pham nay da bi nguoi khac nhat mat roi, bo qua

        Dim spawn As WorldItemSpawn = worldItems(idx)
        worldItems.RemoveAt(idx)

        hub.Broadcast("ITEMPICKOK|" & worldItemId & "|" & pickerSlot)

        If pickerSlot = 0 Then AddItemToLocalInventory(spawn.ItemId) ' host tu nhat cho chinh minh

        If worldItems.Count < 3 Then
            SpawnWorldItems(5)
            hub.Broadcast(BuildItemSyncLine("ITEMSYNC"))
        End If
    End Sub

    ' Client-only: nhan xac nhan roi moi thuc su xoa vat pham khoi the gioi cua minh,
    ' va CHI nhet vao tui rieng neu chinh minh la nguoi duoc xac nhan (slot = localSlot).
    Private Sub ApplyItemPickOkLocally(line As String)
        Dim parts() As String = line.Split("|"c)
        If parts.Length <> 3 Then Return
        Dim wid As Integer, slot As Integer
        If Not (Integer.TryParse(parts(1), wid) AndAlso Integer.TryParse(parts(2), slot)) Then Return

        Dim idx As Integer = worldItems.FindIndex(Function(ww) ww.Id = wid)
        Dim pickedItemId As String = Nothing
        If idx >= 0 Then
            pickedItemId = worldItems(idx).ItemId
            worldItems.RemoveAt(idx)
        End If
        pendingItemPickupRequests.Remove(wid)

        If slot = localSlot AndAlso pickedItemId IsNot Nothing Then
            AddItemToLocalInventory(pickedItemId)
        End If
    End Sub

    ' Gui vi tri cua minh cho doi phuong moi frame (theo lua chon dong bo cua nguoi dung)
    Private Sub NetworkTick()
        If curNetMode = NetMode.Host Then
            hub.Broadcast(BuildPosLine(0, playerX, playerY, playerAngle, playerZ, crouchAmount, LocalHeldItemId()))
        ElseIf curNetMode = NetMode.Client AndAlso peer IsNot Nothing AndAlso peer.IsConnected Then
            peer.SendLine(BuildPosLine(localSlot, playerX, playerY, playerAngle, playerZ, crouchAmount, LocalHeldItemId()))
        End If
    End Sub

End Class
