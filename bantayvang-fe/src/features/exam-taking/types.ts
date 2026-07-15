export interface ExamQuestionDto {
  id: number
  content: string | null
  imageUrl: string | null
  thuTuCau: number
  options: ExamChoiceDto[]
  idLuaChonDaChon: number | null
  idLuaChonDaChonList?: number[]
  cauTraLoiTuLuan: string | null
  daLuu: boolean
  choPhepChonNhieu?: boolean
}

export interface ExamChoiceDto {
  id: number
  content: string | null
  orderIndex: number
}

export interface ExamSubmissionDto {
  id: number
  userId: number
  examPaperId: number
  examCampaignId?: number | null
  status: string | null
  submitTime: string | null
  totalScore: number | null
  diemSo?: number | null  // computed: (correctAnswers/tongSoCau)*10
  correctAnswers: number | null
  tongSoCau: number | null
  tongSoCanhBao: number | null
  examPaperName: string | null
  examPaperCode: string | null
  durationMinutes: number | null
  thoiGianBatDau: string | null
  thoiGianConLai: number | null // seconds
  isResultPublished?: boolean
  pass?: boolean | null
}

export interface StartExamDto {
  examPaperCode?: string
  examCampaignId?: number
}

export interface SubmitAnswerDto {
  examSubmissionId: number
  questionId: number
  idLuaChonDaChon: number | null
  cauTraLoiTuLuan?: string
  daLuu: boolean
}

export interface SubmitExamDto {
  examSubmissionId: number
  danhSachCauTraLoi: SubmitAnswerDto[]
}

export interface CheatingWarningDto {
  examSubmissionId: number
  warningType: string
  moTa?: string
}