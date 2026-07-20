# GamePvP3D_HaiNam (Bản test single-player)

Game phiêu lưu 3D góc nhìn thứ nhất kiểu raycasting (giống Wolfenstein 3D
đời đầu), nhân vật đi trong mê cung và nhặt nấm để lên điểm, đủ số lượng thì
lên cấp và được buff tốc độ. Toàn bộ phần render là GDI+ thuần (không dùng
DirectX/OpenGL/thư viện ngoài), biên dịch trực tiếp bằng `vbc.exe`, không
cần Visual Studio.

## Tính năng bản test

- Engine raycasting DDA tự viết, độ phân giải nội bộ 320x200 rồi phóng to
  lên cửa sổ 960x600 (giữ phong cách retro, giữ hiệu năng tốt).
- Va chạm tường trượt theo từng trục (không bị dính cứng khi đi sát tường).
- Nấm mọc lên map ngẫu nhiên, vẽ dạng billboard sprite (luôn quay mặt về
  camera) có z-buffer để bị tường che khuất đúng cách.
- HUD hiện điểm, cấp độ, hệ số tốc độ, số nấm còn lại.
- Minimap góc trên-phải hiện vị trí người chơi, hướng nhìn, vị trí nấm.
- Hệ thống lên cấp đơn giản: cứ 10 nấm là +1 cấp và +0.15 hệ số tốc độ.

## Điều khiển

| Phím / Chuột      | Chức năng                                    |
|--------------------|-----------------------------------------------|
| W / Up             | Đi tới                                        |
| S / Down           | Đi lùi                                        |
| A                  | Đi ngang trái                                 |
| D                  | Đi ngang phải                                 |
| Di chuột           | Xoay camera (mouse-look, con trỏ tự động khóa vào giữa màn hình) |
| Left               | Đi ngang trái (giống A)                       |
| Right              | Đi ngang phải (giống D)                       |
| Chuột phải         | Nhảy                                          |
| Ctrl / C (giữ)     | Ngồi xuống                                    |
| Chuột trái         | Dùng dụng cụ / vũ khí đang cầm (dành cho nâng cấp sau) |
| ESC                | Thoát game                                    |
| Del                | Vất vật phẩm đang chọn ở hotbar ra ngoài      |

**Lưu ý về mouse-look**: khi cửa sổ game đang active, con trỏ chuột sẽ tự
động ẩn đi và bị khóa (clip) trong phạm vi cửa sổ, cứ di chuyển chuột là
xoay được 360 độ không giới hạn (giống FPS thông thường). Khi Alt+Tab ra
ngoài, con trỏ tự động hiện lại và thả khóa. Muốn chỉnh độ nhạy (sensitivity)
thì sửa hằng số `MOUSE_SENSITIVITY` trong `Form1.vb` (mặc định `0.0035`,
tăng lên để xoay nhanh hơn).

## Trục Z thật (không còn chỉ là hiệu ứng hình ảnh)

Bản đồ có thêm 2 loại ô mới, dùng để test cơ chế nhảy/ngồi thật sự ảnh hưởng
đến va chạm chứ không chỉ đổi camera:

- **Loại 3 - Kiện hàng thấp** (màu cam trên minimap): chặn đường bình
  thường, chỉ vượt qua được khi đang nhảy đủ cao (`playerZ >= 0.45`).
- **Loại 4 - Khe chui** (màu xanh nhạt trên minimap): chặn đường khi đứng
  thẳng, chỉ chui qua được khi đang ngồi đủ thấp (`crouchAmount >= 0.6`).

Có một đoạn test sẵn trong mê cung (hàng trên cùng, gần điểm xuất phát):
đi thẳng sẽ gặp kiện hàng phải nhảy qua, rồi đến khe phải ngồi xuống mới
chui qua được. Tia raycasting cũng được chỉnh để "nhìn xuyên" các ô này khi
người chơi đủ điều kiện vượt qua, đồng thời vẫn vẽ đúng khối nửa-chiều-cao
tại vị trí của nó (kiện hàng chiếm nửa dưới, khe chui chiếm nửa trên) thay
vì biến mất hoàn toàn - giữ cảm giác chướng ngại vật thật.

## Dụng cụ / vũ khí (chuột trái) - ĐÃ CÓ LOGIC THẬT

`UseHeldItem()` trong `Form1.vb` giờ xử lý theo `item.Kind`:

- **Dao găm / Kiếm** (cận chiến): kiểm tra đối thủ trong tầm `Range` và trong
  "hình nón" hướng nhìn (`MELEE_HIT_CONE_RAD`), trúng thì trừ máu ngay.
- **Cung** (tầm xa): bắn ra 1 `Projectile` (mũi tên) bay theo hướng nhìn với
  `ProjectileSpeed`, va chạm tường thì biến mất, va chạm đối thủ thì trừ máu
  bằng `item.Damage` rồi biến mất.
- **Bình thuốc** (Consumable): hồi `HealAmount` máu (tối đa `PLAYER_MAX_HEALTH`),
  tiêu hao rồi biến mất khỏi ô đang trang bị.

Mỗi vũ khí có `Damage`/`Cooldown` riêng khai báo trong `InitItemCatalog()`
(class `ItemDefinition` trong `GameModels.vb`). HP đồng bộ qua mạng bằng các
message mới: `ATKREQ` (client xin Host xử lý 1 đòn đánh, cả cận chiến lẫn
mũi tên trúng đích), `SHOOTREQ`/`ARROW` (đồng bộ vị trí mũi tên bay giữa các
máy), `DMG` (Host báo HP mới cho tất cả), `RESPAWN` (hồi sinh tại vị trí
ngẫu nhiên với đầy máu khi HP về 0), `HPSELF` (tự báo HP khi uống thuốc).
Host vẫn là nguồn dữ liệu gốc duy nhất thực sự trừ máu, giống hệt cơ chế
`PICKREQ`/`ApplyPickup` đang dùng cho nấm - client chỉ đề xuất, Host quyết
định và broadcast lại.

**Lưu ý**: vì `EquipSlot()` gọi `UseHeldItem()` ngay khi bấm phím số để
trang bị (theo đúng ý ban đầu: "bấm số là trang bị VÀ dùng luôn"), nên đổi
vũ khí bằng phím số cũng sẽ lập tức vung/bắn/uống 1 lần (có cooldown chặn
spam). Chế độ Solo không có đối thủ (`remotePlayers` rỗng) nên cận chiến/
mũi tên vẫn chơi được để xem hoạt cảnh nhưng sẽ không có ai để trừ máu.

## Cấu trúc thư mục và code (đã tách file - dễ nâng cấp)

Dự án giờ chia rõ thư mục theo vai trò, không còn để lung tung hết ở
thư mục gốc:

