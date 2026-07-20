Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

' =====================================================================
'  Form1 - Combat & Inventory: vu khi, sat thuong, item, trang bi
'  (Mot phan cua Form1 - xem README.md muc 'Cau truc code' de biet
'  toan bo cac file partial class va vai tro cua tung file)
' =====================================================================

Partial Public Class Form1

    Private Sub UseHeldItem()
        Dim item As ItemDefinition = CurrentItem()
        If item Is Nothing Then Return
        If playerHealth <= 0 Then Return ' da guc, cho hoi sinh, chua lam gi duoc

        Dim now As DateTime = DateTime.Now
        Dim cooldownSec As Double = If(item.Cooldown > 0, item.Cooldown, 0.5)
        If (now - lastAttackTime).TotalSeconds < cooldownSec Then Return ' con hoi, bo qua

        Select Case item.Kind
            Case ItemKind.Weapon
                If item.IsRanged Then
                    ' Cung gio ban qua co che giu-nha chuot trai (BeginBowDraw/ReleaseBowDraw),
                    ' KHONG con tu ban ngay o day nua. Ham nay van duoc EquipSlot() goi khi bam
                    ' phim so de trang bi, nen chi bo qua: trang bi cung khong tu ban, phai giu
                    ' chuot trai that su moi len day va ban.
                    Return
                End If
                lastAttackTime = now
                attackSwingTime = ATTACK_SWING_DURATION
                PerformMeleeAttack(item)
            Case ItemKind.Consumable
                lastAttackTime = now
                UseConsumable(item)
            Case ItemKind.Tool
                ' TODO: logic dung cu (mo khoa, dao dat...) danh cho nang cap sau
        End Select
    End Sub

    ' Can chien: tim doi thu GAN NHAT trong tam Range va trong "hinh non" huong nhin
    ' (MELEE_HIT_CONE_RAD moi ben) roi bao trung - khong can raycast phuc tap vi ban do
    ' nho va so nguoi choi it, kiem tra khoang cach + goc la du dung trong thuc te.
    Private Sub PerformMeleeAttack(item As ItemDefinition)
        Dim bestSlot As Integer = -1
        Dim bestDist As Double = Double.MaxValue
        For Each kv In remotePlayers
            Dim rp As RemotePlayerState = kv.Value
            If rp.Health <= 0 Then Continue For
            Dim dx As Double = rp.X - playerX
            Dim dy As Double = rp.Y - playerY
            Dim dist As Double = Math.Sqrt(dx * dx + dy * dy)
            If dist > item.Range Then Continue For
            Dim diff As Double = NormalizeAngle(Math.Atan2(dy, dx) - playerAngle)
            If Math.Abs(diff) > MELEE_HIT_CONE_RAD Then Continue For
            If dist < bestDist Then
                bestDist = dist
                bestSlot = kv.Key
            End If
        Next
        If bestSlot >= 0 Then SendAttack(bestSlot, item.Damage)
    End Sub

    Private Function NormalizeAngle(a As Double) As Double
        Dim result As Double = a
        While result > Math.PI
            result -= 2.0 * Math.PI
        End While
        While result < -Math.PI
            result += 2.0 * Math.PI
        End While
        Return result
    End Function

    ' Tam xa: tao 1 Projectile cuc bo NGAY (client-side prediction, cam giac ban muot tay),
    ' dong thoi bao cho cac may khac ve (Host broadcast / Client gui SHOOTREQ xin Host chuyen tiep).
    ' chargeT: 0..1 - muc do keo day cung luc nha chuot (0 = vua qua nguong toi thieu,
    ' 1 = keo toi da BOW_MAX_DRAW_SECONDS). Anh huong sat thuong (BOW_MAX_DAMAGE_MULT)
    ' va toc do bay (BOW_MAX_SPEED_MULT) cua mui ten, khong doi tam ban (Life giu nguyen).
    Private Sub FireProjectile(item As ItemDefinition, Optional chargeT As Double = 0.0)
        Dim dmgMult As Double = 1.0 + chargeT * (BOW_MAX_DAMAGE_MULT - 1.0)
        Dim speedMult As Double = 1.0 + chargeT * (BOW_MAX_SPEED_MULT - 1.0)
        Dim finalSpeed As Double = item.ProjectileSpeed * speedMult
        Dim finalDamage As Integer = CInt(Math.Round(item.Damage * dmgMult))

        Dim p As New Projectile() With {
            .Id = nextProjectileId, .OwnerSlot = localSlot, .X = playerX, .Y = playerY,
            .Angle = playerAngle, .Speed = finalSpeed, .Damage = finalDamage,
            .Life = ARROW_LIFE_SECONDS, .Resolved = False}
        nextProjectileId += 1
        projectiles.Add(p)

        If curNetMode = NetMode.Host Then
            hub.Broadcast("ARROW|" & localSlot & "|" & p.Id & "|" & FmtD(p.X) & "|" & FmtD(p.Y) & "|" & FmtD(p.Angle) & "|" & FmtD(p.Speed))
        ElseIf curNetMode = NetMode.Client AndAlso peer IsNot Nothing AndAlso peer.IsConnected Then
            peer.SendLine("SHOOTREQ|" & p.Id & "|" & FmtD(p.X) & "|" & FmtD(p.Y) & "|" & FmtD(p.Angle) & "|" & FmtD(p.Speed))
        End If
    End Sub

    ' Gui bao cao "da trung don" ve Host - Host la nguon du lieu goc duy nhat thuc su
    ' tru mau, giong het co che PICKREQ/ApplyPickup dang dung cho nam.
    Private Sub SendAttack(targetSlot As Integer, damage As Integer)
        If curNetMode = NetMode.Host Then
            ApplyDamage(localSlot, targetSlot, damage)
        ElseIf curNetMode = NetMode.Client AndAlso peer IsNot Nothing AndAlso peer.IsConnected Then
            peer.SendLine("ATKREQ|" & localSlot & "|" & targetSlot & "|" & damage)
        End If
        ' Solo (NetMode.None): remotePlayers luon rong nen khong bao gio toi day
    End Sub

    Private Sub UseConsumable(item As ItemDefinition)
        If item.HealAmount > 0 Then
            playerHealth = Math.Min(PLAYER_MAX_HEALTH, playerHealth + item.HealAmount)
            BroadcastOwnHealth()
        End If
        ' Tieu hao 1 lan dung: go item khoi o dang active roi cap nhat lai ten hien thi
        If activeSlotIndex >= 0 AndAlso activeSlotIndex < inventorySlots.Count Then
            inventorySlots(activeSlotIndex).Item = Nothing
        End If
        SyncHeldItemFromSlot()
    End Sub

    ' Bao HP cua CHINH MINH cho nguoi choi khac (dung cho truong hop tu hoi mau, khac voi
    ' DMG - la Host bao "ai do vua bi ai do danh trung" sau khi ApplyDamage).
    Private Sub BroadcastOwnHealth()
        If curNetMode = NetMode.Host Then
            hub.Broadcast("HPSELF|0|" & playerHealth)
        ElseIf curNetMode = NetMode.Client AndAlso peer IsNot Nothing AndAlso peer.IsConnected Then
            peer.SendLine("HPSELF|" & localSlot & "|" & playerHealth)
        End If
    End Sub

    Private Function FmtD(v As Double) As String
        Return v.ToString(CultureInfo.InvariantCulture)
    End Function

    ' ---- Host-only: xu ly va cham cua 1 don danh (ca cua host lan cua khach qua ATKREQ) ----
    Private Sub ApplyDamage(attackerSlot As Integer, targetSlot As Integer, damage As Integer)
        Dim curHp As Integer = GetHealthOfSlot(targetSlot)
        If curHp <= 0 Then Return ' da guc san roi (dang cho hoi sinh), bo qua don tiep theo

        Dim newHp As Integer = Math.Max(0, curHp - damage)
        SetHealthOfSlot(targetSlot, newHp)
        If targetSlot = localSlot Then lastDamageTakenTime = DateTime.Now
        hub.Broadcast("DMG|" & targetSlot & "|" & newHp & "|" & attackerSlot)

        If newHp <= 0 Then
            Dim spawnPt As PointF = FindRandomWalkableSpawn()
            SetHealthOfSlot(targetSlot, PLAYER_MAX_HEALTH)
            If targetSlot = 0 Then
                playerX = spawnPt.X : playerY = spawnPt.Y : playerZ = 0.0 : crouchAmount = 0.0
            Else
                Dim rp As RemotePlayerState = GetOrCreateRemote(targetSlot)
                rp.X = spawnPt.X : rp.Y = spawnPt.Y
            End If
            hub.Broadcast("RESPAWN|" & targetSlot & "|" & FmtD(spawnPt.X) & "|" & FmtD(spawnPt.Y) & "|" & PLAYER_MAX_HEALTH)
        End If
    End Sub

    Private Function GetHealthOfSlot(slot As Integer) As Integer
        If slot = 0 Then Return playerHealth
        Return GetOrCreateRemote(slot).Health
    End Function

    Private Sub SetHealthOfSlot(slot As Integer, hp As Integer)
        If slot = 0 Then
            playerHealth = hp
        Else
            GetOrCreateRemote(slot).Health = hp
        End If
    End Sub

    Private Function FindRandomWalkableSpawn() As PointF
        Dim attempts As Integer = 0
        While attempts < 500
            attempts += 1
            Dim mx As Integer = rng.Next(1, MAP_W - 1)
            Dim my As Integer = rng.Next(1, MAP_H - 1)
            If mapData(my, mx) = 0 Then Return New PointF(mx + 0.5F, my + 0.5F)
        End While
        Return New PointF(1.5F, 1.5F) ' fallback ve diem xuat phat, khong nen xay ra
    End Function

    ' Cap nhat vi tri/tuoi tho phi tieu moi frame. Chi may SO HUU (OwnerSlot = localSlot)
    ' moi kiem tra va cham voi nguoi choi khac va bao sat thuong - cac may con lai chi
    ' mo phong chuyen dong de VE cho dep, tranh 1 mui ten bi bao trung nhieu lan.
    Private Sub UpdateProjectiles(dt As Double)
        Dim i As Integer = projectiles.Count - 1
        While i >= 0
            Dim p As Projectile = projectiles(i)
            p.X += Math.Cos(p.Angle) * p.Speed * dt
            p.Y += Math.Sin(p.Angle) * p.Speed * dt
            p.Life -= dt

            If p.OwnerSlot = localSlot AndAlso Not p.Resolved Then
                For Each kv In remotePlayers
                    Dim rp As RemotePlayerState = kv.Value
                    If rp.Health <= 0 Then Continue For
                    Dim dx As Double = rp.X - p.X
                    Dim dy As Double = rp.Y - p.Y
                    If (dx * dx + dy * dy) <= (ARROW_HIT_RADIUS * ARROW_HIT_RADIUS) Then
                        SendAttack(kv.Key, p.Damage)
                        p.Resolved = True
                        Exit For
                    End If
                Next
            End If

            If p.Resolved OrElse p.Life <= 0 OrElse IsBlockedForProjectile(p.X, p.Y) Then
                projectiles.RemoveAt(i)
            End If
            i -= 1
        End While
    End Sub

    Private Function IsBlockedForProjectile(x As Double, y As Double) As Boolean
        Dim mx As Integer = CInt(Math.Floor(x))
        Dim my As Integer = CInt(Math.Floor(y))
        If mx < 0 OrElse mx >= MAP_W OrElse my < 0 OrElse my >= MAP_H Then Return True
        Return mapData(my, mx) <> 0
    End Function

    ' =================================================================
    '  TRANG BI / INVENTORY (hotbar kieu Lien Minh: bam phim so de doi
    '  va dung luon). Danh sach item hien la du lieu test - thay/them
    '  ItemDefinition that khi co gameplay that su can trang bi.
    ' =================================================================
    Private inventorySlots As New List(Of InventorySlot)
    Private activeSlotIndex As Integer = 0
    Private itemIcons As New Dictionary(Of String, Bitmap)
    Private itemIconPixels As New Dictionary(Of String, Integer())
    Private itemIconSize As New Dictionary(Of String, Point)

    ' Catalog dinh nghia tat ca loai vat pham co the xuat hien (ca 2 may Host/Client
    ' deu tu build catalog giong het nhau tu code, nen mang chi can gui "ItemId"
    ' (chuoi khoa nhu "sword") thay vi phai truyen ca dinh nghia).
    Private itemCatalog As New Dictionary(Of String, ItemDefinition)
    Private itemCatalogKeys As New List(Of String)

    ' Vat pham dang nam ngoai the gioi (chua ai nhat), tuong tu co che voi mushrooms.
    Private worldItems As New List(Of WorldItemSpawn)
    Private nextWorldItemId As Integer = 1
    Private pendingItemPickupRequests As New HashSet(Of Integer)

    Private Sub InitItemCatalog()
        itemCatalog.Clear()
        itemCatalogKeys.Clear()
        AddCatalogItem(New ItemDefinition() With {.Id = "dagger", .DisplayName = "Dao gam", .Kind = ItemKind.Weapon, .IconFileName = "dagger.png",
                                                   .Damage = 15, .Range = 0.9, .Cooldown = 0.35, .IsRanged = False})
        AddCatalogItem(New ItemDefinition() With {.Id = "sword", .DisplayName = "Kiem", .Kind = ItemKind.Weapon, .IconFileName = "sword.png",
                                                   .Damage = 30, .Range = 1.15, .Cooldown = 0.6, .IsRanged = False})
        AddCatalogItem(New ItemDefinition() With {.Id = "bow", .DisplayName = "Cung", .Kind = ItemKind.Weapon, .IconFileName = "bow.png",
                                                   .Damage = 22, .Cooldown = 0.75, .IsRanged = True, .ProjectileSpeed = 7.0})
        AddCatalogItem(New ItemDefinition() With {.Id = "potion", .DisplayName = "Binh thuoc", .Kind = ItemKind.Consumable, .IconFileName = "potion.png",
                                                   .HealAmount = 40, .Cooldown = 0.3})
    End Sub

    Private Sub AddCatalogItem(def As ItemDefinition)
        itemCatalog(def.Id) = def
        itemCatalogKeys.Add(def.Id)
    End Sub

    ' Tui gio chi la 5 o TRONG - phai di nhat vat pham ngoai the gioi moi co do dung.
    Private Sub InitInventory()
        inventorySlots.Clear()
        For i As Integer = 1 To 5
            inventorySlots.Add(New InventorySlot() With {.HotkeyNumber = i, .Item = Nothing})
        Next
        activeSlotIndex = 0
        SyncHeldItemFromSlot()
    End Sub

    Private Function CurrentItem() As ItemDefinition
        If activeSlotIndex < 0 OrElse activeSlotIndex >= inventorySlots.Count Then Return Nothing
        Return inventorySlots(activeSlotIndex).Item
    End Function

    Private Sub EquipSlot(idx As Integer)
        If idx < 0 OrElse idx >= inventorySlots.Count Then Return
        CancelBowDraw() ' dang giu chuot trai keo cung do ma doi tay: huy luon, khong ban
        activeSlotIndex = idx
        SyncHeldItemFromSlot()
        UseHeldItem() ' bam so la trang bi VA dung luon, dung y yeu cau
    End Sub

    Private Sub SyncHeldItemFromSlot()
        Dim item As ItemDefinition = CurrentItem()
        heldItemName = If(item Is Nothing, "(chua trang bi)", item.DisplayName)
    End Sub

    ' Vat vu khi/vat pham dang chon (o dang active) ra ngoai the gioi, ngay truoc mat nguoi
    ' choi. Phim Del. O trong (chua trang bi gi) thi khong lam gi ca.
    Private Sub DropHeldItem()
        Dim item As ItemDefinition = CurrentItem()
        If item Is Nothing Then Return

        CancelBowDraw() ' phong khi dang keo cung ma vat: huy luon, khong ban

        Dim dropX As Double = playerX + Math.Cos(playerAngle) * 0.6
        Dim dropY As Double = playerY + Math.Sin(playerAngle) * 0.6

        inventorySlots(activeSlotIndex).Item = Nothing
        SyncHeldItemFromSlot()

        ' Khac voi nhat do (co the tranh chap giua nhieu nguoi cung lao vao 1 vat, can Host
        ' phan xu ai nhat truoc), vat do la tai san cua chinh minh nen KHONG can "xin phep"
        ' Host moi duoc hien - hien ngay lap tuc tren may cua minh (du la Host, Client, hay
        ' Solo), Host van la noi luu du lieu goc nhung chi de dong bo cho NGUOI KHAC thay,
        ' khong lam nguoi vat phai cho.
        worldItems.Add(New WorldItemSpawn() With {.Id = nextWorldItemId, .Pos = New PointF(CSng(dropX), CSng(dropY)), .ItemId = item.Id})
        nextWorldItemId += 1

        If curNetMode = NetMode.Client Then
            ' Chi la THONG BAO cho Host de Host cap nhat du lieu goc va bao cho nguoi khac,
            ' khong phai xin phep - vat pham cua minh da hien tren may minh roi, khong doi
            ' phan hoi. Lan ITEMSYNC ke tiep tu Host se tu dong khop lai (thay toan bo danh
            ' sach), khong tao trung do vi ITEMSYNC luon GHI DE toan bo chu khong cong don.
            If peer IsNot Nothing AndAlso peer.IsConnected Then
                peer.SendLine("ITEMDROPREQ|" & localSlot & "|" & item.Id & "|" &
                              dropX.ToString(CultureInfo.InvariantCulture) & "|" &
                              dropY.ToString(CultureInfo.InvariantCulture))
            End If
        ElseIf curNetMode = NetMode.Host Then
            hub.Broadcast(BuildItemSyncLine("ITEMSYNC"))
        End If
    End Sub

    ' Icon duoc cache lai (chi doc dia 1 lan cho moi ten file), Nothing = chua co
    ' anh rieng thi HUD se tu ve o mau + chu cai dau thay the.
    Private Function GetItemIcon(item As ItemDefinition) As Bitmap
        If item Is Nothing OrElse String.IsNullOrEmpty(item.IconFileName) Then Return Nothing
        If Not itemIcons.ContainsKey(item.IconFileName) Then
            itemIcons(item.IconFileName) = TryLoadBitmap("Items", item.IconFileName)
        End If
        Return itemIcons(item.IconFileName)
    End Function

    ' Ban raw-pixel (dung cho ve trong the gioi 3D qua raycasting), khac voi
    ' GetItemIcon o tren la ban Bitmap (dung cho HUD ve qua Graphics.DrawImage).
    Private Function GetItemIconPixels(item As ItemDefinition, ByRef outW As Integer, ByRef outH As Integer) As Integer()
        If item Is Nothing OrElse String.IsNullOrEmpty(item.IconFileName) Then
            outW = 0 : outH = 0
            Return Nothing
        End If
        If Not itemIconPixels.ContainsKey(item.IconFileName) Then
            Dim w As Integer, h As Integer
            Dim data() As Integer = TryLoadTexturePixels("Items", item.IconFileName, w, h)
            itemIconPixels(item.IconFileName) = data
            itemIconSize(item.IconFileName) = New Point(w, h)
        End If
        Dim sz As Point = itemIconSize(item.IconFileName)
        outW = sz.X : outH = sz.Y
        Return itemIconPixels(item.IconFileName)
    End Function

    Private Function HasEmptySlot() As Boolean
        For Each slot As InventorySlot In inventorySlots
            If slot.Item Is Nothing Then Return True
        Next
        Return False
    End Function

    ' Nhet item vao O TRONG DAU TIEN trong tui. Neu tui day thi bo qua (khong lam gi).
    Private Sub AddItemToLocalInventory(itemId As String)
        If Not itemCatalog.ContainsKey(itemId) Then Return
        Dim def As ItemDefinition = itemCatalog(itemId)
        For Each slot As InventorySlot In inventorySlots
            If slot.Item Is Nothing Then
                slot.Item = def
                SyncHeldItemFromSlot() ' phong khi vua nhat dung vao o dang active
                Return
            End If
        Next
    End Sub

    Private Function ItemFallbackColor(kind As ItemKind) As Color
        Select Case kind
            Case ItemKind.Weapon : Return Color.FromArgb(180, 60, 60)
            Case ItemKind.Tool : Return Color.FromArgb(80, 130, 180)
            Case ItemKind.Consumable : Return Color.FromArgb(90, 170, 90)
            Case Else : Return Color.Gray
        End Select
    End Function

    ' =================================================================
    '  PVP MANG: khoi dong, su kien Host (NetworkHub) / Client (NetworkPeer),
    '  va giao thuc du lieu dang dong text phan cach boi "|".
    '  POS|slot|x|y|angle|z|crouch          - cap nhat vi tri 1 nguoi choi
    '  PICKREQ|slot|mushroomId              - (Client -> Host) xin nhat nam
    '  PICKOK|mushroomId|slot|score|level   - (Host -> tat ca) xac nhan nhat
    '  MSYNC|id,x,y|id,x,y|...              - (Host -> tat ca) dong bo lai toan bo nam
    '  WELCOME|slot|id,x,y|id,x,y|...       - (Host -> khach moi) giao slot + nam hien co
    '  LEAVE|slot                           - (Host -> tat ca) 1 nguoi da roi phong
    ' =================================================================

End Class
