Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

' =====================================================================
'  Form1 - HUD & Viewmodel: ve tay cam vu khi, HUD, minimap, hieu ung man hinh
'  (Mot phan cua Form1 - xem README.md muc 'Cau truc code' de biet
'  toan bo cac file partial class va vai tro cua tung file)
' =====================================================================

Partial Public Class Form1

    Private Function ToArgbColor(c As Color, factor As Double) As Integer
        Return ToArgb(CInt(c.R * factor), CInt(c.G * factor), CInt(c.B * factor))
    End Function

    ' Nhuom mau theo slot (nhan RGB goc voi ti le mau slot) + do bong theo khoang cach.
    ' Anh nhan vat nen ve mau xam/trang nhat de nhuom cho dep (giong ky thuat tint sprite).
    Private Function ShadeAndTint(argb As Integer, tint As Color, factor As Double) As Integer
        Dim r As Integer = CInt(((argb >> 16) And &HFF) * (tint.R / 255.0) * factor)
        Dim g As Integer = CInt(((argb >> 8) And &HFF) * (tint.G / 255.0) * factor)
        Dim b As Integer = CInt((argb And &HFF) * (tint.B / 255.0) * factor)
        Return ToArgb(r, g, b)
    End Function

    Private Function SlotColor(slot As Integer) As Color
        Select Case slot Mod 4
            Case 0 : Return Color.FromArgb(90, 150, 230)   ' Host - xanh duong
            Case 1 : Return Color.FromArgb(220, 90, 90)    ' do
            Case 2 : Return Color.FromArgb(100, 200, 120)  ' xanh la
            Case Else : Return Color.FromArgb(230, 180, 60) ' vang cam
        End Select
    End Function

    Private Function ToArgb(r As Integer, g As Integer, b As Integer) As Integer
        r = Math.Max(0, Math.Min(255, r))
        g = Math.Max(0, Math.Min(255, g))
        b = Math.Max(0, Math.Min(255, b))
        Return &HFF000000 Or (r << 16) Or (g << 8) Or b
    End Function

    Private Sub BlitBuffer()
        Dim rect As New Rectangle(0, 0, RES_W, RES_H)
        Dim bmpData As BitmapData = frameBmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb)
        Marshal.Copy(pixelBuf, 0, bmpData.Scan0, pixelBuf.Length)
        frameBmp.UnlockBits(bmpData)
    End Sub

    Private Sub Form1_Paint(sender As Object, e As PaintEventArgs)
        RenderFrame()
        BlitBuffer()

        e.Graphics.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
        e.Graphics.PixelOffsetMode = Drawing2D.PixelOffsetMode.Half
        e.Graphics.DrawImage(frameBmp, New Rectangle(0, 0, WIN_W, WIN_H))

        DrawViewmodel(e.Graphics)
        DrawInventoryHud(e.Graphics)
        DrawHud(e.Graphics)
        DrawMinimap(e.Graphics)
        DrawDamageFlash(e.Graphics)
    End Sub

    ' Vien man hinh do nhat dan, lam mo khi vua bi trung don, giup nguoi choi
    ' nhan biet ngay ca khi khong nhin thang vao HUD chi so mau.
    Private Sub DrawDamageFlash(g As Graphics)
        Dim elapsed As Double = (DateTime.Now - lastDamageTakenTime).TotalSeconds
        Const FLASH_DURATION As Double = 0.45
        If elapsed >= FLASH_DURATION Then Return
        Dim alpha As Integer = CInt(120.0 * (1.0 - elapsed / FLASH_DURATION))
        Using flashBrush As New SolidBrush(Color.FromArgb(alpha, 200, 20, 20))
            g.FillRectangle(flashBrush, 0, 0, WIN_W, WIN_H)
        End Using
    End Sub

    ' ---------------------------------------------------------------
    '  Ve 2 tay (view model) o goc duoi man hinh kieu FPS (Half-Life...),
    '  co nhun nhe theo nhip di bo, tu "tho" mo/nam nhe khi dung yen,
    '  va nam chat lai khi dang cam do vat. Ve bang GDI+ vector - khi
    '  co texture hand_open.png / hand_fist.png / hand_holding.png that,
    '  chi can thay phan ve nay bang sampling anh, giu nguyen logic gripAmount.
    ' ---------------------------------------------------------------
    Private Sub DrawViewmodel(g As Graphics)
        Dim bobY As Single = CSng(Math.Sin(bobPhase) * 8.0 * bobAmount)
        Dim bobX As Single = CSng(Math.Sin(bobPhase * 0.5) * 5.0 * bobAmount)
        Dim crouchDrop As Single = CSng(crouchAmount * 55.0)
        Dim jumpRise As Single = CSng(playerZ * 45.0)

        Dim isHolding As Boolean = heldItemName <> "(chua trang bi)"
        Dim curItemForHands As ItemDefinition = CurrentItem()

        ' Tay hien thi theo dung slot nhan vat cua chinh minh (localSlot) - moi nhan
        ' vat co bo anh tay rieng (mau da/bao tay/dây quan khac nhau, xem GameAssets.vb).
        Dim handSlot As Integer = localSlot Mod CHARACTER_SLOT_COUNT

        ' Anh tay-cam-vu-khi rieng: moi Item Id co the co bo anh rieng (nhu hand_holding_bow_N),
        ' neu Item chua co bo anh rieng thi tra ve Nothing va DrawHandTextured se tu fallback
        ' ve hand_holding thuong (tay trong, khong thay vu khi) - khong crash, khong bat buoc
        ' phai co du anh cho moi vu khi.
        ' Cung: cam bang tay TRAI (dung tu the ban cung thuc te, tay phai keo day - xem
        ' bowDrawT/pullBack ben duoi). Dao/kiem: cam bang tay PHAI (tay thuan), dung ban
        ' da LAT GUONG vi anh goc ve theo khung tay trai giong hand_open_N.
        Dim leftOverrideImg As Bitmap = Nothing
        Dim rightOverrideImg As Bitmap = Nothing
        If curItemForHands IsNot Nothing Then
            Select Case curItemForHands.Id
                Case "bow"
                    ' Dang keo day: doi sang bo anh "cung da cong + co mui ten" va tay phai
                    ' "cac ngon keo day" (neu co ve, thieu thi tu fallback ve anh cung
                    ' thuong / nam dam nhu truoc, khong bat buoc phai co du anh moi).
                    leftOverrideImg = If(isDrawingBow AndAlso handHoldingBowDrawnImgBySlot(handSlot) IsNot Nothing,
                                          handHoldingBowDrawnImgBySlot(handSlot), handHoldingBowImgBySlot(handSlot))
                    If isDrawingBow Then rightOverrideImg = handPullingStringImgMirrorBySlot(handSlot)
                Case "dagger" : rightOverrideImg = handHoldingDaggerImgMirrorBySlot(handSlot)
                Case "sword" : rightOverrideImg = handHoldingSwordImgMirrorBySlot(handSlot)
            End Select
        End If

        ' Do nam tay (0 = mo het, 1 = nam chat): tay trong (khong cam do) giu co dinh,
        ' khong con dao dong/doi anh nua du dung yen hay dang di - tranh roi mat.
        ' Chi khi cam do (isHolding) moi nam chat het co.
        Dim gripAmount As Double = If(isHolding, 1.0, 0.30)
        gripAmount = Math.Max(0.0, Math.Min(1.0, gripAmount))

        ' Khi dang cam do, 2 tay khep lai gan giua man hinh hon (nhu dang giu chung 1 vat)
        Dim handSpread As Double = If(isHolding, 0.34, 0.20)

        ' Hoat anh vung vu khi: 0 -> 1 -> 0 trong suot ATTACK_SWING_DURATION (tay dua len-ra
        ' truoc roi tro ve), dung sin de vao/ra muot thay vi giat cuc.
        Dim swingT As Double = If(isHolding AndAlso attackSwingTime > 0.0, 1.0 - attackSwingTime / ATTACK_SWING_DURATION, 0.0)
        Dim swingPunch As Single = CSng(Math.Sin(swingT * Math.PI))
        Dim swingRise As Single = swingPunch * 70.0F
        Dim swingForward As Single = swingPunch * 45.0F

        ' Hoat anh hai nam: tay phai cui/vuon xuong-vao roi tu tro ve, 0 -> 1 -> 0 trong suot
        ' PICKUP_ANIM_DURATION (nguoc huong voi vung vu khi o tren: xuong thay vi len).
        Dim pickupT As Double = If(pickupAnimTime > 0.0, 1.0 - pickupAnimTime / PICKUP_ANIM_DURATION, 0.0)
        Dim pickupPunch As Single = CSng(Math.Sin(pickupT * Math.PI))
        Dim pickupDip As Single = pickupPunch * 55.0F     ' tay ha xuong bao nhieu px
        Dim pickupIn As Single = pickupPunch * 30.0F      ' tay khep vao giua bao nhieu px

        ' Dang giu chuot trai keo cung: 2 tay nang len cao hon (tu the ngam ban), va tay
        ' phai (tay keo day) lui dan ra xa hon theo thoi gian giu, mo phong keo day cung
        ' ve gan mat - dung lai texture hand_fist (nam chat) san co, khong can ve them anh.
        Dim bowDrawT As Double = 0.0
        If isDrawingBow Then
            Dim elapsed As Double = (DateTime.Now - drawStartTime).TotalSeconds
            bowDrawT = Math.Max(0.0, Math.Min(1.0, elapsed / BOW_MAX_DRAW_SECONDS))
        End If
        Dim aimRise As Single = If(isDrawingBow, 35.0F + CSng(bowDrawT * 20.0), 0.0F)
        Dim pullBack As Single = CSng(bowDrawT * BOW_DRAW_PULLBACK_PX)

        Dim baseY As Single = WIN_H + 20 + crouchDrop - jumpRise - swingRise - aimRise
        Dim leftX As Single = CSng(WIN_W * handSpread) + bobX + swingForward
        Dim rightX As Single = CSng(WIN_W * (1.0 - handSpread)) - bobX - swingForward + pullBack - pickupIn
        Dim leftY As Single = baseY + bobY
        Dim rightY As Single = baseY - bobY - CSng(bowDrawT * 15.0) + pickupDip ' tay keo day nhich len gan ma hon

        If handOpenImgBySlot(handSlot) IsNot Nothing OrElse handFistImgBySlot(handSlot) IsNot Nothing OrElse handHoldingImgBySlot(handSlot) IsNot Nothing Then
            DrawHandTextured(g, leftX, leftY, True, handSlot, gripAmount, isHolding, overrideHoldingImg:=leftOverrideImg)
            DrawHandTextured(g, rightX, rightY, False, handSlot, gripAmount, isHolding OrElse isDrawingBow, forceFist:=isDrawingBow, overrideHoldingImg:=rightOverrideImg)
        Else
            Dim skin As Color = Color.FromArgb(224, 172, 130)
            Dim skinShadow As Color = Color.FromArgb(178, 128, 92)
            Dim sleeve As Color = Color.FromArgb(68, 62, 58)
            Dim sleeveShadow As Color = Color.FromArgb(46, 42, 38)

            DrawOneHand(g, leftX, leftY, True, gripAmount, skin, skinShadow, sleeve, sleeveShadow)
            DrawOneHand(g, rightX, rightY, False, gripAmount, skin, skinShadow, sleeve, sleeveShadow)

            If isHolding Then
                DrawHeldItemPlaceholder(g, WIN_W / 2.0F, baseY - 250 + (bobY - bobY) / 2.0F)
            End If
        End If
    End Sub

    ' Ve 1 ben tay bang anh that: dung yen/di chuyen thi crossfade muot giua hand_open
    ' va hand_fist theo gripAmount; khi dang cam do (isHolding) thi chuyen han sang
    ' hand_holding.png. Neu thieu anh nao thi bo qua lop do (khong crash).
    ' forceFist: dung khi tay dang keo day cung (nam that chat quanh day), uu tien hon
    ' ca hand_holding - vi day khong phai "cam do" ma la "keo cang", nam chat truc quan hon.
    ' overrideHoldingImg: neu co (vd hand_holding_bow.png cho tay giu cung), dung THAY the
    ' cho handHoldingImgBySlot/handHoldingImgMirrorBySlot mac dinh - chi ap dung cho ben goi truyen vao
    ' (thuong la tay trai, anh khong can ban lat vi luon dung o vi tri trai).
    Private Sub DrawHandTextured(g As Graphics, cx As Single, wristY As Single, isLeft As Boolean, handSlot As Integer, gripAmount As Double, isHolding As Boolean, Optional forceFist As Boolean = False, Optional overrideHoldingImg As Bitmap = Nothing)
        Dim imgOpen As Bitmap = If(isLeft, handOpenImgBySlot(handSlot), handOpenImgMirrorBySlot(handSlot))
        Dim imgFist As Bitmap = If(isLeft, handFistImgBySlot(handSlot), handFistImgMirrorBySlot(handSlot))
        Dim imgHolding As Bitmap = If(overrideHoldingImg IsNot Nothing, overrideHoldingImg, If(isLeft, handHoldingImgBySlot(handSlot), handHoldingImgMirrorBySlot(handSlot)))

        Dim refImg As Bitmap = If(imgHolding, If(imgFist, imgOpen))
        If refImg Is Nothing Then Return

        Dim drawH As Single = 360.0F
        Dim aspect As Single = CSng(refImg.Width) / CSng(refImg.Height)
        Dim drawW As Single = drawH * aspect

        ' Canh duoi anh (co tay ao) neo o wristY, nghieng vao giua man hinh
        Dim destX As Single = If(isLeft, cx - drawW * 0.30F, cx - drawW * 0.70F)
        Dim destY As Single = wristY - drawH

        If overrideHoldingImg IsNot Nothing Then
            ' Anh rieng duoc truyen vao ro rang (vd hand_pulling_string luc keo cung) -
            ' uu tien hon ca forceFist, vi day la tu the co chu dich, khong phai nam dam
            ' mac dinh dung tam luc chua co anh rieng.
            g.DrawImage(imgHolding, destX, destY, drawW, drawH)
        ElseIf forceFist AndAlso imgFist IsNot Nothing Then
            g.DrawImage(imgFist, destX, destY, drawW, drawH)
        ElseIf isHolding AndAlso imgHolding IsNot Nothing Then
            g.DrawImage(imgHolding, destX, destY, drawW, drawH)
        Else
            If imgOpen IsNot Nothing Then g.DrawImage(imgOpen, destX, destY, drawW, drawH)
            If imgFist IsNot Nothing Then DrawImageWithAlpha(g, imgFist, destX, destY, drawW, drawH, CSng(gripAmount))
        End If
    End Sub

    ' Ve 1 anh voi do mo (alpha) tuy chinh, dung de crossfade giua 2 tu the tay
    Private Sub DrawImageWithAlpha(g As Graphics, img As Bitmap, x As Single, y As Single, w As Single, h As Single, alpha As Single)
        If alpha <= 0.02F Then Return
        If alpha >= 0.98F Then
            g.DrawImage(img, x, y, w, h)
            Return
        End If
        Dim cm As New ColorMatrix()
        cm.Matrix33 = alpha
        Using attr As New ImageAttributes()
            attr.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap)
            g.DrawImage(img, New Rectangle(CInt(x), CInt(y), CInt(w), CInt(h)), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, attr)
        End Using
    End Sub

    Private Sub DrawOneHand(g As Graphics, cx As Single, wristY As Single, isLeft As Boolean, gripAmount As Double, skin As Color, skinShadow As Color, sleeve As Color, sleeveShadow As Color)
        Dim shadowSide As Single = If(isLeft, -1.0F, 1.0F)

        ' Tay ao (sleeve) tu duoi man hinh di len
        Using sleeveBrush As New SolidBrush(sleeve)
            g.FillRectangle(sleeveBrush, cx - 58, wristY - 190, 116, 190)
        End Using
        Using sleeveShadowBrush As New SolidBrush(sleeveShadow)
            g.FillRectangle(sleeveShadowBrush, cx - 58 + If(shadowSide > 0, 96, 0), wristY - 190, 20, 190)
        End Using

        ' Long ban tay (palm): hoi phinh to ra khi nam chat (mo phong khop ngon tay cuon vao)
        Dim palmBulge As Single = CSng(gripAmount * 10.0)
        Using fistBrush As New SolidBrush(skin)
            g.FillEllipse(fistBrush, cx - 52 - palmBulge / 2, wristY - 230, 104 + palmBulge, 92)
        End Using

        ' 3 ngon tay: khi mo (gripAmount~0) thi vuon dai len tren; khi nam (gripAmount~1)
        ' thi cuon ngan lai gan nhu bien mat vao long ban tay (fist).
        Dim fingerLen As Single = CSng(50.0 * (1.0 - gripAmount) + 8.0 * gripAmount)
        Dim fingerXs() As Single = {cx - 40, cx - 8, cx + 22}
        Using fingerBrush As New SolidBrush(skin)
            For Each fx As Single In fingerXs
                g.FillRectangle(fingerBrush, fx, wristY - 230 - fingerLen, 22, fingerLen + 6)
            Next
        End Using
        ' Bong do dot khop ngon tay ro hon khi nam chat
        Using knuckleBrush As New SolidBrush(skinShadow)
            g.FillEllipse(knuckleBrush, cx - 44, wristY - 212, 28, CSng(20 + gripAmount * 6))
            g.FillEllipse(knuckleBrush, cx - 12, wristY - 218, 28, CSng(20 + gripAmount * 6))
            g.FillEllipse(knuckleBrush, cx + 18, wristY - 208, 26, CSng(18 + gripAmount * 6))
        End Using

        ' Ngon cai: mo thi choia ra ngoai, nam thi khep sat vao long ban tay
        Dim thumbOutward As Single = CSng(20.0 * (1.0 - gripAmount))
        Using thumbBrush As New SolidBrush(skin)
            Dim thumbX As Single = If(isLeft, cx + 30 + thumbOutward, cx - 55 - thumbOutward)
            g.FillEllipse(thumbBrush, thumbX, wristY - 190, 26, CSng(46 - gripAmount * 8))
        End Using
    End Sub

    ' Placeholder cho vat dang cam (chua co texture item that). Khi trang bi item
    ' thuc su, thay hinh khoi xam nay bang sprite item/vu khi tuong ung.
    Private Sub DrawHeldItemPlaceholder(g As Graphics, cx As Single, cy As Single)
        Using itemBrush As New SolidBrush(Color.FromArgb(120, 120, 130))
            g.FillRectangle(itemBrush, cx - 14, cy - 60, 28, 100)
        End Using
        Using highlightBrush As New SolidBrush(Color.FromArgb(160, 160, 175))
            g.FillRectangle(highlightBrush, cx - 14, cy - 60, 8, 100)
        End Using
    End Sub

    ' ---------------------------------------------------------------
    '  Hotbar trang bi kieu Lien Minh Huyen Thoai: o vuong xep hang ngang
    '  giua man hinh phia duoi, o dang chon vien vang, so phim tat goc
    '  tren-trai moi o. Chua co icon rieng thi tu ve mau + chu cai dau.
    ' ---------------------------------------------------------------
    Private Sub DrawInventoryHud(g As Graphics)
        If inventorySlots.Count = 0 Then Return

        Dim slotSize As Integer = 56
        Dim gap As Integer = 8
        Dim totalWidth As Integer = inventorySlots.Count * slotSize + (inventorySlots.Count - 1) * gap
        Dim startX As Integer = (WIN_W - totalWidth) \ 2
        Dim y As Integer = WIN_H - slotSize - 46

        Using fLetter As New Font("Consolas", 18, FontStyle.Bold)
            Using fNum As New Font("Consolas", 9, FontStyle.Bold)
                For i As Integer = 0 To inventorySlots.Count - 1
                    Dim slot As InventorySlot = inventorySlots(i)
                    Dim x As Integer = startX + i * (slotSize + gap)
                    Dim rect As New Rectangle(x, y, slotSize, slotSize)

                    Using bg As New SolidBrush(Color.FromArgb(190, 20, 20, 25))
                        g.FillRectangle(bg, rect)
                    End Using

                    If slot.Item Is Nothing Then
                        Using tb As New SolidBrush(Color.Gainsboro)
                            Dim sz As SizeF = g.MeasureString("-", fLetter)
                            g.DrawString("-", fLetter, tb, x + (slotSize - sz.Width) / 2.0F, y + (slotSize - sz.Height) / 2.0F)
                        End Using
                    Else
                        Dim icon As Bitmap = GetItemIcon(slot.Item)
                        If icon IsNot Nothing Then
                            g.DrawImage(icon, rect)
                        Else
                            Using fb As New SolidBrush(ItemFallbackColor(slot.Item.Kind))
                                g.FillRectangle(fb, x + 4, y + 4, slotSize - 8, slotSize - 8)
                            End Using
                            Using tb As New SolidBrush(Color.White)
                                Dim letter As String = slot.Item.DisplayName.Substring(0, 1)
                                Dim sz As SizeF = g.MeasureString(letter, fLetter)
                                g.DrawString(letter, fLetter, tb, x + (slotSize - sz.Width) / 2.0F, y + (slotSize - sz.Height) / 2.0F)
                            End Using
                        End If
                    End If

                    Dim borderColor As Color = If(i = activeSlotIndex, Color.Gold, Color.FromArgb(200, 90, 90, 100))
                    Dim borderWidth As Single = If(i = activeSlotIndex, 3.0F, 1.0F)
                    Using pen As New Pen(borderColor, borderWidth)
                        g.DrawRectangle(pen, rect)
                    End Using

                    Using shadow As New SolidBrush(Color.Black)
                        g.DrawString(slot.HotkeyNumber.ToString(), fNum, shadow, x + 3, y + 2)
                    End Using
                    Using tb As New SolidBrush(Color.White)
                        g.DrawString(slot.HotkeyNumber.ToString(), fNum, tb, x + 2, y + 1)
                    End Using
                Next
            End Using
        End Using
    End Sub

    Private Sub DrawHud(g As Graphics)
        Using f As New Font("Consolas", 12, FontStyle.Bold)
            Using brush As New SolidBrush(Color.White)
                Using shadow As New SolidBrush(Color.Black)
                    Dim hpText As String = If(playerHealth <= 0, "DA GUC - dang hoi sinh...", "Mau: " & playerHealth & "/" & PLAYER_MAX_HEALTH)
                    Dim mapName As String = If(currentMapIndex >= 0 AndAlso currentMapIndex < MapNames.Length, MapNames(currentMapIndex), "?")
                    Dim hudText As String = String.Format("[{0}]    {1}    Diem: {2}    Cap do: {3}    Toc do: x{4:0.00}    Nam con lai: {5}    Vat pham: {6}    Dung cu: {7}", mapName, hpText, score, level, speedMultiplier, mushrooms.Count, worldItems.Count, heldItemName)
                    g.DrawString(hudText, f, shadow, 11, 11)
                    g.DrawString(hudText, f, brush, 10, 10)
                    Dim hint As String = "Di chuot: xoay | WASD: di chuyen | Chuot phai: nhay | Ctrl/C: ngoi | 1-5: doi do | ESC: thoat"
                    g.DrawString(hint, f, shadow, 11, WIN_H - 29)
                    g.DrawString(hint, f, brush, 10, WIN_H - 30)
                End Using
            End Using
        End Using

        If curNetMode <> NetMode.None Then DrawNetHud(g)
    End Sub

    Private Sub DrawNetHud(g As Graphics)
        Using f As New Font("Consolas", 11, FontStyle.Bold)
            Using shadow As New SolidBrush(Color.Black)
                g.DrawString(netStatusText, f, shadow, 11, 39)
                Using statusBrush As New SolidBrush(Color.LightGreen)
                    g.DrawString(netStatusText, f, statusBrush, 10, 38)
                End Using

                Dim y As Integer = 60
                Using ownBrush As New SolidBrush(SlotColor(localSlot))
                    Dim ownLine As String = "Ban (#" & localSlot & "): " & score & " diem - " & playerHealth & " HP"
                    g.DrawString(ownLine, f, shadow, 11, y + 1)
                    g.DrawString(ownLine, f, ownBrush, 10, y)
                End Using
                y += 20
                For Each kv In remotePlayers
                    Using slotBrush As New SolidBrush(SlotColor(kv.Key))
                        Dim line As String = "Nguoi choi #" & kv.Key & ": " & kv.Value.Score & " diem - " & kv.Value.Health & " HP"
                        g.DrawString(line, f, shadow, 11, y + 1)
                        g.DrawString(line, f, slotBrush, 10, y)
                    End Using
                    y += 20
                Next
            End Using
        End Using
    End Sub

    Private Sub DrawMinimap(g As Graphics)
        Dim cell As Integer = If(MAP_W > 16, 6, 10)
        Dim ox As Integer = WIN_W - MAP_W * cell - 14
        Dim oy As Integer = 14

        Using bg As New SolidBrush(Color.FromArgb(180, 0, 0, 0))
            g.FillRectangle(bg, ox - 4, oy - 4, MAP_W * cell + 8, MAP_H * cell + 8)
        End Using

        For y As Integer = 0 To MAP_H - 1
            For x As Integer = 0 To MAP_W - 1
                If mapData(y, x) > 0 Then
                    Dim col As Color
                    Select Case mapData(y, x)
                        Case 2
                            col = Color.MediumPurple
                        Case 3
                            col = Color.SandyBrown
                        Case 4
                            col = Color.LightSkyBlue
                        Case Else
                            col = Color.Gray
                    End Select
                    Using b As New SolidBrush(col)
                        g.FillRectangle(b, ox + x * cell, oy + y * cell, cell - 1, cell - 1)
                    End Using
                End If
            Next
        Next

        Using yb As New SolidBrush(Color.Gold)
            For Each m As MushroomItem In mushrooms
                g.FillEllipse(yb, ox + m.Pos.X * cell - 2, oy + m.Pos.Y * cell - 2, 4, 4)
            Next
        End Using

        Using ib As New SolidBrush(Color.Silver)
            For Each w As WorldItemSpawn In worldItems
                g.FillRectangle(ib, ox + w.Pos.X * cell - 2, oy + w.Pos.Y * cell - 2, 4, 4)
            Next
        End Using

        Using pb As New SolidBrush(Color.Lime)
            Dim px As Single = ox + CSng(playerX) * cell
            Dim py As Single = oy + CSng(playerY) * cell
            g.FillEllipse(pb, px - 3, py - 3, 6, 6)
            Dim dx As Single = CSng(Math.Cos(playerAngle) * 10)
            Dim dy As Single = CSng(Math.Sin(playerAngle) * 10)
            Using pen As New Pen(Color.Lime, 2)
                g.DrawLine(pen, px, py, px + dx, py + dy)
            End Using
        End Using

        If curNetMode <> NetMode.None Then
            For Each kv In remotePlayers
                Using rb As New SolidBrush(SlotColor(kv.Key))
                    g.FillEllipse(rb, ox + CSng(kv.Value.X) * cell - 3, oy + CSng(kv.Value.Y) * cell - 3, 6, 6)
                End Using
            Next
        End If
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            gameTimer.Stop()
            gameTimer.Dispose()
            If frameBmp IsNot Nothing Then frameBmp.Dispose()
            For i As Integer = 0 To CHARACTER_SLOT_COUNT - 1
                If handOpenImgBySlot(i) IsNot Nothing Then handOpenImgBySlot(i).Dispose()
                If handFistImgBySlot(i) IsNot Nothing Then handFistImgBySlot(i).Dispose()
                If handHoldingImgBySlot(i) IsNot Nothing Then handHoldingImgBySlot(i).Dispose()
                If handHoldingBowImgBySlot(i) IsNot Nothing Then handHoldingBowImgBySlot(i).Dispose()
                If handHoldingBowDrawnImgBySlot(i) IsNot Nothing Then handHoldingBowDrawnImgBySlot(i).Dispose()
                If handPullingStringImgBySlot(i) IsNot Nothing Then handPullingStringImgBySlot(i).Dispose()
                If handPullingStringImgMirrorBySlot(i) IsNot Nothing Then handPullingStringImgMirrorBySlot(i).Dispose()
                If handHoldingDaggerImgBySlot(i) IsNot Nothing Then handHoldingDaggerImgBySlot(i).Dispose()
                If handHoldingSwordImgBySlot(i) IsNot Nothing Then handHoldingSwordImgBySlot(i).Dispose()
                If handHoldingDaggerImgMirrorBySlot(i) IsNot Nothing Then handHoldingDaggerImgMirrorBySlot(i).Dispose()
                If handHoldingSwordImgMirrorBySlot(i) IsNot Nothing Then handHoldingSwordImgMirrorBySlot(i).Dispose()
                If handOpenImgMirrorBySlot(i) IsNot Nothing Then handOpenImgMirrorBySlot(i).Dispose()
                If handFistImgMirrorBySlot(i) IsNot Nothing Then handFistImgMirrorBySlot(i).Dispose()
                If handHoldingImgMirrorBySlot(i) IsNot Nothing Then handHoldingImgMirrorBySlot(i).Dispose()
            Next
            If hub IsNot Nothing Then hub.StopListening()
            If peer IsNot Nothing Then peer.CloseConnection()
        End If
        ReleaseMouseLook()
        MyBase.Dispose(disposing)
    End Sub

End Class
