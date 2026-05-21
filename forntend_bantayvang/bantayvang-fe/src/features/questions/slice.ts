import { createSlice, createAsyncThunk } from '@reduxjs/toolkit'
import { questionsApi } from './api'
import type { CauhoiDto, QuestionFilterDto, DanhmucauhoiDto, LoaicauhoiDto } from './types'
import type { PaginationDto } from '@/types'

interface QuestionsState {
  questions: CauhoiDto[]
  pagination: PaginationDto | null
  categories: DanhmucauhoiDto[]
  questionTypes: LoaicauhoiDto[]
  isLoading: boolean
  error: string | null
  filter: QuestionFilterDto
}

const initialState: QuestionsState = {
  questions: [],
  pagination: null,
  categories: [],
  questionTypes: [],
  isLoading: false,
  error: null,
  filter: {
    pageNumber: 1,
    pageSize: 20,
  },
}

export const fetchQuestions = createAsyncThunk(
  'questions/fetchQuestions',
  async (filter: QuestionFilterDto, { rejectWithValue }) => {
    try {
      const response = await questionsApi.list(filter)
      if (!response.data.success) return rejectWithValue(response.data.message)
      return response.data.data!
    } catch (error: unknown) {
      const err = error as { response?: { data?: { message?: string } } }
      return rejectWithValue(err.response?.data?.message || 'Lỗi tải câu hỏi')
    }
  }
)

export const fetchCategories = createAsyncThunk(
  'questions/fetchCategories',
  async (_, { rejectWithValue }) => {
    try {
      const response = await questionsApi.getCategories()
      if (!response.data.success) return rejectWithValue(response.data.message)
      return response.data.data!
    } catch (error: unknown) {
      const err = error as { response?: { data?: { message?: string } } }
      return rejectWithValue(err.response?.data?.message || 'Lỗi tải danh mục')
    }
  }
)

export const fetchQuestionTypes = createAsyncThunk(
  'questions/fetchQuestionTypes',
  async (_, { rejectWithValue }) => {
    try {
      const response = await questionsApi.getQuestionTypes()
      if (!response.data.success) return rejectWithValue(response.data.message)
      return response.data.data!
    } catch (error: unknown) {
      const err = error as { response?: { data?: { message?: string } } }
      return rejectWithValue(err.response?.data?.message || 'Lỗi tải loại câu hỏi')
    }
  }
)

const questionsSlice = createSlice({
  name: 'questions',
  initialState,
  reducers: {
    setFilter: (state, action) => {
      state.filter = { ...state.filter, ...action.payload }
    },
    clearError: (state) => {
      state.error = null
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchQuestions.pending, (state) => {
        state.isLoading = true
        state.error = null
      })
      .addCase(fetchQuestions.fulfilled, (state, action) => {
        state.isLoading = false
        state.questions = action.payload.items
        state.pagination = action.payload.pagination
      })
      .addCase(fetchQuestions.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.payload as string
      })
      .addCase(fetchCategories.fulfilled, (state, action) => {
        state.categories = action.payload
      })
      .addCase(fetchQuestionTypes.fulfilled, (state, action) => {
        state.questionTypes = action.payload
      })
  },
})

export const { setFilter, clearError } = questionsSlice.actions
export default questionsSlice.reducer
