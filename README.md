# GamePvP3D_HaiNam (Ban test single-player)

Game phieu luu 3D goc nhin thu nhat kieu raycasting (giong Wolfenstein 3D
doi), nhan vat di trong me cung va nhat nam de len diem, du so luong thi
len cap va duoc buff toc do. Toan bo phan render la GDI+ thuan (khong dung
DirectX/OpenGL/thu vien ngoai), bien dich truc tiep bang `vbc.exe`, khong
can Visual Studio.

## Tinh nang ban test

- Engine raycasting DDA tu viet, do phan giai noi bo 320x200 rồi phong to
  len cua so 960x600 (giu phong cach retro, giu hieu nang tot).
- Va cham tuong truot theo tung truc (khong bi dinh cung khi di sat tuong).
- Nam moc len map ngau nhien, ve dang billboard sprite (luon quay mat ve
  camera) co z-buffer de bi tuong che khuat dung cach.
- HUD hien diem, cap do, he so toc do, so nam con lai.
- Minimap goc tren-phai hien vi tri nguoi choi, huong nhin, vi tri nam.
- He thong len cap don gian: cu 10 nam la +1 cap va +0.15 he so toc do.

## Dieu khien

| Phim / Chuot     | Chuc nang                                   |
|-------------------|----------------------------------------------|
| W / Up            | Di toi                                       |
| S / Down          | Di lui                                       |
| A                 | Di ngang trai                                |
| D                 | Di ngang phai                                |
| Di chuot          | Xoay camera (mouse-look, con tro tu dong khoa vao giua man hinh) |
| Left / Right      | Xoay camera (du phong neu khong muon dung chuot) |
| Chuot phai        | Nhay                                         |
| Ctrl / C (giu)    | Ngoi xuong                                   |
| Chuot trai        | Dung dung cu / vu khi dang cam (danh cho nang cap sau) |
| ESC               | Thoat game                                   |

**Luu y ve mouse-look**: khi cua so game dang active, con tro chuot se tu
dong an di va bi khoa (clip) trong pham vi cua so, cu di chuyen chuot la
xoay duoc 360 do khong gioi han (giong FPS thong thuong). Khi Alt+Tab ra
ngoai, con tro tu dong hien lai va tha khoa. Muon chinh do nhay (sensitivity)
thi sua hang so `MOUSE_SENSITIVITY` trong `Form1.vb` (mac dinh `0.0035`,
tang len de xoay nhanh hon).

## Truc Z that (khong con chi la hieu ung hinh anh)

Ban do co them 2 loai o moi, dung de test co che nhay/ngoi that su anh huong
den va cham chu khong chi doi camera:

- **Loai 3 - Kien hang thap** (mau cam tren minimap): chan duong binh
  thuong, chi vuot qua duoc khi dang nhay du cao (`playerZ >= 0.45`).
- **Loai 4 - Khe chui** (mau xanh nhat tren minimap): chan duong khi dung
  thang, chi chui qua duoc khi dang ngoi du thap (`crouchAmount >= 0.6`).

Co mot doan test san trong me cung (hang tren cung, gan diem xuat phat):
di thang se gap kien hang phai nhay qua, roi den khe phai ngoi xuong moi
chui qua duoc. Tia raycasting cung duoc chinh de "nhin xuyen" cac o nay khi
nguoi choi du dieu kien vuot qua, dong thoi van ve dung khoi nua-chieu-cao
tai vi tri cua no (kien hang chiem nua duoi, khe chui chiem nua tren) thay
vi bien mat hoan toan - giu cam giac chuong ngai vat that.

## Dung cu / vu khi (chuot trai) - DA CO LOGIC THAT

`UseHeldItem()` trong `Form1.vb` gio xu ly theo `item.Kind`:

- **Dao gam / Kiem** (can chien): kiem tra doi thu trong tam `Range` va trong
  "hinh non" huong nhin (`MELEE_HIT_CONE_RAD`), trung thi tru mau ngay.
- **Cung** (tam xa): ban ra 1 `Projectile` (mui ten) bay theo huong nhin voi
  `ProjectileSpeed`, va cham tuong thi bien mat, va cham doi thu thi tru mau
  bang `item.Damage` roi bien mat.
