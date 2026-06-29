-- Migration 006: Thêm cột DanhGiaKhoa vào bảng baithi
-- Cho phép quản lý khoa nhận xét và đánh giá thí sinh

-- Thêm cột đánh giá của khoa
ALTER TABLE baithi ADD COLUMN IF NOT EXISTS danh_gia_khoa TEXT DEFAULT NULL;

-- Index để tìm theo đề thi + user (phục vụ thi lại)
CREATE INDEX IF NOT EXISTS idx_baithi_dethi_user ON baithi(id_de_thi, id_tai_khoan);

-- Comment để tham khảo
COMMENT ON COLUMN baithi.danh_gia_khoa IS 'Nhận xét/đánh giá của quản lý khoa về kết quả thi của thí sinh';
