export interface QuestionOptionDto {
  id: number
  questionId: number | null
  content: string | null
  isCorrect: boolean | null
  orderIndex: number | null
}

export interface QuestionDto {
  id: number
  questionCategoryId: number | null
  categoryName: string | null
  content: string | null
  difficulty: string | null
  department: string | null
  imageUrl: string | null
  createdAt: string | null
  updatedAt: string | null
  options: QuestionOptionDto[]
  campaigns?: string[]
  examPapers?: string[]
}

export interface CreateQuestionDto {
  content: string
  questionCategoryId?: number
  difficulty?: string
  level?: string
  imageUrl?: string
  department?: string
  options?: CreateQuestionOptionDto[]
}

export interface CreateQuestionOptionDto {
  content: string
  orderIndex: number
  isCorrect: boolean
}

export interface UpdateQuestionDto extends CreateQuestionDto {
  id: number
}

export interface QuestionFilterDto {
  questionCategoryId?: number
  difficulty?: string
  department?: string
  searchKeyword?: string
  showDuplicatesOnly?: boolean
  examCampaignId?: number
  deThiId?: number
  pageNumber: number
  pageSize: number
}



export interface LoaicauhoiDto {
  id: number
  categoryName: string | null
  description: string | null
  totalQuestions?: number
}



export interface CreateLoaicauhoiDto {
  categoryName: string
  description?: string
}
