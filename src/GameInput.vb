Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

' =====================================================================
'  Form1 - Input: ban phim, chuot, mouse-look, di chuyen, va cham tuong
'  (Mot phan cua Form1 - xem README.md muc 'Cau truc code' de biet
'  toan bo cac file partial class va vai tro cua tung file)
' =====================================================================

Partial Public Class Form1

    Private Sub Form1_KeyDown(sender As Object, e As KeyEventArgs)
        pressedKeys.Add(e.KeyCode)
        If e.KeyCode = Keys.Escape Then Me.Close()

        Select Case e.KeyCode
            Case Keys.D1, Keys.NumPad1 : EquipSlot(0)
            Case Keys.D2, Keys.NumPad2 : EquipSlot(1)
            Case Keys.D3, Keys.NumPad3 : EquipSlot(2)
            Case Keys.D4, Keys.NumPad4 : EquipSlot(3)
            Case Keys.D5, Keys.NumPad5 : EquipSlot(4)
            Case Keys.Delete : DropHeldItem()
        End Select
    End Sub

    Private Sub Form1_KeyUp(sender As Object, e As KeyEventArgs)
        pressedKeys.Remove(e.KeyCode)
    End Sub

    Private Sub Form1_MouseDown(sender As Object, e As MouseEventArgs)
        If e.Button = MouseButtons.Right Then
            jumpRequested = True
        ElseIf e.Button = MouseButtons.Left Then
            Dim item As ItemDefinition = CurrentItem()
            If item IsNot Nothing AndAlso item.Kind = ItemKind.Weapon AndAlso item.IsRanged Then
                BeginBowDraw(item) ' vu khi tam xa (cung): bam xuong chi len day, chua ban
            Else
                UseHeldItem() ' can chien / binh thuoc: giu nguyen hanh vi bam la dung ngay
            End If
        End If
    End Sub

    ' Nha chuot trai ra: neu dang keo cung thi phat ban that su xay ra o day (xem ReleaseBowDraw).
    Private Sub Form1_MouseUp(sender As Object, e As MouseEventArgs)
        If e.Button = MouseButtons.Left Then ReleaseBowDraw()
    End Sub

    ' Bat dau giu chuot tren cung: chi "len day" (dang animation), CHUA tao mui ten.
    ' Kiem tra cooldown ngay tu day (giong het cach UseHeldItem cu kiem tra) de khong
    ' cho keo neu vu khi con dang hoi sau phat truoc.
    Private Sub BeginBowDraw(item As ItemDefinition)
        If playerHealth <= 0 Then Return
        If isDrawingBow Then Return ' da dang keo roi, giu them khong lam gi
        Dim cooldownSec As Double = If(item.Cooldown > 0, item.Cooldown, 0.5)
        If (DateTime.Now - lastAttackTime).TotalSeconds < cooldownSec Then Return
        isDrawingBow = True
        drawStartTime = DateTime.Now
        drawingItem = item
    End Sub

    ' Nha chuot ra: neu chua giu du BOW_MIN_DRAW_SECONDS thi HUY hoan toan (khong ban,
    ' khong tru cooldown/ten) - dung y "tha som thi khong an gi ca". Giu cang lau (den
    ' moc BOW_MAX_DRAW_SECONDS) thi phat ban cang manh/nhanh, xem cong thuc trong FireProjectile.
    Private Sub ReleaseBowDraw()
        If Not isDrawingBow Then Return
        isDrawingBow = False
        Dim item As ItemDefinition = drawingItem
        drawingItem = Nothing
        If item Is Nothing OrElse playerHealth <= 0 Then Return

        Dim elapsed As Double = (DateTime.Now - drawStartTime).TotalSeconds
        If elapsed < BOW_MIN_DRAW_SECONDS Then Return ' nha qua som, huy phat ban

        Dim chargeT As Double = (elapsed - BOW_MIN_DRAW_SECONDS) / (BOW_MAX_DRAW_SECONDS - BOW_MIN_DRAW_SECONDS)
        chargeT = Math.Max(0.0, Math.Min(1.0, chargeT))

        lastAttackTime = DateTime.Now
        attackSwingTime = ATTACK_SWING_DURATION
        FireProjectile(item, chargeT)
    End Sub

    ' Huy hoat anh keo dang do (khong ban, khong tru cooldown) - dung khi doi vu khi
    ' (bam phim so khac) hoac nguoi choi guc giua luc dang giu chuot trai keo cung.
    Private Sub CancelBowDraw()
        isDrawingBow = False
        drawingItem = Nothing
    End Sub

    ' ---- Mouse-look: khoa va an con tro, xoay playerAngle theo do lech chuot ----
    Private Sub Form1_Activated(sender As Object, e As EventArgs)
        EngageMouseLook()
    End Sub

    Private Sub Form1_Deactivate(sender As Object, e As EventArgs)
        ReleaseMouseLook()
    End Sub

    Private Sub EngageMouseLook()
        mouseLookEnabled = True
        If Not cursorCaptured Then
            Cursor.Hide()
            cursorCaptured = True
        End If
        Cursor.Clip = Me.Bounds
        CenterCursor()
    End Sub

    Private Sub ReleaseMouseLook()
        mouseLookEnabled = False
        Cursor.Clip = Rectangle.Empty
        If cursorCaptured Then
            Cursor.Show()
            cursorCaptured = False
        End If
    End Sub

    Private Sub CenterCursor()
        Dim center As Point = Me.PointToScreen(New Point(Me.ClientSize.Width \ 2, Me.ClientSize.Height \ 2))
        ignoreNextMouseMove = True
        Cursor.Position = center
    End Sub

    Private Sub Form1_MouseMove(sender As Object, e As MouseEventArgs)
        If Not mouseLookEnabled Then Return
        If ignoreNextMouseMove Then
            ignoreNextMouseMove = False
            Return
        End If
        Dim centerX As Integer = Me.ClientSize.Width \ 2
        Dim centerY As Integer = Me.ClientSize.Height \ 2
        Dim dx As Integer = e.X - centerX
        Dim dy As Integer = e.Y - centerY
        If dx <> 0 Then
            playerAngle += dx * MOUSE_SENSITIVITY
        End If
        If dy <> 0 Then
            ' dy < 0 (chuot len) -> ngua len -> pitchShiftPx tang (thay them tran/tren)
            Dim pitchSign As Integer = If(INVERT_MOUSE_PITCH, -1, 1)
            pitchShiftPx -= CInt(dy * MOUSE_PITCH_SENSITIVITY) * pitchSign
            pitchShiftPx = Math.Max(-PITCH_MAX_PX, Math.Min(PITCH_MAX_PX, pitchShiftPx))
        End If
        If dx <> 0 OrElse dy <> 0 Then CenterCursor()
    End Sub

    ' Cho nang cap sau: goc de xu ly danh/ban khi da co item/vu khi trang bi ben chuot trai.
    ' Vi du: neu equipped item la vu khi thi phat animation chem/ban + kiem tra va cham voi doi thu (PvP).
    Private Sub HandleInput(dt As Double)
        If playerHealth <= 0 Then Return ' da guc, cho goi RESPAWN tu Host, khong di chuyen duoc

        Dim dirX As Double = Math.Cos(playerAngle)
        Dim dirY As Double = Math.Sin(playerAngle)

        ' ---- Ngoi xuong (giu Ctrl hoac C), noi suy muot cho khong bi giat ----
        Dim wantCrouch As Boolean = pressedKeys.Contains(Keys.ControlKey) OrElse pressedKeys.Contains(Keys.C)
        Dim crouchTarget As Double = If(wantCrouch, 1.0, 0.0)
        If crouchAmount < crouchTarget Then
            crouchAmount = Math.Min(crouchTarget, crouchAmount + CROUCH_LERP_SPEED * dt)
        ElseIf crouchAmount > crouchTarget Then
            crouchAmount = Math.Max(crouchTarget, crouchAmount - CROUCH_LERP_SPEED * dt)
        End If

        ' ---- Nhay (chuot phai), khong the nhay khi dang ngoi ----
        If jumpRequested AndAlso Not isJumping AndAlso crouchAmount < 0.5 Then
            isJumping = True
            zVelocity = JUMP_SPEED
        End If
        jumpRequested = False
        If isJumping Then
            playerZ += zVelocity * dt
            zVelocity -= GRAVITY * dt
            If playerZ <= 0.0 Then
                playerZ = 0.0
                zVelocity = 0.0
                isJumping = False
            End If
        End If

        ' Ghi chu: viewShiftPx (offset man hinh do nhay/ngoi/buc/chuot ngua-cui) duoc tinh
        ' 1 lan duy nhat o cuoi ham nay, sau khi da biet standHeight cua o vua di toi.

        ' Di chuyen cham hon khi dang ngoi
        Dim crouchSpeedFactor As Double = 1.0 - crouchAmount * 0.5
        Dim moveStep As Double = moveSpeed * speedMultiplier * crouchSpeedFactor * dt


        ' ---- Hoat anh nhun tay khi di bo ----
        Dim isMoving As Boolean = pressedKeys.Contains(Keys.W) OrElse pressedKeys.Contains(Keys.Up) OrElse
                                   pressedKeys.Contains(Keys.S) OrElse pressedKeys.Contains(Keys.Down) OrElse
                                   pressedKeys.Contains(Keys.A) OrElse pressedKeys.Contains(Keys.D) OrElse
                                   pressedKeys.Contains(Keys.Left) OrElse pressedKeys.Contains(Keys.Right)
        Dim bobTarget As Double = If(isMoving AndAlso Not isJumping, 1.0, 0.0)
        If bobAmount < bobTarget Then
            bobAmount = Math.Min(bobTarget, bobAmount + BOB_LERP_SPEED * dt)
        Else
            bobAmount = Math.Max(bobTarget, bobAmount - BOB_LERP_SPEED * dt)
        End If
        If isMoving Then bobPhase += BOB_SPEED * dt * crouchSpeedFactor
        idlePhase += IDLE_BREATH_SPEED * dt
        worldTime += dt

        Dim newX As Double = playerX
        Dim newY As Double = playerY

        If pressedKeys.Contains(Keys.W) OrElse pressedKeys.Contains(Keys.Up) Then
            newX += dirX * moveStep : newY += dirY * moveStep
        End If
        If pressedKeys.Contains(Keys.S) OrElse pressedKeys.Contains(Keys.Down) Then
            newX -= dirX * moveStep : newY -= dirY * moveStep
        End If
        If pressedKeys.Contains(Keys.A) OrElse pressedKeys.Contains(Keys.Left) Then
            newX += dirY * moveStep : newY -= dirX * moveStep
        End If
        If pressedKeys.Contains(Keys.D) OrElse pressedKeys.Contains(Keys.Right) Then
            newX -= dirY * moveStep : newY += dirX * moveStep
        End If

        ' Va cham truot theo tung truc rieng de co the luot doc tuong
        If IsWalkable(newX, playerY) Then playerX = newX
        If IsWalkable(playerX, newY) Then playerY = newY

        ' ---- Buc/bac cao (loai o 5, 6): noi suy muot chieu cao nen theo o dang dung ----
        Dim standMx As Integer = CInt(Math.Floor(playerX))
        Dim standMy As Integer = CInt(Math.Floor(playerY))
        Dim standCellType As Integer = 0
        If standMx >= 0 AndAlso standMx < MAP_W AndAlso standMy >= 0 AndAlso standMy < MAP_H Then
            standCellType = mapData(standMy, standMx)
        End If
        Dim targetStandHeight As Double = CellFloorHeight(standCellType)
        If standHeight < targetStandHeight Then
            standHeight = Math.Min(targetStandHeight, standHeight + STAND_HEIGHT_LERP_SPEED * dt)
        Else
            standHeight = Math.Max(targetStandHeight, standHeight - STAND_HEIGHT_LERP_SPEED * dt)
        End If

        ' Cong them do cao buc vao offset camera (da tinh o tren, truoc khi biet o dung moi -
        ' cap nhat lai viewShiftPx voi standHeight moi nhat de khong bi cham 1 frame)
        viewShiftPx = CInt((playerZ + standHeight - crouchAmount * CROUCH_MAX_HEIGHT) * VIEW_SHIFT_SCALE) + pitchShiftPx
    End Sub

    Private Function IsWalkable(x As Double, y As Double) As Boolean
        Dim mx As Integer = CInt(Math.Floor(x))
        Dim my As Integer = CInt(Math.Floor(y))
        If mx < 0 OrElse mx >= MAP_W OrElse my < 0 OrElse my >= MAP_H Then Return False
        Dim cellType As Integer = mapData(my, mx)
        Select Case cellType
            Case 0, 5, 6 ' san thuong, buc thap, buc cao: luon di vao duoc, khong can nhay
                Return True
            Case 3 ' kien hang thap: chi qua duoc khi dang nhay du cao
                Return playerZ >= CRATE_JUMP_HEIGHT
            Case 4 ' khe chui: chi qua duoc khi dang ngoi du thap
                Return crouchAmount >= CROUCH_PASS_THRESHOLD
            Case Else ' tuong da (1, 2) luon chan
                Return False
        End Select
    End Function

    ' Do cao san cua 1 loai o (0 = san thuong / khe chui / kien hang, dung khi dang o duoi/qua duoc).
    ' Dung de noi suy standHeight khi nguoi choi buoc len/xuong buc (loai 5, 6).
    Private Function CellFloorHeight(cellType As Integer) As Double
        Select Case cellType
            Case 5 : Return PLATFORM_LOW_HEIGHT
            Case 6 : Return PLATFORM_HIGH_HEIGHT
            Case Else : Return 0.0
        End Select
    End Function

End Class
