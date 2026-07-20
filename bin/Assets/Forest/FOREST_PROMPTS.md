# Prompt vẽ texture rừng (thay ảnh sinh bằng công thức)

Đặt tất cả file vào thư mục **`Assets\Forest\`** (thư mục mới, tạo cạnh
`Assets\Characters\` hiện có). Code đã được sửa để **tự động ưu tiên dùng
ảnh thật** nếu tìm thấy đúng tên file dưới đây — thiếu file nào thì tự
động rơi về hình vẽ bằng công thức (không crash, không cần chỉnh code
thêm gì cả, chỉ cần thả ảnh đúng tên vào đúng chỗ rồi build lại).

Phong cách chung: khớp với các ảnh nhân vật/tay/nấm hiện có trong
`Assets\Characters\` — tranh vẽ kỹ thuật số bán tả thực (semi-realistic
digital painting), ánh sáng mềm, KHÔNG phải pixel art 8-bit vuông cạnh.

---

## 1. `tree_bark.png` — vỏ cây (tường chặn đường, loại ô `7`)

- **Kích thước bắt buộc: đúng 128x128 px** (sai kích thước sẽ bị game bỏ
  qua, tự động dùng lại hình công thức).
- **Phải tileable** (lặp cạnh trái-phải liền mạch) vì texture được dán
  lặp lại quanh toàn bộ bề mặt tường/thân cây.
- Prompt gợi ý:
  > Seamless tileable texture of rough tree bark, close-up view, deep
  > brown and grey-brown tones with vertical grooves and ridges, natural
  > wood grain detail, soft even lighting (no strong directional shadow
  > so it tiles cleanly), semi-realistic digital painting style, no text
  > or watermark, square seamless pattern, 128x128.

## 2. `forest_floor.png` — sàn cỏ rừng

- **Kích thước bắt buộc: đúng 128x128 px.**
- **Phải tileable cả 2 chiều** (trái-phải và trên-dưới) vì được dán lặp
  toàn bộ nền.
- Prompt gợi ý:
  > Seamless tileable top-down texture of forest ground, mixed green
  > grass with patches of brown dirt and small pebbles, natural uneven
  > coloring, soft diffuse lighting, semi-realistic digital painting
  > style, no shadows from objects, no text or watermark, square
  > seamless pattern, 128x128.

## 3. `fallen_log.png` — khúc gỗ đổ (vật cản thấp, nhảy qua được, loại ô `3`)

- **Kích thước bắt buộc: đúng 128x128 px.**
- Tileable (texture dán lên toàn bộ mặt khối gỗ).
- Prompt gợi ý:
  > Seamless tileable texture of a fallen tree log surface, horizontal
  > wood grain with visible growth rings, weathered brown bark and
  > exposed wood patches, soft even lighting, semi-realistic digital
  > painting style, no text or watermark, square seamless pattern,
  > 128x128.

## 4. `mossy_rock.png` — khe đá phủ rêu (vật cản, ngồi chui qua được, loại ô `4`)

- **Kích thước bắt buộc: đúng 128x128 px.**
- Tileable.
- Prompt gợi ý:
  > Seamless tileable texture of grey stone rock surface covered with
  > patches of green moss, natural weathered texture, soft even
  > lighting, semi-realistic digital painting style, no text or
  > watermark, square seamless pattern, 128x128.

## 5. `tree_billboard.png` — cây đứng riêng lẻ (trang trí nền, không chặn đường)

- **Không bắt buộc 128x128** — có thể dùng bất kỳ kích thước nào, engine
  tự tính lại tỉ lệ. Khuyến nghị **tỉ lệ dọc** (cao hơn rộng), ví dụ
  256x512.
- **Bắt buộc nền trong suốt** (PNG có alpha channel), giống hệt cách
  `mushroom.png` đang làm — không phải hoạ tiết lặp, chỉ là 1 hình cây
  đứng đơn lẻ nhìn thẳng.
- Prompt gợi ý:
  > A single whole tree standing alone, viewed from the front at eye
  > level, full tree with trunk and leafy canopy visible, natural green
  > foliage, semi-realistic digital painting style, soft even lighting,
  > transparent background, no ground/shadow/grass at the base, no text
  > or watermark, isolated on transparent PNG.

---

---

## Phiên bản ẢNH THẬT (photorealistic) - thay cho bán-tả-thực ở trên

Dùng khi muốn texture rừng trông như ảnh chụp thật thay vì tranh vẽ. LƯU Ý:
nhân vật/tay/vật phẩm hiện tại vẫn đang là tranh vẽ bán-tả-thực, nên dùng
bộ prompt này sẽ làm môi trường "thật" hơn nhân vật - cân nhắc trước khi
đổi hết cả 5 file. Vẫn giữ nguyên tên file/kích thước/yêu cầu tileable như
trên, chỉ đổi phong cách trong prompt.

### 1. `tree_bark.png` (128x128, tileable)
> Seamless tileable macro photography texture of real tree bark, oak or
> pine, deep brown and grey-brown natural tones, rough vertical grooves
> and ridges, photorealistic high detail, soft even overcast lighting
> with no strong directional shadow so it tiles cleanly, no text or
> watermark, square seamless pattern, 128x128.

### 2. `forest_floor.png` (128x128, tileable cả 2 chiều)
> Seamless tileable top-down photograph of real forest ground, green
> grass mixed with brown dirt patches, small pebbles and scattered dry
> leaves, natural uneven coloring, photorealistic, soft diffuse overcast
> lighting with no cast shadows, no text or watermark, square seamless
> pattern, 128x128.

### 3. `fallen_log.png` (128x128, tileable)
> Seamless tileable macro photograph of a real fallen tree log surface,
> horizontal wood grain with visible growth rings, weathered bark and
> exposed wood patches, photorealistic high detail, soft even lighting
> with no strong directional shadow, no text or watermark, square
> seamless pattern, 128x128.

### 4. `mossy_rock.png` (128x128, tileable)
> Seamless tileable macro photograph of real grey stone rock surface
> covered with patches of green moss, natural weathered granite texture,
> photorealistic high detail, soft even diffuse lighting with no strong
> directional shadow, no text or watermark, square seamless pattern,
> 128x128.

### 5. `tree_billboard.png` (không bắt buộc 128x128, khuyến nghị tỉ lệ dọc, vd 256x512)
> A single whole real tree photographed from the front at eye level,
> full deciduous tree with trunk and complete leafy canopy in sharp
> focus, natural daylight, photorealistic nature photography, isolated
> on transparent background, no ground, no grass, no shadow at the base,
> no other trees or objects, no text or watermark, centered composition,
> PNG cutout.

### Lưu ý kỹ thuật khi dùng ảnh thật thay vì tranh vẽ

- Độ phân giải nội bộ của engine chỉ 320x200 rồi phóng to lên 960x600 -
  chi tiết ảnh chụp macro rất mịn (vân đá li ti, sợi rêu nhỏ) sẽ bị mờ
  nhòe thành một mảng màu khi thu nhỏ. Ưu tiên ảnh có độ tương phản/chi
  tiết ở mức trung-lớn (đường vân gỗ rõ, mảng rêu rõ khối) thay vì texture
  quá mịn đều màu - texture mịn đều dễ nhìn "phẳng" hơn khi vào game so
  với bản vẽ tay đã có.
- Nhắc "no strong directional shadow" trong MỌI prompt texture tileable
  vì engine tự đổ bóng theo khoảng cách + đuốc (torch light) đè lên sau -
  ảnh có sẵn bóng đổ một hướng sẽ bị "chồng sáng" nhìn giả khi vào game.
- `tree_billboard.png` nhiều AI tạo ảnh không xuất được nền trong suốt
  thật (PNG alpha) mà chỉ ra nền trắng/nền xanh lá - nếu vậy thì chụp
  trên nền phông xanh lá cây thuần (không phải màu lá cây thật) rồi dùng
  Pillow flood-fill/chroma-key giống quy trình đã dùng cho các sprite
  khác (vd lá bài trong Xì Tố 5 Lá) để tách alpha, thay vì crop bounding
  box theo alpha có sẵn.

## Sau khi có ảnh

1. Tạo thư mục `Assets\Forest\` (cùng cấp với `Assets\Characters\`).
2. Thả 5 file đúng tên ở trên vào đó.
3. Build lại (`build.bat`) — không cần sửa code gì thêm, `GenerateForestTextures()`
   trong `GameRender.vb` tự dò file, có thì dùng ảnh thật, không có thì
   vẫn chạy bằng hình vẽ công thức như hiện tại.
4. Muốn kiểm tra ảnh có được nhận không: nếu sai kích thước (4 file đầu
   không đúng 128x128), game âm thầm bỏ qua và dùng lại hình công thức —
   không có thông báo lỗi, nên nhớ resize đúng trước khi thả vào.

---

## Nhóm 2: texture viền bản đồ ngoài trời (thay tường đá cứng)

Dùng cho `Layout_CanhDongRong()` (và các map ngoài trời mới sau này) — viền
map dùng cảnh vật tự nhiên (bụi cây, vách đá, khe nứt, bờ suối) thay vì
tường đá, để không có cảm giác "đụng khung tường". Cùng yêu cầu kỹ thuật
128x128, tileable, ánh sáng đều như nhóm 1 ở trên. Đã có ảnh thật cho cả 4
file này (ảnh thật do người dùng cung cấp, đã crop-center + resize 128x128).

### `bush_wall.png` — bụi cỏ rậm (loại ô `8`, hiện dùng cạnh Tây)
> photorealistic seamless tileable texture, dense thick green bush wall,
> real overlapping leaves and small branches, natural forest undergrowth,
> shot in even daylight, no strong shadows, high detail macro photography
> style, 4k texture

### `cliff_wall.png` — vách đá (loại ô `9`, hiện dùng cạnh Nam)
> photorealistic seamless tileable texture, rugged natural rock cliff
> face, weathered granite with cracks and moss patches, even daylight, no
> strong shadows, high detail photography style, 4k texture

### `crevice_wall.png` — khe nứt đất (loại ô `10`, hiện dùng cạnh Đông)
> photorealistic seamless tileable texture, dry cracked earth ravine
> wall, jagged dirt fissures with small stones and dust, even daylight,
> no strong shadows, high detail photography style, 4k texture

### `riverbank_wall.png` — bờ suối (loại ô `11`, hiện dùng cạnh Bắc)
> photorealistic seamless tileable texture, reeds and wet stones along a
> riverbank, shallow water edge, even daylight, no strong shadows, high
> detail photography style, 4k texture

### Quy trình thêm loại viền mới (loại ô tiếp theo dùng số `12`)
1. Tạo ảnh 128x128 theo mẫu prompt trên (khuyến nghị tạo ảnh lớn 768-1024px
   rồi crop giữa + resize xuống, đỡ vỡ nét hơn tạo thẳng 128x128), đặt tên
   `<ten>_wall.png`, bỏ vào thư mục này.
2. `src/Form1.vb`: thêm `Private tex<Ten>Wall() As Integer`.
3. `src/GameRender.vb` trong `GenerateForestTextures()`: thêm khối
   `TryLoadForestTexture("<ten>_wall.png")` + fallback tự sinh (copy công
   thức của bush/cliff/crevice/riverbank, đổi màu/hoạ tiết).
4. Vẫn `GameRender.vb`: thêm `Case <so>: tex = tex<Ten>Wall` vào bộ chọn
   texture tường, và thêm `<so>` vào điều kiện chặn tia raycasting.
5. `src/GameMaps.vb`: dùng số loại ô mới trong layout map ở vị trí viền
   mong muốn.

