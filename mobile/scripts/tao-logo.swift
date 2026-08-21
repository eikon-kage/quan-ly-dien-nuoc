// Sinh bộ icon cho app Chấm công từ logo phần mềm máy tính (WinForms).
// Logo gốc vẽ bằng GDI+ trong src/QuanLyDienNuoc/Ui/ThanhBen.cs (hàm TaoLogo):
// ô bo góc 42x42 bo 10, gradient chéo Theme.Chinh -> Theme.Xanh, giọt nước trắng giữa ô.
// Chạy: swift mobile/scripts/tao-logo.swift   (ghi thẳng vào mobile/assets)

import CoreGraphics
import Foundation
import ImageIO
import UniformTypeIdentifiers

// Màu lấy từ src/QuanLyDienNuoc/Ui/Theme.cs
let mauChinh = (r: 19.0 / 255, g: 102.0 / 255, b: 217.0 / 255) // Theme.Chinh  #1366D9
let mauXanh = (r: 16.0 / 255, g: 167.0 / 255, b: 96.0 / 255)   // Theme.Xanh   #10A760

let khongGianMau = CGColorSpaceCreateDeviceRGB()

/// Đường viền giọt nước, chuẩn hoá trong ô vuông 1x1 (y hướng xuống như GDI+).
/// Theo GraphicsPath trong TaoLogo: ô 42x42, chóp giọt ở (21,11), bầu dưới là
/// vòng tròn tâm (21,26) bán kính 8. Bản gốc nối chóp vào cung bằng đoạn thẳng tới
/// (29,24) — lệch khỏi cung chưa tới nửa điểm ảnh ở cỡ 42px nên không thấy, nhưng
/// phóng lên 1024px thì thành vết gấp, nên ở đây cạnh giọt đi đúng tiếp tuyến.
func duongGiotNuoc() -> CGPath {
    let don = 42.0
    let chop = CGPoint(x: 21 / don, y: 11 / don)
    let tam = CGPoint(x: 21 / don, y: 26 / don)
    let banKinh = 8 / don
    // Chóp nằm ngay trên tâm; tiếp điểm lệch acos(r/d) về hai bên.
    let lech = acos(banKinh / (tam.y - chop.y)) * 180 / .pi
    let gocDau = 270 + lech
    let quet = 360 - 2 * lech

    let duong = CGMutablePath()
    duong.move(to: chop)
    var goc = gocDau
    while goc <= gocDau + quet {
        let rad = goc * .pi / 180
        duong.addLine(to: CGPoint(x: tam.x + banKinh * cos(rad), y: tam.y + banKinh * sin(rad)))
        goc += 0.25
    }
    duong.closeSubpath()
    return duong
}

/// Đưa đường chuẩn hoá 1x1 về khung thật: cao `caoGiot` phần khung, đặt giữa.
func giotTrongKhung(canh: Double, tiLe: Double) -> CGPath {
    // Trong ô gốc giọt cao 23/42 ~ 0.548 khung. `tiLe` là chiều cao giọt so với `canh`.
    let heSo = canh * tiLe / (23.0 / 42.0)
    let rongGiot = heSo * 16.0 / 42.0
    let caoGiot = canh * tiLe
    var bien = CGAffineTransform(translationX: (canh - rongGiot) / 2 - heSo * 13.0 / 42.0,
                                 y: (canh - caoGiot) / 2 - heSo * 11.0 / 42.0)
    bien = bien.scaledBy(x: heSo, y: heSo)
    return duongGiotNuoc().copy(using: &bien)!
}

func taoBoiCanh(_ canh: Int) -> CGContext {
    let ctx = CGContext(data: nil, width: canh, height: canh, bitsPerComponent: 8, bytesPerRow: 0,
                        space: khongGianMau, bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue)!
    // Lật trục y để dùng cùng hệ toạ độ với GDI+ (y hướng xuống).
    ctx.translateBy(x: 0, y: CGFloat(canh))
    ctx.scaleBy(x: 1, y: -1)
    ctx.setAllowsAntialiasing(true)
    ctx.interpolationQuality = .high
    return ctx
}

/// Tô gradient chéo trên-trái -> dưới-phải, giống LinearGradientMode.ForwardDiagonal.
func toGradient(_ ctx: CGContext, khung: CGRect) {
    let gradient = CGGradient(colorsSpace: khongGianMau, colors: [
        CGColor(red: mauChinh.r, green: mauChinh.g, blue: mauChinh.b, alpha: 1),
        CGColor(red: mauXanh.r, green: mauXanh.g, blue: mauXanh.b, alpha: 1),
    ] as CFArray, locations: [0, 1])!
    ctx.drawLinearGradient(gradient,
                           start: CGPoint(x: khung.minX, y: khung.minY),
                           end: CGPoint(x: khung.maxX, y: khung.maxY),
                           options: [])
}

