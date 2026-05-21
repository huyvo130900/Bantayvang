import { createSlice, createAsyncThunk } from '@reduxjs/toolkit'
import { examsApi } from './api'
import type { DethiDto, ExamAssignmentDto } from './types'

interface ExamsState {
  exams: DethiDto[]
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

export const fetchActiveExams = createAsyncThunk(
  'exams/fetchActive',
  async (_, { rejectWithValue }) => {
    try {
      const response = await examsApi.getActive()
      if (!response.data.success) return rejectWithValue(response.data.message)
      return response.data.data!
    } catch (error: unknown) {
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
    } catch (error: unknown) {
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
