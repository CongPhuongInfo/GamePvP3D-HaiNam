Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

' =====================================================================
'  Form1 - Render: raycasting DDA va ve sprite (nam, item, nguoi choi, dan)
'  (Mot phan cua Form1 - xem README.md muc 'Cau truc code' de biet
'  toan bo cac file partial class va vai tro cua tung file)
' =====================================================================

Partial Public Class Form1

    ' ---------------------------------------------------------------
    '  Anh sang diem tu den duoc (torch light): tra ve muc do sang cong
    '  them (0..1) tai 1 toa do the gioi, co nhap nhay nhe theo thoi gian.
    ' ---------------------------------------------------------------
    Private Function TorchLightAmount(wx As Double, wy As Double) As Double
        Dim total As Double = 0.0
        For Each t As TorchLight In torchLights
            Dim dx As Double = wx - t.X
            Dim dy As Double = wy - t.Y
            Dim d As Double = Math.Sqrt(dx * dx + dy * dy)
            If d < t.Radius Then
                Dim flicker As Double = 0.85 + 0.15 * Math.Sin(worldTime * 9.0 + t.FlickerSeed)
                total += (1.0 - d / t.Radius) * flicker
            End If
        Next
        Return Math.Min(1.0, total)
    End Function

    ' ---------------------------------------------------------------
    '  To mau pixel theo do sang khoang cach (fog) + anh sang den (litAmount),
    '  dong thoi pha dan mau ve FOG_COLOR khi o xa - tao chieu sau thay vi
    '  chi lam toi don thuan nhu ShadeColor goc.
    ' ---------------------------------------------------------------
    Private Function ShadeColorFogLit(srcColor As Integer, fog As Double, litAmount As Double) As Integer
        Dim brightness As Double = Math.Min(1.15, fog + litAmount * TORCH_BRIGHTNESS)
        Dim r As Integer = (srcColor >> 16) And &HFF
        Dim g As Integer = (srcColor >> 8) And &HFF
        Dim b As Integer = srcColor And &HFF
        r = CInt(r * brightness)
        g = CInt(g * brightness)
        b = CInt(b * brightness)

        Dim fogMix As Double = Math.Max(0.0, 1.0 - fog) * 0.6
        r = CInt(r * (1.0 - fogMix) + FOG_COLOR_R * fogMix)
        g = CInt(g * (1.0 - fogMix) + FOG_COLOR_G * fogMix)
        b = CInt(b * (1.0 - fogMix) + FOG_COLOR_B * fogMix)

        Return ToArgb(r, g, b)
    End Function

    ' ---------------------------------------------------------------
    '  Ve bong do gia hinh elip mo tren san ngay duoi 1 sprite, tao cam
    '  giac vat "dinh" xuong dat thay vi lo lung. Goi truoc khi ve pixel
    '  sprite chinh (de bong nam duoi, sprite ve de len tren).
    ' ---------------------------------------------------------------
    Private Sub DrawGroundShadow(centerScreenX As Integer, feetScreenY As Integer, widthPx As Integer, dist As Double)
        If dist >= SHADOW_MAX_DIST Then Return
        Dim shadowW As Integer = Math.Max(2, CInt(widthPx * 0.5))
        Dim shadowH As Integer = Math.Max(1, shadowW \ 3)
        Dim x0 As Integer = Math.Max(0, centerScreenX - shadowW)
        Dim x1 As Integer = Math.Min(RES_W - 1, centerScreenX + shadowW)
        Dim y0 As Integer = Math.Max(0, feetScreenY - shadowH)
        Dim y1 As Integer = Math.Min(RES_H - 1, feetScreenY + shadowH)
        For sy As Integer = y0 To y1
            Dim ny As Double = (sy - feetScreenY) / CDbl(Math.Max(1, shadowH))
            Dim rowOff As Integer = sy * RES_W
            For sx As Integer = x0 To x1
                If dist >= zBuffer(sx) Then Continue For
                Dim nx As Double = (sx - centerScreenX) / CDbl(Math.Max(1, shadowW))
                Dim r2 As Double = nx * nx + ny * ny
                If r2 > 1.0 Then Continue For
                Dim darkenAmt As Double = (1.0 - r2) * 0.5
                Dim idx As Integer = rowOff + sx
                Dim c As Integer = pixelBuf(idx)
                Dim rr As Integer = CInt(((c >> 16) And &HFF) * (1.0 - darkenAmt))
                Dim gg As Integer = CInt(((c >> 8) And &HFF) * (1.0 - darkenAmt))
                Dim bb As Integer = CInt((c And &HFF) * (1.0 - darkenAmt))
                pixelBuf(idx) = &HFF000000 Or (rr << 16) Or (gg << 8) Or bb
            Next
        Next
    End Sub

    ' ---------------------------------------------------------------
    '  Toi nhe 4 goc man hinh (vignette), goi 1 lan cuoi cung sau khi ve
    '  xong toan bo canh + sprite, truoc khi Blit ra bitmap.
    ' ---------------------------------------------------------------
    Private Sub ApplyVignette()
        Dim cx As Double = RES_W / 2.0
        Dim cy As Double = RES_H / 2.0
        Dim maxDist As Double = Math.Sqrt(cx * cx + cy * cy)
        For y As Integer = 0 To RES_H - 1
            Dim dy As Double = y - cy
            Dim rowOff As Integer = y * RES_W
            For x As Integer = 0 To RES_W - 1
                Dim dx As Double = x - cx
                Dim dist As Double = Math.Sqrt(dx * dx + dy * dy) / maxDist
                If dist > VIGNETTE_START Then
                    Dim t As Double = Math.Min(1.0, (dist - VIGNETTE_START) / (1.0 - VIGNETTE_START))
                    Dim darken As Double = 1.0 - t * VIGNETTE_STRENGTH
                    Dim idx As Integer = rowOff + x
                    Dim c As Integer = pixelBuf(idx)
                    Dim r As Integer = CInt(((c >> 16) And &HFF) * darken)
                    Dim g As Integer = CInt(((c >> 8) And &HFF) * darken)
                    Dim b As Integer = CInt((c And &HFF) * darken)
                    pixelBuf(idx) = &HFF000000 Or (r << 16) Or (g << 8) Or b
                End If
            Next
        Next
    End Sub

    ' ---------------------------------------------------------------
    '  Sinh texture vo cay va san co BANG CONG THUC, khong can file anh
    '  nao ca (cung tinh than voi GrassPixel/FlowerPixel da co san).
    '  Goi 1 lan trong Sub New, NGAY SAU LoadTextures() - se GHI DE
    '  texFloor da nap tu floor.png (neu co) bang co/dat rung.
    ' ---------------------------------------------------------------
    ' ---------------------------------------------------------------
    '  Thu nap 1 file anh that tu Assets\Forest\<fileName> (128x128 ARGB).
    '  Tra ve Nothing neu file khong ton tai, loi khi doc, hoac SAI kich
    '  thuoc (bat buoc dung 128x128 de khop voi TEX_SIZE, tranh meo hinh).
    ' ---------------------------------------------------------------
    Private Function TryLoadForestTexture(fileName As String) As Integer()
        Dim fullPath As String = System.IO.Path.Combine(Application.StartupPath, "Assets", "Forest", fileName)
        If Not System.IO.File.Exists(fullPath) Then Return Nothing
        Try
            Using bmp As New Bitmap(fullPath)
                If bmp.Width <> TEX_SIZE OrElse bmp.Height <> TEX_SIZE Then Return Nothing
                Dim result(TEX_SIZE * TEX_SIZE - 1) As Integer
                Dim rect As New Rectangle(0, 0, TEX_SIZE, TEX_SIZE)
                Dim bd As BitmapData = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)
                Marshal.Copy(bd.Scan0, result, 0, TEX_SIZE * TEX_SIZE)
                bmp.UnlockBits(bd)
                Return result
            End Using
        Catch
            Return Nothing
        End Try
    End Function

    ' Giong TryLoadForestTexture nhung KHONG bat buoc 128x128 (dung cho sprite billboard
    ' nhu cay nen - kich thuoc that duoc tra ve qua outW/outH de sample dung ti le).
    Private Function TryLoadForestSprite(fileName As String, ByRef outW As Integer, ByRef outH As Integer) As Integer()
        Dim fullPath As String = System.IO.Path.Combine(Application.StartupPath, "Assets", "Forest", fileName)
        If Not System.IO.File.Exists(fullPath) Then Return Nothing
        Try
            Using bmp As New Bitmap(fullPath)
                outW = bmp.Width
                outH = bmp.Height
                Dim result(outW * outH - 1) As Integer
                Dim rect As New Rectangle(0, 0, outW, outH)
                Dim bd As BitmapData = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)
                Marshal.Copy(bd.Scan0, result, 0, outW * outH)
                bmp.UnlockBits(bd)
                Return result
            End Using
        Catch
            Return Nothing
        End Try
    End Function

    Private Sub GenerateForestTextures()
        ' ---- Vo cay (loai o 7) ----
        Dim realBark() As Integer = TryLoadForestTexture("tree_bark.png")
        If realBark IsNot Nothing Then
            texTreeBark = realBark
        Else
            Dim barkRng As New Random(20260719)
            texTreeBark = New Integer(TEX_SIZE * TEX_SIZE - 1) {}
            For y As Integer = 0 To TEX_SIZE - 1
                For x As Integer = 0 To TEX_SIZE - 1
                    Dim ridge As Double = Math.Sin(x * 0.35 + Math.Sin(y * 0.05) * 2.0) * 0.5 + 0.5
                    Dim noise As Double = barkRng.NextDouble() * 0.15
                    Dim shade As Double = 0.45 + ridge * 0.35 + noise
                    Dim r As Integer = Math.Min(255, CInt(92 * shade))
                    Dim g As Integer = Math.Min(255, CInt(64 * shade))
                    Dim b As Integer = Math.Min(255, CInt(42 * shade))
                    texTreeBark(y * TEX_SIZE + x) = ToArgb(r, g, b)
                Next
            Next
        End If

        ' ---- Bui co ram (loai o 8) - dung lam vien ban do ngoai troi thay cho tuong da,
        '      de co cam giac "het duong vi bi co rung chan" thay vi "dung tuong". Tha
        '      Assets\Forest\bush_wall.png (anh that, ne 128x128) de ghi de len ban tu sinh. ----
        Dim realBush() As Integer = TryLoadForestTexture("bush_wall.png")
        If realBush IsNot Nothing Then
            texBushWall = realBush
        Else
            Dim bushRng As New Random(20260721)
            texBushWall = New Integer(TEX_SIZE * TEX_SIZE - 1) {}
            For y As Integer = 0 To TEX_SIZE - 1
                For x As Integer = 0 To TEX_SIZE - 1
                    ' Nhieu cum la chong len nhau (vai tan so sin khac nhau) de trong
                    ' "ram rap" thay vi mot mau phang - gan giong bui/tan la thap.
                    Dim clump As Double = Math.Sin(x * 0.5 + y * 0.31) * 0.5 +
                                           Math.Sin(x * 0.19 - y * 0.47 + 1.7) * 0.3 +
                                           Math.Sin((x + y) * 0.61) * 0.2
                    clump = clump * 0.5 + 0.5
                    Dim noise As Double = bushRng.NextDouble() * 0.2
                    Dim shade As Double = 0.35 + clump * 0.45 + noise
                    ' Thinh thoang diem toi de gia lam khe ho giua cac cum la.
                    Dim gap As Boolean = bushRng.NextDouble() < 0.05
                    If gap Then shade *= 0.5
                    Dim r As Integer = Math.Min(255, CInt(58 * shade))
                    Dim g As Integer = Math.Min(255, CInt(96 * shade))
                    Dim b As Integer = Math.Min(255, CInt(40 * shade))
                    texBushWall(y * TEX_SIZE + x) = ToArgb(r, g, b)
                Next
            Next
        End If

        ' ---- Vach da (loai o 9) ----
        Dim realCliff() As Integer = TryLoadForestTexture("cliff_wall.png")
        If realCliff IsNot Nothing Then
            texCliffWall = realCliff
        Else
            Dim cliffRng As New Random(20260722)
            texCliffWall = New Integer(TEX_SIZE * TEX_SIZE - 1) {}
            For y As Integer = 0 To TEX_SIZE - 1
                For x As Integer = 0 To TEX_SIZE - 1
                    Dim facet As Double = Math.Sin(x * 0.22 + y * 0.09) * 0.5 + Math.Sin(y * 0.31 - x * 0.05) * 0.3
                    facet = facet * 0.5 + 0.5
                    Dim noise As Double = cliffRng.NextDouble() * 0.2
                    Dim shade As Double = 0.4 + facet * 0.4 + noise
                    Dim r As Integer = Math.Min(255, CInt(120 * shade))
                    Dim g As Integer = Math.Min(255, CInt(114 * shade))
                    Dim b As Integer = Math.Min(255, CInt(100 * shade))
                    texCliffWall(y * TEX_SIZE + x) = ToArgb(r, g, b)
                Next
            Next
        End If

        ' ---- Khe nut dat (loai o 10) ----
        Dim realCrevice() As Integer = TryLoadForestTexture("crevice_wall.png")
        If realCrevice IsNot Nothing Then
            texCreviceWall = realCrevice
        Else
            Dim crevRng As New Random(20260723)
            texCreviceWall = New Integer(TEX_SIZE * TEX_SIZE - 1) {}
            For y As Integer = 0 To TEX_SIZE - 1
                For x As Integer = 0 To TEX_SIZE - 1
                    Dim crack As Double = Math.Sin(x * 0.4 + y * 0.15) * Math.Sin(y * 0.28 - x * 0.12)
                    Dim noise As Double = crevRng.NextDouble() * 0.18
                    Dim shade As Double = 0.5 + crack * 0.25 + noise
                    Dim r As Integer = Math.Min(255, CInt(150 * shade))
                    Dim g As Integer = Math.Min(255, CInt(112 * shade))
                    Dim b As Integer = Math.Min(255, CInt(80 * shade))
                    texCreviceWall(y * TEX_SIZE + x) = ToArgb(r, g, b)
                Next
            Next
        End If

        ' ---- Bo suoi (loai o 11) ----
        Dim realRiverbank() As Integer = TryLoadForestTexture("riverbank_wall.png")
        If realRiverbank IsNot Nothing Then
            texRiverbankWall = realRiverbank
        Else
            Dim riverRng As New Random(20260724)
            texRiverbankWall = New Integer(TEX_SIZE * TEX_SIZE - 1) {}
            For y As Integer = 0 To TEX_SIZE - 1
                For x As Integer = 0 To TEX_SIZE - 1
                    Dim ripple As Double = Math.Sin(x * 0.3 + y * 0.06) * 0.5 + 0.5
                    Dim noise As Double = riverRng.NextDouble() * 0.2
                    Dim shade As Double = 0.4 + ripple * 0.35 + noise
                    Dim r As Integer = Math.Min(255, CInt(90 * shade))
                    Dim g As Integer = Math.Min(255, CInt(110 * shade))
                    Dim b As Integer = Math.Min(255, CInt(120 * shade))
                    texRiverbankWall(y * TEX_SIZE + x) = ToArgb(r, g, b)
                Next
            Next
        End If

        ' ---- San co ----
        Dim realFloor() As Integer = TryLoadForestTexture("forest_floor.png")
        If realFloor IsNot Nothing Then
            texFloor = realFloor
        Else
            Dim floorRng As New Random(20260720)
            texFloor = New Integer(TEX_SIZE * TEX_SIZE - 1) {}
            For y As Integer = 0 To TEX_SIZE - 1
                For x As Integer = 0 To TEX_SIZE - 1
                    Dim n As Double = (Math.Sin(x * 0.18 + y * 0.23) + Math.Sin(x * 0.07 - y * 0.11)) * 0.5
                    n = n * 0.5 + 0.5
                    Dim r As Integer, g As Integer, b As Integer
                    If floorRng.NextDouble() < 0.12 Then
                        Dim s As Double = 0.6 + n * 0.3
                        r = CInt(110 * s) : g = CInt(80 * s) : b = CInt(50 * s)
                    Else
                        Dim s As Double = 0.55 + n * 0.45
                        r = CInt(45 * s) : g = CInt(95 * s) : b = CInt(35 * s)
                    End If
                    texFloor(y * TEX_SIZE + x) = ToArgb(Math.Min(255, r), Math.Min(255, g), Math.Min(255, b))
                Next
            Next
        End If

        ' ---- Khuc go do (loai o 3) ----
        Dim realLog() As Integer = TryLoadForestTexture("fallen_log.png")
        If realLog IsNot Nothing Then
            texCrate = realLog
        Else
            Dim logRng As New Random(20260721)
            texCrate = New Integer(TEX_SIZE * TEX_SIZE - 1) {}
            For y As Integer = 0 To TEX_SIZE - 1
                For x As Integer = 0 To TEX_SIZE - 1
                    Dim ring As Double = Math.Sin(y * 0.55 + Math.Sin(x * 0.04) * 3.0) * 0.5 + 0.5
                    Dim noise As Double = logRng.NextDouble() * 0.12
                    Dim shade As Double = 0.4 + ring * 0.4 + noise
                    Dim r As Integer = Math.Min(255, CInt(120 * shade))
                    Dim g As Integer = Math.Min(255, CInt(84 * shade))
                    Dim b As Integer = Math.Min(255, CInt(50 * shade))
                    texCrate(y * TEX_SIZE + x) = ToArgb(r, g, b)
                Next
            Next
        End If

        ' ---- Khe da phu reu (loai o 4) ----
        Dim realRock() As Integer = TryLoadForestTexture("mossy_rock.png")
        If realRock IsNot Nothing Then
            texVent = realRock
        Else
            Dim rockRng As New Random(20260722)
            texVent = New Integer(TEX_SIZE * TEX_SIZE - 1) {}
            For y As Integer = 0 To TEX_SIZE - 1
                For x As Integer = 0 To TEX_SIZE - 1
                    Dim n As Double = (Math.Sin(x * 0.12 + y * 0.09) + Math.Sin(x * 0.21 - y * 0.17)) * 0.5 + 0.5
                    Dim mossChance As Double = rockRng.NextDouble()
                    Dim r As Integer, g As Integer, b As Integer
                    If mossChance < 0.3 + n * 0.2 Then
                        Dim s As Double = 0.5 + n * 0.4
                        r = CInt(55 * s) : g = CInt(95 * s) : b = CInt(45 * s)
                    Else
                        Dim s As Double = 0.5 + n * 0.35
                        r = CInt(110 * s) : g = CInt(108 * s) : b = CInt(102 * s)
                    End If
                    texVent(y * TEX_SIZE + x) = ToArgb(Math.Min(255, r), Math.Min(255, g), Math.Min(255, b))
                Next
            Next
        End If

        ' ---- Cay nen trang tri (sprite billboard, Kind=2) - anh khong bat buoc 128x128 ----
        texTree = TryLoadForestSprite("tree_billboard.png", texTreeW, texTreeH)
    End Sub

    Private Sub RenderFrame()
        Dim skyColor As Integer = ToArgb(120, 172, 224) ' troi xanh ban ngay thay vi mau xanh dem cu
        Dim horizon As Integer = RES_H \ 2 + viewShiftPx
        If horizon < 0 Then horizon = 0
        If horizon > RES_H Then horizon = RES_H

        Dim dirX As Double = Math.Cos(playerAngle)
        Dim dirY As Double = Math.Sin(playerAngle)
        Dim planeX As Double = -dirY * FOV_SCALE
        Dim planeY As Double = dirX * FOV_SCALE

        ' ---- Troi: sample tu anh toan canh (xoay theo goc nhin) neu co, khong thi mau phang ----
        If texSky IsNot Nothing Then
            For x As Integer = 0 To RES_W - 1
                Dim cameraX As Double = 2.0 * x / RES_W - 1.0
                Dim rayDirX As Double = dirX + planeX * cameraX
                Dim rayDirY As Double = dirY + planeY * cameraX
                Dim angle As Double = Math.Atan2(rayDirY, rayDirX)

                Dim texXf As Double = ((angle + Math.PI) / (2.0 * Math.PI)) * texSkyW
                Dim texX As Integer = CInt(texXf) Mod texSkyW
                If texX < 0 Then texX += texSkyW

                For y As Integer = 0 To horizon - 1
                    Dim texY As Integer = CInt((y / CDbl(Math.Max(1, horizon))) * texSkyH)
                    texY = Math.Max(0, Math.Min(texSkyH - 1, texY))
                    pixelBuf(y * RES_W + x) = texSky(texY * texSkyW + texX)
                Next
            Next
        Else
            For y As Integer = 0 To horizon - 1
                Dim rowOff As Integer = y * RES_W
                For x As Integer = 0 To RES_W - 1
                    pixelBuf(rowOff + x) = skyColor
                Next
            Next
        End If

        ' ---- San nha: floor-casting that su voi texFloor ----
        Dim rayDirX0 As Double = dirX - planeX
        Dim rayDirY0 As Double = dirY - planeY
        Dim rayDirX1 As Double = dirX + planeX
        Dim rayDirY1 As Double = dirY + planeY

        For y As Integer = horizon To RES_H - 1
            Dim p As Integer = y - horizon
            If p < 1 Then p = 1
            Dim rowDistance As Double = (0.5 * RES_H) / p

            Dim floorStepX As Double = rowDistance * (rayDirX1 - rayDirX0) / RES_W
            Dim floorStepY As Double = rowDistance * (rayDirY1 - rayDirY0) / RES_W
            Dim floorX As Double = playerX + rowDistance * rayDirX0
            Dim floorY As Double = playerY + rowDistance * rayDirY0

            Dim rowFog As Double = Math.Max(0.2, 1.0 - rowDistance / mapFogDist)
            Dim rowOff As Integer = y * RES_W

            For x As Integer = 0 To RES_W - 1
                Dim cellX As Integer = CInt(Math.Floor(floorX))
                Dim cellY As Integer = CInt(Math.Floor(floorY))
                Dim tx As Integer = CInt((floorX - cellX) * TEX_SIZE) And (TEX_SIZE - 1)
                Dim ty As Integer = CInt((floorY - cellY) * TEX_SIZE) And (TEX_SIZE - 1)
                Dim litAmount As Double = TorchLightAmount(floorX, floorY)
                floorX += floorStepX
                floorY += floorStepY
                pixelBuf(rowOff + x) = ShadeColorFogLit(texFloor(ty * TEX_SIZE + tx), rowFog, litAmount)
            Next
        Next

        ' ---- Tuong + vat can: raycasting DDA co texture ----
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
            Dim obstacleType As Integer = 0
            Dim obstacleDist As Double = 0.0
            Dim obstacleSide As Integer = 0
            Dim obstacleStepX As Integer = 0, obstacleStepY As Integer = 0
            While Not hit AndAlso safety < 64
                safety += 1
                If sideDistX < sideDistY Then
                    sideDistX += deltaDistX : mapX += stepX : side = 0
                Else
                    sideDistY += deltaDistY : mapY += stepY : side = 1
                End If
                If mapX < 0 OrElse mapX >= MAP_W OrElse mapY < 0 OrElse mapY >= MAP_H Then
                    hit = True
                Else
                    Dim cellType As Integer = mapData(mapY, mapX)
                    If cellType = 1 OrElse cellType = 2 OrElse cellType = 7 OrElse cellType = 8 OrElse cellType = 9 OrElse cellType = 10 OrElse cellType = 11 Then
                        hit = True
                    ElseIf cellType = 3 OrElse cellType = 4 OrElse cellType = 5 OrElse cellType = 6 Then
                        Dim passable As Boolean
                        Select Case cellType
                            Case 3 : passable = playerZ >= CRATE_JUMP_HEIGHT
                            Case 4 : passable = crouchAmount >= CROUCH_PASS_THRESHOLD
                            Case Else : passable = True ' buc 5, 6: luon di/nhin xuyen qua duoc, khong chan tia
                        End Select
                        If passable Then
                            ' Nguoi choi du kien nhay/ngoi qua duoc: khong chan tia, nhung ghi lai
                            ' vi tri de sau do ve khoi nua-chieu-cao dung cho o nay.
                            If obstacleType = 0 Then
                                obstacleType = cellType
                                obstacleSide = side
                                obstacleStepX = stepX
                                obstacleStepY = stepY
                                If side = 0 Then
                                    obstacleDist = (mapX - playerX + (1 - stepX) / 2.0) / If(rayDirX = 0, 0.000000001, rayDirX)
                                Else
                                    obstacleDist = (mapY - playerY + (1 - stepY) / 2.0) / If(rayDirY = 0, 0.000000001, rayDirY)
                                End If
                            End If
                        Else
                            hit = True
                        End If
                    End If
                End If
            End While

            Dim perpWallDist As Double
            If side = 0 Then
                perpWallDist = (mapX - playerX + (1 - stepX) / 2.0) / If(rayDirX = 0, 0.000000001, rayDirX)
            Else
                perpWallDist = (mapY - playerY + (1 - stepY) / 2.0) / If(rayDirY = 0, 0.000000001, rayDirY)
            End If
            If perpWallDist < 0.05 Then perpWallDist = 0.05
            zBuffer(x) = If(obstacleType <> 0, Math.Min(perpWallDist, obstacleDist), perpWallDist)

            Dim lineHeight As Integer = CInt(RES_H / perpWallDist)
            Dim wallScreenTop As Integer = -lineHeight \ 2 + RES_H \ 2
            Dim drawStart As Integer = Math.Max(0, wallScreenTop + viewShiftPx)
            Dim drawEnd As Integer = Math.Min(RES_H - 1, lineHeight \ 2 + RES_H \ 2 + viewShiftPx)

            Dim wallType As Integer = 1
            If mapX >= 0 AndAlso mapX < MAP_W AndAlso mapY >= 0 AndAlso mapY < MAP_H Then
                wallType = mapData(mapY, mapX)
                If wallType = 0 Then wallType = 1
            End If

            Dim tex() As Integer
            Select Case wallType
                Case 2 : tex = texWall2
                Case 3 : tex = texCrate
                Case 4 : tex = texVent
                Case 7 : tex = texTreeBark
                Case 8 : tex = texBushWall
                Case 9 : tex = texCliffWall
                Case 10 : tex = texCreviceWall
                Case 11 : tex = texRiverbankWall
                Case Else : tex = texWall1
            End Select

            ' Toa do X tren texture (vi tri chinh xac tia cham vao tuong)
            Dim wallX As Double
            If side = 0 Then
                wallX = playerY + perpWallDist * rayDirY
            Else
                wallX = playerX + perpWallDist * rayDirX
            End If
            wallX -= Math.Floor(wallX)
            Dim texX As Integer = CInt(wallX * TEX_SIZE)
            If side = 0 AndAlso rayDirX > 0 Then texX = TEX_SIZE - texX - 1
            If side = 1 AndAlso rayDirY < 0 Then texX = TEX_SIZE - texX - 1
            texX = Math.Max(0, Math.Min(TEX_SIZE - 1, texX))

            Dim fog As Double = Math.Max(0.25, 1.0 - perpWallDist / mapFogDist)
            If side = 1 Then fog *= 0.7

            Dim wallWorldX As Double = playerX + perpWallDist * rayDirX
            Dim wallWorldY As Double = playerY + perpWallDist * rayDirY
            Dim wallLit As Double = TorchLightAmount(wallWorldX, wallWorldY)

            For y As Integer = drawStart To drawEnd
                Dim texY As Integer = CInt((y - viewShiftPx - wallScreenTop) * TEX_SIZE / CDbl(Math.Max(1, lineHeight)))
                texY = Math.Max(0, Math.Min(TEX_SIZE - 1, texY))
                pixelBuf(y * RES_W + x) = ShadeColorFogLit(tex(texY * TEX_SIZE + texX), fog, wallLit)
            Next

            ' Ve khoi nua-chieu-cao (kien hang / khe chui) ma tia da "nhin xuyen qua" o tren
            If obstacleType <> 0 Then
                Dim obsLineHeight As Integer = CInt(RES_H / Math.Max(0.05, obstacleDist))
                Dim obsIdealTop As Integer = -obsLineHeight \ 2 + RES_H \ 2
                Dim obsTop As Integer = obsIdealTop + viewShiftPx
                Dim obsBot As Integer = obsLineHeight \ 2 + RES_H \ 2 + viewShiftPx
                Dim obsMid As Integer = (obsTop + obsBot) \ 2

                Dim slabStart As Integer, slabEnd As Integer
                Dim obsTex() As Integer
                Select Case obstacleType
                    Case 3
                        obsTex = texCrate
                        slabStart = obsMid : slabEnd = obsBot           ' kien hang: chiem nua duoi
                    Case 4
                        obsTex = texVent
                        slabStart = obsTop : slabEnd = obsMid            ' khe chui: chiem nua tren
                    Case Else ' 5 (buc thap) hoac 6 (buc cao): khoi noi tu san len, cao theo dung ti le
                        obsTex = If(obstacleType = 6, texWall2, texCrate)
                        Dim hFrac As Double = If(obstacleType = 6, PLATFORM_HIGH_HEIGHT, PLATFORM_LOW_HEIGHT)
                        slabStart = obsBot - CInt((obsBot - obsTop) * hFrac)
                        slabEnd = obsBot
                End Select
                slabStart = Math.Max(0, slabStart)
                slabEnd = Math.Min(RES_H - 1, slabEnd)

                Dim obsWallX As Double
                If obstacleSide = 0 Then
                    obsWallX = playerY + obstacleDist * rayDirY
                Else
                    obsWallX = playerX + obstacleDist * rayDirX
                End If
                obsWallX -= Math.Floor(obsWallX)
                Dim obsTexX As Integer = CInt(obsWallX * TEX_SIZE)
                If obstacleSide = 0 AndAlso rayDirX > 0 Then obsTexX = TEX_SIZE - obsTexX - 1
                If obstacleSide = 1 AndAlso rayDirY < 0 Then obsTexX = TEX_SIZE - obsTexX - 1
                obsTexX = Math.Max(0, Math.Min(TEX_SIZE - 1, obsTexX))

                Dim obsFog As Double = Math.Max(0.25, 1.0 - obstacleDist / mapFogDist)
                If obstacleSide = 1 Then obsFog *= 0.7

                Dim obsWorldX As Double = playerX + obstacleDist * rayDirX
                Dim obsWorldY As Double = playerY + obstacleDist * rayDirY
                Dim obsLit As Double = TorchLightAmount(obsWorldX, obsWorldY)

                For y As Integer = slabStart To slabEnd
                    Dim texY As Integer = CInt((y - viewShiftPx - obsIdealTop) * TEX_SIZE / CDbl(Math.Max(1, obsLineHeight)))
                    texY = Math.Max(0, Math.Min(TEX_SIZE - 1, texY))
                    pixelBuf(y * RES_W + x) = ShadeColorFogLit(obsTex(texY * TEX_SIZE + obsTexX), obsFog, obsLit)
                Next
            End If
        Next

        DrawDecorationSprites(dirX, dirY, planeX, planeY)
        DrawWorldItemSprites(dirX, dirY, planeX, planeY)
        DrawMushroomSprites(dirX, dirY, planeX, planeY)
        DrawRemotePlayerSprites(dirX, dirY, planeX, planeY)
        DrawProjectileSprites(dirX, dirY, planeX, planeY)

        ApplyVignette()
    End Sub

    ' ---------------------------------------------------------------
    '  Ve bui co / cay hoa - trang tri thuan tuy, khong tuong tac. Neo
    '  phan chan xuong dung vi tri san (giong cong thuc floor-casting)
    '  thay vi can giua man hinh nhu nam/nguoi.
    ' ---------------------------------------------------------------
    Private Sub DrawDecorationSprites(dirX As Double, dirY As Double, planeX As Double, planeY As Double)
        If decorations.Count = 0 Then Return

        Dim sorted As New List(Of DecorationItem)(decorations)
        sorted.Sort(Function(a, b)
                        Dim da As Double = (a.Pos.X - playerX) ^ 2 + (a.Pos.Y - playerY) ^ 2
                        Dim db As Double = (b.Pos.X - playerX) ^ 2 + (b.Pos.Y - playerY) ^ 2
                        Return db.CompareTo(da)
                    End Function)

        Dim invDet As Double = 1.0 / (planeX * dirY - dirX * planeY)

        For Each d As DecorationItem In sorted
            Dim sx As Double = d.Pos.X - playerX
            Dim sy As Double = d.Pos.Y - playerY
            Dim transformX As Double = invDet * (dirY * sx - dirX * sy)
            Dim transformY As Double = invDet * (-planeY * sx + planeX * sy)
            If transformY <= 0.1 Then Continue For

            Dim fog As Double = Math.Max(0.3, 1.0 - transformY / mapFogDist)
            Dim screenX As Integer = CInt((RES_W / 2.0) * (1.0 + transformX / transformY))

            ' Chan cay neo dung vao san (cung cong thuc voi floor-casting: p = 0.5*RES_H/dist)
            Dim groundY As Integer = RES_H \ 2 + viewShiftPx + CInt(0.5 * RES_H / transformY)
            Dim worldHeight As Double = If(d.Kind = 2, 1.6, 0.4) * d.Scale ' cay nen cao hon han co/hoa
            Dim size As Integer = CInt((RES_H / transformY) * worldHeight)
            If size <= 0 Then Continue For

            Dim drawStartY As Integer = Math.Max(0, groundY - size)
            Dim drawEndY As Integer = Math.Min(RES_H - 1, groundY)
            Dim drawStartX As Integer = Math.Max(0, screenX - size \ 2)
            Dim drawEndX As Integer = Math.Min(RES_W - 1, screenX + size \ 2)

            Dim useTex() As Integer = If(d.Kind = 0, texGrass, If(d.Kind = 1, texFlower, Nothing))
            Dim texW As Integer = If(d.Kind = 0, texGrassW, texFlowerW)
            Dim texH As Integer = If(d.Kind = 0, texGrassH, texFlowerH)

            For stripe As Integer = drawStartX To drawEndX
                If transformY >= zBuffer(stripe) Then Continue For
                Dim u As Double = (stripe - drawStartX) / CDbl(Math.Max(1, drawEndX - drawStartX))
                For y As Integer = drawStartY To drawEndY
                    Dim v As Double = (y - drawStartY) / CDbl(Math.Max(1, drawEndY - drawStartY))
                    Dim col As Integer = 0

                    If d.Kind = 2 AndAlso texTree IsNot Nothing Then
                        Dim tx As Integer = Math.Max(0, Math.Min(texTreeW - 1, CInt(u * texTreeW)))
                        Dim ty As Integer = Math.Max(0, Math.Min(texTreeH - 1, CInt(v * texTreeH)))
                        Dim srcColor As Integer = texTree(ty * texTreeW + tx)
                        If ((srcColor >> 24) And &HFF) > 128 Then col = srcColor
                    ElseIf d.Kind = 2 Then
                        col = TreePixel(u, v, d.HueSeed)
                    ElseIf useTex IsNot Nothing Then
                        Dim tx As Integer = Math.Max(0, Math.Min(texW - 1, CInt(u * texW)))
                        Dim ty As Integer = Math.Max(0, Math.Min(texH - 1, CInt(v * texH)))
                        Dim srcColor As Integer = useTex(ty * texW + tx)
                        If ((srcColor >> 24) And &HFF) > 128 Then col = srcColor
                    ElseIf d.Kind = 0 Then
                        col = GrassPixel(u, v, d.HueSeed)
                    Else
                        col = FlowerPixel(u, v, d.HueSeed)
                    End If

                    If col <> 0 Then
                        If useTex IsNot Nothing AndAlso d.Kind = 1 Then
                            pixelBuf(y * RES_W + stripe) = ShadeAndTint(col, GetFlowerColor(d.HueSeed), fog)
                        Else
                            pixelBuf(y * RES_W + stripe) = ShadeColor(col, fog)
                        End If
                    End If
                Next
            Next
        Next
    End Sub

    ' Cay nen trang tri (khong chan duong, chi tao chieu sau): than nau hep o duoi,
    ' tan la hinh tron/oval lon phia tren, mau xanh la hoi lech theo HueSeed cho da dang.
    Private Function TreePixel(u As Double, v As Double, hueSeed As Single) As Integer
        If v > 0.78 AndAlso Math.Abs(u - 0.5) < 0.05 Then
            Return ToArgb(70, 48, 30)
        End If
        Dim dx As Double = u - 0.5
        Dim dy As Double = (v - 0.42) * 1.15
        Dim edgeNoise As Double = 0.06 * Math.Sin(Math.Atan2(dy, dx) * 7.0 + hueSeed * 10.0)
        If Math.Sqrt(dx * dx + dy * dy) < 0.40 + edgeNoise Then
            Dim greenVariant As Double = 0.85 + 0.3 * hueSeed
            Return ToArgb(CInt(35 * greenVariant), CInt(95 * greenVariant), CInt(35 * greenVariant))
        End If
        Return 0
    End Function

    ' Bui co: 5 luoi co hep, cao thap khac nhau, hoi nghieng cho tu nhien.
    ' u,v trong [0,1] (v=1 la goc, v=0 la dinh). Tra ve 0 la trong suot.
    Private Function GrassPixel(u As Double, v As Double, hueSeed As Single) As Integer
        Dim bladeCenters() As Double = {0.22, 0.38, 0.5, 0.63, 0.79}
        Dim bladeHeights() As Double = {0.55, 0.85, 1.0, 0.7, 0.5}
        For i As Integer = 0 To bladeCenters.Length - 1
            Dim topV As Double = 1.0 - bladeHeights(i)
            If v >= topV Then
                Dim t As Double = (v - topV) / bladeHeights(i) ' 0 tai dinh la, 1 tai goc
                Dim widthAtV As Double = 0.015 + 0.035 * t
                Dim sway As Double = 0.03 * Math.Sin(t * 2.5 + i + hueSeed * 6.0)
                If Math.Abs(u - (bladeCenters(i) + sway)) < widthAtV Then
                    Dim shade As Double = 0.55 + 0.45 * t
                    Return ToArgb(CInt(55 * shade), CInt(150 * shade), CInt(55 * shade))
                End If
            End If
        Next
        Return 0
    End Function

    ' Cay hoa: than xanh + 1 bong hoa tron o dinh, mau ngau nhien theo HueSeed.
    Private Function FlowerPixel(u As Double, v As Double, hueSeed As Single) As Integer
        If v > 0.32 AndAlso Math.Abs(u - 0.5) < 0.035 Then
            Return ToArgb(40, 110, 45)
        End If
        Dim dist As Double = Math.Sqrt((u - 0.5) ^ 2 + ((v - 0.20) * 1.2) ^ 2)
        If dist < 0.05 Then Return ToArgb(255, 220, 80) ' nhuy hoa
        If dist < 0.16 Then
            Dim petal As Color = GetFlowerColor(hueSeed)
            Return ToArgb(petal.R, petal.G, petal.B)
        End If
        Return 0
    End Function

    Private Function GetFlowerColor(seed As Single) As Color
        Select Case CInt(seed * 4) Mod 4
            Case 0 : Return Color.FromArgb(230, 90, 120)    ' hong
            Case 1 : Return Color.FromArgb(230, 200, 60)    ' vang
            Case 2 : Return Color.FromArgb(180, 110, 220)   ' tim
            Case Else : Return Color.FromArgb(230, 130, 60) ' cam
        End Select
    End Function

    ' ---------------------------------------------------------------
    '  Ve nam theo kieu billboard sprite (luon quay mat ve camera)
    ' ---------------------------------------------------------------
    Private Sub DrawMushroomSprites(dirX As Double, dirY As Double, planeX As Double, planeY As Double)
        Dim sorted As New List(Of MushroomItem)(mushrooms)
        sorted.Sort(Function(a, b)
                        Dim da As Double = (a.Pos.X - playerX) ^ 2 + (a.Pos.Y - playerY) ^ 2
                        Dim db As Double = (b.Pos.X - playerX) ^ 2 + (b.Pos.Y - playerY) ^ 2
                        Return db.CompareTo(da)
                    End Function)

        Dim invDet As Double = 1.0 / (planeX * dirY - dirX * planeY)

        For Each m As MushroomItem In sorted
            Dim sx As Double = m.Pos.X - playerX
            Dim sy As Double = m.Pos.Y - playerY

            Dim transformX As Double = invDet * (dirY * sx - dirX * sy)
            Dim transformY As Double = invDet * (-planeY * sx + planeX * sy)

            If transformY <= 0.1 Then Continue For

            Dim spriteFog As Double = Math.Max(0.3, 1.0 - transformY / mapFogDist)

            Dim spriteScreenX As Integer = CInt((RES_W / 2.0) * (1.0 + transformX / transformY))
            Dim spriteSize As Integer = CInt(Math.Abs(RES_H / transformY) * 0.4)
            If spriteSize <= 0 Then Continue For

            Dim drawStartY As Integer = Math.Max(0, -spriteSize \ 2 + RES_H \ 2 + viewShiftPx)
            Dim drawEndY As Integer = Math.Min(RES_H - 1, spriteSize \ 2 + RES_H \ 2 + viewShiftPx)
            Dim drawStartX As Integer = Math.Max(0, -spriteSize \ 2 + spriteScreenX)
            Dim drawEndX As Integer = Math.Min(RES_W - 1, spriteSize \ 2 + spriteScreenX)

            DrawGroundShadow(spriteScreenX, drawEndY, spriteSize, transformY)

            For stripe As Integer = drawStartX To drawEndX
                If transformY >= zBuffer(stripe) Then Continue For
                Dim texX As Integer = CInt(((stripe - (spriteScreenX - spriteSize / 2.0)) / spriteSize) * TEX_SIZE)
                texX = Math.Max(0, Math.Min(TEX_SIZE - 1, texX))
                For y As Integer = drawStartY To drawEndY
                    Dim texY As Integer = CInt(((y - drawStartY) / CDbl(Math.Max(1, drawEndY - drawStartY))) * TEX_SIZE)
                    texY = Math.Max(0, Math.Min(TEX_SIZE - 1, texY))
                    Dim srcColor As Integer = texMushroom(texY * TEX_SIZE + texX)
                    Dim alpha As Integer = (srcColor >> 24) And &HFF
                    If alpha > 128 Then
                        pixelBuf(y * RES_W + stripe) = ShadeColor(srcColor, spriteFog)
                    End If
                Next
            Next
        Next
    End Sub

    ' ---------------------------------------------------------------
    '  Ve vat pham nam ngoai the gioi (chua nhat) theo kieu billboard
    '  giong nam, dung chinh icon dang hien trong hotbar lam sprite.
    '  Chua co icon rieng thi ve hinh thoi mau theo loai (Weapon/Tool/...).
    ' ---------------------------------------------------------------
    Private Sub DrawWorldItemSprites(dirX As Double, dirY As Double, planeX As Double, planeY As Double)
        If worldItems.Count = 0 Then Return

        Dim sorted As New List(Of WorldItemSpawn)(worldItems)
        sorted.Sort(Function(a, b)
                        Dim da As Double = (a.Pos.X - playerX) ^ 2 + (a.Pos.Y - playerY) ^ 2
                        Dim db As Double = (b.Pos.X - playerX) ^ 2 + (b.Pos.Y - playerY) ^ 2
                        Return db.CompareTo(da)
                    End Function)

        Dim invDet As Double = 1.0 / (planeX * dirY - dirX * planeY)

        For Each w As WorldItemSpawn In sorted
            Dim def As ItemDefinition = Nothing
            If itemCatalog.ContainsKey(w.ItemId) Then def = itemCatalog(w.ItemId)

            Dim sx As Double = w.Pos.X - playerX
            Dim sy As Double = w.Pos.Y - playerY
            Dim transformX As Double = invDet * (dirY * sx - dirX * sy)
            Dim transformY As Double = invDet * (-planeY * sx + planeX * sy)
            If transformY <= 0.1 Then Continue For

            Dim spriteFog As Double = Math.Max(0.3, 1.0 - transformY / mapFogDist)
            Dim spriteScreenX As Integer = CInt((RES_W / 2.0) * (1.0 + transformX / transformY))
            Dim spriteSize As Integer = CInt(Math.Abs(RES_H / transformY) * 0.6)
            If spriteSize <= 0 Then Continue For

            Dim drawStartY As Integer = Math.Max(0, -spriteSize \ 2 + RES_H \ 2 + viewShiftPx)
            Dim drawEndY As Integer = Math.Min(RES_H - 1, spriteSize \ 2 + RES_H \ 2 + viewShiftPx)
            Dim drawStartX As Integer = Math.Max(0, -spriteSize \ 2 + spriteScreenX)
            Dim drawEndX As Integer = Math.Min(RES_W - 1, spriteSize \ 2 + spriteScreenX)

            DrawGroundShadow(spriteScreenX, drawEndY, spriteSize, transformY)

            Dim iconW As Integer = 0, iconH As Integer = 0
            Dim iconPixels() As Integer = GetItemIconPixels(def, iconW, iconH)

            For stripe As Integer = drawStartX To drawEndX
                If transformY >= zBuffer(stripe) Then Continue For
                Dim u As Double = (stripe - (spriteScreenX - spriteSize / 2.0)) / spriteSize
                For y As Integer = drawStartY To drawEndY
                    Dim v As Double = (y - drawStartY) / CDbl(Math.Max(1, drawEndY - drawStartY))
                    Dim col As Integer = 0

                    If iconPixels IsNot Nothing Then
                        Dim tx As Integer = Math.Max(0, Math.Min(iconW - 1, CInt(u * iconW)))
                        Dim ty As Integer = Math.Max(0, Math.Min(iconH - 1, CInt(v * iconH)))
                        Dim srcColor As Integer = iconPixels(ty * iconW + tx)
                        If ((srcColor >> 24) And &HFF) > 128 Then col = srcColor
                    ElseIf def IsNot Nothing Then
                        col = WorldItemFallbackPixel(u, v, def.Kind)
                    End If

                    If col <> 0 Then pixelBuf(y * RES_W + stripe) = ShadeColor(col, spriteFog)
                Next
            Next
        Next
    End Sub

    ' Hinh thoi don gian lam bieu tuong vat pham chung khi chua co icon rieng.
    Private Function WorldItemFallbackPixel(u As Double, v As Double, kind As ItemKind) As Integer
        Dim dist As Double = Math.Abs(u - 0.5) + Math.Abs(v - 0.5) ' khoang cach Manhattan -> hinh thoi
        If dist < 0.35 Then
            Dim c As Color = ItemFallbackColor(kind)
            Return ToArgb(c.R, c.G, c.B)
        End If
        Return 0
    End Function

    ' Chon anh nhan vat theo HUONG NHIN: so sanh huong nguoi choi kia dang quay mat
    ' (rp.Angle - da co san qua dong bo mang, khong can giao thuc moi) voi huong tu
    ' ho toi camera cua minh. Goc lech ~0 nghia la ho dang quay MAT ve phia minh (thay
    ' mat = FRONT); goc lech ~180 do la quay LUNG ve phia minh (BACK); con lai la SIDE,
    ' lat guong (Mirror) tuy theo minh dang o ben trai hay ben phai cua ho.
    Private Function PickDirectionalTexture(slot As Integer, rp As RemotePlayerState, walkFrameOn As Boolean, ByRef outW As Integer, ByRef outH As Integer, ByRef outIsHoldingPose As Boolean) As Integer()
        outIsHoldingPose = False
        Dim idx As Integer = slot Mod CHARACTER_SLOT_COUNT
        Dim viewerDir As Double = Math.Atan2(playerY - rp.Y, playerX - rp.X)
        Dim relAngle As Double = NormalizeAngle(viewerDir - rp.Angle)
        Dim absRel As Double = Math.Abs(relAngle)

        ' Dang cam vu khi (rp.HeldItemId dong bo qua goi POS) - UU TIEN CAO NHAT, truoc ca
        ' jump/crouch/walk, vi day la sprite rieng "dung yen + cam vu khi" dung chung cho
        ' moi tu the (xem giai thich trong Assets\Characters\PROMPTS.md, muc "SPRITE CAM
        ' VU KHI"). Chi ap dung neu THAT SU co anh (Dictionary co key tuong ung); thieu anh
        ' (chua ve/generate) thi roi thang xuong cac nhanh cu ben duoi nhu chua co gi thay
        ' doi - khong bat buoc phai co du 36 anh moi chay duoc.
        If Not String.IsNullOrEmpty(rp.HeldItemId) Then
            Dim dirName As String = "front"
            If absRel > (3.0 * Math.PI / 4.0) Then
                dirName = "back"
            ElseIf absRel >= (Math.PI / 4.0) Then
                dirName = "side"
            End If
            Dim key As String = rp.HeldItemId & "_" & dirName & "_" & idx

            If dirName = "side" Then
                Dim mirrorTex As Integer() = Nothing
                Dim normalTex As Integer() = Nothing
                If texCharacterHolding.TryGetValue(key, normalTex) Then
                    outW = texCharacterHoldingW(key) : outH = texCharacterHoldingH(key)
                    outIsHoldingPose = True
                    If relAngle > 0 Then
                        Return normalTex
                    ElseIf texCharacterHoldingSideMirror.TryGetValue(key, mirrorTex) Then
                        Return mirrorTex
                    Else
                        Return normalTex ' khong co ban lat (khong nen xay ra vi luon nap chung voi ban goc), dung tam ban goc
                    End If
                End If
            Else
                Dim tex As Integer() = Nothing
                If texCharacterHolding.TryGetValue(key, tex) Then
                    outW = texCharacterHoldingW(key) : outH = texCharacterHoldingH(key)
                    outIsHoldingPose = True
                    Return tex
                End If
            End If
            ' Khong tim thay anh cho tren hop nay - roi tiep xuong logic cu (jump/crouch/walk/dung yen tay khong)
        End If

        ' Dang lo lung tren khong (nhay) uu tien hon ca ngoi va di bo - mot nguoi dang nhay thi
        ' khong the dong thoi dang ngoi/buoc chan mot cach hop ly ve mat hinh anh.
        If rp.Z >= REMOTE_JUMP_POSE_THRESHOLD Then
            If absRel < (Math.PI / 4.0) Then
                outW = texCharacterJumpWBySlot(idx) : outH = texCharacterJumpHBySlot(idx)
                Return texCharacterJumpBySlot(idx)
            ElseIf absRel > (3.0 * Math.PI / 4.0) Then
                outW = texCharacterBackJumpWBySlot(idx) : outH = texCharacterBackJumpHBySlot(idx)
                Return texCharacterBackJumpBySlot(idx)
            Else
                outW = texCharacterSideJumpWBySlot(idx) : outH = texCharacterSideJumpHBySlot(idx)
                If relAngle > 0 Then
                    Return texCharacterSideJumpBySlot(idx)
                Else
                    Return texCharacterSideJumpMirrorBySlot(idx)
                End If
            End If
        End If

        ' Dang ngoi (crouch) uu tien hon hoat anh di bo sai chan - nguoi ngoi thi khong doi
        ' frame buoc chan, chi dung anh tu the ngoi cho tung huong.
        If rp.Crouch >= REMOTE_CROUCH_POSE_THRESHOLD Then
            If absRel < (Math.PI / 4.0) Then
                outW = texCharacterCrouchWBySlot(idx) : outH = texCharacterCrouchHBySlot(idx)
                Return texCharacterCrouchBySlot(idx)
            ElseIf absRel > (3.0 * Math.PI / 4.0) Then
                outW = texCharacterBackCrouchWBySlot(idx) : outH = texCharacterBackCrouchHBySlot(idx)
                Return texCharacterBackCrouchBySlot(idx)
            Else
                outW = texCharacterSideCrouchWBySlot(idx) : outH = texCharacterSideCrouchHBySlot(idx)
                If relAngle > 0 Then
                    Return texCharacterSideCrouchBySlot(idx)
                Else
                    Return texCharacterSideCrouchMirrorBySlot(idx)
                End If
            End If
        End If

        If absRel < (Math.PI / 4.0) Then
            If walkFrameOn Then
                outW = texCharacterWalkWBySlot(idx) : outH = texCharacterWalkHBySlot(idx)
                Return texCharacterWalkBySlot(idx)
            Else
                outW = texCharacterWBySlot(idx) : outH = texCharacterHBySlot(idx)
                Return texCharacterBySlot(idx)
            End If
        ElseIf absRel > (3.0 * Math.PI / 4.0) Then
            If walkFrameOn Then
                outW = texCharacterBackWalkWBySlot(idx) : outH = texCharacterBackWalkHBySlot(idx)
                Return texCharacterBackWalkBySlot(idx)
            Else
                outW = texCharacterBackWBySlot(idx) : outH = texCharacterBackHBySlot(idx)
                Return texCharacterBackBySlot(idx)
            End If
        Else
            If walkFrameOn Then
                outW = texCharacterSideWalkWBySlot(idx) : outH = texCharacterSideWalkHBySlot(idx)
                If relAngle > 0 Then
                    Return texCharacterSideWalkBySlot(idx)
                Else
                    Return texCharacterSideWalkMirrorBySlot(idx)
                End If
            Else
                outW = texCharacterSideWBySlot(idx) : outH = texCharacterSideHBySlot(idx)
                If relAngle > 0 Then
                    Return texCharacterSideBySlot(idx)
                Else
                    Return texCharacterSideMirrorBySlot(idx)
                End If
            End If
        End If
    End Function

    ' ---------------------------------------------------------------
    '  Ve nguoi choi khac (PvP) theo kieu billboard giong nam, nhung ve
    '  hinh nguoi don gian bang toan hoc (chua co sprite/texture nhan vat
    '  that) va to mau rieng theo slot de phan biet tung nguoi.
    ' ---------------------------------------------------------------
    Private Sub DrawRemotePlayerSprites(dirX As Double, dirY As Double, planeX As Double, planeY As Double)
        If curNetMode = NetMode.None OrElse remotePlayers.Count = 0 Then Return

        ' Don dep nguoi choi mat ket noi lau (qua 6s khong co goi POS moi)
        Dim now As DateTime = DateTime.Now
        Dim stale As New List(Of Integer)
        For Each kv In remotePlayers
            If (now - kv.Value.LastSeen).TotalSeconds > 6.0 Then stale.Add(kv.Key)
        Next
        For Each k As Integer In stale
            remotePlayers.Remove(k)
        Next

        Dim sorted As New List(Of KeyValuePair(Of Integer, RemotePlayerState))(remotePlayers)
        sorted.Sort(Function(a, b)
                        Dim da As Double = (a.Value.X - playerX) ^ 2 + (a.Value.Y - playerY) ^ 2
                        Dim db As Double = (b.Value.X - playerX) ^ 2 + (b.Value.Y - playerY) ^ 2
                        Return db.CompareTo(da)
                    End Function)

        Dim invDet As Double = 1.0 / (planeX * dirY - dirX * planeY)

        For Each kv In sorted
            Dim rp As RemotePlayerState = kv.Value
            Dim baseColor As Color = SlotColor(kv.Key)

            ' Dang di bo (bob dang len >50%) thi doi frame theo nua chu ky sai chan (Sin doi dau
            ' moi nua vong lap): True = frame "sai chan", False = frame dung yen/2 chan cham dat.
            Dim walkFrameOn As Boolean = rp.BobAmount > 0.5 AndAlso Math.Sin(rp.BobPhase) > 0.0

            Dim texW As Integer = 0, texH As Integer = 0
            Dim isHoldingPose As Boolean = False
            Dim texForSlot As Integer() = PickDirectionalTexture(kv.Key, rp, walkFrameOn, texW, texH, isHoldingPose)

            Dim sx As Double = rp.X - playerX
            Dim sy As Double = rp.Y - playerY
            Dim transformX As Double = invDet * (dirY * sx - dirX * sy)
            Dim transformY As Double = invDet * (-planeY * sx + planeX * sy)
            If transformY <= 0.1 Then Continue For

            Dim spriteFog As Double = Math.Max(0.35, 1.0 - transformY / mapFogDist)
            Dim spriteScreenX As Integer = CInt((RES_W / 2.0) * (1.0 + transformX / transformY))

            ' "Tho" nhe khi dung yen: phong to/thu nho chieu cao theo nhip cham (tat dan khi
            ' bat dau di bo, vi luc do da co bob ro hon roi nen khong can tho nua).
            Dim breathScale As Double = 1.0 + Math.Sin(rp.IdlePhase) * REMOTE_BREATH_AMPLITUDE * (1.0 - rp.BobAmount)

            ' Nguoi cao hon rong, khac ti le vuong cua nam. Khi dang ngoi (Crouch) thi thap
            ' di theo REMOTE_CROUCH_HEIGHT_SCALE, noi suy muot theo rp.Crouch (0..1) chu
            ' khong "nhay cap" dot ngot luc vua bam Ctrl.
            Dim crouchHeightFactor As Double = 1.0 - rp.Crouch * (1.0 - REMOTE_CROUCH_HEIGHT_SCALE)
            Dim spriteHeight As Integer = CInt(Math.Abs(RES_H / transformY) * 1.35 * breathScale * crouchHeightFactor)
            Dim spriteWidth As Integer = CInt(spriteHeight * 0.55)
            If spriteHeight <= 0 OrElse spriteWidth <= 0 Then Continue For

            ' Nhun len xuong khi dang di bo (2 lan nhun moi chu ky sai chan, giong dang di that),
            ' cong don voi do lech do nhay (Z) da co san. Ngoi thi khong nhun buoc chan (BobAmount
            ' se tu ve 0 vi nguoi ngoi khong the vua di nhanh), nhung tru them mot khoang theo
            ' Crouch de chan sprite "dinh" xuong san thay vi lo lung khi than nguoi thap lai.
            Dim walkBobPx As Double = Math.Abs(Math.Sin(rp.BobPhase)) * REMOTE_WALK_BOB_PIXELS * rp.BobAmount
            Dim crouchDropPx As Double = rp.Crouch * CROUCH_MAX_HEIGHT * VIEW_SHIFT_SCALE
            Dim footShift As Integer = CInt(rp.Z * VIEW_SHIFT_SCALE + walkBobPx - crouchDropPx)
            Dim drawStartY As Integer = Math.Max(0, -spriteHeight \ 2 + RES_H \ 2 + viewShiftPx - footShift)
            Dim drawEndY As Integer = Math.Min(RES_H - 1, spriteHeight \ 2 + RES_H \ 2 + viewShiftPx - footShift)
            Dim drawStartX As Integer = Math.Max(0, -spriteWidth \ 2 + spriteScreenX)
            Dim drawEndX As Integer = Math.Min(RES_W - 1, spriteWidth \ 2 + spriteScreenX)

            DrawGroundShadow(spriteScreenX, drawEndY, spriteWidth, transformY)

            For stripe As Integer = drawStartX To drawEndX
                If transformY >= zBuffer(stripe) Then Continue For
                Dim u As Double = (stripe - (spriteScreenX - spriteWidth / 2.0)) / spriteWidth
                For y As Integer = drawStartY To drawEndY
                    Dim v As Double = (y - drawStartY) / CDbl(Math.Max(1, drawEndY - drawStartY))

                    If texForSlot IsNot Nothing Then
                        Dim tx As Integer = Math.Max(0, Math.Min(texW - 1, CInt(u * texW)))
                        Dim ty As Integer = Math.Max(0, Math.Min(texH - 1, CInt(v * texH)))
                        Dim srcColor As Integer = texForSlot(ty * texW + tx)
                        Dim alpha As Integer = (srcColor >> 24) And &HFF
                        If alpha > 128 Then
                            ' Anh nhan vat la tranh ve tay that (mau da/toc that), KHONG nhan
                            ' mau slot (SlotColor) len nua nhu ban cu - chi lam toi theo khoang
                            ' cach (spriteFog) de giu dung mau sac goc cua tung nhan vat.
                            pixelBuf(y * RES_W + stripe) = ShadeColor(srcColor, spriteFog)
                        End If
                    Else
                        Dim col As Integer = RemotePlayerPixel(u, v, baseColor)
                        If col <> 0 Then
                            pixelBuf(y * RES_W + stripe) = ShadeColor(col, spriteFog)
                        End If
                    End If
                Next
            Next

            ' Badge nho bao nguoi choi nay dang cam vu khi gi (dong bo qua goi POS, xem
            ' HeldItemId trong GameModels.vb) - khong phai tay that nhu goc nhin cua chinh
            ' minh, chi la 1 icon nho gan vi tri "tay" tren sprite, an theo zBuffer giong
            ' sprite chinh (bi tuong che khuat binh thuong).
            ' CHI ve badge khi CHUA co anh cam-vu-khi that (isHoldingPose = False) - neu da
            ' dung sprite "character_holding_..." thi vu khi da nam san trong tay ao trong
            ' anh do roi, ve them badge se bi thua/chong len nhau vo ly.
            If Not isHoldingPose AndAlso Not String.IsNullOrEmpty(rp.HeldItemId) Then
                Dim iconPixels As Integer() = Nothing
                Dim iconW As Integer = 0, iconH As Integer = 0
                Select Case rp.HeldItemId
                    Case "bow" : iconPixels = weaponIconPixelsBow : iconW = weaponIconWBow : iconH = weaponIconHBow
                    Case "dagger" : iconPixels = weaponIconPixelsDagger : iconW = weaponIconWDagger : iconH = weaponIconHDagger
                    Case "sword" : iconPixels = weaponIconPixelsSword : iconW = weaponIconWSword : iconH = weaponIconHSword
                End Select

                If iconPixels IsNot Nothing AndAlso iconW > 0 AndAlso iconH > 0 Then
                    ' Kich thuoc badge ti le theo chieu cao sprite (o gan hon thi badge cung to hon,
                    ' giong nhu chinh sprite), dat lech sang ben phai ngang tam "tay" (~55% chieu cao).
                    Dim iconDrawH As Integer = CInt(spriteHeight * 0.30)
                    Dim iconDrawW As Integer = CInt(iconDrawH * (iconW / CDbl(iconH)))
                    If iconDrawH > 0 AndAlso iconDrawW > 0 Then
                        Dim iconCx As Integer = spriteScreenX + CInt(spriteWidth * 0.55)
                        Dim iconCy As Integer = drawStartY + CInt((drawEndY - drawStartY) * 0.55)
                        Dim iconStartX As Integer = Math.Max(0, iconCx - iconDrawW \ 2)
                        Dim iconEndX As Integer = Math.Min(RES_W - 1, iconCx + iconDrawW \ 2)
                        Dim iconStartY As Integer = Math.Max(0, iconCy - iconDrawH \ 2)
                        Dim iconEndY As Integer = Math.Min(RES_H - 1, iconCy + iconDrawH \ 2)

                        For istripe As Integer = iconStartX To iconEndX
                            If transformY >= zBuffer(istripe) Then Continue For
                            Dim iu As Double = (istripe - (iconCx - iconDrawW / 2.0)) / iconDrawW
                            If iu < 0.0 OrElse iu > 1.0 Then Continue For
                            For iy As Integer = iconStartY To iconEndY
                                Dim iv As Double = (iy - (iconCy - iconDrawH / 2.0)) / iconDrawH
                                If iv < 0.0 OrElse iv > 1.0 Then Continue For
                                Dim itx As Integer = Math.Max(0, Math.Min(iconW - 1, CInt(iu * iconW)))
                                Dim ity As Integer = Math.Max(0, Math.Min(iconH - 1, CInt(iv * iconH)))
                                Dim isrc As Integer = iconPixels(ity * iconW + itx)
                                Dim ialpha As Integer = (isrc >> 24) And &HFF
                                If ialpha > 128 Then
                                    pixelBuf(iy * RES_W + istripe) = ShadeColor(isrc, spriteFog)
                                End If
                            Next
                        Next
                    End If
                End If
            End If
        Next
    End Sub

    ' Hinh nguoi don gian: dau tron o tren, than hinh oval thu nho dan xuong duoi.
    ' u,v trong [0,1]; tra ve 0 la trong suot.
    Private Function RemotePlayerPixel(u As Double, v As Double, baseColor As Color) As Integer
        Dim headCx As Double = 0.5, headCy As Double = 0.14, headR As Double = 0.15
        Dim dHead As Double = Math.Sqrt((u - headCx) ^ 2 + ((v - headCy) * 1.2) ^ 2)
        If dHead < headR Then
            Return ToArgbColor(baseColor, If(u > 0.5, 0.72, 1.0))
        End If

        If v >= 0.28 AndAlso v <= 0.98 Then
            Dim bodyHalfWidth As Double = 0.30 - (v - 0.28) * 0.12
            If bodyHalfWidth < 0.08 Then bodyHalfWidth = 0.08
            If Math.Abs(u - 0.5) < bodyHalfWidth Then
                Return ToArgbColor(baseColor, If(u > 0.5, 0.68, 1.0))
            End If
        End If

        Return 0
    End Function

    ' ---------------------------------------------------------------
    '  Ve mui ten/phi tieu dang bay theo kieu billboard (giong nam), nhung
    '  hep va dep hon vi la 1 vat the nho bay nhanh - chua co texture rieng
    '  nen ve bang toan hoc: than nau go, dau mui ten xam nhon.
    ' ---------------------------------------------------------------
    Private Sub DrawProjectileSprites(dirX As Double, dirY As Double, planeX As Double, planeY As Double)
        If projectiles.Count = 0 Then Return
        Dim invDet As Double = 1.0 / (planeX * dirY - dirX * planeY)

        For Each p As Projectile In projectiles
            Dim sx As Double = p.X - playerX
            Dim sy As Double = p.Y - playerY
            Dim transformX As Double = invDet * (dirY * sx - dirX * sy)
            Dim transformY As Double = invDet * (-planeY * sx + planeX * sy)
            If transformY <= 0.1 Then Continue For

            Dim spriteFog As Double = Math.Max(0.35, 1.0 - transformY / mapFogDist)
            Dim spriteScreenX As Integer = CInt((RES_W / 2.0) * (1.0 + transformX / transformY))
            Dim spriteSize As Integer = CInt(Math.Abs(RES_H / transformY) * 0.22)
            If spriteSize <= 0 Then Continue For

            Dim drawStartY As Integer = Math.Max(0, -spriteSize \ 2 + RES_H \ 2 + viewShiftPx)
            Dim drawEndY As Integer = Math.Min(RES_H - 1, spriteSize \ 2 + RES_H \ 2 + viewShiftPx)
            Dim drawStartX As Integer = Math.Max(0, -spriteSize \ 2 + spriteScreenX)
            Dim drawEndX As Integer = Math.Min(RES_W - 1, spriteSize \ 2 + spriteScreenX)

            For stripe As Integer = drawStartX To drawEndX
                If transformY >= zBuffer(stripe) Then Continue For
                Dim u As Double = (stripe - (spriteScreenX - spriteSize / 2.0)) / spriteSize
                For y As Integer = drawStartY To drawEndY
                    Dim v As Double = (y - drawStartY) / CDbl(Math.Max(1, drawEndY - drawStartY))
                    Dim col As Integer = ProjectilePixel(u, v)
                    If col <> 0 Then pixelBuf(y * RES_W + stripe) = ShadeColor(col, spriteFog)
                Next
            Next
        Next
    End Sub

    ' Sample tu arrow.png (anh goc nam ngang: dau nhon o CANH PHAI, long vu o CANH TRAI)
    ' nhung ve len billboard THEO TRUC DOC (dau nhon o TREN, long vu o DUOI) - giong
    ' cam giac mui ten dang lap tren day cung chuan bi ban, khong nam ngang kieu bay la.
    ' Hoan doi truc: canh dai cua anh goc (X nguon) -> truc doc billboard (v, dao nguoc
    ' vi v=0 la tren = dau nhon = X nguon lon nhat); be day anh goc (Y nguon) -> truc
    ' ngang billboard (u).
    Private Function ProjectilePixel(u As Double, v As Double) As Integer
        Dim srcX As Integer = CInt((1.0 - v) * (TEX_SIZE - 1))
        Dim srcY As Integer = CInt(u * (TEX_SIZE - 1))
        srcX = Math.Max(0, Math.Min(TEX_SIZE - 1, srcX))
        srcY = Math.Max(0, Math.Min(TEX_SIZE - 1, srcY))
        Dim srcColor As Integer = texArrow(srcY * TEX_SIZE + srcX)
        Dim alpha As Integer = (srcColor >> 24) And &HFF
        If alpha > 128 Then Return srcColor
        Return 0 ' trong suot - khong ve
    End Function

End Class
