-- Script để verify dữ liệu đã được setup đúng

-- 1. Kiểm tra số lượng records trong các bảng chính
SELECT 'DANHMUCAUHOI' as TableName, COUNT(*) as RecordCount FROM DANHMUCAUHOI
UNION ALL
SELECT 'LOAICAUHOI', COUNT(*) FROM LOAICAUHOI
UNION ALL
SELECT 'TAIKHOAN', COUNT(*) FROM TAIKHOAN
UNION ALL
SELECT 'VAITRO', COUNT(*) FROM VAITRO
UNION ALL
SELECT 'TAIKHOAN_VAITRO', COUNT(*) FROM TAIKHOAN_VAITRO
UNION ALL
SELECT 'CAUHOI', COUNT(*) FROM CAUHOI WHERE DaXoa = 0
UNION ALL
SELECT 'LUACHON', COUNT(*) FROM LUACHON
UNION ALL
SELECT 'DETHI', COUNT(*) FROM DETHI
UNION ALL
SELECT 'DETHI_CAUHOI', COUNT(*) FROM DETHI_CAUHOI
UNION ALL
SELECT 'BAITHI', COUNT(*) FROM BAITHI
UNION ALL
SELECT 'CHITIETLAMBAI', COUNT(*) FROM CHITIETLAMBAI
UNION ALL
SELECT 'CANHBAOGIANLAN', COUNT(*) FROM CANHBAOGIANLAN;

-- 2. Xem chi tiết dữ liệu
PRINT '=== DANH MUC CAU HOI ===';
SELECT Id, TenDanhMuc FROM DANHMUCAUHOI;

PRINT '=== LOAI CAU HOI ===';
SELECT Id, TenLoai FROM LOAICAUHOI;

PRINT '=== TAI KHOAN ===';
SELECT Id, TenDangNhap, MaNhanVien, ChucDanh FROM TAIKHOAN;

PRINT '=== VAI TRO ===';
SELECT Id, MaVaiTro, TenVaiTro FROM VAITRO;

PRINT '=== CAU HOI ===';
SELECT 
    c.Id,
    c.NoiDung,
    d.TenDanhMuc,
    l.TenLoai,
    c.Diem,
    c.DoKho,
    (SELECT COUNT(*) FROM LUACHON WHERE IdCauHoi = c.Id) as SoLuaChon
FROM CAUHOI c
LEFT JOIN DANHMUCAUHOI d ON c.IdDanhMuc = d.Id
LEFT JOIN LOAICAUHOI l ON c.IdLoaiCauHoi = l.Id
WHERE c.DaXoa = 0
ORDER BY c.Id;

PRINT '=== DE THI ===';
SELECT 
    dt.Id,
    dt.MaDeThi,
    dt.TenDeThi,
    dt.ThoiGianLamBai,
    dt.TrangThai,
    (SELECT COUNT(*) FROM DETHI_CAUHOI WHERE IdDeThi = dt.Id) as SoCauHoi
FROM DETHI dt
ORDER BY dt.Id;

PRINT '=== BAI THI ===';
SELECT 
    bt.Id,
    tk.TenDangNhap,
    dt.MaDeThi,
    bt.TrangThai,
    bt.TongDiem,
    bt.SoCauDung,
    bt.TongSoCau,
    bt.TongSoCanhBao
FROM BAITHI bt
LEFT JOIN TAIKHOAN tk ON bt.IdTaiKhoan = tk.Id
LEFT JOIN DETHI dt ON bt.IdDeThi = dt.Id
ORDER BY bt.Id;

-- 3. Kiểm tra tính toàn vẹn dữ liệu
PRINT '=== KIEM TRA TOAN VEN ===';

-- Kiểm tra câu hỏi có ít nhất 1 đáp án đúng
SELECT 
    c.Id,
    c.NoiDung,
    COUNT(l.Id) as TongLuaChon,
    SUM(CASE WHEN l.LaDapAnDung = 1 THEN 1 ELSE 0 END) as SoDapAnDung
FROM CAUHOI c
LEFT JOIN LUACHON l ON c.Id = l.IdCauHoi
WHERE c.DaXoa = 0
GROUP BY c.Id, c.NoiDung
HAVING SUM(CASE WHEN l.LaDapAnDung = 1 THEN 1 ELSE 0 END) = 0
ORDER BY c.Id;

-- Nếu query trên trả về kết quả, có nghĩa là có câu hỏi không có đáp án đúng

PRINT '=== KIEM TRA HOAN TAT ===';
PRINT 'Neu khong co loi nao hien thi, du lieu da duoc setup thanh cong!';