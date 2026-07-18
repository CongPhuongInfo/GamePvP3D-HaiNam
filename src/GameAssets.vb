Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

' =====================================================================
'  Form1 - Assets: nap texture, sprite, anh tay tu thu muc Assets\
'  (Mot phan cua Form1 - xem README.md muc 'Cau truc code' de biet
'  toan bo cac file partial class va vai tro cua tung file)
' =====================================================================

Partial Public Class Form1

    Private Sub LoadTextures()
        texWall1 = LoadTexturePixels("Textures", "wall1.png", 150, 100, 60)
        texWall2 = LoadTexturePixels("Textures", "wall2.png", 120, 70, 160)
        texCrate = LoadTexturePixels("Textures", "crate.png", 180, 120, 60)
        texVent = LoadTexturePixels("Textures", "vent.png", 90, 150, 170)
        texFloor = LoadTexturePixels("Textures", "floor.png", 50, 40, 30)
        texMushroom = LoadTexturePixels("Sprites", "mushroom.png", 220, 40, 40)
        texArrow = LoadTexturePixels("Items", "arrow.png", 120, 78, 40)
    End Sub

    Private Function LoadTexturePixels(subfolder As String, fileName As String, fallbackR As Integer, fallbackG As Integer, fallbackB As Integer) As Integer()
        Dim fullPath As String = System.IO.Path.Combine(Application.StartupPath, "Assets", subfolder, fileName)
        Try
            Using bmp As New Bitmap(fullPath)
                Dim w As Integer = bmp.Width
                Dim h As Integer = bmp.Height
                Dim data(w * h - 1) As Integer
                Dim rect As New Rectangle(0, 0, w, h)
                Dim bmpData As BitmapData = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)
                Marshal.Copy(bmpData.Scan0, data, 0, data.Length)
                bmp.UnlockBits(bmpData)
                Return data
            End Using
        Catch ex As Exception
            ' Thieu file Assets\<fileName> - dung mau phang thay the, khong lam crash game
            Dim fallback(TEX_SIZE * TEX_SIZE - 1) As Integer
            Dim col As Integer = ToArgb(fallbackR, fallbackG, fallbackB)
            For i As Integer = 0 To fallback.Length - 1
                fallback(i) = col
            Next
            Return fallback
        End Try
    End Function

    ' Nap 1 anh bat ky (kich thuoc tuy y) thanh mang Integer ARGB de sample nhanh.
    ' Tra ve Nothing (khong nem loi) neu thieu file - noi goi se tu fallback.
    Private Function TryLoadTexturePixels(subfolder As String, fileName As String, ByRef outW As Integer, ByRef outH As Integer) As Integer()
        Dim fullPath As String = System.IO.Path.Combine(Application.StartupPath, "Assets", subfolder, fileName)
        Try
            Using bmp As New Bitmap(fullPath)
                outW = bmp.Width
                outH = bmp.Height
                Dim data(outW * outH - 1) As Integer
                Dim rect As New Rectangle(0, 0, outW, outH)
                Dim bmpData As BitmapData = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)
                Marshal.Copy(bmpData.Scan0, data, 0, data.Length)
                bmp.UnlockBits(bmpData)
                Return data
            End Using
        Catch ex As Exception
            outW = 0
            outH = 0
            Return Nothing
        End Try
    End Function

    ' Sprite nhan vat co the co ti le/kich thuoc khac 128x128 (nguoi cao hon rong),
    ' nen nap rieng thay vi dung chung TEX_SIZE co dinh nhu tuong/nam.
    Private Sub LoadCharacterTexture()
        Dim defaultW As Integer, defaultH As Integer
        Dim defaultTex As Integer() = TryLoadTexturePixels("Characters", "character.png", defaultW, defaultH)
        For i As Integer = 0 To CHARACTER_SLOT_COUNT - 1
            Dim w As Integer, h As Integer
            Dim tex As Integer() = TryLoadTexturePixels("Characters", "character_" & i & ".png", w, h)
            If tex IsNot Nothing Then
                texCharacterBySlot(i) = tex
                texCharacterWBySlot(i) = w
                texCharacterHBySlot(i) = h
            Else
                texCharacterBySlot(i) = defaultTex
                texCharacterWBySlot(i) = defaultW
                texCharacterHBySlot(i) = defaultH
            End If

            ' Anh ngang (side) - thieu thi tam dung anh truoc cua CHINH slot nay thay the
            Dim sw As Integer, sh As Integer
            Dim sideTex As Integer() = TryLoadTexturePixels("Characters", "character_" & i & "_side.png", sw, sh)
            If sideTex Is Nothing Then
                sideTex = texCharacterBySlot(i) : sw = texCharacterWBySlot(i) : sh = texCharacterHBySlot(i)
            End If
            texCharacterSideBySlot(i) = sideTex
            texCharacterSideWBySlot(i) = sw
            texCharacterSideHBySlot(i) = sh
            texCharacterSideMirrorBySlot(i) = MirrorPixelArray(sideTex, sw, sh)

            ' Anh sau (back) - thieu thi tam dung anh truoc cua CHINH slot nay thay the
            Dim bw As Integer, bh As Integer
            Dim backTex As Integer() = TryLoadTexturePixels("Characters", "character_" & i & "_back.png", bw, bh)
            If backTex Is Nothing Then
                backTex = texCharacterBySlot(i) : bw = texCharacterWBySlot(i) : bh = texCharacterHBySlot(i)
            End If
            texCharacterBackBySlot(i) = backTex
            texCharacterBackWBySlot(i) = bw
            texCharacterBackHBySlot(i) = bh

            ' Anh "sai chan" mat truoc - thieu thi dung anh dung yen mat truoc thay the
            Dim ww As Integer, wh As Integer
            Dim walkTex As Integer() = TryLoadTexturePixels("Characters", "character_" & i & "_walk.png", ww, wh)
            If walkTex Is Nothing Then
                walkTex = texCharacterBySlot(i) : ww = texCharacterWBySlot(i) : wh = texCharacterHBySlot(i)
            End If
            texCharacterWalkBySlot(i) = walkTex
            texCharacterWalkWBySlot(i) = ww
            texCharacterWalkHBySlot(i) = wh

            ' Anh "sai chan" mat ngang - thieu thi dung anh ngang dung yen thay the
            Dim sww As Integer, swh As Integer
            Dim sideWalkTex As Integer() = TryLoadTexturePixels("Characters", "character_" & i & "_side_walk.png", sww, swh)
            If sideWalkTex Is Nothing Then
                sideWalkTex = texCharacterSideBySlot(i) : sww = texCharacterSideWBySlot(i) : swh = texCharacterSideHBySlot(i)
            End If
            texCharacterSideWalkBySlot(i) = sideWalkTex
            texCharacterSideWalkWBySlot(i) = sww
            texCharacterSideWalkHBySlot(i) = swh
            texCharacterSideWalkMirrorBySlot(i) = MirrorPixelArray(sideWalkTex, sww, swh)

            ' Anh "sai chan" mat sau - thieu thi dung anh sau dung yen thay the
            Dim bww As Integer, bwh As Integer
            Dim backWalkTex As Integer() = TryLoadTexturePixels("Characters", "character_" & i & "_back_walk.png", bww, bwh)
            If backWalkTex Is Nothing Then
                backWalkTex = texCharacterBackBySlot(i) : bww = texCharacterBackWBySlot(i) : bwh = texCharacterBackHBySlot(i)
            End If
            texCharacterBackWalkBySlot(i) = backWalkTex
            texCharacterBackWalkWBySlot(i) = bww
            texCharacterBackWalkHBySlot(i) = bwh

            ' Anh tu the ngoi mat truoc - thieu thi tam dung anh dung yen mat truoc thay the
            Dim cw As Integer, ch As Integer
            Dim crouchTex As Integer() = TryLoadTexturePixels("Characters", "character_" & i & "_crouch.png", cw, ch)
            If crouchTex Is Nothing Then
                crouchTex = texCharacterBySlot(i) : cw = texCharacterWBySlot(i) : ch = texCharacterHBySlot(i)
            End If
            texCharacterCrouchBySlot(i) = crouchTex
            texCharacterCrouchWBySlot(i) = cw
            texCharacterCrouchHBySlot(i) = ch

            ' Anh tu the ngoi mat ngang - thieu thi tam dung anh ngang dung yen thay the
            Dim scw As Integer, sch As Integer
            Dim sideCrouchTex As Integer() = TryLoadTexturePixels("Characters", "character_" & i & "_side_crouch.png", scw, sch)
            If sideCrouchTex Is Nothing Then
                sideCrouchTex = texCharacterSideBySlot(i) : scw = texCharacterSideWBySlot(i) : sch = texCharacterSideHBySlot(i)
            End If
            texCharacterSideCrouchBySlot(i) = sideCrouchTex
            texCharacterSideCrouchWBySlot(i) = scw
            texCharacterSideCrouchHBySlot(i) = sch
            texCharacterSideCrouchMirrorBySlot(i) = MirrorPixelArray(sideCrouchTex, scw, sch)

            ' Anh tu the ngoi mat sau - thieu thi tam dung anh sau dung yen thay the
            Dim bcw As Integer, bch As Integer
            Dim backCrouchTex As Integer() = TryLoadTexturePixels("Characters", "character_" & i & "_back_crouch.png", bcw, bch)
            If backCrouchTex Is Nothing Then
                backCrouchTex = texCharacterBackBySlot(i) : bcw = texCharacterBackWBySlot(i) : bch = texCharacterBackHBySlot(i)
            End If
            texCharacterBackCrouchBySlot(i) = backCrouchTex
            texCharacterBackCrouchWBySlot(i) = bcw
            texCharacterBackCrouchHBySlot(i) = bch

            ' Anh tu the nhay mat truoc - thieu thi tam dung anh dung yen mat truoc thay the
            Dim jw As Integer, jh As Integer
            Dim jumpTex As Integer() = TryLoadTexturePixels("Characters", "character_" & i & "_jump.png", jw, jh)
            If jumpTex Is Nothing Then
                jumpTex = texCharacterBySlot(i) : jw = texCharacterWBySlot(i) : jh = texCharacterHBySlot(i)
            End If
            texCharacterJumpBySlot(i) = jumpTex
            texCharacterJumpWBySlot(i) = jw
            texCharacterJumpHBySlot(i) = jh

            ' Anh tu the nhay mat ngang - thieu thi tam dung anh ngang dung yen thay the
            Dim sjw As Integer, sjh As Integer
            Dim sideJumpTex As Integer() = TryLoadTexturePixels("Characters", "character_" & i & "_side_jump.png", sjw, sjh)
            If sideJumpTex Is Nothing Then
                sideJumpTex = texCharacterSideBySlot(i) : sjw = texCharacterSideWBySlot(i) : sjh = texCharacterSideHBySlot(i)
            End If
            texCharacterSideJumpBySlot(i) = sideJumpTex
            texCharacterSideJumpWBySlot(i) = sjw
            texCharacterSideJumpHBySlot(i) = sjh
            texCharacterSideJumpMirrorBySlot(i) = MirrorPixelArray(sideJumpTex, sjw, sjh)

            ' Anh tu the nhay mat sau - thieu thi tam dung anh sau dung yen thay the
            Dim bjw As Integer, bjh As Integer
            Dim backJumpTex As Integer() = TryLoadTexturePixels("Characters", "character_" & i & "_back_jump.png", bjw, bjh)
            If backJumpTex Is Nothing Then
                backJumpTex = texCharacterBackBySlot(i) : bjw = texCharacterBackWBySlot(i) : bjh = texCharacterBackHBySlot(i)
            End If
            texCharacterBackJumpBySlot(i) = backJumpTex
            texCharacterBackJumpWBySlot(i) = bjw
            texCharacterBackJumpHBySlot(i) = bjh
        Next
    End Sub

    ' Nap bo anh nhan vat DANG CAM VU KHI theo dung quy uoc ten file trong
    ' Assets\Characters\PROMPTS.md: "character_holding_<vukhi>_<huong>_<slot>.png"
    ' (vukhi = bow/dagger/sword, huong = front/side/back). KHAC voi LoadCharacterTexture(),
    ' o day KHONG fallback ve anh dung yen tay khong ngay tai buoc nap - de trong
    ' (Dictionary khong co key) va de PickDirectionalTexture() tu quyet dinh fallback luc
    ' render, vi ham do can biet "co anh cam vu khi that hay khong" de con lua chon tiep
    ' co ve badge icon nho hay khong (xem DrawRemotePlayerSprites).
    Private Sub LoadCharacterHoldingTextures()
        Dim weapons() As String = {"bow", "dagger", "sword"}
        Dim dirs() As String = {"front", "side", "back"}
        For Each weapon As String In weapons
            For Each dir As String In dirs
                For i As Integer = 0 To CHARACTER_SLOT_COUNT - 1
                    Dim fileName As String = "character_holding_" & weapon & "_" & dir & "_" & i & ".png"
                    Dim w As Integer, h As Integer
                    Dim tex As Integer() = TryLoadTexturePixels("Characters", fileName, w, h)
                    If tex Is Nothing Then Continue For ' chua co anh nay - bo qua, giu Dictionary trong cho key nay

                    Dim key As String = weapon & "_" & dir & "_" & i
                    texCharacterHolding(key) = tex
                    texCharacterHoldingW(key) = w
                    texCharacterHoldingH(key) = h
                    If dir = "side" Then
                        texCharacterHoldingSideMirror(key) = MirrorPixelArray(tex, w, h)
                    End If
                Next
            Next
        Next
    End Sub

    ' Lat guong ngang 1 mang pixel ARGB (dung cho anh "ngang" chi ve 1 ben, tao ben con
    ' lai bang code thay vi phai ve them anh). Khac MirrorBitmap() la ham nay lam viec
    ' truc tiep tren mang Integer (dung cho sprite raycasting) thay vi doi tuong Bitmap.
    Private Function MirrorPixelArray(src As Integer(), w As Integer, h As Integer) As Integer()
        If src Is Nothing Then Return Nothing
        Dim result(src.Length - 1) As Integer
        For y As Integer = 0 To h - 1
            Dim rowOff As Integer = y * w
            For x As Integer = 0 To w - 1
                result(rowOff + (w - 1 - x)) = src(rowOff + x)
            Next
        Next
        Return result
    End Function

    ' Anh co/hoa (neu co) - Assets\Sprites\grass.png va flower.png. Neu chua co thi
    ' DrawDecorationSprites se tu ve bang toan hoc (GrassPixel/FlowerPixel).
    Private Sub LoadDecorationTextures()
        texGrass = TryLoadTexturePixels("Sprites", "grass.png", texGrassW, texGrassH)
        texFlower = TryLoadTexturePixels("Sprites", "flower.png", texFlowerW, texFlowerH)
    End Sub

    ' Anh bau troi toan canh (panorama) - Assets\Sky\sky.png. Anh nay PHAI la
    ' anh rong noi lien duoc theo chieu ngang (seamless horizontal tile) vi no
    ' se duoc quan quanh 360 do theo goc quay cua camera. Neu chua co thi
    ' RenderFrame se fallback ve mau phang nhu truoc.
    Private Sub LoadSkyTexture()
        texSky = TryLoadTexturePixels("Sky", "sky.png", texSkyW, texSkyH)
    End Sub


    ' ---------------------------------------------------------------
    '  Nap anh tay that RIENG CHO TUNG SLOT NHAN VAT (hand_open_N.png,
    '  hand_fist_N.png, hand_holding_N.png, hand_holding_bow_N.png voi
    '  N = 0..CHARACTER_SLOT_COUNT-1). Neu thieu anh rieng cho 1 slot thi
    '  dung tam ban khong danh so (hand_open.png...) lam du phong chung;
    '  neu ca hai deu khong co thi de Nothing - DrawViewmodel se tu dong
    '  fallback ve tay ve bang vector.
    ' ---------------------------------------------------------------
    Private Sub LoadHandTextures()
        Dim defaultOpen As Bitmap = TryLoadBitmap("Hands", "hand_open.png")
        Dim defaultFist As Bitmap = TryLoadBitmap("Hands", "hand_fist.png")
        Dim defaultHolding As Bitmap = TryLoadBitmap("Hands", "hand_holding.png")
        Dim defaultHoldingBow As Bitmap = TryLoadBitmap("Hands", "hand_holding_bow.png")
        Dim defaultHoldingBowDrawn As Bitmap = TryLoadBitmap("Hands", "hand_holding_bow_drawn.png")
        Dim defaultPullingString As Bitmap = TryLoadBitmap("Hands", "hand_pulling_string.png")
        Dim defaultHoldingDagger As Bitmap = TryLoadBitmap("Hands", "hand_holding_dagger.png")
        Dim defaultHoldingSword As Bitmap = TryLoadBitmap("Hands", "hand_holding_sword.png")

        For i As Integer = 0 To CHARACTER_SLOT_COUNT - 1
            handOpenImgBySlot(i) = If(TryLoadBitmap("Hands", "hand_open_" & i & ".png"), defaultOpen)
            handFistImgBySlot(i) = If(TryLoadBitmap("Hands", "hand_fist_" & i & ".png"), defaultFist)
            handHoldingImgBySlot(i) = If(TryLoadBitmap("Hands", "hand_holding_" & i & ".png"), defaultHolding)
            handHoldingBowImgBySlot(i) = If(TryLoadBitmap("Hands", "hand_holding_bow_" & i & ".png"), defaultHoldingBow) ' Nothing neu ca 2 deu thieu -> tu dong fallback ve hand_holding thuong
            handHoldingBowDrawnImgBySlot(i) = If(TryLoadBitmap("Hands", "hand_holding_bow_drawn_" & i & ".png"), defaultHoldingBowDrawn) ' Nothing neu chua co -> DrawViewmodel tu fallback ve hand_holding_bow_N thuong (cung khong doi hinh luc keo)
            handPullingStringImgBySlot(i) = If(TryLoadBitmap("Hands", "hand_pulling_string_" & i & ".png"), defaultPullingString) ' Nothing neu chua co -> tu fallback ve nam dam (hand_fist) nhu truoc
            handHoldingDaggerImgBySlot(i) = If(TryLoadBitmap("Hands", "hand_holding_dagger_" & i & ".png"), defaultHoldingDagger) ' Nothing neu chua co -> tu dong fallback ve hand_holding thuong (tay trong, khong thay dao)
            handHoldingSwordImgBySlot(i) = If(TryLoadBitmap("Hands", "hand_holding_sword_" & i & ".png"), defaultHoldingSword) ' Nothing neu chua co -> tu dong fallback ve hand_holding thuong (tay trong, khong thay kiem)
            If handHoldingDaggerImgBySlot(i) IsNot Nothing Then handHoldingDaggerImgMirrorBySlot(i) = MirrorBitmap(handHoldingDaggerImgBySlot(i)) ' dao/kiem hien o tay PHAI -> can lat guong (khac cung)
            If handHoldingSwordImgBySlot(i) IsNot Nothing Then handHoldingSwordImgMirrorBySlot(i) = MirrorBitmap(handHoldingSwordImgBySlot(i))
            If handPullingStringImgBySlot(i) IsNot Nothing Then handPullingStringImgMirrorBySlot(i) = MirrorBitmap(handPullingStringImgBySlot(i)) ' ve theo khung tay trai giong dao/kiem -> lat sang tay phai (tay keo day)

            If handOpenImgBySlot(i) IsNot Nothing Then handOpenImgMirrorBySlot(i) = MirrorBitmap(handOpenImgBySlot(i))
            If handFistImgBySlot(i) IsNot Nothing Then handFistImgMirrorBySlot(i) = MirrorBitmap(handFistImgBySlot(i))
            If handHoldingImgBySlot(i) IsNot Nothing Then handHoldingImgMirrorBySlot(i) = MirrorBitmap(handHoldingImgBySlot(i))
        Next
    End Sub

    ' ---------------------------------------------------------------
    '  Nap icon vu khi (Assets\Items\bow|dagger|sword.png) thanh mang pixel
    '  ARGB tho de renderer ban tia (DrawRemotePlayerSprites) ve badge nho
    '  bao nguoi choi KHAC dang cam vu khi gi. Thieu file thi Nothing - badge
    '  se tu bo qua, khong crash (giong moi cho TryLoadTexturePixels khac).
    ' ---------------------------------------------------------------
    Private Sub LoadWeaponIconPixels()
        weaponIconPixelsBow = TryLoadTexturePixels("Items", "bow.png", weaponIconWBow, weaponIconHBow)
        weaponIconPixelsDagger = TryLoadTexturePixels("Items", "dagger.png", weaponIconWDagger, weaponIconHDagger)
        weaponIconPixelsSword = TryLoadTexturePixels("Items", "sword.png", weaponIconWSword, weaponIconHSword)
    End Sub

    Private Function TryLoadBitmap(subfolder As String, fileName As String) As Bitmap
        Dim fullPath As String = System.IO.Path.Combine(Application.StartupPath, "Assets", subfolder, fileName)
        Try
            Return New Bitmap(fullPath)
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    ' Lat ngang anh tay goc de lam tay con lai (chi ve/nap 1 anh, dung chung cho 2 tay)
    Private Function MirrorBitmap(src As Bitmap) As Bitmap
        Dim copy As New Bitmap(src)
        copy.RotateFlip(RotateFlipType.RotateNoneFlipX)
        Return copy
    End Function

    Private Function ShadeColor(argb As Integer, factor As Double) As Integer
        Dim r As Integer = CInt(((argb >> 16) And &HFF) * factor)
        Dim g As Integer = CInt(((argb >> 8) And &HFF) * factor)
        Dim b As Integer = CInt((argb And &HFF) * factor)
        Return ToArgb(r, g, b)
    End Function

End Class
