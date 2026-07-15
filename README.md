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

| Phim         | Chuc nang        |
|--------------|-------------------|
| W / Up       | Di toi            |
| S / Down     | Di lui            |
| A            | Di ngang trai     |
| D            | Di ngang phai     |
| Left / Right | Xoay camera        |
| Space        | Nhay              |
| Ctrl / C     | Ngoi (giu de ngoi)|
| ESC          | Thoat game        |

**Luu y ve nhay/ngoi**: vi day la engine raycasting 2.5D (nguoi choi luon
di chuyen tren mot mat phang 2D, khong co truc Z that su cho va cham), nhay
va ngoi duoc mo phong bang cach dich chuyen "camera" theo chieu doc tren man
hinh (view-shift) cong voi vat ly parabol don gian cho cu nhay - tao cam
giac len/xuong that ma khong pha vo cach ban do va va cham dang hoat dong.
Ngoi con lam giam toc do di chuyen 50%. Day khong phai nhay vuot vat can
hay ngoi nup that (chua co he thong che khuat/stealth).

## Build

Chay `build.bat` (yeu cau da cai .NET Framework 4.x, script tu do tim
`vbc.exe` trong `Framework64` hoac `Framework`). File exe se duoc xuat ra
ngay thu muc goc, khong tao thu muc `bin\`.

## Huong nang cap tiep theo (goi y)

- **He thong item/skill that**: thay `speedMultiplier` bang inventory,
  hieu ung tam thoi, hoac nam co loai khac nhau (nam do, nam vang, nam doc...).
- **Texture that cho tuong/sprite**: hien tai tuong to mau phang theo do
  sau (fog), nam ve thu cong bang toan hoc; co the thay bang anh bitmap
  nap tu thu muc `Assets\` (dung `xcopy` trong build.bat nhu cac game khac).
- **PvP nhieu nguoi choi**: gan `NetworkHub.vb` / `NetworkPeer.vb` theo kien
  truc star-topology dang dung o cac game GamePvP khac (Contra, Mario,
  Tarzan...), dong bo vi tri nguoi choi + trang thai nam qua mang thay vi
  chi chay local.
- **Va cham/dam va PvP**: them projectile hoac melee giua cac nguoi choi
  khi da co lop network.
