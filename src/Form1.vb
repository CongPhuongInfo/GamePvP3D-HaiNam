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

    ' ==== Ban do (0 = dat trong, 1 = tuong da [con lai rat it, chi lam moc ranh gioi cu],
    '      2 = tuong da tim, 3 = khuc go do [nhay qua], 4 = khe da [ngoi qua],
    '      5 = mo dat/re cay thap [di len duoc], 6 = tang da cao [di len duoc],
    '      7 = CAY (than cay doc lap, di vong quanh duoc, la loai chinh cua rung)).
    '  Ban than du lieu map (nhieu lua chon) nam trong GameMaps.vb - MAP_W/H GIONG
    '  NHAU cho tat ca map (16x16) de don gian hoa, chi noi dung o ben trong khac
    '  nhau. mapData/torchLights KHONG con la ReadOnly vi duoc gan lai luc chon map
    '  (ApplyMapSelection trong GameMaps.vb), ke ca gan lai lan 2 luc Client nhan
    '  duoc WELCOME tu Host bao map thuc su dang choi (xem Peer_LineReceived). ====
    Private Const MAP_W As Integer = 32
    Private Const MAP_H As Integer = 32
    Private mapData As Integer(,)
    Private currentMapIndex As Integer = 0
    Private mapFogDist As Double = 12.0 ' tam nhin truoc khi mo suong day dac - moi map tu dat rieng, xem MapDefinition.FogDistance

    ' ==== Buc/bac cao do di duoc (khong can nhay, camera tu noi len khi buoc vao) ====
    Private Const PLATFORM_LOW_HEIGHT As Double = 0.35   ' do cao buc thap (loai o 5), don vi = 1 buc tuong day
    Private Const PLATFORM_HIGH_HEIGHT As Double = 0.7   ' do cao buc cao (loai o 6)
    Private Const STAND_HEIGHT_LERP_SPEED As Double = 3.0 ' toc do noi suy camera khi buoc len/xuong buc

    ' ==== Nguoi choi ====
    Private playerX As Double = 1.5
    Private playerY As Double = 1.5
    Private playerAngle As Double = 0.3
    Private moveSpeed As Double = 2.6
    Private rotSpeed As Double = 2.2 ' KHONG con dung nua - Left/Right da doi thanh di ngang (strafe) trong GameInput.vb, xoay camera gio chi con bang chuot. Giu lai phong khi can dung lai.
    Private speedMultiplier As Double = 1.0

    ' ==== Den duoc / diem sang (torch light) ====
    Private Structure TorchLight
        Public X As Double
        Public Y As Double
        Public Radius As Double       ' ban kinh chieu sang toi da, don vi = 1 o ban do
        Public FlickerSeed As Double  ' lech pha rieng de cac den khong nhap nhay dong bo voi nhau
    End Structure
    ' Du lieu thuc te (toa do theo tung map) nam trong GameMaps.vb, gan qua ApplyMapSelection.
    Private torchLights As TorchLight()
    Private Const TORCH_BRIGHTNESS As Double = 0.65 ' do sang toi da den cong them vao fog nen

    ' ==== Fog pha mau (blend ve mau suong toi thay vi chi lam toi don thuan) ====
    Private Const FOG_COLOR_R As Integer = 150 ' suong rung ban ngay - xanh xam nhat thay vi den xanh dem
    Private Const FOG_COLOR_G As Integer = 175
    Private Const FOG_COLOR_B As Integer = 150

    ' ==== Vignette (toi nhe 4 goc man hinh, tang chieu sau/cam giac ong kinh) ====
    Private Const VIGNETTE_START As Double = 0.55  ' ban kinh (0..1 tinh tu giua man hinh) bat dau toi dan
    Private Const VIGNETTE_STRENGTH As Double = 0.55 ' do toi toi da o 4 goc (0 = khong toi, 1 = den han)

    ' ==== Bong do gia duoi chan sprite ====
    Private Const SHADOW_MAX_DIST As Double = 14.0 ' qua khoang cach nay thi khong ve bong (da bi fog che)
    Private Const MOUSE_SENSITIVITY As Double = 0.0035
    Private mouseLookEnabled As Boolean = False
    Private ignoreNextMouseMove As Boolean = False
    Private cursorCaptured As Boolean = False

    ' ==== Ngua/cui bang chuot (mouse pitch look) ====
    ' Ky thuat "horizon shift" don gian (dich toan bo man hinh len/xuong theo pixel),
    ' khong phai true 3D pitch that su (raycaster nay chi co tia ngang, khong nghieng
    ' tia theo chieu doc) - du de tao cam giac ngua/cui trong pham vi vua phai, gioi han
    ' boi PITCH_MAX_PX de tranh meo hinh qua nang o goc gan +-90 do.
    Private pitchShiftPx As Integer = 0
    Private Const MOUSE_PITCH_SENSITIVITY As Double = 0.6
    Private Const PITCH_MAX_PX As Integer = 70
    Private Const INVERT_MOUSE_PITCH As Boolean = False ' True neu muon dao chieu (keo chuot len = cui xuong)

    ' ==== Hoat anh nhun tay khi di bo (view model kieu FPS) ====
    Private bobPhase As Double = 0.0
    Private bobAmount As Double = 0.0   ' 0 = dung yen, 1 = dang nhun het co
    Private Const BOB_SPEED As Double = 8.0
    Private Const BOB_LERP_SPEED As Double = 6.0
    Private idlePhase As Double = 0.0   ' nhip "tho" cham cua tay khi dung yen (mo/nam nhe tu nhien)
    Private Const IDLE_BREATH_SPEED As Double = 1.3
    Private worldTime As Double = 0.0   ' thoi gian troi qua tu luc mo game (giay), dung cho nhap nhay den/hieu ung theo thoi gian

    ' ==== Nhay / ngoi (mo phong bang do lech camera theo chieu doc) ====
    Private playerZ As Double = 0.0        ' do cao hien tai khi nhay, 0 = duoi dat (tinh tu mat buc dang dung)
    Private zVelocity As Double = 0.0
    Private isJumping As Boolean = False
    Private crouchAmount As Double = 0.0   ' 0 = dung thang, 1 = ngoi het co (noi suy muot)
    Private standHeight As Double = 0.0    ' do cao cua buc/nen dang dung (noi suy muot khi buoc len/xuong buc 5,6)
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
    Private pickupAnimTime As Double = 0.0    ' > 0 = dang trong hoat anh tay hai nam, dem nguoc ve 0
    Private isDrawingBow As Boolean = False       ' dang giu chuot trai keo cung (chua ban)
    Private drawStartTime As DateTime = DateTime.MinValue
    Private drawingItem As ItemDefinition = Nothing ' item cung dang duoc keo, phong khi doi tay giua chung
    Private Const ATTACK_SWING_DURATION As Double = 0.22
    Private Const PICKUP_ANIM_DURATION As Double = 0.35 ' hoi lau hon vung vu khi vi la dong tac cui/vuon xuong
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
    Private curNetMode As NetMode = NetMode.None
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
    Private texTreeBark() As Integer ' KHONG nap tu file - tu sinh bang code (xem GenerateForestTextures trong GameRender.vb)
    Private texBushWall() As Integer ' Bui co ram - vien ban do "Canh Dong Rong" (loai o 8), xem GenerateForestTextures
    Private texCliffWall() As Integer ' Vach da - loai o 9
    Private texCreviceWall() As Integer ' Khe nut dat - loai o 10
    Private texRiverbankWall() As Integer ' Bo suoi - loai o 11
    Private texTree() As Integer = Nothing ' Anh that Assets\Forest\tree_billboard.png (tuy chon) - Nothing = dung TreePixel ve bang cong thuc
    Private texTreeW As Integer = 0
    Private texTreeH As Integer = 0

    ' Anh mui ten bay (Assets\Items\arrow.png) - anh goc VE NGANG (dau nhon ben phai,
    ' long vu ben trai) nhung billboard bay ve THEO TRUC DOC (dau nhon o tren) cho dung
    ' cam giac "dang lap ten chuan bi ban". ProjectilePixel() se hoan doi truc luc sample.
    Private texArrow() As Integer

    ' ==== Sprite nhan vat cho nguoi choi khac (PvP) - Nothing neu chua co, se fallback ve hinh nguoi don gian ====
    ' Moi slot (0=Host, 1-3=khach) co the co 1 anh nhan vat rieng (character_0.png..character_3.png).
    ' Slot nao thieu file rieng thi dung chung "character.png" mac dinh - khong bat buoc phai
    ' co du 4 anh moi chay duoc.
    Private Const CHARACTER_SLOT_COUNT As Integer = 4
    Private texCharacterBySlot(CHARACTER_SLOT_COUNT - 1)() As Integer
    Private texCharacterWBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterHBySlot(CHARACTER_SLOT_COUNT - 1) As Integer

    ' Sprite nhan vat theo HUONG NHIN (truoc/ngang/sau), rieng cho tung slot. Anh "ngang"
    ' chi can ve 1 ben (vd nhin sang phai), ben con lai tu lat guong (Mirror) luc nap, khong
    ' can ve 2 ban. Slot nao thieu anh _side/_back rieng thi tam dung anh truoc (character_N.png)
    ' thay the - khong bat buoc phai co du 3 huong moi chay duoc.
    Private texCharacterSideBySlot(CHARACTER_SLOT_COUNT - 1)() As Integer
    Private texCharacterSideMirrorBySlot(CHARACTER_SLOT_COUNT - 1)() As Integer
    Private texCharacterSideWBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterSideHBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterBackBySlot(CHARACTER_SLOT_COUNT - 1)() As Integer
    Private texCharacterBackWBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterBackHBySlot(CHARACTER_SLOT_COUNT - 1) As Integer

    ' Anh "sai chan" (mid-stride) rieng cho tung huong - dung de tao hoat anh di bo 2-frame
    ' (frame dung yen <-> frame sai chan) cho nguoi choi khac. File: character_N_walk.png,
    ' character_N_side_walk.png, character_N_back_walk.png. Thieu file nao thi tam dung anh
    ' dung yen cung huong do thay the (khong bat buoc phai co du moi huong moi chay duoc,
    ' se chi khong thay doi frame khi di bo nhung van con nhun/tho).
    Private texCharacterWalkBySlot(CHARACTER_SLOT_COUNT - 1)() As Integer
    Private texCharacterWalkWBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterWalkHBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterSideWalkBySlot(CHARACTER_SLOT_COUNT - 1)() As Integer
    Private texCharacterSideWalkMirrorBySlot(CHARACTER_SLOT_COUNT - 1)() As Integer
    Private texCharacterSideWalkWBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterSideWalkHBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterBackWalkBySlot(CHARACTER_SLOT_COUNT - 1)() As Integer
    Private texCharacterBackWalkWBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterBackWalkHBySlot(CHARACTER_SLOT_COUNT - 1) As Integer

    ' Anh tu the NGOI (crouch) rieng cho tung huong - dung khi nguoi choi khac dang ngoi
    ' (rp.Crouch >= REMOTE_CROUCH_POSE_THRESHOLD). File: character_N_crouch.png,
    ' character_N_side_crouch.png, character_N_back_crouch.png. Thieu file nao thi tam dung
    ' anh dung yen cung huong thay the (khong bat buoc phai co du moi huong moi chay duoc,
    ' se chi khong doi dang khi ngoi nhung sprite van thap xuong dung theo Crouch).
    Private texCharacterCrouchBySlot(CHARACTER_SLOT_COUNT - 1)() As Integer
    Private texCharacterCrouchWBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterCrouchHBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterSideCrouchBySlot(CHARACTER_SLOT_COUNT - 1)() As Integer
    Private texCharacterSideCrouchMirrorBySlot(CHARACTER_SLOT_COUNT - 1)() As Integer
    Private texCharacterSideCrouchWBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterSideCrouchHBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterBackCrouchBySlot(CHARACTER_SLOT_COUNT - 1)() As Integer
    Private texCharacterBackCrouchWBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterBackCrouchHBySlot(CHARACTER_SLOT_COUNT - 1) As Integer

    ' Anh tu the NHAY (jump) rieng cho tung huong - dung khi nguoi choi khac dang lo lung
    ' tren khong (rp.Z >= REMOTE_JUMP_POSE_THRESHOLD). File: character_N_jump.png,
    ' character_N_side_jump.png, character_N_back_jump.png. Thieu file nao thi tam dung
    ' anh dung yen cung huong thay the (khong bat buoc phai co du moi huong moi chay duoc,
    ' se chi khong doi dang khi nhay nhung sprite van duoc nang len dung theo Z co san).
    Private texCharacterJumpBySlot(CHARACTER_SLOT_COUNT - 1)() As Integer
    Private texCharacterJumpWBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterJumpHBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterSideJumpBySlot(CHARACTER_SLOT_COUNT - 1)() As Integer
    Private texCharacterSideJumpMirrorBySlot(CHARACTER_SLOT_COUNT - 1)() As Integer
    Private texCharacterSideJumpWBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterSideJumpHBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterBackJumpBySlot(CHARACTER_SLOT_COUNT - 1)() As Integer
    Private texCharacterBackJumpWBySlot(CHARACTER_SLOT_COUNT - 1) As Integer
    Private texCharacterBackJumpHBySlot(CHARACTER_SLOT_COUNT - 1) As Integer

    ' Anh nhan vat DANG CAM VU KHI (3rd-person, xem Assets\Characters\PROMPTS.md muc
    ' "SPRITE CAM VU KHI"). Chi co ban DUNG YEN cho 3 huong (front/side/back), dung chung
    ' cho ca luc di/ngoi/nhay vi nhan vat hien thi nho va xa. Key dictionary dang
    ' "<vukhi>_<huong>_<slot>", vd "sword_front_0", "bow_side_2". Neu thieu key nao thi
    ' PickDirectionalTexture() tu dong fallback ve sprite tay khong nhu cu, KHONG bat buoc
    ' phai co du 36 anh moi chay duoc. Huong "side" luu them ban lat guong rieng
    ' (tex...SideMirror) giong cach lam voi character_N_side.png.
    Private texCharacterHolding As New Dictionary(Of String, Integer())
    Private texCharacterHoldingW As New Dictionary(Of String, Integer)
    Private texCharacterHoldingH As New Dictionary(Of String, Integer)
    Private texCharacterHoldingSideMirror As New Dictionary(Of String, Integer())

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

    ' ==== Anh tay that (view model), RIENG THEO TUNG SLOT NHAN VAT (0..3) - Nothing
    ' neu thieu file, se fallback ve vector. File theo slot: hand_open_N.png...; neu
    ' thieu file rieng cho slot do thi tu dong dung ban khong so (hand_open.png...)
    ' lam du phong chung (xem LoadHandTextures trong GameAssets.vb). ====
    Private handOpenImgBySlot(CHARACTER_SLOT_COUNT - 1) As Bitmap
    Private handFistImgBySlot(CHARACTER_SLOT_COUNT - 1) As Bitmap
    Private handHoldingImgBySlot(CHARACTER_SLOT_COUNT - 1) As Bitmap
    Private handHoldingBowImgBySlot(CHARACTER_SLOT_COUNT - 1) As Bitmap ' tay trai cam cung rieng, khong can ban lat
    Private handHoldingBowDrawnImgBySlot(CHARACTER_SLOT_COUNT - 1) As Bitmap ' tay trai cam cung LUC DANG KEO day (day cong, co mui ten) - thieu thi fallback ve handHoldingBowImgBySlot thuong
    Private handPullingStringImgBySlot(CHARACTER_SLOT_COUNT - 1) As Bitmap ' tay PHAI (ve theo khung tay trai) dang keo day cung, phai lat guong nhu dao/kiem
    Private handPullingStringImgMirrorBySlot(CHARACTER_SLOT_COUNT - 1) As Bitmap
    Private handOpenImgMirrorBySlot(CHARACTER_SLOT_COUNT - 1) As Bitmap
    Private handFistImgMirrorBySlot(CHARACTER_SLOT_COUNT - 1) As Bitmap
    Private handHoldingImgMirrorBySlot(CHARACTER_SLOT_COUNT - 1) As Bitmap
    Private handHoldingDaggerImgBySlot(CHARACTER_SLOT_COUNT - 1) As Bitmap ' tay PHAI cam dao gam (xem ghi chu o handHoldingSwordImgBySlot)
    Private handHoldingSwordImgBySlot(CHARACTER_SLOT_COUNT - 1) As Bitmap ' tay PHAI cam kiem - dao/kiem/cung deu ve theo khung tay trai
    ' (giong hand_open_N) nen phai lat guong truoc khi dan sang vi tri tay phai, khac voi
    ' handHoldingBowImgBySlot (dung nguyen cho tay trai, khong lat).
    Private handHoldingDaggerImgMirrorBySlot(CHARACTER_SLOT_COUNT - 1) As Bitmap
    Private handHoldingSwordImgMirrorBySlot(CHARACTER_SLOT_COUNT - 1) As Bitmap

    ' ==== Icon vu khi (Assets\Items\bow|dagger|sword.png) dang o dang mang pixel ARGB tho,
    ' dung de ve "badge" nho bao nguoi choi KHAC dang cam vu khi gi (xem DrawRemotePlayerSprites
    ' trong GameRender.vb va LoadWeaponIconPixels trong GameAssets.vb). Rieng biet voi
    ' itemIcons(Bitmap, dung cho HUD/inventory cua chinh minh) vi renderer ban tia can
    ' mang Integer() tho de sample nhanh, khong dung GDI+ Bitmap. Nothing = chua co file.
    Private weaponIconPixelsBow As Integer(), weaponIconWBow As Integer, weaponIconHBow As Integer
    Private weaponIconPixelsDagger As Integer(), weaponIconWDagger As Integer, weaponIconHDagger As Integer
    Private weaponIconPixelsSword As Integer(), weaponIconWSword As Integer, weaponIconHSword As Integer

    ' mapIndexArg: map do nguoi choi chon o ConnectForm truoc khi bat dau (xem GameMaps.vb
    ' de biet danh sach map). Voi che do Join, day chi la GIA TRI TAM (placeholder) de co
    ' gi do de ve ngay khi cua so hien len - ApplyMapSelection() se duoc goi LAI voi map
    ' THAT SU cua Host ngay khi nhan duoc WELCOME (xem Peer_LineReceived trong GameHub.vb),
    ' vi trong PvP ca phong BAT BUOC phai dung chung 1 map, Client khong tu quyet duoc.
    Public Sub New(modeArg As String, ipArg As String, portArg As Integer, mapIndexArg As Integer)
        Me.Text = "GamePvP 3D - Phieu Luu Hai Nam (Test Build)"
        Me.ClientSize = New Size(WIN_W, WIN_H)
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.KeyPreview = True
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer, True)

        frameBmp = New Bitmap(RES_W, RES_H, PixelFormat.Format32bppRgb)
        LoadTextures()
        GenerateForestTextures() ' sinh texture vo cay + ghi de san co bang cong thuc, khong can file anh
        LoadHandTextures()
        LoadWeaponIconPixels()
        LoadCharacterTexture()
        LoadCharacterHoldingTextures()
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
        ApplyMapSelection(mapIndexArg) ' gan mapData/torchLights/vi tri spawn + tu goi SpawnDecorations()
        SpawnMushrooms(20)
        SpawnWorldItems(8)

        lastTick = DateTime.Now
        gameTimer.Interval = 16

        netPort = portArg
        netHostIp = ipArg
        StartNetworking(modeArg)
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
                Application.Run(New Form1(connectForm.ResultMode, connectForm.ResultIp, connectForm.ResultPort, connectForm.ResultMapIndex))
            End If
        End Using
    End Sub
End Module
