IF NOT EXISTS (SELECT * FROM [VAITRO] WHERE [MaVaiTro] = 'ThiSinhNgoai')
BEGIN
    INSERT INTO [VAITRO] ([MaVaiTro], [TenVaiTro], [MoTa])
    VALUES ('ThiSinhNgoai', N'Thí Sinh Ngoài', N'Thí sinh đăng ký dự thi từ bên ngoài bệnh viện');
END
GO
