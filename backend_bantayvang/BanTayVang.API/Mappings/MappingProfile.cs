using AutoMapper;
using BanTayVang.API.DTOs.Question;
using BanTayVang.API.DTOs.Exam;
using BanTayVang.API.Models;

namespace BanTayVang.API.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Cauhoi mappings
            CreateMap<Cauhoi, CauhoiDto>()
                .ForMember(dest => dest.TenDanhMuc, opt => opt.MapFrom(src => src.IdDanhMucNavigation!.TenDanhMuc))
                .ForMember(dest => dest.TenLoaiCauHoi, opt => opt.MapFrom(src => src.IdLoaiCauHoiNavigation!.TenLoai))
                .ForMember(dest => dest.DanhSachLuaChon, opt => opt.MapFrom(src => src.Luachons));

            CreateMap<CreateCauhoiDto, Cauhoi>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.NgayTao, opt => opt.Ignore())
                .ForMember(dest => dest.NgayCapNhat, opt => opt.Ignore())
                .ForMember(dest => dest.NguoiTao, opt => opt.Ignore())
                .ForMember(dest => dest.NguoiCapNhat, opt => opt.Ignore())
                .ForMember(dest => dest.DaXoa, opt => opt.Ignore())
                .ForMember(dest => dest.Luachons, opt => opt.Ignore());

            CreateMap<UpdateCauhoiDto, Cauhoi>()
                .ForMember(dest => dest.NgayTao, opt => opt.Ignore())
                .ForMember(dest => dest.NgayCapNhat, opt => opt.Ignore())
                .ForMember(dest => dest.NguoiTao, opt => opt.Ignore())
                .ForMember(dest => dest.NguoiCapNhat, opt => opt.Ignore())
                .ForMember(dest => dest.DaXoa, opt => opt.Ignore())
                .ForMember(dest => dest.Luachons, opt => opt.Ignore());

            // Luachon mappings
            CreateMap<Luachon, LuachonDto>();
            CreateMap<CreateLuachonDto, Luachon>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.IdCauHoi, opt => opt.Ignore());

            // Dethi mappings
            CreateMap<Dethi, DethiDto>()
                .ForMember(dest => dest.SoCauHoi, opt => opt.MapFrom(src => src.DethiCauhois.Count))
                .ForMember(dest => dest.DanhSachCauHoi, opt => opt.Ignore());

            CreateMap<CreateDethiDto, Dethi>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.NgayTao, opt => opt.Ignore())
                .ForMember(dest => dest.NguoiTao, opt => opt.Ignore())
                .ForMember(dest => dest.TongDiem, opt => opt.Ignore())
                .ForMember(dest => dest.LinkTruyCap, opt => opt.Ignore());

            // Baithi mappings
            CreateMap<Baithi, BaithiDto>()
                .ForMember(dest => dest.TenDeThi, opt => opt.Ignore())
                .ForMember(dest => dest.ThoiGianLamBai, opt => opt.Ignore())
                .ForMember(dest => dest.ThoiGianBatDau, opt => opt.Ignore())
                .ForMember(dest => dest.ThoiGianConLai, opt => opt.Ignore());

            // Exam Question mappings
            CreateMap<Cauhoi, ExamQuestionDto>()
                .ForMember(dest => dest.DanhSachLuaChon, opt => opt.MapFrom(src => src.Luachons))
                .ForMember(dest => dest.ThuTuCau, opt => opt.Ignore())
                .ForMember(dest => dest.IdLuaChonDaChon, opt => opt.Ignore())
                .ForMember(dest => dest.CauTraLoiTuLuan, opt => opt.Ignore())
                .ForMember(dest => dest.DaLuu, opt => opt.Ignore());

            CreateMap<Luachon, ExamChoiceDto>();

            // Answer mappings
            CreateMap<SubmitAnswerDto, Chitietlambai>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ThoiGianTraLoi, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.DiemDatDuoc, opt => opt.Ignore());
        }
    }
}