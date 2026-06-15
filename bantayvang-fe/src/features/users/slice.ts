import { createSlice, createAsyncThunk } from '@reduxjs/toolkit'
import { usersApi } from './api'
import type { UserDto, UserFilterDto, CreateUserDto, UpdateUserDto } from './types'

interface UsersState {
  users: UserDto[]
  selectedUser: UserDto | null
  isLoading: boolean
  error: string | null
  filter: UserFilterDto
}

const initialState: UsersState = {
  users: [],
  selectedUser: null,
  isLoading: false,
  error: null,
  filter: {
    pageNumber: 1,
    pageSize: 20,
  },
}

export const fetchUsers = createAsyncThunk(
  'users/fetchUsers',
  async (filter: UserFilterDto, { rejectWithValue }) => {
    try {
      const response = await usersApi.list(filter)
      if (!response.data.success) {
        return rejectWithValue(response.data.message)
      }
      return response.data.data!
    } catch (error: unknown) {
      const err = error as { response?: { data?: { message?: string } } }
      return rejectWithValue(err.response?.data?.message || 'Lỗi tải danh sách')
    }
  }
)

export const createUser = createAsyncThunk(
  'users/createUser',
  async (data: CreateUserDto, { rejectWithValue }) => {
    try {
      const response = await usersApi.create(data)
      if (!response.data.success) {
        return rejectWithValue(response.data.message)
      }
      return response.data.data!
    } catch (error: unknown) {
      const err = error as { response?: { data?: { message?: string } } }
      return rejectWithValue(err.response?.data?.message || 'Lỗi tạo người dùng')
    }
  }
)

export const updateUser = createAsyncThunk(
  'users/updateUser',
  async ({ id, data }: { id: number; data: UpdateUserDto }, { rejectWithValue }) => {
    try {
      const response = await usersApi.update(id, data)
      if (!response.data.success) {
        return rejectWithValue(response.data.message)
      }
      return response.data.data!
    } catch (error: unknown) {
      const err = error as { response?: { data?: { message?: string } } }
      return rejectWithValue(err.response?.data?.message || 'Lỗi cập nhật')
    }
  }
)

export const toggleUserStatus = createAsyncThunk(
  'users/toggleStatus',
  async ({ id, activate }: { id: number; activate: boolean }, { rejectWithValue }) => {
    try {
      const response = activate
        ? await usersApi.activate(id)
        : await usersApi.deactivate(id)
      if (!response.data.success) {
        return rejectWithValue(response.data.message)
      }
      return { id, activate }
    } catch (error: unknown) {
      const err = error as { response?: { data?: { message?: string } } }
      return rejectWithValue(err.response?.data?.message || 'Lỗi thay đổi trạng thái')
    }
  }
)

const usersSlice = createSlice({
  name: 'users',
  initialState,
  reducers: {
    setFilter: (state, action) => {
      state.filter = { ...state.filter, ...action.payload }
    },
    setSelectedUser: (state, action) => {
      state.selectedUser = action.payload
    },
    clearError: (state) => {
      state.error = null
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchUsers.pending, (state) => {
        state.isLoading = true
        state.error = null
      })
      .addCase(fetchUsers.fulfilled, (state, action) => {
        state.isLoading = false
        state.users = action.payload
      })
      .addCase(fetchUsers.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.payload as string
      })
      .addCase(createUser.fulfilled, (state, action) => {
        state.users.unshift(action.payload)
      })
      .addCase(updateUser.fulfilled, (state, action) => {
        const index = state.users.findIndex((u) => u.id === action.payload.id)
        if (index !== -1) {
          state.users[index] = action.payload
        }
      })
      .addCase(toggleUserStatus.fulfilled, (state, action) => {
        const { id, activate } = action.payload
        const user = state.users.find((u) => u.id === id)
        if (user) {
          user.trangThai = activate
        }
      })
  },
})

export const { setFilter, setSelectedUser, clearError } = usersSlice.actions
export default usersSlice.reducer