```
GamePvP3D-HaiNam/
├── src/            <- TOÀN BỘ mã nguồn .vb, sửa code thì vào đây
│   ├── Form1.vb        (lõi: fields, constructor, entry point)
│   ├── GameInput.vb     (bàn phím/chuột, di chuyển, va chạm)
│   ├── GameCombat.vb    (vũ khí, sát thương, inventory)
│   ├── GameHub.vb       (đồng bộ mạng Host/Client)
│   ├── GameAssets.vb    (nạp texture/sprite)
│   ├── GameWorld.vb     (vòng lặp game, spawn nấm/item)
│   ├── GameRender.vb    (engine raycasting + vẽ sprite)
│   ├── GameHud.vb       (HUD, viewmodel, minimap)
│   ├── ConnectForm.vb   (form kết nối mạng)
│   ├── GameModels.vb    (định nghĩa class dữ liệu)
│   ├── NetworkHub.vb    (mạng: phía Host)
│   └── NetworkPeer.vb   (mạng: phía Client)
├── Assets/         <- texture/sprite gốc, KHÔNG động chạm, chỉ đọc
├── bin/            <- KẾT QUẢ BUILD, tự động sinh ra, có thể xóa an
│   │                  toàn rồi build.bat lại là có lại (build.bat tự
│   │                  tạo lại nếu chưa có)
│   ├── GamePvP3D_HaiNam.exe   <- file chạy được, mở file này để chơi
│   └── Assets/               <- bản sao Assets\ gốc, build.bat tự
│                                 động copy vào đây mỗi lần build để
│                                 exe chạy được ngay, không cần copy
│                                 tay
├── legacy/
│   └── Form1.orig.vb   <- bản gốc TRƯỚC khi tách file, giữ làm tham
│                          chiếu, KHÔNG nằm trong danh sách biên dịch
├── build.bat
└── README.md
```

