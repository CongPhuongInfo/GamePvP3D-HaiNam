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

    Private Sub RenderFrame()
        Dim skyColor As Integer = ToArgb(40, 40, 70)
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

            Dim rowFog As Double = Math.Max(0.2, 1.0 - rowDistance / 12.0)
            Dim rowOff As Integer = y * RES_W

            For x As Integer = 0 To RES_W - 1
                Dim cellX As Integer = CInt(Math.Floor(floorX))
                Dim cellY As Integer = CInt(Math.Floor(floorY))
                Dim tx As Integer = CInt((floorX - cellX) * TEX_SIZE) And (TEX_SIZE - 1)
                Dim ty As Integer = CInt((floorY - cellY) * TEX_SIZE) And (TEX_SIZE - 1)
                floorX += floorStepX
                floorY += floorStepY
                pixelBuf(rowOff + x) = ShadeColor(texFloor(ty * TEX_SIZE + tx), rowFog)
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
                    If cellType = 1 OrElse cellType = 2 Then
                        hit = True
                    ElseIf cellType = 3 OrElse cellType = 4 Then
                        Dim passable As Boolean = If(cellType = 3, playerZ >= CRATE_JUMP_HEIGHT, crouchAmount >= CROUCH_PASS_THRESHOLD)
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

            Dim fog As Double = Math.Max(0.25, 1.0 - perpWallDist / 12.0)
            If side = 1 Then fog *= 0.7

            For y As Integer = drawStart To drawEnd
                Dim texY As Integer = CInt((y - viewShiftPx - wallScreenTop) * TEX_SIZE / CDbl(Math.Max(1, lineHeight)))
                texY = Math.Max(0, Math.Min(TEX_SIZE - 1, texY))
                pixelBuf(y * RES_W + x) = ShadeColor(tex(texY * TEX_SIZE + texX), fog)
            Next

            ' Ve khoi nua-chieu-cao (kien hang / khe chui) ma tia da "nhin xuyen qua" o tren
            If obstacleType <> 0 Then
                Dim obsLineHeight As Integer = CInt(RES_H / Math.Max(0.05, obstacleDist))
                Dim obsIdealTop As Integer = -obsLineHeight \ 2 + RES_H \ 2
                Dim obsTop As Integer = obsIdealTop + viewShiftPx
                Dim obsBot As Integer = obsLineHeight \ 2 + RES_H \ 2 + viewShiftPx
                Dim obsMid As Integer = (obsTop + obsBot) \ 2

                Dim slabStart As Integer, slabEnd As Integer
                Dim obsTex() As Integer = If(obstacleType = 3, texCrate, texVent)
                If obstacleType = 3 Then
                    slabStart = obsMid : slabEnd = obsBot           ' kien hang: chiem nua duoi
                Else
                    slabStart = obsTop : slabEnd = obsMid            ' khe chui: chiem nua tren
                End If
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

                Dim obsFog As Double = Math.Max(0.25, 1.0 - obstacleDist / 12.0)
                If obstacleSide = 1 Then obsFog *= 0.7

                For y As Integer = slabStart To slabEnd
                    Dim texY As Integer = CInt((y - viewShiftPx - obsIdealTop) * TEX_SIZE / CDbl(Math.Max(1, obsLineHeight)))
                    texY = Math.Max(0, Math.Min(TEX_SIZE - 1, texY))
                    pixelBuf(y * RES_W + x) = ShadeColor(obsTex(texY * TEX_SIZE + obsTexX), obsFog)
                Next
            End If
        Next

        DrawDecorationSprites(dirX, dirY, planeX, planeY)
        DrawWorldItemSprites(dirX, dirY, planeX, planeY)
        DrawMushroomSprites(dirX, dirY, planeX, planeY)
        DrawRemotePlayerSprites(dirX, dirY, planeX, planeY)
        DrawProjectileSprites(dirX, dirY, planeX, planeY)
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

            Dim fog As Double = Math.Max(0.3, 1.0 - transformY / 12.0)
            Dim screenX As Integer = CInt((RES_W / 2.0) * (1.0 + transformX / transformY))

            ' Chan cay neo dung vao san (cung cong thuc voi floor-casting: p = 0.5*RES_H/dist)
            Dim groundY As Integer = RES_H \ 2 + viewShiftPx + CInt(0.5 * RES_H / transformY)
            Dim worldHeight As Double = 0.4 * d.Scale
            Dim size As Integer = CInt((RES_H / transformY) * worldHeight)
            If size <= 0 Then Continue For

            Dim drawStartY As Integer = Math.Max(0, groundY - size)
            Dim drawEndY As Integer = Math.Min(RES_H - 1, groundY)
            Dim drawStartX As Integer = Math.Max(0, screenX - size \ 2)
            Dim drawEndX As Integer = Math.Min(RES_W - 1, screenX + size \ 2)

            Dim useTex() As Integer = If(d.Kind = 0, texGrass, texFlower)
            Dim texW As Integer = If(d.Kind = 0, texGrassW, texFlowerW)
            Dim texH As Integer = If(d.Kind = 0, texGrassH, texFlowerH)

            For stripe As Integer = drawStartX To drawEndX
                If transformY >= zBuffer(stripe) Then Continue For
                Dim u As Double = (stripe - drawStartX) / CDbl(Math.Max(1, drawEndX - drawStartX))
                For y As Integer = drawStartY To drawEndY
                    Dim v As Double = (y - drawStartY) / CDbl(Math.Max(1, drawEndY - drawStartY))
                    Dim col As Integer = 0

                    If useTex IsNot Nothing Then
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

            Dim spriteFog As Double = Math.Max(0.3, 1.0 - transformY / 12.0)

            Dim spriteScreenX As Integer = CInt((RES_W / 2.0) * (1.0 + transformX / transformY))
            Dim spriteSize As Integer = CInt(Math.Abs(RES_H / transformY))
            If spriteSize <= 0 Then Continue For

            Dim drawStartY As Integer = Math.Max(0, -spriteSize \ 2 + RES_H \ 2 + viewShiftPx)
            Dim drawEndY As Integer = Math.Min(RES_H - 1, spriteSize \ 2 + RES_H \ 2 + viewShiftPx)
            Dim drawStartX As Integer = Math.Max(0, -spriteSize \ 2 + spriteScreenX)
            Dim drawEndX As Integer = Math.Min(RES_W - 1, spriteSize \ 2 + spriteScreenX)

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

            Dim spriteFog As Double = Math.Max(0.3, 1.0 - transformY / 12.0)
            Dim spriteScreenX As Integer = CInt((RES_W / 2.0) * (1.0 + transformX / transformY))
            Dim spriteSize As Integer = CInt(Math.Abs(RES_H / transformY) * 0.6)
            If spriteSize <= 0 Then Continue For

            Dim drawStartY As Integer = Math.Max(0, -spriteSize \ 2 + RES_H \ 2 + viewShiftPx)
            Dim drawEndY As Integer = Math.Min(RES_H - 1, spriteSize \ 2 + RES_H \ 2 + viewShiftPx)
            Dim drawStartX As Integer = Math.Max(0, -spriteSize \ 2 + spriteScreenX)
            Dim drawEndX As Integer = Math.Min(RES_W - 1, spriteSize \ 2 + spriteScreenX)

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

            Dim spriteFog As Double = Math.Max(0.35, 1.0 - transformY / 12.0)
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

            Dim spriteFog As Double = Math.Max(0.35, 1.0 - transformY / 12.0)
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