- **Binh thuoc** (Consumable): hoi `HealAmount` mau (toi da `PLAYER_MAX_HEALTH`),
  tieu hao roi bien mat khoi o dang trang bi.

Moi vu khi co `Damage`/`Cooldown` rieng khai bao trong `InitItemCatalog()`
(class `ItemDefinition` trong `GameModels.vb`). HP dong bo qua mang bang cac
message moi: `ATKREQ` (client xin Host xu ly 1 don danh, ca can chien lan
mui ten trung dich), `SHOOTREQ`/`ARROW` (dong bo vi tri mui ten bay giua cac
may), `DMG` (Host bao HP moi cho tat ca), `RESPAWN` (hoi sinh tai vi tri
ngau nhien voi day mau khi HP ve 0), `HPSELF` (tu bao HP khi uong thuoc).
Host van la nguon du lieu goc duy nhat thuc su tru mau, giong het co che
`PICKREQ`/`ApplyPickup` dang dung cho nam - client chi de xuat, Host quyet
dinh va broadcast lai.

**Luu y**: vi `EquipSlot()` goi `UseHeldItem()` ngay khi bam phim so de
trang bi (theo dung y ban dau: "bam so la trang bi VA dung luon"), nen doi
vu khi bang phim so cung se lap tuc vung/ban/uong 1 lan (co cooldown chan
spam). Che do Solo khong co doi thu (`remotePlayers` rong) nen can chien/
mui ten van choi duoc de xem hoat canh nhung se khong co ai de tru mau.

## Cau truc thu muc va code (da tach file - de nang cap)

Du an gio chia ro thu muc theo vai tro, khong con de lung tung het o
thu muc goc:

```
GamePvP3D-HaiNam/
├── src/            <- TOAN BO ma nguon .vb, sua code thi vao day
│   ├── Form1.vb        (loi: fields, constructor, entry point)
│   ├── GameInput.vb     (ban phim/chuot, di chuyen, va cham)
│   ├── GameCombat.vb    (vu khi, sat thuong, inventory)
│   ├── GameHub.vb       (dong bo mang Host/Client)
│   ├── GameAssets.vb    (nap texture/sprite)
│   ├── GameWorld.vb     (vong lap game, spawn nam/item)
│   ├── GameRender.vb    (engine raycasting + ve sprite)
│   ├── GameHud.vb       (HUD, viewmodel, minimap)
│   ├── ConnectForm.vb   (form ket noi mang)
│   ├── GameModels.vb    (dinh nghia class du lieu)
│   ├── NetworkHub.vb    (mang: phia Host)
│   └── NetworkPeer.vb   (mang: phia Client)
├── Assets/         <- texture/sprite goc, KHONG dong cham, chi doc
├── bin/            <- KET QUA BUILD, tu dong sinh ra, co the xoa an
│   │                  toan roi build.bat lai la co lai (build.bat tu
│   │                  tao lai neu chua co)
│   ├── GamePvP3D_HaiNam.exe   <- file chay duoc, mo file nay de choi
│   └── Assets/               <- ban sao Assets\ goc, build.bat tu
│                                 dong copy vao day moi lan build de
│                                 exe chay duoc ngay, khong can copy
│                                 tay
├── legacy/
│   └── Form1.orig.vb   <- ban goc TRUOC khi tach file, giu lam tham
│                          chieu, KHONG nam trong danh sach bien dich
├── build.bat
└── README.md
```

