Imports System
Imports System.Drawing

' =====================================================================
'  GameModels.vb - Cac class du lieu (model) thuan tuy cua game, tach
'  rieng khoi Form1.vb (noi xu ly render/input/mang) de code do roi hon
'  khi Form1.vb ngay cang phinh to.
'
'  QUY UOC cho nang cap sau: khi them vat pham hoac vu khi moi, tao
'  class rieng trong file nay (hoac 1 file moi nhu Weapon.vb / Item.vb
'  neu class lon), roi tham chieu tu Form1.vb - KHONG nhoi them field/
'  logic thang vao Form1 nua.
' =====================================================================

' Nam moc len ban do (co the nhat de tang diem). Dung Id on dinh (khong
' phai index trong List) vi trong PvP, Host/Client can tham chieu chinh
' xac 1 cay nam cu the ngay ca khi danh sach thay doi thu tu do xoa nam.
Public Class MushroomItem
    Public Property Id As Integer
    Public Property Pos As PointF
End Class

' Trang tri (bui co / cay hoa) - thuan tuy hinh anh, khong tuong tac,
' khong dong bo qua mang (moi may tu sinh giong nhau bang seed co dinh).
Public Class DecorationItem
    Public Property Pos As PointF
    Public Property Kind As Integer     ' 0 = bui co, 1 = cay hoa
    Public Property Scale As Single     ' bien the kich thuoc cho tu nhien
    Public Property HueSeed As Single   ' bien the mau (chu yeu dung cho hoa)
End Class

' Trang thai 1 nguoi choi khac trong PvP (vi tri/goc/trang thai nhay-ngoi
' nhan duoc qua mang), dung de ve billboard sprite va hien diem tren HUD.
Public Class RemotePlayerState
    Public Property X As Double
    Public Property Y As Double
    Public Property Angle As Double
    Public Property Z As Double
    Public Property Crouch As Double
    Public Property Score As Integer = 0
    Public Property Health As Integer = 100
    Public Property LastSeen As DateTime = DateTime.Now
    Public Property HeldItemId As String = "" ' Id trong itemCatalog (vd "bow"/"dagger"/"sword"), "" = chua trang bi. Dong bo qua goi POS.

    ' ---- Du lieu phuc vu hoat anh di bo / tho (tinh trong Form1.UpdateRemoteAnimations) ----
    Public Property HasPrevPos As Boolean = False   ' False cho toi khi nhan duoc goi POS thu 2 tro di
    Public Property PrevX As Double = 0.0
    Public Property PrevY As Double = 0.0
    Public Property PrevPosTime As DateTime = DateTime.Now
    Public Property MoveSpeedEst As Double = 0.0    ' don vi map/giay, uoc luong tu khoang cach giua 2 goi POS gan nhat
    Public Property BobPhase As Double = 0.0        ' pha vong lap sai chan khi dang di bo
    Public Property BobAmount As Double = 0.0       ' 0 = dung yen, 1 = dang di bo het co (noi suy muot)
    Public Property IdlePhase As Double = 0.0       ' pha "tho" cham khi dung yen
End Class

' Loai vat pham - anh huong cach xu ly khi "dung" (UseHeldItem) va mau fallback
' khi chua co icon rieng.
Public Enum ItemKind
    Weapon
    Tool
    Consumable
End Enum

' Dinh nghia 1 loai vat pham/vu khi (dung chung cho nhieu InventorySlot neu can).
' Cac field chien dau (Damage/Range/Cooldown/IsRanged/ProjectileSpeed/HealAmount) chi
' co y nghia tuy theo Kind: Weapon dung Damage+Cooldown, them Range (can chien) hoac
' IsRanged+ProjectileSpeed (tam xa); Consumable dung HealAmount.
Public Class ItemDefinition
    Public Property Id As String
    Public Property DisplayName As String
    Public Property Kind As ItemKind = ItemKind.Weapon
    Public Property IconFileName As String = ""   ' ten file trong Assets\Items\, rong = chua co icon

    Public Property Damage As Integer = 0
    Public Property Cooldown As Double = 0.5      ' giay giua 2 lan dung lien tiep
    Public Property Range As Double = 1.0         ' tam can chien (o ban do), chi dung khi khong IsRanged
    Public Property IsRanged As Boolean = False   ' True = ban ten/phi tieu thay vi vung can chien
    Public Property ProjectileSpeed As Double = 6.0 ' o ban do / giay, chi dung khi IsRanged
    Public Property HealAmount As Integer = 0     ' chi dung cho Consumable (vd binh thuoc)
End Class

' 1 o trang bi (hotbar) gan voi 1 phim so. Item = Nothing nghia la o trong / tay khong.
Public Class InventorySlot
    Public Property HotkeyNumber As Integer
    Public Property Item As ItemDefinition = Nothing
End Class

' 1 vat pham dang nam ngoai the gioi (chua nhat), cho toi khi nguoi choi di ngang
' nhat vao tui. Dung Id on dinh (giong MushroomItem) de dong bo PvP an toan.
' ItemId tham chieu toi "Id" trong catalog ItemDefinition (Form1.itemCatalog),
' KHONG phai chinh no la ItemDefinition, de khong phai gui ca dinh nghia qua mang.
Public Class WorldItemSpawn
    Public Property Id As Integer
    Public Property Pos As PointF
    Public Property ItemId As String
End Class

' Phi tieu/mui ten dang bay trong khong gian (vu khi tam xa, vd cung). OwnerSlot dung
' de biet la CUA AI: chi may cua nguoi so huu (OwnerSlot = localSlot) moi tu kiem tra
' va cham/bao cao sat thuong len Host, cac may khac chi mo phong de VE cho dep, tranh
' 1 mui ten bi nhieu may cung bao trung lam sai lech sat thuong.
Public Class Projectile
    Public Property Id As Integer
    Public Property OwnerSlot As Integer
    Public Property X As Double
    Public Property Y As Double
    Public Property Angle As Double
    Public Property Speed As Double
    Public Property Damage As Integer
    Public Property Life As Double        ' giay con lai truoc khi tu huy (het tam)
    Public Property Resolved As Boolean   ' da bao trung dich/tuong roi, cho xoa khoi danh sach
End Class