Trước đây toàn bộ logic nằm chung trong 1 file `Form1.vb` duy nhất (~2900
dòng: input, combat, network, load asset, spawn, render... trộn lẫn nhau,
khó tìm/khó sửa). Giờ đã tách thành nhiều file nhỏ trong `src\` theo từng
mảng chức năng, dùng kỹ thuật **Partial Class** của VB.NET (`Partial
Public Class Form1` trong nhiều file .vb khác nhau vẫn được trình biên
dịch gộp lại thành **đúng 1 class Form1 duy nhất**, không đổi hành vi
chương trình, không cần sửa logic bên trong từng hàm). Danh sách file
trong `src\`:

| File | Vai trò | Các hàm/thành phần chính |
|---|---|---|
| `Form1.vb` | Lõi: khai báo class, toàn bộ biến/hằng số cấu hình, hàm khởi tạo `New()` (gọi các hàm Load*/Init*/Spawn* lúc mở game) | Fields, `Sub New`, entry point `MainModule.Main` |
| `GameInput.vb` | Đọc bàn phím/chuột, mouse-look, di chuyển, va chạm tường | `Form1_KeyDown/KeyUp`, `Form1_MouseDown/Up/Move`, `HandleInput`, `IsWalkable`, `BeginBowDraw`/`ReleaseBowDraw` |
| `GameCombat.vb` | Vũ khí, sát thương, inventory | `UseHeldItem`, `PerformMeleeAttack`, `FireProjectile`, `ApplyDamage`, `InitItemCatalog`, `EquipSlot` |
| `GameHub.vb` | Đồng bộ Host/Client qua `NetworkHub`/`NetworkPeer` | `StartNetworking`, `Hub_*`, `Peer_*`, `ApplyRemotePos`, `ApplyPickup`, `NetworkTick` |
| `GameAssets.vb` | Nạp texture/sprite/ảnh tay từ thư mục `Assets\` | `LoadTextures`, `LoadCharacterTexture`, `LoadHandTextures` |
| `GameWorld.vb` | Vòng lặp game, spawn nấm/item/trang trí, nhặt đồ vật | `gameTimer_Tick`, `SpawnMushrooms`, `CheckPickup`, `CheckItemPickup` |
| `GameRender.vb` | Engine raycasting DDA và vẽ sprite (nấm, item, người chơi, đạn) | `RenderFrame`, `DrawMushroomSprites`, `DrawRemotePlayerSprites`, `PickDirectionalTexture` |
| `GameHud.vb` | Vẽ tay cầm vũ khí (viewmodel), HUD, minimap, hiệu ứng màn hình | `Form1_Paint`, `DrawViewmodel`, `DrawHandTextured`, `DrawInventoryHud` |

**Vì sao tách được an toàn**: `Private` trong VB.NET là phạm vi **toàn
class**, không phải phạm vi **theo file**, nên các hàm/biến private khai
báo ở file này vẫn gọi/dùng được bình thường từ file khác (miễn là cùng
một `Partial Class Form1`). Đã kiểm tra đối chiếu từng dòng: nội dung sau
khi tách **giống hệt 100%** bản gốc, chỉ thay đổi vị trí sắp xếp, không
sửa logic bên trong bất kỳ hàm nào.

**Lợi ích khi nâng cấp sau này**:
- Muốn sửa vũ khí/combat -> chỉ mở `src\GameCombat.vb`, không phải cuộn
  qua 2900 dòng.
- Muốn thêm tính năng mạng (VD: PvP nhiều người, đồng bộ thêm dữ liệu) ->
  chỉ động đến `src\GameHub.vb`.
- Muốn đổi engine render / thêm hiệu ứng -> chỉ động `src\GameRender.vb`
  hoặc `src\GameHud.vb`, ít nguy cơ đụng chạm nhầm vào code combat/network.
- Nhiều người cùng sửa code cùng lúc sẽ ít bị conflict hơn (mỗi người
  động 1 file khác nhau).
- `bin\` là thư mục "dùng một lần rồi bỏ" - có thể xóa hết bất cứ lúc
  nào, chạy lại `build.bat` là có lại đầy đủ (exe + Assets), không sợ
  làm bản mã nguồn ở `src\` bị ảnh hưởng.

## Build

Chạy `build.bat` ở thư mục gốc (yêu cầu đã cài .NET Framework 4.x, script
tự dò tìm `vbc.exe` trong `Framework64` hoặc `Framework`). Script sẽ:

1. Biên dịch toàn bộ file `.vb` trong `src\` (không động đến `legacy\`).
2. Xuất file `bin\GamePvP3D_HaiNam.exe`.
3. Tự động copy thư mục `Assets\` (ở thư mục gốc) vào `bin\Assets\`.

Sau khi build xong, chỉ cần mở `bin\GamePvP3D_HaiNam.exe` là chơi được
ngay, không cần copy gì thêm - texture đã nằm sẵn cạnh exe trong `bin\`.

## Texture / đồ họa

Bạn đã có bộ texture pixel art thật (do bạn cung cấp), engine giờ vẽ bằng
texture mapping thật thay vì màu phẳng:

| File trong Assets\  | Dùng cho                           |
|----------------------|-------------------------------------|
| `wall1.png`          | Tường đá thường (loại 1)           |
| `wall2.png`          | Tường đá khác rêu/tím (loại 2)     |
| `crate.png`          | Kiện hàng thấp (loại 3)            |
| `vent.png`           | Khe chui / lưới thông gió (loại 4) |
| `floor.png`          | Sàn nhà (floor-casting thật sự)    |
| `mushroom.png`       | Sprite nấm (billboard, nền trong suốt) |

Tất cả đã được resize sẵn về 128x128 và nằm trong thư mục `Assets\` ở
thư mục gốc dự án. **Không tự chỉnh sửa/di chuyển `Assets\` gốc** - mỗi
lần chạy `build.bat`, script tự động copy nguyên bản thư mục này vào
`bin\Assets\` (ngay cạnh `bin\GamePvP3D_HaiNam.exe`) để game hiện
texture; nếu thiếu, engine tự động dùng màu phẳng thay thế (không
crash, chỉ xấu hơn).

Trần (bầu trời) vẫn là màu phẳng vì chưa có texture trần được cung cấp -
nếu muốn thêm, tạo 1 ảnh nữa rồi báo mình update code.

## Hoạt ảnh người chơi khác (đi bộ / thở khi đứng yên) - MỚI

Người chơi khác giờ không còn là 1 tấm ảnh cứng nhắc nữa mà có 3 hiệu ứng tự động,
tính trong `UpdateRemoteAnimations()` (gọi mỗi frame) dựa trên tốc độ di chuyển
ước lượng từ các gói `POS` nhận qua mạng (`ApplyRemotePos`):

- **Nhún khi đi bộ**: khi phát hiện đối phương đang di chuyển (>= `REMOTE_MOVE_SPEED_THRESHOLD`
  đơn vị map/giây), sprite sẽ nhún lên xuống theo nhịp (`REMOTE_WALK_BOB_PIXELS`), nội suy
  mượt dần vào/ra bằng `REMOTE_BOB_LERP_SPEED` để không bị giật khi vừa dừng/vừa đi.
- **Thở khi đứng yên**: lúc không di chuyển, sprite phóng to/thu nhỏ chiều cao rất nhẹ theo
  nhịp chậm (`REMOTE_BREATH_AMPLITUDE`), tắt dần khi bắt đầu đi bộ.
- **Đổi frame "sai chân"**: nếu có thêm ảnh `character_N_walk.png` / `character_N_side_walk.png`
  / `character_N_back_walk.png` (ảnh dáng bước, 1 chân trước), engine sẽ tự động đổi qua lại
  giữa ảnh đứng yên và ảnh sải chân theo nhịp bước khi đang đi bộ (giống hoạt ảnh 2-frame kiểu
  game cổ điển). **Nếu chưa vẽ thêm các file này thì không sao** - engine tự động fallback về
  ảnh đứng yên cùng hướng, vẫn còn nhún/thở bình thường, chỉ là không đổi tư thế tay chân.
  Ảnh `_side_walk` cũng chỉ cần vẽ 1 bên, code tự lật gương bên còn lại giống ảnh `_side` thường.

**Lưu ý**: đây là hoạt ảnh suy diễn từ vị trí (không phải skeleton/rig thật), nên sẽ không có
mắt chớp hay tóc/vai lay động thật sự - chỉ là nhún-thở-đổi-frame ở mức độ "đủ để thấy dáng sống
động" mà không cần dùng engine 3D thật.

## Tư thế ngồi của người chơi khác (crouch) - MỚI

Trước đây nhấn Ctrl/C ngồi xuống chỉ ảnh hưởng camera và va chạm của **chính mình**
(`crouchAmount`), người chơi khác nhìn sang vẫn thấy mình đứng thẳng. Giờ trạng thái
`Crouch` đã được đồng bộ qua mạng (`POS|slot|x|y|angle|z|crouch`) và được dùng để:

- **Đổi sprite**: khi `rp.Crouch >= REMOTE_CROUCH_POSE_THRESHOLD` (mặc định 0.5),
  `PickDirectionalTexture()` đổi sang bộ ảnh tư thế ngồi riêng theo 3 hướng (xem bảng
  file bên dưới), ưu tiên hơn cả hoạt ảnh đi bộ sai chân - người đang ngồi sẽ không đổi
  frame bước chân nữa.
- **Thu nhỏ + hạ thấp sprite**: chiều cao sprite giảm còn `REMOTE_CROUCH_HEIGHT_SCALE`
  (mặc định 72%) khi ngồi hết cỡ, nội suy mượt theo `rp.Crouch` (không nhảy cấp đột ngột),
  cộng thêm một khoảng dịch xuống (`crouchDropPx`) để nhân vật trông như đang thấp người
  chứ không lơ lửng.

File ảnh mới cần thêm vào `Assets\Characters\` (thiếu file nào thì tự động fallback về
ảnh đứng yên cùng hướng, không bắt buộc phải có đủ - xem giải thích fallback trong
`Assets\Characters\PROMPTS.md`):

| File | Dùng cho |
|---|---|
| `character_N_crouch.png` | Tư thế ngồi, nhìn trực diện |
| `character_N_side_crouch.png` | Tư thế ngồi, nhìn ngang (vẽ 1 bên, code tự lật gương) |
| `character_N_back_crouch.png` | Tư thế ngồi, nhìn từ sau |

Prompt tạo ảnh cho cả 4 slot đã có sẵn trong `Assets\Characters\PROMPTS.md` (phần
"Tư thế NGỒI (crouch)"). Đã có đủ 12 ảnh thật (4 slot x 3 hướng) trong `Assets\Characters\`.

**Lưu ý**: chưa có frame "vừa ngồi vừa đi" (`_crouch_walk`) - ngồi thì luôn dùng 1 tư
thế tĩnh dù có di chuyển hay không (xem ghi chú cuối file PROMPTS.md nếu muốn nâng cấp
thêm sau này).

## Tư thế nhảy của người chơi khác (jump) - MỚI

Tương tự crouch: trước đây `playerZ`/`rp.Z` (chuột phải để nhảy) chỉ nâng vị trí sprite
người chơi khác lên cao (`footShift`, đã có sẵn từ trước) chứ không đổi dáng người - vẫn
đứng thẳng lơ lửng giữa không trung khi nhảy. Giờ đã thêm:

- **Đổi sprite**: khi `rp.Z >= REMOTE_JUMP_POSE_THRESHOLD` (mặc định 0.05, tức vừa rời
  chân khỏi đất), `PickDirectionalTexture()` đổi sang bộ ảnh tư thế nhảy riêng theo 3
  hướng - ưu tiên CAO NHẤT, hơn cả tư thế ngồi và đi bộ, vì đang bay giữa không trung
  thì không thể đồng thời ngồi/bước chân hợp lý về hình ảnh.
- Vị trí sprite (nâng lên theo độ cao nhảy) đã dùng cơ chế `footShift` có sẵn, không cần
  sửa thêm.

File ảnh mới cần thêm vào `Assets\Characters\` (thiếu file nào thì tự động fallback về
ảnh đứng yên cùng hướng):

| File | Dùng cho |
|---|---|
| `character_N_jump.png` | Tư thế nhảy, nhìn trực diện |
| `character_N_side_jump.png` | Tư thế nhảy, nhìn ngang (vẽ 1 bên, code tự lật gương) |
| `character_N_back_jump.png` | Tư thế nhảy, nhìn từ sau |

Prompt tạo ảnh cho cả 4 slot đã có sẵn trong `Assets\Characters\PROMPTS.md` (phần
"Tư thế NHẢY (jump)").

## Nhật ký sửa lỗi / điều chỉnh (v16)

### Sửa lỗi biên dịch (vbc.exe)

- **`netMode` trùng tên với `Enum NetMode`**: VB.NET không phân biệt hoa/thường
  nên field `Private netMode As NetMode` (khai báo trong `Form1.vb`) bị coi
  trùng tên với chính `Enum NetMode` cũng khai báo trong class -> lỗi
  `BC30260` lúc biên dịch, rồi kéo theo `BC30108` ("'netMode' is a type and
  cannot be used as an expression") ở tất cả file khác có gọi đến field này.
  Đã **đổi tên field thành `curNetMode`** và cập nhật lại mọi chỗ gọi trong
  `Form1.vb`, `GameCombat.vb`, `GameHub.vb`, `GameHud.vb`, `GameRender.vb`,
  `GameWorld.vb`. **Nếu còn file nào khác trong `src\` có dùng `netMode` mà
  chưa được đổi (vd `GameAssets.vb`, `ConnectForm.vb`) thì cần đổi tiếp
  thành `curNetMode` trước khi build.**
- **Khai báo mảng jagged sai cú pháp** trong `Form1.vb` (16 dòng, các biến
  `texCharacterBySlot`, `texCharacterSideBySlot`, `texCharacterWalkBySlot`...):
  viết kiểu `Private x(CHARACTER_SLOT_COUNT - 1) As Integer()` bị lỗi
  `BC31087` vì vừa gán kích thước ở tên biến vừa gán `()` ở kiểu dữ liệu.
  Đã sửa thành cú pháp đúng `Private x(CHARACTER_SLOT_COUNT - 1)() As Integer`
  (dấu `()` cho mảng jagged đặt ngay sau ngoặc kích thước, kiểu chỉ còn là
  `Integer` đơn).
- **`Text.StringBuilder` bị mập mờ (ambiguous)** trong `GameHub.vb`: file này
  có cả `Imports System` lẫn `Imports System.Drawing`, nên tên ngắn `Text`
  bị trùng giữa namespace `System.Text` và `System.Drawing.Text` -> lỗi
  `BC30561`. Đã sửa thành tên đầy đủ `System.Text.StringBuilder`.

### Điều chỉnh hoạt ảnh tay cầm vũ khí (`GameHud.vb`, hàm `DrawViewmodel`)

- **Khoảng cách 2 tay (`handSpread`)**: mặc định cũ là `0.30` (đứng yên) /
  `0.40` (đang cầm đồ) khiến 2 tay đứng sát gần giữa màn hình quá. Đã dãn
  rộng ra thành `0.20` (đứng yên) / `0.34` (đang cầm đồ). Số càng nhỏ thì
  tay càng dang xa ra 2 bên, càng lớn thì càng khép về giữa - muốn chỉnh
  tiếp thì sửa trực tiếp 2 giá trị này.
- **Biên độ "thở" của tay lúc đứng yên (`gripAmount` nhịp idle)**: công
  thức cũ `0.5 + 0.3 * Math.Sin(idlePhase)` dao động từ `0.2` đến `0.8`,
  khiến ảnh crossfade giữa tay-mở/tay-nắm đổi quá nhiều mỗi nhịp, nhìn
  giật/nhấp nháy. Đã giảm biên độ xuống `0.30 + 0.08 * Math.Sin(idlePhase)`
  (chỉ dao động nhẹ quanh `0.22`-`0.38`, luôn thiên về tay mở, không còn
  gần chạm sang tay nắm). Nhịp vẫn còn nhanh (do tốc độ tăng của biến
  `idlePhase` nằm ở file khác, chưa sửa được gốc), nên đã nhân giảm thêm
  hệ số pha ngay trong công thức: `0.30 + 0.08 * Math.Sin(idlePhase * 0.3)`
  (chỉ còn 30% tốc độ gốc). **Tốc độ thật sự** của nhịp thở này (biến
  `idlePhase` tăng nhanh/chậm bao nhiêu mỗi frame) nằm ở file khác chưa rõ
  (có thể `GameInput.vb` hoặc phần Tick trong `Form1.vb`) - nếu vẫn thấy
  nhịp nhanh thì gửi file có dòng `idlePhase +=` để sửa tận gốc, hoặc cứ
  báo muốn giảm tiếp hệ số `0.3` này xuống nữa.
- **Nhịp đổi tay lúc đang di chuyển (`bobAmount > 0.05`)**: công thức cũ
  `0.65 + 0.2 * Math.Sin(bobPhase * 2.0) * bobAmount` cũng đổi quá nhanh/
  quá nhiều giống như lúc đứng yên. Đã giảm thành
  `0.55 + 0.12 * Math.Sin(bobPhase * 1.2) * bobAmount` (giảm hệ số nhân
  `bobPhase` từ `2.0` xuống `1.2` cho nhịp chậm lại, giảm biên độ từ `0.2`
  xuống `0.12`, và giảm mốc nền từ `0.65` xuống `0.55` cho khớp với mốc
  nền `0.30` lúc đứng yên, tránh nhảy cấp đột ngột khi bắt đầu bước).
- **Bỏ hẳn dao động ngón tay khi tay trống**: sau khi giảm biên độ vẫn còn
  thấy rối mắt, quyết định **bỏ hẳn hoạt ảnh mở/nắm khi không cầm đồ**
  (không phân biệt đứng yên hay đang di chuyển nữa) - `gripAmount` giờ cố
  định `0.30` (tay hé mở tự nhiên) khi `isHolding = False`, chỉ còn nắm
  chặt (`1.0`) khi thực sự đang cầm đồ (`isHolding = True`). Các biến
  `bobPhase`/`idlePhase` và công thức dao động cũ không còn dùng cho
  `gripAmount` nữa (vẫn còn dùng cho việc khác trong file, không xóa).

### Thêm tính năng ngẩng/cúi bằng chuột (mouse pitch look)

- **File liên quan**: `Form1.vb` (khai báo biến/hằng số), `GameInput.vb`
  (đọc chuột và tính toán).
- Kỹ thuật dùng là **"horizon shift"** - dịch toàn bộ màn hình lên/xuống
  theo pixel dựa trên trục dọc chuột, **không phải true 3D pitch thật sự**
  (raycaster này chỉ bắn tia ngang, không nghiêng tia theo chiều dọc), dù
  đủ để tạo cảm giác ngẩng/cúi trong phạm vi vừa phải.
- Biến mới `pitchShiftPx` (trong `Form1.vb`) được `Form1_MouseMove`
  (`GameInput.vb`) cập nhật theo độ lệch chuột trục Y mỗi lần di chuyển
  chuột, giới hạn trong khoảng `-PITCH_MAX_PX` đến `PITCH_MAX_PX`
  (mặc định `70`, có thể tăng/giảm để ngẩng/cúi được nhiều/ít hơn).
- `HandleInput` (`GameInput.vb`) cộng `pitchShiftPx` vào công thức tính
  `viewShiftPx` cùng với phần offset do nhảy/ngồi, để 2 hiệu ứng không
  đè lên nhau.
- Độ nhạy theo trục dọc: hằng số `MOUSE_PITCH_SENSITIVITY` (mặc định
  `0.6`, cùng đơn vị với `MOUSE_SENSITIVITY` của trục ngang nhưng tính
  theo pixel màn hình trong thay vì radian).
- Hằng số `INVERT_MOUSE_PITCH` (mặc định `False`) - đổi thành `True` nếu
  muốn đảo chiều trục dọc (kéo chuột lên = nhìn xuống, kiểu "invert Y"
  mà một số người chơi thích).

### Thêm bục thấp / bục cao đi lên được (loại ô bản đồ 5, 6)

- **Bối cảnh**: bước đầu tiên trong hướng "nâng cấp raycasting" (không đổi
  sang engine 3D thật, vẫn dùng GDI+/raycasting hiện có) - xem giải thích
  kỹ thuật và các hướng còn lại trong lịch sử trao đổi.
- **File liên quan**: `Form1.vb` (định nghĩa loại ô, hằng số chiều cao,
  biến `standHeight`), `GameInput.vb` (cho phép đi vào, nội suy chiều cao),
  `GameRender.vb` (vẽ khối nổi từ xa).
- **Loại ô mới trong `mapData`**:
  - `5` = bục thấp, cao `PLATFORM_LOW_HEIGHT` (mặc định `0.35`, đơn vị =
    1 lần chiều cao tường đầy)
  - `6` = bục cao, cao `PLATFORM_HIGH_HEIGHT` (mặc định `0.7`)
  - Đã thêm 2 ô test gần điểm xuất phát để thử ngay: `mapData(1,4) = 5`,
    `mapData(3,5) = 6`.
- **Cơ chế**: không phải sector/floor-casting đa tầng thật sự - là kỹ
  thuật mở rộng từ khối "nửa-chiều-cao" có sẵn cho kiện hàng/khe chui:
  - Ô loại 5/6 luôn đi/nhìn xuyên tia được (không chặn như tường), nhưng
    khi tia quét từ bên ngoài vào thì vẽ một **khối nổi từ sàn lên** cao
    đúng tỉ lệ (`GameRender.vb`, đoạn "Vẽ khối obstacle"), dùng texture
    `texCrate` cho bục thấp và `texWall2` cho bục cao (tạm dùng texture
    có sẵn, chưa có asset riêng).
  - Khi người chơi bước vào ô 5/6, biến `standHeight` (`Form1.vb`) nội
    suy mượt lên chiều cao tương ứng (tốc độ `STAND_HEIGHT_LERP_SPEED`,
    mặc định `3.0`/giây) thay vì nhảy cấp đột ngột, tạo cảm giác "leo
    bậc thang" tự nhiên. `standHeight` được cộng vào công thức tính
    `viewShiftPx` cùng với phần nhảy (`playerZ`), ngồi (`crouchAmount`)
    và ngẩng/cúi chuột (`pitchShiftPx`) đã có trước đó.
  - `IsWalkable` (`GameInput.vb`) cho phép đi vào ô 5/6 vô điều kiện
    (khác với kiện hàng/khe chui phải nhảy/ngồi mới qua được).
- **Hạn chế đã biết** (chưa xử lý trong bản này):
  - Sàn-casting (`RenderFrame`, đoạn "Sàn nhà") vẫn giả định 1 mặt
    phẳng duy nhất tính theo độ cao camera hiện tại - **không vẽ được
    cạnh dốc / vách ngoài của bục từ góc nhìn ngang hàng** (ví dụ đứng
    ngay cạnh bục cao mà không nhìn thẳng vào nó thì không thấy rõ ràng
    giới hạn sáng-tối giữa sàn thường và mặt bục). Đây là giới hạn của
    kỹ thuật floor-casting theo hàng hiện tại, muốn xử lý đúng thì phải
    đổi sang ray-march theo cột (bước 2 trong hướng nâng cấp đã đề cập).
  - Khi đang đứng trên bục cao mà nhảy qua kiện hàng (loại 3), ngưỡng
    `CRATE_JUMP_HEIGHT` vẫn so sánh với `playerZ` tuyệt đối (không cộng
    thêm `standHeight`) - trường hợp hiếm, chưa cần xử lý ngay.
  - Chưa có texture riêng cho bục (đang dùng tạm `texCrate`/`texWall2`).

### Bóng đổ giả, đèn đuốc + nhấp nháy, fog pha màu + vignette

- **Bối cảnh**: 3 trong số các mẹo "bù đắp thiếu hụt 3D" cho engine
  raycasting (xem danh sách đầy đủ và các mẹo còn lại trong lịch sử
  trao đổi) - không cần đổi engine, chỉ thêm hàm hỗ trợ vào
  `GameRender.vb` và vài biến/hằng số vào `Form1.vb`.

- **Bóng đổ giả dưới chân sprite** (`DrawGroundShadow` trong
  `GameRender.vb`): vẽ 1 vệt elip mờ trên sàn ngay dưới chân mỗi sprite
  (nấm, vật phẩm rơi, người chơi khác), giúp mắt "neo" vật thể xuống đất
  thay vì trông lơ lửng. Chỉ vẽ trong bán kính `SHADOW_MAX_DIST` (mặc
  định `14.0`), có kiểm tra `zBuffer` để không đè bóng lên vật khác đứng
  gần hơn.

- **Đèn đuốc điểm + nhấp nháy** (`TorchLightAmount` trong
  `GameRender.vb`, danh sách `torchLights` trong `Form1.vb`): mỗi đèn là
  1 `TorchLight` (tọa độ X/Y, bán kính chiếu sáng `Radius`, độ lệch pha
  `FlickerSeed` để các đèn không nhấp nháy đồng bộ với nhau). Ánh sáng
  cộng thêm vào độ sáng nền (fog) của sàn, tường, và khối obstacle/bục
  khi ở gần đèn, độ mạnh tối đa theo hằng số `TORCH_BRIGHTNESS` (mặc
  định `0.65`). Đã đặt sẵn 3 đèn test trong map hiện có. Biến
  `worldTime` (`Form1.vb`, tăng dần trong `HandleInput`) dùng làm pha
  cho nhịp nhấp nháy `Sin`.
- **Sprite (nấm, item, người chơi, đạn) chưa được chiếu sáng bởi đèn
  đuốc** - chỉ áp dụng cho sàn/tường/obstacle để giữ chi phí tính toán
  ở mức vừa phải, có thể mở rộng thêm sau nếu cần.

- **Fog pha màu** (`ShadeColorFogLit` trong `GameRender.vb`, thay cho
  `ShadeColor` gốc - chỉ áp dụng cho sàn/tường/obstacle, sprite vẫn
  dùng `ShadeColor` gốc): thay vì chỉ nhân độ sáng xuống theo khoảng
  cách như trước, giờ còn pha dần màu pixel về phía màu sương tối
  `FOG_COLOR_R/G/B` (mặc định xanh đen `18,16,34`) khi ở xa - tạo chiều
  sâu khí quyển rõ hơn hẳn so với chỉ làm tối đơn thuần.

- **Vignette** (`ApplyVignette` trong `GameRender.vb`, gọi 1 lần cuối
  `RenderFrame` sau khi vẽ xong toàn bộ khung hình): làm tối dần 4 góc
  màn hình, bắt đầu từ bán kính `VIGNETTE_START` (mặc định `0.55`, tính
  từ giữa màn hình ra, 0..1) với độ tối tối đa `VIGNETTE_STRENGTH`
  (mặc định `0.55`). Đây là bước xử lý ảnh toàn màn hình, chạy sau
  cùng nên không ảnh hưởng `zBuffer`/logic va chạm gì cả.

- **Hiệu năng**: cả floor-casting lẫn vignette giờ chạy vòng lặp trên
  gần như toàn bộ 320x200 pixel mỗi frame (thêm phép tính khoảng cách
  tới từng đèn cho mỗi pixel sàn) - vẫn nhẹ với độ phân giải nội bộ
  thấp này, nhưng nếu sau này tăng `RES_W`/`RES_H` hoặc thêm nhiều đèn
  hơn thì nên để ý tới hiệu năng.

### Đổi phím Left/Right từ xoay camera sang đi ngang (strafe)

- **File liên quan**: `GameInput.vb` (`HandleInput`), `Form1.vb` (ghi chú
  biến `rotSpeed`).
- Trước đây `Keys.Left`/`Keys.Right` dùng để xoay camera (`playerAngle
  -= /+ rotSpeed * dt`) - trùng chức năng với chuột và ít ai dùng vì
  xoay bằng phím rất chậm/khó điều khiển. Đã đổi thành đi ngang trái/
  phải, dùng chung công thức với `A`/`D` (`Keys.Left` = như `A`,
  `Keys.Right` = như `D`).
- Biến `rotSpeed` (`Form1.vb`) không còn được dùng ở đâu nữa (chỉ để
  lại kèm ghi chú, không xóa hẳn phòng khi sau này cần thêm lại cách
  xoay bằng phím).
- `Keys.Up`/`Keys.Down` **không đổi** - vẫn đi tới/lùi như cũ (giữ
  nguyên vì người dùng chỉ yêu cầu đổi Left/Right).

### Giảm kích thước sprite nấm + thêm hoạt ảnh tay hái nấm

- **Giảm kích thước nấm** (`GameRender.vb`, `DrawMushroomSprites`): nấm
  trước đây không có hệ số thu nhỏ nào cả (`spriteSize = RES_H /
  transformY`), tức to ngang bằng cả 1 bức tường đầy - quá khổ so với
  một cây nấm nhỏ nhặt được. Đã nhân thêm `* 0.4` (thu còn 40%), tương
  đương cỡ vật phẩm rơi (`DrawWorldItemSprites` đang dùng `* 0.6`) nhưng
  nhỏ hơn 1 chút cho hợp tỉ lệ cây nấm.
- **Hoạt ảnh tay hái nấm** (`Form1.vb`, `GameWorld.vb`, `GameHud.vb`):
  dùng lại đúng cơ chế "0 -> 1 -> 0 theo `Sin`" đã có sẵn cho hoạt ảnh
  vung vũ khí (`attackSwingTime`/`ATTACK_SWING_DURATION`), tạo biến mới
  song song `pickupAnimTime`/`PICKUP_ANIM_DURATION` (mặc định `0.35`
  giây, hơi lâu hơn vung vũ khí vì là động tác cúi/vươn xuống chứ không
  phải đánh nhanh).
  - Kích hoạt `pickupAnimTime = PICKUP_ANIM_DURATION` ngay tại thời điểm
    phát hiện đứng đủ gần nấm để nhặt, ở cả 2 nhánh trong `GameWorld.vb`:
    `CheckPickup` (Host/Solo, nhặt trực tiếp) và
    `CheckPickupClientRequest` (Client, ngay khi gửi yêu cầu, không đợi
    Host xác nhận mới chạy animation - để phản hồi hình ảnh tức thì).
  - Đếm ngược `pickupAnimTime` mỗi frame giống hệt cách `attackSwingTime`
    đang được đếm ngược trong `gameTimer_Tick`.
  - `DrawViewmodel` (`GameHud.vb`) tính `pickupDip`/`pickupIn` theo
    cùng công thức `Sin(pickupT * PI)` như `swingPunch`, nhưng **cộng
    thêm** vào tay phải (`rightY += pickupDip`, `rightX -= pickupIn`)
    để tay lao **xuống và vào trong** (mô phỏng cúi xuống nhặt) thay vì
    lao lên-ra trước như lúc vung vũ khí - đúng nghĩa "chuyển động
    ngược hướng" so với animation tấn công. Chạy độc lập với
    `isHolding`/`gripAmount` nên vẫn hoạt động dù đang cầm vũ khí hay
    tay không.

### Thêm phím Del để vất vật phẩm đang chọn

- **File liên quan**: `GameInput.vb` (bắt phím), `GameCombat.vb`
  (`DropHeldItem` - logic vất), `GameHub.vb` (đồng bộ mạng).
- Bấm `Del` gọi `DropHeldItem()`: lấy vật phẩm ở ô hotbar đang chọn
  (`activeSlotIndex`), nếu ô đang trống thì không làm gì. Nếu có, xóa
  khỏi ô đó (`inventorySlots(activeSlotIndex).Item = Nothing`) và tạo
  1 `WorldItemSpawn` mới rơi ra ngay phía trước mặt (cách 0.6 ô theo
  hướng nhìn hiện tại `playerAngle`). Nếu đang kéo cung thì hủy kéo
  luôn (gọi `CancelBowDraw()`) trước khi vất, tránh lỡ tay bắn khi vừa
  vất cung.
- **Đồng bộ mạng**: khác với nhặt đồ (có thể tranh chấp nếu 2 người
  cùng lao vào 1 vật, nên phải để Host phân xử trước mới được xóa -
  cơ chế `PICKREQ`/`ITEMPICKREQ` cũ), vất đồ là tài sản của chính người
  vất nên **không cần chờ Host duyệt mới hiện** - thêm thẳng vào
  `worldItems` cục bộ ngay lập tức trên máy của người vất (dù đang là
  Host, Client, hay Solo), hiện ra tức thì không có độ trễ:
  - Solo: chỉ thêm cục bộ, không có gì để đồng bộ.
  - Host: thêm cục bộ xong, phát luôn `ITEMSYNC` cho tất cả Client
    thấy vật vừa rơi.
  - Client: thêm cục bộ xong (hiện ngay trên máy mình), đồng thời gửi
    **thông báo** (không phải xin phép) `ITEMDROPREQ|slot|itemId|x|y`
    cho Host để Host cập nhật danh sách gốc và báo cho người chơi khác.
    Vì `ITEMSYNC` luôn ghi đè toàn bộ danh sách (không cộng dồn), lần
    đồng bộ tiếp theo từ Host sẽ tự khớp lại ID thật, không tạo ra vật
    phẩm trùng lặp trên màn hình.
- **Hạn chế đã biết**: object vừa vất luôn rơi cách đúng 0.6 ô phía
  trước, chưa kiểm tra ô đó có phải tường/vật cản hay không - trường
  hợp hiếm (đứng sát tường vất đồ) vật phẩm có thể rơi lọt vào trong
  tường về mặt tọa độ (vẫn nhặt lại được bình thường vì việc nhặt chỉ
  tính khoảng cách, không tính va chạm tường).

## Đổi map sang khu rừng (cây cối, cỏ, texture bằng công thức)

- **Bối cảnh**: toàn bộ đổi mới ở đây dùng lại đúng kỹ thuật vẽ bằng
  công thức toán học đã có sẵn cho cỏ/hoa (`GrassPixel`/`FlowerPixel`)
  - **không cần bất kỳ file ảnh mới nào**, không phụ thuộc việc bạn
  phải tự kiếm/tạo texture. Nếu sau này muốn thay bằng ảnh thật đẹp
  hơn thì vẫn làm được (xem phần "Texture / đồ họa" ở trên - texture
  ghi đè trong `GenerateForestTextures()` chỉ chạy SAU khi nạp file,
  nên có thể tắt việc ghi đè nếu bạn cung cấp ảnh thật).

- **File liên quan**: `Form1.vb` (map mới, field `texTreeBark`),
  `GameRender.vb` (`GenerateForestTextures`, chặn tia loại 7, `TreePixel`),
  `GameWorld.vb` (`SpawnDecorations` rải thêm cây nền).

- **Bố cục map mới**: đổi từ các phòng vuông vắn kiểu dungeon sang
  một khu đất trống lớn với **cây đứng rải rác độc lập** (loại ô mới
  `7`), đi vòng quanh từng cây được thay vì đi trong hành lang kín.
  Về mặt kỹ thuật đây vẫn là các ô tường trong lưới `mapData` như cũ,
  chỉ khác là đặt rải rác từng ô đơn lẻ giữa khoảng trống thay vì xếp
  thành khối phòng liền - một ô tường đơn lẻ bao quanh bởi khoảng
  trống tự nhiên trông giống hệt 1 thân cây trụ đứng khi nhìn qua
  raycasting, không cần code vẽ gì đặc biệt thêm.
  - Loại `3` (trước là kiện hàng) đổi nghĩa thành **khúc gỗ đổ**, loại
    `4` (trước là khe chui) đổi nghĩa thành **khe đá**, loại `5`/`6`
    (bục thấp/cao) đổi nghĩa thành **mô đất/tảng đá** - cơ chế nhảy
    qua/ngồi chui/leo lên giữ nguyên y hệt như cũ, chỉ đổi tên gọi và
    texture cho hợp bối cảnh rừng.
  - Vị trí 3 đèn đuốc (`torchLights`) và điểm xuất phát người chơi vẫn
    nằm trên đất trống hợp lệ sau khi đổi map - đã kiểm tra lại tọa độ.

- **Texture sinh bằng công thức** (`GenerateForestTextures` trong
  `GameRender.vb`, gọi 1 lần trong `Sub New` ngay sau `LoadTextures()`):
  - `texTreeBark` (mới) - vân gỗ dọc cho thân cây (loại ô `7`), không
    có file gốc, sinh hoàn toàn bằng `Math.Sin` + nhiễu ngẫu nhiên.
  - `texFloor` - **ghi đè** ảnh sàn đá đã nạp từ `floor.png` (nếu có)
    thành thảm cỏ lẫn đất mòn, nhiều sắc xanh khác nhau chứ không
    đồng màu.
  - `texCrate`/`texVent` - ghi đè từ ảnh kiện hàng/lưới thông gió cũ
    thành khúc gỗ (vân ngang) và đá phủ rêu (đốm xanh loang lổ).
  - **Giờ đã có sẵn cơ chế ưu tiên ảnh thật tự động** - xem mục ngay
    bên dưới, không cần sửa code thủ công như trước nữa.

- **Ảnh thật cho texture rừng** (tùy chọn, tự động ưu tiên nếu có -
  `TryLoadForestTexture`/`TryLoadForestSprite` trong `GameRender.vb`,
  field `texTree`/`texTreeW`/`texTreeH` trong `Form1.vb`):
  `GenerateForestTextures()` giờ luôn thử đọc file thật trước, chỉ
  dùng công thức khi không tìm thấy file hoặc file sai kích thước -
  không crash, không cần sửa code khi thả ảnh vào. Đặt ảnh vào thư
  mục mới `Assets\Forest\` (cạnh `Assets\Characters\` đã có), đúng 5
  tên file: `tree_bark.png`, `forest_floor.png`, `fallen_log.png`,
  `mossy_rock.png` (4 file này **bắt buộc đúng 128x128px**, sai kích
  thước bị bỏ qua âm thầm) và `tree_billboard.png` (sprite cây đứng
  riêng lẻ cho trang trí nền `Kind=2`, nền trong suốt, không bắt buộc
  kích thước cố định). Prompt vẽ chi tiết cho cả 5 file: xem file
  `FOREST_PROMPTS.md` đính kèm. **`build.bat` chưa được kiểm tra/sửa**
  để copy thư mục con mới `Assets\Forest\` vào `bin\Assets\Forest\` -
  cần xem lại logic copy trong `build.bat` trước khi build.

- **Cây nền trang trí** (`TreePixel` trong `GameRender.vb`, `Kind = 2`
  trong hệ thống decoration có sẵn): cây nhỏ vẽ bằng công thức (thân
  nâu hẹp + tán lá tròn xanh lá, màu hơi lệch ngẫu nhiên theo cây),
  **không chặn đường, không va chạm** - chỉ để tạo cảm giác rừng rậm
  rạp hơn ở hậu cảnh, bổ sung cho lớp cây-thân-cứng (loại `7`) đã có.
  `SpawnDecorations` giờ rải 60% cỏ / 25% hoa / 15% cây nền tại các ô
  đất trống, thay vì chỉ 75% cỏ / 25% hoa như trước.

- **Bầu trời + fog**: đổi màu trời từ xanh đen (đêm) sang xanh da trời
  (ban ngày) trong `RenderFrame`; màu fog pha (`FOG_COLOR_R/G/B`) đổi
  từ xanh đen sang xám xanh nhạt kiểu sương rừng ban ngày.

- **Hạn chế đã biết**:
  - Đèn đuốc (`torchLights`) vẫn còn nguyên với ý nghĩa cũ ("đuốc" giữa
    rừng ban ngày hơi vô lý về logic) - có thể cân nhắc đổi thành
    "vệt nắng xuyên tán lá" hoặc bỏ hẳn, tùy bạn quyết định.
  - Địa hình vẫn hoàn toàn phẳng (chỉ có bục rời rạc như đã nói ở mục
    "hướng nâng cấp raycasting" phía trên) - không có đồi dốc tự nhiên.
  - Map mới đã đổi **toàn bộ** ô tường (kể cả viền ngoài 4 cạnh) sang
    loại `7` (cây) - không còn ô loại `1`/`2` (tường đá cũ) nào trong
    map mặc định nữa. 2 loại này vẫn còn được định nghĩa và xử lý đầy
    đủ trong code (`GameRender.vb` vẫn chọn đúng `texWall1`/`texWall2`
    khi gặp), chỉ là không có ô nào trong map hiện tại dùng tới - vẫn
    dùng lại được bình thường nếu sau này muốn thêm khu vực "tàn tích
    đá cổ" xen giữa rừng.

## Nhiều map + chọn map trước khi chơi (MỚI)

Giờ có **3 map** (`GameMaps.vb`, mảng `MapNames`): "Mê Cung Cổ" (map gốc,
không đổi), "Rừng Mở" (thoáng hơn, ít tường), "Hang Đá Rêu" (nhiều lối
đi/vật cản hơn). Cả 3 đều 16x16, sinh bằng script đảm bảo mọi ô đất trống
đều liên thông tới nhau (không có khu vực bị cô lập).

- **Màn hình `ConnectForm`** giờ có thêm ComboBox "Map" hiện đủ 3 lựa
  chọn. Chọn **Solo/Host** thì map này thực sự được dùng. Chọn **Join**
  thì ComboBox vẫn hiện (để xem trước) nhưng **không có tác dụng** - máy
  Join sẽ tự động đổi sang đúng map của Host ngay khi kết nối thành công
  (xem đoạn dưới).
- **Đồng bộ map trong PvP**: message `WELCOME` (Host gửi cho Client vừa
  vào phòng) giờ có thêm field map index (`WELCOME|slot|mapIndex|...`).
  Client nhận được, nếu khác map đang dùng cục bộ thì gọi lại
  `ApplyMapSelection()` để đổi toàn bộ `mapData`/`torchLights`/vị trí xuất
  phát/trang trí sang đúng map của Host - đảm bảo cả phòng luôn chơi
  chung 1 map, tránh trường hợp mỗi máy thấy 1 mê cung khác nhau (sẽ làm
  va chạm/vị trí bị lệch hoàn toàn).
- **Thêm map mới**: mở `GameMaps.vb`, thêm 1 `Case` trong cả 3 hàm
  `GetMapLayout`/`GetMapTorches`/`GetMapSpawn` (cùng số `idx`), rồi thêm
  tên vào mảng `MapNames`. Nhớ giữ đúng 16x16 và đảm bảo ô `(1,1)` (điểm
  xuất phát mặc định) là loại `0` (đất trống).
- `build.bat` đã được cập nhật để build luôn `src\GameMaps.vb`.

## Hướng nâng cấp tiếp theo (gợi ý)

- **Hệ thống item/skill thật**: thay `speedMultiplier` bằng inventory,
  hiệu ứng tạm thời, hoặc nấm có loại khác nhau (nấm đỏ, nấm vàng, nấm độc...).
- **Texture trần (bầu trời)**: hiện còn là màu phẳng, có thể thêm ảnh
  `sky.png` và load tương tự các texture khác.
- **PvP nhiều người chơi**: gắn `NetworkHub.vb` / `NetworkPeer.vb` theo kiến
  trúc star-topology đang dùng ở các game GamePvP khác (Contra, Mario,
  Tarzan...), đồng bộ vị trí người chơi + trạng thái nấm qua mạng thay vì
  chỉ chạy local.
- **Va chạm/đấm và PvP**: thêm projectile hoặc melee giữa các người chơi
  khi đã có lớp network.
- **Sàn nhiều tầng nhìn xuyên/dốc thật**: bước 2 của hướng nâng cấp raycasting
  - viết lại floor-casting theo kiểu ray-march từng cột (voxel heightmap)
  thay vì theo hàng như hiện tại, cho phép nhìn thấy tầng dưới từ trên cao,
  sàn dốc, và vẽ đúng vách/cạnh của bục từ mọi góc nhìn.