Truoc day toan bo logic nam chung trong 1 file `Form1.vb` duy nhat (~2900
dong: input, combat, network, load asset, spawn, render... tron lan nhau,
kho tim/kho sua). Gio da tach thanh nhieu file nho trong `src\` theo tung
mang chuc nang, dung ky thuat **Partial Class** cua VB.NET (`Partial
Public Class Form1` trong nhieu file .vb khac nhau van duoc trinh bien
dich gop lai thanh **dung 1 class Form1 duy nhat**, khong doi hanh vi
chuong trinh, khong can sua logic ben trong tung ham). Danh sach file
trong `src\`:

| File | Vai tro | Cac ham/thanh phan chinh |
|---|---|---|
| `Form1.vb` | Loi: khai bao class, toan bo bien/hang so cau hinh, ham khoi tao `New()` (goi cac ham Load*/Init*/Spawn* luc mo game) | Fields, `Sub New`, entry point `MainModule.Main` |
| `GameInput.vb` | Doc ban phim/chuot, mouse-look, di chuyen, va cham tuong | `Form1_KeyDown/KeyUp`, `Form1_MouseDown/Up/Move`, `HandleInput`, `IsWalkable`, `BeginBowDraw`/`ReleaseBowDraw` |
| `GameCombat.vb` | Vu khi, sat thuong, inventory | `UseHeldItem`, `PerformMeleeAttack`, `FireProjectile`, `ApplyDamage`, `InitItemCatalog`, `EquipSlot` |
| `GameHub.vb` | Dong bo Host/Client qua `NetworkHub`/`NetworkPeer` | `StartNetworking`, `Hub_*`, `Peer_*`, `ApplyRemotePos`, `ApplyPickup`, `NetworkTick` |
| `GameAssets.vb` | Nap texture/sprite/anh tay tu thu muc `Assets\` | `LoadTextures`, `LoadCharacterTexture`, `LoadHandTextures` |
| `GameWorld.vb` | Vong lap game, spawn nam/item/trang tri, nhat do vat | `gameTimer_Tick`, `SpawnMushrooms`, `CheckPickup`, `CheckItemPickup` |
| `GameRender.vb` | Engine raycasting DDA va ve sprite (nam, item, nguoi choi, dan) | `RenderFrame`, `DrawMushroomSprites`, `DrawRemotePlayerSprites`, `PickDirectionalTexture` |
| `GameHud.vb` | Ve tay cam vu khi (viewmodel), HUD, minimap, hieu ung man hinh | `Form1_Paint`, `DrawViewmodel`, `DrawHandTextured`, `DrawInventoryHud` |

**Vi sao tach duoc an toan**: `Private` trong VB.NET la pham vi **toan
class**, khong phai pham vi **theo file**, nen cac ham/bien private khai
bao o file nay van goi/dung duoc binh thuong tu file khac (mien la cung
mot `Partial Class Form1`). Da kiem tra doi chieu tung dong: noi dung sau
khi tach **giong het 100%** ban goc, chi thay doi vi tri sap xep, khong
sua logic ben trong bat ky ham nao.

**Loi ich khi nang cap sau nay**:
- Muon sua vu khi/combat -> chi mo `src\GameCombat.vb`, khong phai cuon
  qua 2900 dong.
- Muon them tinh nang mang (VD: PvP nhieu nguoi, dong bo them du lieu) ->
  chi dong den `src\GameHub.vb`.
- Muon doi engine render / them hieu ung -> chi dong `src\GameRender.vb`
  hoac `src\GameHud.vb`, it nguy co dung cham nham vao code combat/network.
- Nhieu nguoi cung sua code cung luc se it bi conflict hon (moi nguoi
  dong 1 file khac nhau).
- `bin\` la thu muc "dung mot lan roi bo" - co the xoa het bat cu luc
  nao, chay lai `build.bat` la co lai day du (exe + Assets), khong so
  lam ban ma nguon o `src\` bi anh huong.

## Build

Chay `build.bat` o thu muc goc (yeu cau da cai .NET Framework 4.x, script
tu do tim `vbc.exe` trong `Framework64` hoac `Framework`). Script se:

1. Bien dich toan bo file `.vb` trong `src\` (khong dong den `legacy\`).
2. Xuat file `bin\GamePvP3D_HaiNam.exe`.
3. Tu dong copy thu muc `Assets\` (o thu muc goc) vao `bin\Assets\`.

Sau khi build xong, chi can mo `bin\GamePvP3D_HaiNam.exe` la choi duoc
ngay, khong can copy gi them - texture da nam san canh exe trong `bin\`.

## Texture / do hoa

Ban da co bo texture pixel art that (do ban cung cap), engine gio ve bang
texture mapping that thay vi mau phang:

| File trong Assets\  | Dung cho                          |
|----------------------|------------------------------------|
| `wall1.png`          | Tuong da thuong (loai 1)          |
| `wall2.png`          | Tuong da khac ruc/tim (loai 2)    |
| `crate.png`          | Kien hang thap (loai 3)           |
| `vent.png`           | Khe chui / luoi thong gio (loai 4)|
| `floor.png`          | San nha (floor-casting that su)   |
| `mushroom.png`       | Sprite nam (billboard, nen trong suot) |

Tat ca da duoc resize san ve 128x128 va nam trong thu muc `Assets\` o
thu muc goc du an. **Khong tu chinh sua/di chuyen `Assets\` goc** - moi
lan chay `build.bat`, script tu dong copy nguyen ban thu muc nay vao
`bin\Assets\` (ngay canh `bin\GamePvP3D_HaiNam.exe`) de game hien
texture; neu thieu, engine tu dong dung mau phang thay the (khong
crash, chi xau hon).

Tran (bau troi) van la mau phang vi chua co texture tran duoc cung cap -
neu muon them, tao 1 anh nua roi bao minh update code.

## Hoat anh nguoi choi khac (di bo / tho khi dung yen) - MOI

Nguoi choi khac gio khong con la 1 tam anh cung nhac nua ma co 3 hieu ung tu dong,
tinh trong `UpdateRemoteAnimations()` (goi moi frame) dua tren toc do di chuyen
uoc luong tu cac goi `POS` nhan qua mang (`ApplyRemotePos`):

- **Nhun khi di bo**: khi phat hien doi phuong dang di chuyen (>= `REMOTE_MOVE_SPEED_THRESHOLD`
  don vi map/giay), sprite se nhun len xuong theo nhip (`REMOTE_WALK_BOB_PIXELS`), noi suy
  muot dan vao/ra bang `REMOTE_BOB_LERP_SPEED` de khong bi giat khi vua dung/vua di.
- **Tho khi dung yen**: luc khong di chuyen, sprite phong to/thu nho chieu cao rat nhe theo
  nhip cham (`REMOTE_BREATH_AMPLITUDE`), tat dan khi bat dau di bo.
- **Doi frame "sai chan"**: neu co them anh `character_N_walk.png` / `character_N_side_walk.png`
  / `character_N_back_walk.png` (anh dang buoc, 1 chan truoc), engine se tu dong doi qua lai
  giua anh dung yen va anh sai chan theo nhip buoc khi dang di bo (giong hoat anh 2-frame kieu
  game co dien). **Neu chua ve them cac file nay thi khong sao** - engine tu dong fallback ve
  anh dung yen cung huong, van con nhun/tho binh thuong, chi la khong doi tu the tay chan.
  Anh `_side_walk` cung chi can ve 1 ben, code tu lat guong ben con lai giong anh `_side` thuong.

**Luu y**: day la hoat anh suy dien tu vi tri (khong phai skeleton/rig that), nen se khong co
mat chop hay toc/vai lay dong that su - chi la nhun-tho-doi-frame o muc do "du de thay dang song
dong" ma khong can dung engine 3D that.

## Tu the ngoi cua nguoi choi khac (crouch) - MOI

Truoc day nhan Ctrl/C ngoi xuong chi anh huong camera va va cham cua **chinh minh**
(`crouchAmount`), nguoi choi khac nhin sang van thay minh dung thang. Gio trang thai
`Crouch` da duoc dong bo qua mang (`POS|slot|x|y|angle|z|crouch`) va duoc dung de:

- **Doi sprite**: khi `rp.Crouch >= REMOTE_CROUCH_POSE_THRESHOLD` (mac dinh 0.5),
  `PickDirectionalTexture()` doi sang bo anh tu the ngoi rieng theo 3 huong (xem bang
  file ben duoi), uu tien hon ca hoat anh di bo sai chan - nguoi dang ngoi se khong doi
  frame buoc chan nua.
- **Thu nho + ha thap sprite**: chieu cao sprite giam con `REMOTE_CROUCH_HEIGHT_SCALE`
  (mac dinh 72%) khi ngoi het co, noi suy muot theo `rp.Crouch` (khong nhay cap dot ngot),
  cong them mot khoang dich xuong (`crouchDropPx`) de nhan vat trong nhu dang thap nguoi
  chu khong lo lung.

File anh moi can them vao `Assets\Characters\` (thieu file nao thi tu dong fallback ve
anh dung yen cung huong, khong bat buoc phai co du - xem giai thich fallback trong
`Assets\Characters\PROMPTS.md`):

| File | Dung cho |
|---|---|
| `character_N_crouch.png` | Tu the ngoi, nhin truc dien |
| `character_N_side_crouch.png` | Tu the ngoi, nhin ngang (ve 1 ben, code tu lat guong) |
| `character_N_back_crouch.png` | Tu the ngoi, nhin tu sau |

Prompt tao anh cho ca 4 slot da co san trong `Assets\Characters\PROMPTS.md` (phan
"Tu the NGOI (crouch)"). Da co du 12 anh that (4 slot x 3 huong) trong `Assets\Characters\`.

**Luu y**: chua co frame "vua ngoi vua di" (`_crouch_walk`) - ngoi thi luon dung 1 tu
the tinh du co di chuyen hay khong (xem ghi chu cuoi file PROMPTS.md neu muon nang cap
them sau nay).

## Tu the nhay cua nguoi choi khac (jump) - MOI

Tuong tu crouch: truoc day `playerZ`/`rp.Z` (chuot phai de nhay) chi nang vi tri sprite
nguoi choi khac len cao (`footShift`, da co san tu truoc) chu khong doi dang nguoi - van
dung thang lo lung giua khong trung khi nhay. Gio da them:

- **Doi sprite**: khi `rp.Z >= REMOTE_JUMP_POSE_THRESHOLD` (mac dinh 0.05, tuc vua roi
  chan khoi dat), `PickDirectionalTexture()` doi sang bo anh tu the nhay rieng theo 3
  huong - uu tien CAO NHAT, hon ca tu the ngoi va di bo, vi dang bay giua khong trung
  thi khong the dong thoi ngoi/buoc chan hop ly ve hinh anh.
- Vi tri sprite (nang len theo do cao nhay) da dung co che `footShift` co san, khong can
  sua them.

File anh moi can them vao `Assets\Characters\` (thieu file nao thi tu dong fallback ve
anh dung yen cung huong):

| File | Dung cho |
|---|---|
| `character_N_jump.png` | Tu the nhay, nhin truc dien |
| `character_N_side_jump.png` | Tu the nhay, nhin ngang (ve 1 ben, code tu lat guong) |
| `character_N_back_jump.png` | Tu the nhay, nhin tu sau |

Prompt tao anh cho ca 4 slot da co san trong `Assets\Characters\PROMPTS.md` (phan
"Tu the NHAY (jump)").

## Nhat ky sua loi / dieu chinh (v16)

### Sua loi bien dich (vbc.exe)

- **`netMode` trung ten voi `Enum NetMode`**: VB.NET khong phan biet hoa/thuong
  nen field `Private netMode As NetMode` (khai bao trong `Form1.vb`) bi coi
  trung ten voi chinh `Enum NetMode` cung khai bao trong class -> loi
  `BC30260` luc bien dich, roi keo theo `BC30108` ("'netMode' is a type and
  cannot be used as an expression") o tat ca file khac co goi den field nay.
  Da **doi ten field thanh `curNetMode`** va cap nhat lai moi cho goi trong
  `Form1.vb`, `GameCombat.vb`, `GameHub.vb`, `GameHud.vb`, `GameRender.vb`,
  `GameWorld.vb`. **Neu con file nao khac trong `src\` co dung `netMode` ma
  chua duoc doi (vd `GameInput.vb`, `GameAssets.vb`, `ConnectForm.vb`) thi
  can doi tiep thanh `curNetMode` truoc khi build.**
- **Khai bao mang jagged sai cu phap** trong `Form1.vb` (16 dong, cac bien
  `texCharacterBySlot`, `texCharacterSideBySlot`, `texCharacterWalkBySlot`...):
  viet kieu `Private x(CHARACTER_SLOT_COUNT - 1) As Integer()` bi loi
  `BC31087` vi vua gan kich thuoc o ten bien vua gan `()` o kieu du lieu.
  Da sua thanh cu phap dung `Private x(CHARACTER_SLOT_COUNT - 1)() As Integer`
  (dau `()` cho mang jagged dat ngay sau ngoac kich thuoc, kieu chi con la
  `Integer` don).
- **`Text.StringBuilder` bi mo ho (ambiguous)** trong `GameHub.vb`: file nay
  co ca `Imports System` lan `Imports System.Drawing`, nen ten ngan `Text`
  bi trung giua namespace `System.Text` va `System.Drawing.Text` -> loi
  `BC30561`. Da sua thanh ten day du `System.Text.StringBuilder`.

### Dieu chinh hoat anh tay cam vu khi (`GameHud.vb`, ham `DrawViewmodel`)

- **Khoang cach 2 tay (`handSpread`)**: mac dinh cu la `0.30` (dung yen) /
  `0.40` (dang cam do) khien 2 tay dung sat gan giua man hinh qua. Da dan
  rong ra thanh `0.20` (dung yen) / `0.34` (dang cam do). So cang nho thi
  tay cang dang xa ra 2 ben, cang lon thi cang khep ve giua - muon chinh
  tiep thi sua truc tiep 2 gia tri nay.
- **Bien do "tho" cua tay luc dung yen (`gripAmount` nhanh idle)**: cong
  thuc cu `0.5 + 0.3 * Math.Sin(idlePhase)` dao dong tu `0.2` den `0.8`,
  khien anh crossfade giua tay-mo/tay-nam doi qua nhieu moi nhip, nhin
  giat/nhap nhay. Da giam bien do xuong `0.30 + 0.08 * Math.Sin(idlePhase)`
  (chi dao dong nhe quanh `0.22`-`0.38`, luon thien ve tay mo, khong con
  gan cham sang tay nam). Nhip van con nhanh (do toc do tang cua bien
  `idlePhase` nam o file khac, chua sua duoc goc), nen da nhan giam them
  he so pha ngay trong cong thuc: `0.30 + 0.08 * Math.Sin(idlePhase * 0.3)`
  (chi con 30% toc do goc). **Toc do that su** cua nhip tho nay (bien
  `idlePhase` tang nhanh/cham bao nhieu moi frame) nam o file khac chua ro
  (co the `GameInput.vb` hoac phan Tick trong `Form1.vb`) - neu van thay
  nhip nhanh thi gui file co dong `idlePhase +=` de sua tan goc, hoac cu
  bao muon giam tiep he so `0.3` nay xuong nua.
- **Nhip doi tay luc dang di chuyen (`bobAmount > 0.05`)**: cong thuc cu
  `0.65 + 0.2 * Math.Sin(bobPhase * 2.0) * bobAmount` cung doi qua nhanh/
  qua nhieu giong nhu luc dung yen. Da giam thanh
  `0.55 + 0.12 * Math.Sin(bobPhase * 1.2) * bobAmount` (giam he so nhan
  `bobPhase` tu `2.0` xuong `1.2` cho nhip cham lai, giam bien do tu `0.2`
  xuong `0.12`, va giam moc nen tu `0.65` xuong `0.55` cho khop voi moc
  nen `0.30` luc dung yen, tranh nhay cap dot ngot khi bat dau buoc).

## Huong nang cap tiep theo (goi y)

- **He thong item/skill that**: thay `speedMultiplier` bang inventory,
  hieu ung tam thoi, hoac nam co loai khac nhau (nam do, nam vang, nam doc...).
- **Texture tran (bau troi)**: hien con la mau phang, co the them anh
  `sky.png` va load tuong tu cac texture khac.
- **PvP nhieu nguoi choi**: gan `NetworkHub.vb` / `NetworkPeer.vb` theo kien
  truc star-topology dang dung o cac game GamePvP khac (Contra, Mario,
  Tarzan...), dong bo vi tri nguoi choi + trang thai nam qua mang thay vi
  chi chay local.
- **Va cham/dam va PvP**: them projectile hoac melee giua cac nguoi choi
  khi da co lop network.
