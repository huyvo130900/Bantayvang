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
            // Question mappings
            CreateMap<Question, QuestionDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.IdLoaiCauHoiNavigation!.CategoryName))
                .ForMember(dest => dest.Options, opt => opt.MapFrom(src => src.QuestionOptions))
                .ForMember(dest => dest.Difficulty, opt => opt.MapFrom(src => 
                    src.Difficulty == "3" ? "Khó" : 
                    src.Difficulty == "2" ? "Trung bình" : "Dễ"))
                .ForMember(dest => dest.Campaigns, opt => opt.MapFrom(src => 
                    src.ExamPaperQuestions
                        .Where(dc => dc.IdDeThiNavigation != null && dc.IdDeThiNavigation.KyThiNavigation != null)
                        .Select(dc => dc.IdDeThiNavigation!.KyThiNavigation!.CampaignName)
                        .Distinct()
                        .ToList()))
                .ForMember(dest => dest.ExamPapers, opt => opt.MapFrom(src => 
                    src.ExamPaperQuestions
                        .Where(dc => dc.IdDeThiNavigation != null)
                        .Select(dc => dc.IdDeThiNavigation!.ExamPaperName)
                        .Distinct()
                        .ToList()));

            CreateMap<CreateQuestionDto, Question>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.DaXoa, opt => opt.Ignore())
                .ForMember(dest => dest.QuestionOptions, opt => opt.Ignore());

            CreateMap<UpdateQuestionDto, Question>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.DaXoa, opt => opt.Ignore())
                .ForMember(dest => dest.QuestionOptions, opt => opt.Ignore());

            // QuestionOption mappings
            CreateMap<QuestionOption, QuestionOptionDto>();
            CreateMap<CreateQuestionOptionDto, QuestionOption>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.QuestionId, opt => opt.Ignore());

            // ExamPaper mappings
            CreateMap<ExamPaper, ExamPaperDto>()
                .ForMember(dest => dest.TotalQuestions, opt => opt.MapFrom(src => src.ExamPaperQuestions.Count))
                .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Department))
                .ForMember(dest => dest.IsResultPublished, opt => opt.MapFrom(src => src.IsResultPublished))
                .ForMember(dest => dest.ThoiGianCongBo, opt => opt.MapFrom(src => src.ThoiGianCongBo))
                .ForMember(dest => dest.ExamCampaignId, opt => opt.MapFrom(src => src.ExamCampaignId))
                .ForMember(dest => dest.MinPassQuestions, opt => opt.MapFrom(src => src.MinPassQuestions))
                .ForMember(dest => dest.DanhSachCauHoi, opt => opt.Ignore());

            CreateMap<CreateExamPaperDto, ExamPaper>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.TotalScore, opt => opt.Ignore())
                .ForMember(dest => dest.LinkTruyCap, opt => opt.Ignore());

            // ExamSubmission mappings
            CreateMap<ExamSubmission, ExamSubmissionDto>()
                .ForMember(dest => dest.ExamPaperName, opt => opt.Ignore())
                .ForMember(dest => dest.DurationMinutes, opt => opt.Ignore())
                .ForMember(dest => dest.StartTime, opt => opt.Ignore())
                .ForMember(dest => dest.ThoiGianConLai, opt => opt.Ignore());

            // Exam Question mappings
            CreateMap<Question, ExamQuestionDto>()
                .ForMember(dest => dest.Options, opt => opt.MapFrom(src => src.QuestionOptions))
                .ForMember(dest => dest.ThuTuCau, opt => opt.Ignore())
                .ForMember(dest => dest.SelectedOptionId, opt => opt.Ignore())
                .ForMember(dest => dest.CauTraLoiTuLuan, opt => opt.Ignore())
                .ForMember(dest => dest.DaLuu, opt => opt.Ignore());

            CreateMap<QuestionOption, ExamChoiceDto>();

            // Answer mappings
            CreateMap<SubmitAnswerDto, SubmissionDetail>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ThoiGianTraLoi, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.ScoreObtained, opt => opt.Ignore());
        }
    }
}