Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

' =====================================================================
'  GamePvP3D_HaiNam - Ban test single-player
'  Pseudo-3D raycasting (kieu Wolfenstein) ve bang GDI+ thuan, khong
'  can thu vien ngoai, bien dich bang vbc.exe / .NET Framework 4.x.
'
'  Y tuong: nhan vat di trong me cung, nhat nam de tang diem, du
'  MUSHROOMS_PER_LEVEL nam thi len cap va duoc buff toc do.
'  Cho nang cap sau: thay speedMultiplier bang he thong item/skill that,
'  hoac gan them NetworkHub.vb / NetworkPeer.vb (theo kien truc star-topology
'  da dung o cac game GamePvP khac) de lam PvP nhieu nguoi choi cung map.
' =====================================================================

Public Class Form1
    Inherits Form

    ' ==== Cau hinh render ====
    Private Const RES_W As Integer = 320
    Private Const RES_H As Integer = 200
    Private Const WIN_W As Integer = 960
    Private Const WIN_H As Integer = 600
    Private Const FOV_SCALE As Double = 0.66

    ' ==== Ban do (0 = trong, 1 = tuong da, 2 = tuong da tim) ====
    Private Const MAP_W As Integer = 16
    Private Const MAP_H As Integer = 16
    Private ReadOnly mapData As Integer(,) = {
        {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
        {1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1},
        {1, 0, 2, 2, 0, 0, 1, 0, 1, 1, 1, 1, 1, 0, 0, 1},
        {1, 0, 2, 2, 0, 0, 1, 0, 1, 0, 0, 0, 1, 0, 0, 1},
        {1, 0, 0, 0, 0, 0, 1, 0, 1, 0, 2, 0, 1, 0, 0, 1},
        {1, 0, 1, 1, 1, 0, 1, 0, 1, 0, 2, 0, 1, 0, 0, 1},
        {1, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 1},
        {1, 0, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1},
        {1, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
        {1, 0, 1, 0, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1},
        {1, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 1},
        {1, 1, 1, 0, 1, 0, 1, 0, 2, 2, 2, 0, 0, 1, 0, 1},
        {1, 0, 0, 0, 0, 0, 1, 0, 2, 2, 2, 0, 0, 1, 0, 1},
        {1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1},
        {1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
        {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1}
    }

    ' ==== Nguoi choi ====
    Private playerX As Double = 1.5
    Private playerY As Double = 1.5
    Private playerAngle As Double = 0.3
    Private moveSpeed As Double = 2.6
    Private rotSpeed As Double = 2.2
    Private speedMultiplier As Double = 1.0

    ' ==== Nhay / ngoi (mo phong bang do lech camera theo chieu doc) ====
    Private playerZ As Double = 0.0        ' do cao hien tai khi nhay, 0 = duoi dat
    Private zVelocity As Double = 0.0
    Private isJumping As Boolean = False
    Private crouchAmount As Double = 0.0   ' 0 = dung thang, 1 = ngoi het co (noi suy muot)
    Private viewShiftPx As Integer = 0     ' offset man hinh theo chieu doc, tinh lai moi frame
    Private Const GRAVITY As Double = 9.0
    Private Const JUMP_SPEED As Double = 3.2
    Private Const CROUCH_LERP_SPEED As Double = 7.0
    Private Const CROUCH_MAX_HEIGHT As Double = 0.35
    Private Const VIEW_SHIFT_SCALE As Double = 70.0

    ' ==== Trang thai game ====
    Private mushrooms As New List(Of PointF)
    Private rng As New Random()
    Private score As Integer = 0
    Private level As Integer = 1
    Private mushroomsThisLevel As Integer = 0
    Private Const MUSHROOMS_PER_LEVEL As Integer = 10

    ' ==== Input ====
    Private pressedKeys As New HashSet(Of Keys)

    ' ==== Bo dem render ====
    Private pixelBuf(RES_W * RES_H - 1) As Integer
    Private zBuffer(RES_W - 1) As Double
    Private frameBmp As Bitmap
    Private WithEvents gameTimer As New Timer()
    Private lastTick As DateTime

    Public Sub New()
        Me.Text = "GamePvP 3D - Phieu Luu Hai Nam (Test Build)"
        Me.ClientSize = New Size(WIN_W, WIN_H)
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.KeyPreview = True
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer, True)

        frameBmp = New Bitmap(RES_W, RES_H, PixelFormat.Format32bppRgb)

        AddHandler Me.KeyDown, AddressOf Form1_KeyDown
        AddHandler Me.KeyUp, AddressOf Form1_KeyUp
        AddHandler Me.Paint, AddressOf Form1_Paint

        SpawnMushrooms(20)

        lastTick = DateTime.Now
        gameTimer.Interval = 16
    End Sub

    Private Sub Form1_KeyDown(sender As Object, e As KeyEventArgs)
        pressedKeys.Add(e.KeyCode)
        If e.KeyCode = Keys.Escape Then Me.Close()
    End Sub

    Private Sub Form1_KeyUp(sender As Object, e As KeyEventArgs)
        pressedKeys.Remove(e.KeyCode)
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        gameTimer.Start()
    End Sub

    ' ---------------------------------------------------------------
    '  Sinh nam ngau nhien vao cac o trong, tranh sinh sat nguoi choi
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
                    mushrooms.Add(New PointF(mx + 0.5F, my + 0.5F))
                    placed += 1
                End If
            End If
        End While
    End Sub

    Private Sub gameTimer_Tick(sender As Object, e As EventArgs) Handles gameTimer.Tick
        Dim now As DateTime = DateTime.Now
        Dim dt As Double = (now - lastTick).TotalSeconds
        lastTick = now
        If dt > 0.1 Then dt = 0.1

        HandleInput(dt)
        CheckPickup()
        Me.Invalidate()
    End Sub

    Private Sub HandleInput(dt As Double)
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

        ' ---- Nhay (Space), khong the nhay khi dang ngoi ----
        If pressedKeys.Contains(Keys.Space) AndAlso Not isJumping AndAlso crouchAmount < 0.5 Then
            isJumping = True
            zVelocity = JUMP_SPEED
        End If
        If isJumping Then
            playerZ += zVelocity * dt
            zVelocity -= GRAVITY * dt
            If playerZ <= 0.0 Then
                playerZ = 0.0
                zVelocity = 0.0
                isJumping = False
            End If
        End If

        ' Camera offset man hinh: nhay len -> the gioi lech xuong, ngoi -> the gioi lech len
        viewShiftPx = CInt((playerZ - crouchAmount * CROUCH_MAX_HEIGHT) * VIEW_SHIFT_SCALE)

        ' Di chuyen cham hon khi dang ngoi
        Dim crouchSpeedFactor As Double = 1.0 - crouchAmount * 0.5
        Dim moveStep As Double = moveSpeed * speedMultiplier * crouchSpeedFactor * dt

        If pressedKeys.Contains(Keys.Left) Then playerAngle -= rotSpeed * dt
        If pressedKeys.Contains(Keys.Right) Then playerAngle += rotSpeed * dt

        Dim newX As Double = playerX
        Dim newY As Double = playerY

        If pressedKeys.Contains(Keys.W) OrElse pressedKeys.Contains(Keys.Up) Then
            newX += dirX * moveStep : newY += dirY * moveStep
        End If
        If pressedKeys.Contains(Keys.S) OrElse pressedKeys.Contains(Keys.Down) Then
            newX -= dirX * moveStep : newY -= dirY * moveStep
        End If
        If pressedKeys.Contains(Keys.A) Then
            newX += dirY * moveStep : newY -= dirX * moveStep
        End If
        If pressedKeys.Contains(Keys.D) Then
            newX -= dirY * moveStep : newY += dirX * moveStep
        End If

        ' Va cham truot theo tung truc rieng de co the luot doc tuong
        If IsWalkable(newX, playerY) Then playerX = newX
        If IsWalkable(playerX, newY) Then playerY = newY
    End Sub

    Private Function IsWalkable(x As Double, y As Double) As Boolean
        Dim mx As Integer = CInt(Math.Floor(x))
        Dim my As Integer = CInt(Math.Floor(y))
        If mx < 0 OrElse mx >= MAP_W OrElse my < 0 OrElse my >= MAP_H Then Return False
        Return mapData(my, mx) = 0
    End Function

    Private Sub CheckPickup()
        Dim i As Integer = mushrooms.Count - 1
        While i >= 0
            Dim m As PointF = mushrooms(i)
            Dim ddx As Double = m.X - playerX
            Dim ddy As Double = m.Y - playerY
            If (ddx * ddx + ddy * ddy) < 0.16 Then
                mushrooms.RemoveAt(i)
                score += 10
                mushroomsThisLevel += 1
                If mushroomsThisLevel >= MUSHROOMS_PER_LEVEL Then
                    mushroomsThisLevel = 0
                    level += 1
                    speedMultiplier += 0.15
                    ' TODO (nang cap sau): mo khoa item/skill that thay vi chi tang toc do
                End If
                If mushrooms.Count < 5 Then SpawnMushrooms(10)
            End If
            i -= 1
        End While
    End Sub

    ' ---------------------------------------------------------------
    '  Raycasting: ve tuong tren pixelBuf (do phan giai noi bo RES_W x RES_H)
    ' ---------------------------------------------------------------
    Private Sub RenderFrame()
        Dim skyColor As Integer = ToArgb(40, 40, 70)
        Dim floorColor As Integer = ToArgb(50, 40, 30)
        Dim horizon As Integer = RES_H \ 2 + viewShiftPx
        If horizon < 0 Then horizon = 0
        If horizon > RES_H Then horizon = RES_H
        For y As Integer = 0 To RES_H - 1
            Dim c As Integer = If(y < horizon, skyColor, floorColor)
            Dim rowOff As Integer = y * RES_W
            For x As Integer = 0 To RES_W - 1
                pixelBuf(rowOff + x) = c
            Next
        Next

        Dim dirX As Double = Math.Cos(playerAngle)
        Dim dirY As Double = Math.Sin(playerAngle)
        Dim planeX As Double = -dirY * FOV_SCALE
        Dim planeY As Double = dirX * FOV_SCALE

        For x As Integer = 0 To RES_W - 1
            Dim cameraX As Double = 2.0 * x / RES_W - 1.0
            Dim rayDirX As Double = dirX + planeX * cameraX
            Dim rayDirY As Double = dirY + planeY * cameraX

            Dim mapX As Integer = CInt(Math.Floor(playerX))
            Dim mapY As Integer = CInt(Math.Floor(playerY))

            Dim deltaDistX As Double = If(rayDirX = 0, 1.0E+30, Math.Abs(1.0 / rayDirX))
            Dim deltaDistY As Double = If(rayDirY = 0, 1.0E+30, Math.Abs(1.0 / rayDirY))

            Dim stepX As Integer, stepY As Integer
            Dim sideDistX As Double, sideDistY As Double

            If rayDirX < 0 Then
                stepX = -1 : sideDistX = (playerX - mapX) * deltaDistX
            Else
                stepX = 1 : sideDistX = (mapX + 1.0 - playerX) * deltaDistX
            End If
            If rayDirY < 0 Then
                stepY = -1 : sideDistY = (playerY - mapY) * deltaDistY
            Else
                stepY = 1 : sideDistY = (mapY + 1.0 - playerY) * deltaDistY
            End If

            Dim hit As Boolean = False
            Dim side As Integer = 0
            Dim safety As Integer = 0
            While Not hit AndAlso safety < 64
                safety += 1
                If sideDistX < sideDistY Then
                    sideDistX += deltaDistX : mapX += stepX : side = 0
                Else
                    sideDistY += deltaDistY : mapY += stepY : side = 1
                End If
                If mapX < 0 OrElse mapX >= MAP_W OrElse mapY < 0 OrElse mapY >= MAP_H Then
                    hit = True
                ElseIf mapData(mapY, mapX) > 0 Then
                    hit = True
                End If
            End While

            Dim perpWallDist As Double
            If side = 0 Then
                perpWallDist = (mapX - playerX + (1 - stepX) / 2.0) / If(rayDirX = 0, 0.000000001, rayDirX)
            Else
                perpWallDist = (mapY - playerY + (1 - stepY) / 2.0) / If(rayDirY = 0, 0.000000001, rayDirY)
            End If
            If perpWallDist < 0.05 Then perpWallDist = 0.05
            zBuffer(x) = perpWallDist

            Dim lineHeight As Integer = CInt(RES_H / perpWallDist)
            Dim drawStart As Integer = Math.Max(0, -lineHeight \ 2 + RES_H \ 2 + viewShiftPx)
            Dim drawEnd As Integer = Math.Min(RES_H - 1, lineHeight \ 2 + RES_H \ 2 + viewShiftPx)

            Dim wallType As Integer = 1
            If mapX >= 0 AndAlso mapX < MAP_W AndAlso mapY >= 0 AndAlso mapY < MAP_H Then
                wallType = mapData(mapY, mapX)
                If wallType = 0 Then wallType = 1
            End If

            Dim baseR As Integer, baseG As Integer, baseB As Integer
            Select Case wallType
                Case 2
                    baseR = 120 : baseG = 70 : baseB = 160
                Case Else
                    baseR = 150 : baseG = 100 : baseB = 60
            End Select
            If side = 1 Then
                baseR = CInt(baseR * 0.7) : baseG = CInt(baseG * 0.7) : baseB = CInt(baseB * 0.7)
            End If

            Dim fog As Double = Math.Max(0.25, 1.0 - perpWallDist / 12.0)
            Dim col As Integer = ToArgb(CInt(baseR * fog), CInt(baseG * fog), CInt(baseB * fog))

            For y As Integer = drawStart To drawEnd
                pixelBuf(y * RES_W + x) = col
            Next
        Next

        DrawMushroomSprites(dirX, dirY, planeX, planeY)
    End Sub

    ' ---------------------------------------------------------------
    '  Ve nam theo kieu billboard sprite (luon quay mat ve camera)
    ' ---------------------------------------------------------------
    Private Sub DrawMushroomSprites(dirX As Double, dirY As Double, planeX As Double, planeY As Double)
        Dim sorted As New List(Of PointF)(mushrooms)
        sorted.Sort(Function(a, b)
                        Dim da As Double = (a.X - playerX) ^ 2 + (a.Y - playerY) ^ 2
                        Dim db As Double = (b.X - playerX) ^ 2 + (b.Y - playerY) ^ 2
                        Return db.CompareTo(da)
                    End Function)

        Dim invDet As Double = 1.0 / (planeX * dirY - dirX * planeY)

        For Each m As PointF In sorted
            Dim sx As Double = m.X - playerX
            Dim sy As Double = m.Y - playerY

            Dim transformX As Double = invDet * (dirY * sx - dirX * sy)
            Dim transformY As Double = invDet * (-planeY * sx + planeX * sy)

            If transformY <= 0.1 Then Continue For

            Dim spriteScreenX As Integer = CInt((RES_W / 2.0) * (1.0 + transformX / transformY))
            Dim spriteSize As Integer = CInt(Math.Abs(RES_H / transformY))
            If spriteSize <= 0 Then Continue For

            Dim drawStartY As Integer = Math.Max(0, -spriteSize \ 2 + RES_H \ 2 + viewShiftPx)
            Dim drawEndY As Integer = Math.Min(RES_H - 1, spriteSize \ 2 + RES_H \ 2 + viewShiftPx)
            Dim drawStartX As Integer = Math.Max(0, -spriteSize \ 2 + spriteScreenX)
            Dim drawEndX As Integer = Math.Min(RES_W - 1, spriteSize \ 2 + spriteScreenX)

            For stripe As Integer = drawStartX To drawEndX
                If transformY >= zBuffer(stripe) Then Continue For
                Dim texX As Double = (stripe - (spriteScreenX - spriteSize / 2.0)) / spriteSize
                For y As Integer = drawStartY To drawEndY
                    Dim texY As Double = (y - drawStartY) / CDbl(Math.Max(1, drawEndY - drawStartY))
                    Dim col As Integer = MushroomPixel(texX, texY)
                    If col <> 0 Then pixelBuf(y * RES_W + stripe) = col
                Next
            Next
        Next
    End Sub

    ' Sprite nam ve thu cong bang toa do u,v trong [0,1] (0 = trong suot)
    Private Function MushroomPixel(u As Double, v As Double) As Integer
        Dim cx As Double = u - 0.5
        Dim cy As Double = v - 0.35
        Dim capDist As Double = Math.Sqrt(cx * cx + (cy * 1.3) * (cy * 1.3))
        If v < 0.55 AndAlso capDist < 0.42 Then
            Dim spot1 As Double = Math.Sqrt((u - 0.35) ^ 2 + (v - 0.2) ^ 2)
            Dim spot2 As Double = Math.Sqrt((u - 0.62) ^ 2 + (v - 0.28) ^ 2)
            If spot1 < 0.06 OrElse spot2 < 0.05 Then Return ToArgb(255, 255, 255)
            Return ToArgb(220, 40, 40)
        ElseIf v >= 0.5 AndAlso v < 0.85 AndAlso Math.Abs(u - 0.5) < 0.15 Then
            Return ToArgb(230, 210, 170)
        End If
        Return 0
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

        DrawHud(e.Graphics)
        DrawMinimap(e.Graphics)
    End Sub

    Private Sub DrawHud(g As Graphics)
        Using f As New Font("Consolas", 12, FontStyle.Bold)
            Using brush As New SolidBrush(Color.White)
                Using shadow As New SolidBrush(Color.Black)
                    Dim hudText As String = String.Format("Diem: {0}    Cap do: {1}    Toc do: x{2:0.00}    Nam con lai: {3}", score, level, speedMultiplier, mushrooms.Count)
                    g.DrawString(hudText, f, shadow, 11, 11)
                    g.DrawString(hudText, f, brush, 10, 10)
                    Dim hint As String = "WASD: di chuyen | Left/Right: xoay | Space: nhay | Ctrl/C: ngoi | ESC: thoat"
                    g.DrawString(hint, f, shadow, 11, WIN_H - 29)
                    g.DrawString(hint, f, brush, 10, WIN_H - 30)
                End Using
            End Using
        End Using
    End Sub

    Private Sub DrawMinimap(g As Graphics)
        Dim cell As Integer = 10
        Dim ox As Integer = WIN_W - MAP_W * cell - 14
        Dim oy As Integer = 14

        Using bg As New SolidBrush(Color.FromArgb(180, 0, 0, 0))
            g.FillRectangle(bg, ox - 4, oy - 4, MAP_W * cell + 8, MAP_H * cell + 8)
        End Using

        For y As Integer = 0 To MAP_H - 1
            For x As Integer = 0 To MAP_W - 1
                If mapData(y, x) > 0 Then
                    Dim col As Color = If(mapData(y, x) = 2, Color.MediumPurple, Color.Gray)
                    Using b As New SolidBrush(col)
                        g.FillRectangle(b, ox + x * cell, oy + y * cell, cell - 1, cell - 1)
                    End Using
                End If
            Next
        Next

        Using yb As New SolidBrush(Color.Gold)
            For Each m As PointF In mushrooms
                g.FillEllipse(yb, ox + m.X * cell - 2, oy + m.Y * cell - 2, 4, 4)
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
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            gameTimer.Stop()
            gameTimer.Dispose()
            If frameBmp IsNot Nothing Then frameBmp.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

End Class

' =====================================================================
'  Entry point
' =====================================================================
Module MainModule
    <STAThread()>
    Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.Run(New Form1())
    End Sub
End Module
