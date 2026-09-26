import { createSlice, createAsyncThunk } from '@reduxjs/toolkit'
import { examsApi, examsApiExtended } from './api'
import type { ExamPaperDto, ExamAssignmentDto } from './types'

interface ExamsState {
  exams: ExamPaperDto[]
  assignments: ExamAssignmentDto[]
  isLoading: boolean
  error: string | null
}

const initialState: ExamsState = {
  exams: [],
  assignments: [],
  isLoading: false,
  error: null,
}

// Dùng cho trang admin: lấy TẤT CẢ đề thi (không lọc status)
export const fetchAllExams = createAsyncThunk(
  'exams/fetchAll',
  async (_, { rejectWithValue }) => {
    try {
      const response = await examsApiExtended.getAll()
      if (!response.data.success) return rejectWithValue(response.data.message)
      return response.data.data!
    } catch (error: any) {
      const err = error as { response?: { data?: { message?: string } } }
      return rejectWithValue(err.response?.data?.message || 'Lỗi tải đề thi')
    }
  }
)

// Vẫn giữ fetchActiveExams để trang học sinh dùng
export const fetchActiveExams = createAsyncThunk(
  'exams/fetchActive',
  async (_, { rejectWithValue }) => {
    try {
      const response = await examsApi.getActive()
      if (!response.data.success) return rejectWithValue(response.data.message)
      return response.data.data!
    } catch (error: any) {
      const err = error as { response?: { data?: { message?: string } } }
      return rejectWithValue(err.response?.data?.message || 'Lỗi tải đề thi')
    }
  }
)

export const fetchAssignmentsByExam = createAsyncThunk(
  'exams/fetchAssignments',
  async (examId: number, { rejectWithValue }) => {
    try {
      const response = await examsApi.getAssignmentsByExam(examId)
      if (!response.data.success) return rejectWithValue(response.data.message)
      return response.data.data!
    } catch (error: any) {
      const err = error as { response?: { data?: { message?: string } } }
      return rejectWithValue(err.response?.data?.message || 'Lỗi tải phân công')
    }
  }
)

const examsSlice = createSlice({
  name: 'exams',
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = null
    },
    clearAssignments: (state) => {
      state.assignments = []
    },
  },
  extraReducers: (builder) => {
    builder
      // fetchAllExams
      .addCase(fetchAllExams.pending, (state) => {
        state.isLoading = true
        state.error = null
      })
      .addCase(fetchAllExams.fulfilled, (state, action) => {
        state.isLoading = false
        state.exams = action.payload
      })
      .addCase(fetchAllExams.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.payload as string
      })
      // fetchActiveExams
      .addCase(fetchActiveExams.pending, (state) => {
        state.isLoading = true
        state.error = null
      })
      .addCase(fetchActiveExams.fulfilled, (state, action) => {
        state.isLoading = false
        state.exams = action.payload
      })
      .addCase(fetchActiveExams.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.payload as string
      })
      .addCase(fetchAssignmentsByExam.fulfilled, (state, action) => {
        state.assignments = action.payload
      })
  },
})

export const { clearError, clearAssignments } = examsSlice.actions
export default examsSlice.reducer
