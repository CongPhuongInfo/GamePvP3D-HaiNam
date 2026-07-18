Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

' =====================================================================
'  GamePvP3D_HaiNam - Ho tro Solo va PvP 3-4 nguoi (NetworkHub/NetworkPeer)
'  Pseudo-3D raycasting (kieu Wolfenstein) ve bang GDI+ thuan, khong
'  can thu vien ngoai, bien dich bang vbc.exe / .NET Framework 4.x.
'
'  Y tuong: nhan vat di trong me cung, nhat nam de tang diem, du
'  MUSHROOMS_PER_LEVEL nam thi len cap va duoc buff toc do.
'  PvP: Host chay NetworkHub (toi da 3 khach), Client chay NetworkPeer
'  ket noi vao Host - dung kien truc star-topology giong cac game
'  GamePvP khac. Host la nguon du lieu goc (authoritative) cho danh
'  sach nam de tranh 2 nguoi cung nhat trung 1 cay nam.
' =====================================================================

Public Class Form1
    Inherits Form

    ' ==== Cau hinh render ====
    Private Const RES_W As Integer = 320
    Private Const RES_H As Integer = 200
    Private Const WIN_W As Integer = 960
    Private Const WIN_H As Integer = 600
    Private Const FOV_SCALE As Double = 0.66

    ' ==== Ban do (0 = trong, 1 = tuong da, 2 = tuong da tim, 3 = kien hang thap [nhay qua], 4 = khe chui [ngoi qua]) ====
    Private Const MAP_W As Integer = 16
    Private Const MAP_H As Integer = 16
    Private ReadOnly mapData As Integer(,) = {
        {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
        {1, 0, 0, 0, 0, 0, 1, 0, 3, 0, 0, 4, 0, 0, 0, 1},
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

    ' ==== Xoay bang chuot (mouse-look) ====
    Private Const MOUSE_SENSITIVITY As Double = 0.0035
    Private mouseLookEnabled As Boolean = False
    Private ignoreNextMouseMove As Boolean = False
    Private cursorCaptured As Boolean = False

    ' ==== Hoat anh nhun tay khi di bo (view model kieu FPS) ====
    Private bobPhase As Double = 0.0
    Private bobAmount As Double = 0.0   ' 0 = dung yen, 1 = dang nhun het co
    Private Const BOB_SPEED As Double = 8.0
    Private Const BOB_LERP_SPEED As Double = 6.0
    Private idlePhase As Double = 0.0   ' nhip "tho" cham cua tay khi dung yen (mo/nam nhe tu nhien)
    Private Const IDLE_BREATH_SPEED As Double = 1.3

    ' ==== Nhay / ngoi (mo phong bang do lech camera theo chieu doc) ====
    Private playerZ As Double = 0.0        ' do cao hien tai khi nhay, 0 = duoi dat
    Private zVelocity As Double = 0.0
    Private isJumping As Boolean = False
    Private crouchAmount As Double = 0.0   ' 0 = dung thang, 1 = ngoi het co (noi suy muot)
    Private viewShiftPx As Integer = 0     ' offset man hinh theo chieu doc, tinh lai moi frame
    Private jumpRequested As Boolean = False ' bat len khi bam chuot phai, tieu thu trong HandleInput
    Private Const GRAVITY As Double = 9.0
    Private Const JUMP_SPEED As Double = 3.2
    Private Const CROUCH_LERP_SPEED As Double = 7.0
    Private Const CROUCH_MAX_HEIGHT As Double = 0.35
    Private Const VIEW_SHIFT_SCALE As Double = 70.0
    Private Const CRATE_JUMP_HEIGHT As Double = 0.45   ' phai nhay cao hon muc nay moi vuot qua duoc kien hang (loai 3)
    Private Const CROUCH_PASS_THRESHOLD As Double = 0.6 ' phai ngoi it nhat muc nay moi chui qua duoc khe (loai 4)

    ' ==== Dung cu / vu khi cam tren tay (chuot trai) ====
    Private heldItemName As String = "(chua trang bi)"

    ' ==== Combat: mau, cooldown, hoat anh vung vu khi, phi tieu dang bay ====
    Private Const PLAYER_MAX_HEALTH As Integer = 100
    Private playerHealth As Integer = PLAYER_MAX_HEALTH
    Private lastAttackTime As DateTime = DateTime.MinValue
    Private lastDamageTakenTime As DateTime = DateTime.MinValue ' cho hieu ung do man hinh khi bi trung don
    Private attackSwingTime As Double = 0.0   ' > 0 = dang trong hoat anh vung/ban, dem nguoc ve 0
    Private isDrawingBow As Boolean = False       ' dang giu chuot trai keo cung (chua ban)
    Private drawStartTime As DateTime = DateTime.MinValue
    Private drawingItem As ItemDefinition = Nothing ' item cung dang duoc keo, phong khi doi tay giua chung
    Private Const ATTACK_SWING_DURATION As Double = 0.22
    Private Const MELEE_HIT_CONE_RAD As Double = 0.9   ' ~51 do moi ben tinh tu huong nhin, du rong de de trung
    Private Const ARROW_HIT_RADIUS As Double = 0.35
    Private Const ARROW_LIFE_SECONDS As Double = 3.0

    ' Giu chuot trai = "keo cung": nha ra moi thuc su ban. Giu chua toi BOW_MIN_DRAW_SECONDS
    ' thi HUY (khong ban, khong tru cooldown/ten). Giu cang lau (toi da o moc
    ' BOW_MAX_DRAW_SECONDS) thi sat thuong + toc do bay cua ten cang cao, xem FireProjectile.
    Private Const BOW_MIN_DRAW_SECONDS As Double = 0.22
    Private Const BOW_MAX_DRAW_SECONDS As Double = 1.1
    Private Const BOW_MAX_DAMAGE_MULT As Double = 2.0
    Private Const BOW_MAX_SPEED_MULT As Double = 1.5
    Private Const BOW_DRAW_PULLBACK_PX As Double = 60.0 ' tay keo day lui ve bao nhieu px luc keo toi da
    Private projectiles As New List(Of Projectile)
    Private nextProjectileId As Integer = 1

    ' ==== PvP mang (NetworkHub cho Host, NetworkPeer cho Client) ====
    Private Enum NetMode
        None
        Host
        Client
    End Enum
    Private netMode As NetMode = NetMode.None
    Private localSlot As Integer = 0
    Private hub As NetworkHub
    Private peer As NetworkPeer
    Private netStatusText As String = ""
    Private netPort As Integer = 27015
    Private netHostIp As String = ""

    Private remotePlayers As New Dictionary(Of Integer, RemotePlayerState)
    Private pendingPickupRequests As New HashSet(Of Integer)
    Private nextMushroomId As Integer = 1

    ' ==== Trang thai game ====
    Private mushrooms As New List(Of MushroomItem)
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

    ' ==== Texture (nap tu thu muc Assets\, moi anh la 128x128 ARGB) ====
    Private Const TEX_SIZE As Integer = 128
    Private texWall1() As Integer
    Private texWall2() As Integer
    Private texCrate() As Integer
    Private texVent() As Integer
    Private texFloor() As Integer
    Private texMushroom() As Integer

    ' Anh mui ten bay (Assets\Items\arrow.png) - anh goc VE NGANG (dau nhon ben phai,
    ' long vu ben trai) nhung billboard bay ve THEO TRUC DOC (dau nhon o tren) cho dung
    ' cam giac "dang lap ten chuan bi ban". ProjectilePixel() se hoan doi truc luc sample.
    Private texArrow() As Integer

    ' ==== Sprite nhan vat cho nguoi choi khac (PvP) - Nothing neu chua co, se fallback ve hinh nguoi don gian ====
    ' Moi slot (0=Host, 1-3=khach) co the co 1 anh nhan vat rieng (character_0.png..character_3.png).
    ' Slot nao thieu file rieng thi dung chung "character.png" mac dinh - khong bat buoc phai
    ' co du 4 anh moi chay duoc.
    Private Const CHARACTER_SLOT_COUNT As Integer = 4
    Private texCharacterBySlot(CHARACTER_SLOT_COUNT - 1) As Integer()
    Private texCharacterWBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterHBySlot(CHARACTER_SLOT_COUNT - 1) As Integer

    ' Sprite nhan vat theo HUONG NHIN (truoc/ngang/sau), rieng cho tung slot. Anh "ngang"
    ' chi can ve 1 ben (vd nhin sang phai), ben con lai tu lat guong (Mirror) luc nap, khong
    ' can ve 2 ban. Slot nao thieu anh _side/_back rieng thi tam dung anh truoc (character_N.png)
    ' thay the - khong bat buoc phai co du 3 huong moi chay duoc.
    Private texCharacterSideBySlot(CHARACTER_SLOT_COUNT - 1) As Integer()
    Private texCharacterSideMirrorBySlot(CHARACTER_SLOT_COUNT - 1) As Integer()
    Private texCharacterSideWBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterSideHBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterBackBySlot(CHARACTER_SLOT_COUNT - 1) As Integer()
    Private texCharacterBackWBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterBackHBySlot(CHARACTER_SLOT_COUNT - 1) As Integer

    ' Anh "sai chan" (mid-stride) rieng cho tung huong - dung de tao hoat anh di bo 2-frame
    ' (frame dung yen <-> frame sai chan) cho nguoi choi khac. File: character_N_walk.png,
    ' character_N_side_walk.png, character_N_back_walk.png. Thieu file nao thi tam dung anh
    ' dung yen cung huong do thay the (khong bat buoc phai co du moi huong moi chay duoc,
    ' se chi khong thay doi frame khi di bo nhung van con nhun/tho).
    Private texCharacterWalkBySlot(CHARACTER_SLOT_COUNT - 1) As Integer()
    Private texCharacterWalkWBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterWalkHBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterSideWalkBySlot(CHARACTER_SLOT_COUNT - 1) As Integer()
    Private texCharacterSideWalkMirrorBySlot(CHARACTER_SLOT_COUNT - 1) As Integer()
    Private texCharacterSideWalkWBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterSideWalkHBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterBackWalkBySlot(CHARACTER_SLOT_COUNT - 1) As Integer()
    Private texCharacterBackWalkWBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterBackWalkHBySlot(CHARACTER_SLOT_COUNT - 1) As Integer

    ' Anh tu the NGOI (crouch) rieng cho tung huong - dung khi nguoi choi khac dang ngoi
    ' (rp.Crouch >= REMOTE_CROUCH_POSE_THRESHOLD). File: character_N_crouch.png,
    ' character_N_side_crouch.png, character_N_back_crouch.png. Thieu file nao thi tam dung
    ' anh dung yen cung huong thay the (khong bat buoc phai co du moi huong moi chay duoc,
    ' se chi khong doi dang khi ngoi nhung sprite van thap xuong dung theo Crouch).
    Private texCharacterCrouchBySlot(CHARACTER_SLOT_COUNT - 1) As Integer()
    Private texCharacterCrouchWBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterCrouchHBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterSideCrouchBySlot(CHARACTER_SLOT_COUNT - 1) As Integer()
    Private texCharacterSideCrouchMirrorBySlot(CHARACTER_SLOT_COUNT - 1) As Integer()
    Private texCharacterSideCrouchWBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterSideCrouchHBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterBackCrouchBySlot(CHARACTER_SLOT_COUNT - 1) As Integer()
    Private texCharacterBackCrouchWBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterBackCrouchHBySlot(CHARACTER_SLOT_COUNT - 1) As Integer

    ' Anh tu the NHAY (jump) rieng cho tung huong - dung khi nguoi choi khac dang lo lung
    ' tren khong (rp.Z >= REMOTE_JUMP_POSE_THRESHOLD). File: character_N_jump.png,
    ' character_N_side_jump.png, character_N_back_jump.png. Thieu file nao thi tam dung
    ' anh dung yen cung huong thay the (khong bat buoc phai co du moi huong moi chay duoc,
    ' se chi khong doi dang khi nhay nhung sprite van duoc nang len dung theo Z co san).
    Private texCharacterJumpBySlot(CHARACTER_SLOT_COUNT - 1) As Integer()
    Private texCharacterJumpWBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterJumpHBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterSideJumpBySlot(CHARACTER_SLOT_COUNT - 1) As Integer()
    Private texCharacterSideJumpMirrorBySlot(CHARACTER_SLOT_COUNT - 1) As Integer()
    Private texCharacterSideJumpWBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterSideJumpHBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterBackJumpBySlot(CHARACTER_SLOT_COUNT - 1) As Integer()
    Private texCharacterBackJumpWBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterBackJumpHBySlot(CHARACTER_SLOT_COUNT - 1) As Integer

    ' ==== Hoat anh nguoi choi khac (di bo / tho khi dung yen), tinh moi frame trong
    ' UpdateRemoteAnimations() dua tren toc do di chuyen uoc luong tu cac goi POS nhan duoc ====
    Private Const REMOTE_WALK_BOB_SPEED As Double = 8.0        ' toc do vong lap sai chan, giong BOB_SPEED cua tay local
    Private Const REMOTE_BOB_LERP_SPEED As Double = 6.0        ' toc do noi suy muot bat/tat hoat anh di bo
    Private Const REMOTE_WALK_BOB_PIXELS As Double = 5.0       ' bien do nhun len xuong khi di bo (px o do phan giai 320x200)
    Private Const REMOTE_IDLE_BREATH_SPEED As Double = 1.3     ' giong IDLE_BREATH_SPEED cua local cho dong bo cam giac
    Private Const REMOTE_BREATH_AMPLITUDE As Double = 0.025    ' bien do phong to/thu nho khi "tho" luc dung yen (ty le)
    Private Const REMOTE_MOVE_SPEED_THRESHOLD As Double = 0.15 ' toc do (don vi map/giay) tro len moi tinh la "dang di bo"
    Private Const REMOTE_CROUCH_POSE_THRESHOLD As Double = 0.5 ' rp.Crouch tu muc nay tro len moi doi sang anh tu the ngoi
    Private Const REMOTE_CROUCH_HEIGHT_SCALE As Double = 0.72  ' sprite thap di con lai bao nhieu % chieu cao khi ngoi het co
    Private Const REMOTE_JUMP_POSE_THRESHOLD As Double = 0.05  ' rp.Z (do cao) tu muc nay tro len moi tinh la "dang lo lung" - doi sang anh tu the nhay

    ' ==== Trang tri (bui co, cay hoa) - chi de nhin, khong nhat duoc, khong va cham ====
    Private texGrass() As Integer
    Private texGrassW As Integer = 0
    Private texGrassH As Integer = 0
    Private texFlower() As Integer
    Private texFlowerW As Integer = 0
    Private texFlowerH As Integer = 0

    ' ==== Bau troi toan canh (panorama), xoay theo goc nhin - Nothing neu chua co, fallback mau phang ====
    Private texSky() As Integer
    Private texSkyW As Integer = 0
    Private texSkyH As Integer = 0

    Private decorations As New List(Of DecorationItem)
    Private Const DECORATION_DENSITY As Double = 0.35

    ' ==== Anh tay that (view model) - Nothing neu thieu file, se fallback ve vector ====
    Private handOpenImg As Bitmap
    Private handFistImg As Bitmap
    Private handHoldingImg As Bitmap
    Private handHoldingBowImg As Bitmap ' tay trai cam cung rieng (Assets\Hands\hand_holding_bow.png), khong can ban lat
    Private handOpenImgMirror As Bitmap
    Private handFistImgMirror As Bitmap
    Private handHoldingImgMirror As Bitmap

    Public Sub New(modeArg As String, ipArg As String, portArg As Integer)
        Me.Text = "GamePvP 3D - Phieu Luu Hai Nam (Test Build)"
        Me.ClientSize = New Size(WIN_W, WIN_H)
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.KeyPreview = True
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer, True)

        frameBmp = New Bitmap(RES_W, RES_H, PixelFormat.Format32bppRgb)
        LoadTextures()
        LoadHandTextures()
        LoadCharacterTexture()
        LoadDecorationTextures()
        LoadSkyTexture()

        AddHandler Me.KeyDown, AddressOf Form1_KeyDown
        AddHandler Me.KeyUp, AddressOf Form1_KeyUp
        AddHandler Me.MouseDown, AddressOf Form1_MouseDown
        AddHandler Me.MouseUp, AddressOf Form1_MouseUp
        AddHandler Me.MouseMove, AddressOf Form1_MouseMove
        AddHandler Me.Activated, AddressOf Form1_Activated
        AddHandler Me.Deactivate, AddressOf Form1_Deactivate
        AddHandler Me.Paint, AddressOf Form1_Paint

        InitItemCatalog()
        InitInventory()
        SpawnMushrooms(20)
        SpawnDecorations()
        SpawnWorldItems(8)

        lastTick = DateTime.Now
        gameTimer.Interval = 16

        netPort = portArg
        netHostIp = ipArg
        StartNetworking(modeArg)
    End Sub

    Private Sub Form1_KeyDown(sender As Object, e As KeyEventArgs)
        pressedKeys.Add(e.KeyCode)
        If e.KeyCode = Keys.Escape Then Me.Close()

        Select Case e.KeyCode
            Case Keys.D1, Keys.NumPad1 : EquipSlot(0)
            Case Keys.D2, Keys.NumPad2 : EquipSlot(1)
            Case Keys.D3, Keys.NumPad3 : EquipSlot(2)
            Case Keys.D4, Keys.NumPad4 : EquipSlot(3)
            Case Keys.D5, Keys.NumPad5 : EquipSlot(4)
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
        Dim dx As Integer = e.X - centerX
        If dx <> 0 Then
            playerAngle += dx * MOUSE_SENSITIVITY
            CenterCursor()
        End If
    End Sub

    ' Cho nang cap sau: goc de xu ly danh/ban khi da co item/vu khi trang bi ben chuot trai.
    ' Vi du: neu equipped item la vu khi thi phat animation chem/ban + kiem tra va cham voi doi thu (PvP).
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

        If netMode = NetMode.Host Then
            hub.Broadcast("ARROW|" & localSlot & "|" & p.Id & "|" & FmtD(p.X) & "|" & FmtD(p.Y) & "|" & FmtD(p.Angle) & "|" & FmtD(p.Speed))
        ElseIf netMode = NetMode.Client AndAlso peer IsNot Nothing AndAlso peer.IsConnected Then
            peer.SendLine("SHOOTREQ|" & p.Id & "|" & FmtD(p.X) & "|" & FmtD(p.Y) & "|" & FmtD(p.Angle) & "|" & FmtD(p.Speed))
        End If
    End Sub

    ' Gui bao cao "da trung don" ve Host - Host la nguon du lieu goc duy nhat thuc su
    ' tru mau, giong het co che PICKREQ/ApplyPickup dang dung cho nam.
    Private Sub SendAttack(targetSlot As Integer, damage As Integer)
        If netMode = NetMode.Host Then
            ApplyDamage(localSlot, targetSlot, damage)
        ElseIf netMode = NetMode.Client AndAlso peer IsNot Nothing AndAlso peer.IsConnected Then
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
        If netMode = NetMode.Host Then
            hub.Broadcast("HPSELF|0|" & playerHealth)
        ElseIf netMode = NetMode.Client AndAlso peer IsNot Nothing AndAlso peer.IsConnected Then
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
    Private Sub StartNetworking(modeArg As String)
        Select Case modeArg
            Case "host"
                netMode = NetMode.Host
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
                netMode = NetMode.Client
                peer = New NetworkPeer(Me)
                AddHandler peer.Connected, AddressOf Peer_Connected
                AddHandler peer.Disconnected, AddressOf Peer_Disconnected
                AddHandler peer.LineReceived, AddressOf Peer_LineReceived
                netStatusText = "Dang ket noi toi " & netHostIp & ":" & netPort & "..."
                peer.ConnectToHost(netHostIp, netPort)
            Case Else
                netMode = NetMode.None
                netStatusText = ""
        End Select
    End Sub

    ' ---- Host: co khach moi vao ----
    Private Sub Hub_ClientConnected(slotIndex As Integer)
        netStatusText = "Nguoi choi #" & slotIndex & " da vao phong (" & (hub.ConnectedCount) & "/3 khach)."
        hub.SendTo(slotIndex, BuildWelcomeLine(slotIndex))
        hub.SendTo(slotIndex, BuildItemSyncLine("ITEMSYNC"))
        hub.SendTo(slotIndex, BuildPosLine(0, playerX, playerY, playerAngle, playerZ, crouchAmount))
        For Each kv In remotePlayers
            hub.SendTo(slotIndex, BuildPosLine(kv.Key, kv.Value.X, kv.Value.Y, kv.Value.Angle, kv.Value.Z, kv.Value.Crouch))
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
    Private Function BuildPosLine(slot As Integer, x As Double, y As Double, ang As Double, z As Double, crouch As Double) As String
        Return "POS|" & slot & "|" & x.ToString(CultureInfo.InvariantCulture) & "|" & y.ToString(CultureInfo.InvariantCulture) &
               "|" & ang.ToString(CultureInfo.InvariantCulture) & "|" & z.ToString(CultureInfo.InvariantCulture) &
               "|" & crouch.ToString(CultureInfo.InvariantCulture)
    End Function

    Private Function BuildMushroomSyncLine(prefix As String) As String
        Dim sb As New Text.StringBuilder(prefix)
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
        If slot = localSlot AndAlso netMode <> NetMode.None Then Return
        Dim x As Double, y As Double, ang As Double, z As Double, crouch As Double
        If Double.TryParse(parts(2), NumberStyles.Float, CultureInfo.InvariantCulture, x) AndAlso
           Double.TryParse(parts(3), NumberStyles.Float, CultureInfo.InvariantCulture, y) AndAlso
           Double.TryParse(parts(4), NumberStyles.Float, CultureInfo.InvariantCulture, ang) AndAlso
           Double.TryParse(parts(5), NumberStyles.Float, CultureInfo.InvariantCulture, z) AndAlso
           Double.TryParse(parts(6), NumberStyles.Float, CultureInfo.InvariantCulture, crouch) Then
            Dim rp As RemotePlayerState = GetOrCreateRemote(slot)
            Dim now As DateTime = DateTime.Now

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
        Dim sb As New Text.StringBuilder(prefix)
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
        If netMode = NetMode.Host Then
            hub.Broadcast(BuildPosLine(0, playerX, playerY, playerAngle, playerZ, crouchAmount))
        ElseIf netMode = NetMode.Client AndAlso peer IsNot Nothing AndAlso peer.IsConnected Then
            peer.SendLine(BuildPosLine(localSlot, playerX, playerY, playerAngle, playerZ, crouchAmount))
        End If
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        gameTimer.Start()
        EngageMouseLook()
    End Sub

    ' ---------------------------------------------------------------
    '  Nap texture tu thu muc Assets\ (cung cap bang, resize san 128x128).
    '  Neu thieu file thi dung mau phang thay the de game khong bi crash.
    ' ---------------------------------------------------------------
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
    '  Nap anh tay that (hand_open/hand_fist/hand_holding.png). Neu thieu
    '  file thi de Nothing - DrawViewmodel se tu dong fallback ve vector.
    ' ---------------------------------------------------------------
    Private Sub LoadHandTextures()
        handOpenImg = TryLoadBitmap("Hands", "hand_open.png")
        handFistImg = TryLoadBitmap("Hands", "hand_fist.png")
        handHoldingImg = TryLoadBitmap("Hands", "hand_holding.png")
        handHoldingBowImg = TryLoadBitmap("Hands", "hand_holding_bow.png") ' Nothing neu chua co file -> tu dong fallback ve hand_holding thuong
        If handOpenImg IsNot Nothing Then handOpenImgMirror = MirrorBitmap(handOpenImg)
        If handFistImg IsNot Nothing Then handFistImgMirror = MirrorBitmap(handFistImg)
        If handHoldingImg IsNot Nothing Then handHoldingImgMirror = MirrorBitmap(handHoldingImg)
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
                            Dim kind As Integer = If(decorRng.NextDouble() < 0.75, 0, 1)
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
        UpdateRemoteAnimations(dt)
        NetworkTick()
        Me.Invalidate()
    End Sub

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

        ' Camera offset man hinh: nhay len -> the gioi lech xuong, ngoi -> the gioi lech len
        viewShiftPx = CInt((playerZ - crouchAmount * CROUCH_MAX_HEIGHT) * VIEW_SHIFT_SCALE)

        ' Di chuyen cham hon khi dang ngoi
        Dim crouchSpeedFactor As Double = 1.0 - crouchAmount * 0.5
        Dim moveStep As Double = moveSpeed * speedMultiplier * crouchSpeedFactor * dt

        ' ---- Hoat anh nhun tay khi di bo ----
        Dim isMoving As Boolean = pressedKeys.Contains(Keys.W) OrElse pressedKeys.Contains(Keys.Up) OrElse
                                   pressedKeys.Contains(Keys.S) OrElse pressedKeys.Contains(Keys.Down) OrElse
                                   pressedKeys.Contains(Keys.A) OrElse pressedKeys.Contains(Keys.D)
        Dim bobTarget As Double = If(isMoving AndAlso Not isJumping, 1.0, 0.0)
        If bobAmount < bobTarget Then
            bobAmount = Math.Min(bobTarget, bobAmount + BOB_LERP_SPEED * dt)
        Else
            bobAmount = Math.Max(bobTarget, bobAmount - BOB_LERP_SPEED * dt)
        End If
        If isMoving Then bobPhase += BOB_SPEED * dt * crouchSpeedFactor
        idlePhase += IDLE_BREATH_SPEED * dt

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
        Dim cellType As Integer = mapData(my, mx)
        Select Case cellType
            Case 0
                Return True
            Case 3 ' kien hang thap: chi qua duoc khi dang nhay du cao
                Return playerZ >= CRATE_JUMP_HEIGHT
            Case 4 ' khe chui: chi qua duoc khi dang ngoi du thap
                Return crouchAmount >= CROUCH_PASS_THRESHOLD
            Case Else ' tuong da (1, 2) luon chan
                Return False
        End Select
    End Function

    Private Sub CheckPickup()
        If netMode = NetMode.Client Then
            CheckPickupClientRequest()
            Return
        End If

        Dim i As Integer = mushrooms.Count - 1
        While i >= 0
            Dim m As MushroomItem = mushrooms(i)
            Dim ddx As Double = m.Pos.X - playerX
            Dim ddy As Double = m.Pos.Y - playerY
            If (ddx * ddx + ddy * ddy) < 0.16 Then
                If netMode = NetMode.Host Then
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
                If peer IsNot Nothing AndAlso peer.IsConnected Then
                    peer.SendLine("PICKREQ|" & localSlot & "|" & m.Id)
                End If
            End If
        Next
    End Sub

    ' ---- Nhat vat pham (dao/kiem/cung/thuoc...) vao tui - cung co che voi nam ----
    Private Sub CheckItemPickup()
        If netMode = NetMode.Client Then
            CheckItemPickupClientRequest()
            Return
        End If

        Dim i As Integer = worldItems.Count - 1
        While i >= 0
            Dim w As WorldItemSpawn = worldItems(i)
            Dim ddx As Double = w.Pos.X - playerX
            Dim ddy As Double = w.Pos.Y - playerY
            If (ddx * ddx + ddy * ddy) < 0.16 AndAlso HasEmptySlot() Then
                If netMode = NetMode.Host Then
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
    Private Function PickDirectionalTexture(slot As Integer, rp As RemotePlayerState, walkFrameOn As Boolean, ByRef outW As Integer, ByRef outH As Integer) As Integer()
        Dim idx As Integer = slot Mod CHARACTER_SLOT_COUNT
        Dim viewerDir As Double = Math.Atan2(playerY - rp.Y, playerX - rp.X)
        Dim relAngle As Double = NormalizeAngle(viewerDir - rp.Angle)
        Dim absRel As Double = Math.Abs(relAngle)

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
        If netMode = NetMode.None OrElse remotePlayers.Count = 0 Then Return

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
            Dim texForSlot As Integer() = PickDirectionalTexture(kv.Key, rp, walkFrameOn, texW, texH)

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
        Dim isRangedHeld As Boolean = curItemForHands IsNot Nothing AndAlso curItemForHands.Kind = ItemKind.Weapon AndAlso curItemForHands.IsRanged

        ' Do nam tay (0 = mo het, 1 = nam chat): dung yen thi "tho" nhe qua lai,
        ' dang chay thi nam chac hon voi nhip theo buoc chan, dang cam do thi luon nam chat.
        Dim gripAmount As Double
        If isHolding Then
            gripAmount = 1.0
        ElseIf bobAmount > 0.05 Then
            gripAmount = 0.65 + 0.2 * Math.Sin(bobPhase * 2.0) * bobAmount
        Else
            gripAmount = 0.5 + 0.3 * Math.Sin(idlePhase)
        End If
        gripAmount = Math.Max(0.0, Math.Min(1.0, gripAmount))

        ' Khi dang cam do, 2 tay khep lai gan giua man hinh hon (nhu dang giu chung 1 vat)
        Dim handSpread As Double = If(isHolding, 0.40, 0.30)

        ' Hoat anh vung vu khi: 0 -> 1 -> 0 trong suot ATTACK_SWING_DURATION (tay dua len-ra
        ' truoc roi tro ve), dung sin de vao/ra muot thay vi giat cuc.
        Dim swingT As Double = If(isHolding AndAlso attackSwingTime > 0.0, 1.0 - attackSwingTime / ATTACK_SWING_DURATION, 0.0)
        Dim swingPunch As Single = CSng(Math.Sin(swingT * Math.PI))
        Dim swingRise As Single = swingPunch * 70.0F
        Dim swingForward As Single = swingPunch * 45.0F

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
        Dim rightX As Single = CSng(WIN_W * (1.0 - handSpread)) - bobX - swingForward + pullBack
        Dim leftY As Single = baseY + bobY
        Dim rightY As Single = baseY - bobY - CSng(bowDrawT * 15.0) ' tay keo day nhich len gan ma hon

        If handOpenImg IsNot Nothing OrElse handFistImg IsNot Nothing OrElse handHoldingImg IsNot Nothing Then
            DrawHandTextured(g, leftX, leftY, True, gripAmount, isHolding, overrideHoldingImg:=If(isRangedHeld, handHoldingBowImg, Nothing))
            DrawHandTextured(g, rightX, rightY, False, gripAmount, isHolding OrElse isDrawingBow, forceFist:=isDrawingBow)
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
    ' cho handHoldingImg/handHoldingImgMirror mac dinh - chi ap dung cho ben goi truyen vao
    ' (thuong la tay trai, anh khong can ban lat vi luon dung o vi tri trai).
    Private Sub DrawHandTextured(g As Graphics, cx As Single, wristY As Single, isLeft As Boolean, gripAmount As Double, isHolding As Boolean, Optional forceFist As Boolean = False, Optional overrideHoldingImg As Bitmap = Nothing)
        Dim imgOpen As Bitmap = If(isLeft, handOpenImg, handOpenImgMirror)
        Dim imgFist As Bitmap = If(isLeft, handFistImg, handFistImgMirror)
        Dim imgHolding As Bitmap = If(overrideHoldingImg IsNot Nothing, overrideHoldingImg, If(isLeft, handHoldingImg, handHoldingImgMirror))

        Dim refImg As Bitmap = If(imgHolding, If(imgFist, imgOpen))
        If refImg Is Nothing Then Return

        Dim drawH As Single = 360.0F
        Dim aspect As Single = CSng(refImg.Width) / CSng(refImg.Height)
        Dim drawW As Single = drawH * aspect

        ' Canh duoi anh (co tay ao) neo o wristY, nghieng vao giua man hinh
        Dim destX As Single = If(isLeft, cx - drawW * 0.30F, cx - drawW * 0.70F)
        Dim destY As Single = wristY - drawH

        If forceFist AndAlso imgFist IsNot Nothing Then
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
                    Dim hudText As String = String.Format("{0}    Diem: {1}    Cap do: {2}    Toc do: x{3:0.00}    Nam con lai: {4}    Vat pham: {5}    Dung cu: {6}", hpText, score, level, speedMultiplier, mushrooms.Count, worldItems.Count, heldItemName)
                    g.DrawString(hudText, f, shadow, 11, 11)
                    g.DrawString(hudText, f, brush, 10, 10)
                    Dim hint As String = "Di chuot: xoay | WASD: di chuyen | Chuot phai: nhay | Ctrl/C: ngoi | 1-5: doi do | ESC: thoat"
                    g.DrawString(hint, f, shadow, 11, WIN_H - 29)
                    g.DrawString(hint, f, brush, 10, WIN_H - 30)
                End Using
            End Using
        End Using

        If netMode <> NetMode.None Then DrawNetHud(g)
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
        Dim cell As Integer = 10
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

        If netMode <> NetMode.None Then
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
            If handOpenImg IsNot Nothing Then handOpenImg.Dispose()
            If handFistImg IsNot Nothing Then handFistImg.Dispose()
            If handHoldingImg IsNot Nothing Then handHoldingImg.Dispose()
            If handHoldingBowImg IsNot Nothing Then handHoldingBowImg.Dispose()
            If handOpenImgMirror IsNot Nothing Then handOpenImgMirror.Dispose()
            If handFistImgMirror IsNot Nothing Then handFistImgMirror.Dispose()
            If handHoldingImgMirror IsNot Nothing Then handHoldingImgMirror.Dispose()
            If hub IsNot Nothing Then hub.StopListening()
            If peer IsNot Nothing Then peer.CloseConnection()
        End If
        ReleaseMouseLook()
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

        Using connectForm As New ConnectForm()
            If connectForm.ShowDialog() = DialogResult.OK Then
                Application.Run(New Form1(connectForm.ResultMode, connectForm.ResultIp, connectForm.ResultPort))
            End If
        End Using
    End Sub
End Module