func ghi(_ ctx: CGContext, _ tenTep: String) {
    let anh = ctx.makeImage()!
    let duongDan = URL(fileURLWithPath: FileManager.default.currentDirectoryPath)
        .appendingPathComponent("assets/\(tenTep)")
    let dich = CGImageDestinationCreateWithURL(duongDan as CFURL, UTType.png.identifier as CFString, 1, nil)!
    CGImageDestinationAddImage(dich, anh, nil)
    guard CGImageDestinationFinalize(dich) else { fatalError("Không ghi được \(tenTep)") }
    print("đã ghi assets/\(tenTep)  \(ctx.width)x\(ctx.height)")
}

/// Ô gradient tràn viền + giọt trắng — dùng cho icon app (hệ điều hành tự bo góc).
func iconTranVien(canh: Int, tiLeGiot: Double, tenTep: String) {
    let ctx = taoBoiCanh(canh)
    let khung = CGRect(x: 0, y: 0, width: canh, height: canh)
    toGradient(ctx, khung: khung)
    ctx.addPath(giotTrongKhung(canh: Double(canh), tiLe: tiLeGiot))
    ctx.setFillColor(CGColor(red: 1, green: 1, blue: 1, alpha: 1))
    ctx.fillPath()
    ghi(ctx, tenTep)
}

/// Ô bo góc gradient + giọt trắng, nền trong suốt — logo nguyên bản của phần mềm máy tính.
func iconBoGoc(canh: Int, tiLeO: Double, tenTep: String) {
    let ctx = taoBoiCanh(canh)
    let canhO = Double(canh) * tiLeO
    let le = (Double(canh) - canhO) / 2
    let o = CGRect(x: le, y: le, width: canhO, height: canhO)
    let bo = canhO * 10.0 / 42.0 // đúng tỉ lệ bo 10 trên ô 42 của logo gốc
    ctx.saveGState()
    ctx.addPath(CGPath(roundedRect: o, cornerWidth: bo, cornerHeight: bo, transform: nil))
    ctx.clip()
    toGradient(ctx, khung: o)
    ctx.restoreGState()

    var dich = CGAffineTransform(translationX: le, y: le)
    ctx.addPath(giotTrongKhung(canh: canhO, tiLe: 23.0 / 42.0).copy(using: &dich)!)
    ctx.setFillColor(CGColor(red: 1, green: 1, blue: 1, alpha: 1))
    ctx.fillPath()
    ghi(ctx, tenTep)
}

// --- Bộ asset cho Expo ---

// Icon chính (iOS/Android cũ): gradient tràn viền, giọt chiếm 52% chiều cao.
iconTranVien(canh: 1024, tiLeGiot: 0.52, tenTep: "icon.png")

// Adaptive icon Android: nền gradient riêng, giọt nằm trong vùng an toàn 66% giữa.
do {
    let ctx = taoBoiCanh(1024)
    toGradient(ctx, khung: CGRect(x: 0, y: 0, width: 1024, height: 1024))
    ghi(ctx, "android-icon-background.png")
}
do {
    let ctx = taoBoiCanh(1024)
    ctx.addPath(giotTrongKhung(canh: 1024, tiLe: 0.34))
    ctx.setFillColor(CGColor(red: 1, green: 1, blue: 1, alpha: 1))
    ctx.fillPath()
    ghi(ctx, "android-icon-foreground.png")
}
// Bản đơn sắc: chỉ hình giọt, hệ thống tự tô màu theo chủ đề.
do {
    let ctx = taoBoiCanh(1024)
    ctx.addPath(giotTrongKhung(canh: 1024, tiLe: 0.34))
    ctx.setFillColor(CGColor(red: 1, green: 1, blue: 1, alpha: 1))
    ctx.fillPath()
    ghi(ctx, "android-icon-monochrome.png")
}

// Splash trên nền trắng và favicon web: giữ đúng ô bo góc như logo trên thanh bên.
iconBoGoc(canh: 1024, tiLeO: 0.7, tenTep: "splash-icon.png")
iconBoGoc(canh: 48, tiLeO: 1.0, tenTep: "favicon.png")
