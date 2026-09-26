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
    } catch (error: any) {
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
    } catch (error: any) {
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
    } catch (error: any) {
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
    } catch (error: any) {
      const err = error as { response?: { data?: { message?: string } } }
      return rejectWithValue(err.response?.data?.message || 'Lỗi thay đổi trạng thái')
    }
  }
)

export const deleteUser = createAsyncThunk(
  'users/deleteUser',
  async (id: number, { rejectWithValue }) => {
    try {
      const response = await usersApi.delete(id)
      if (!response.data.success) {
        return rejectWithValue(response.data.message)
      }
      return id
    } catch (error: any) {
      const err = error as { response?: { data?: { message?: string } } }
      return rejectWithValue(err.response?.data?.message || 'Lỗi xóa người dùng')
    }
  }
)

export const restoreUser = createAsyncThunk(
  'users/restoreUser',
  async (id: number, { rejectWithValue }) => {
    try {
      const response = await usersApi.restore(id)
      if (!response.data.success) {
        return rejectWithValue(response.data.message)
      }
      return id
    } catch (error: any) {
      const err = error as { response?: { data?: { message?: string } } }
      return rejectWithValue(err.response?.data?.message || 'Lỗi khôi phục người dùng')
    }
  }
)

export const hardDeleteUser = createAsyncThunk(
  'users/hardDeleteUser',
  async (id: number, { rejectWithValue }) => {
    try {
      const response = await usersApi.hardDelete(id)
      if (!response.data.success) {
        return rejectWithValue(response.data.message)
      }
      return id
    } catch (error: any) {
      const err = error as { response?: { data?: { message?: string } } }
      return rejectWithValue(err.response?.data?.message || 'Lỗi xóa vĩnh viễn người dùng')
    }
  }
)

// BUG FIX: these two thunks used to discard the backend's response entirely and just return the
// original `ids` array back to the caller on success - so when the server skipped some users
// (DeptManager selecting a user outside their department, or an Admin account that bulk-delete
// always skips) and returned a message with the REAL count, the UI had no way to know and showed
// a hardcoded "Đã xóa {selectedIds.length} tài khoản thành công" - overstating how many accounts
// were actually affected. Now returns the server's own message so the caller can display it.
export const bulkDeleteUsers = createAsyncThunk(
  'users/bulkDeleteUsers',
  async (ids: number[], { rejectWithValue }) => {
    try {
      const response = await usersApi.bulkDelete(ids)
      if (!response.data.success) {
        return rejectWithValue(response.data.message)
      }
      return response.data.message
    } catch (error: any) {
      const err = error as { response?: { data?: { message?: string } } }
      return rejectWithValue(err.response?.data?.message || 'Lỗi xóa hàng loạt')
    }
  }
)

export const bulkHardDeleteUsers = createAsyncThunk(
  'users/bulkHardDeleteUsers',
  async (ids: number[], { rejectWithValue }) => {
    try {
      const response = await usersApi.bulkHardDelete(ids)
      if (!response.data.success) {
        return rejectWithValue(response.data.message)
      }
      return response.data.message
    } catch (error: any) {
      const err = error as { response?: { data?: { message?: string } } }
      return rejectWithValue(err.response?.data?.message || 'Lỗi xóa vĩnh viễn hàng loạt')
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
          user.status = activate
        }
      })
      .addCase(deleteUser.fulfilled, (state, action) => {
        // Nếu đang xem thùng rác thì đánh dấu isDeleted, nếu không thì xóa khỏi list
        if (state.filter.includeDeleted) {
          const index = state.users.findIndex((u) => u.id === action.payload)
          if (index !== -1) {
            state.users[index].isDeleted = true
          }
        } else {
          state.users = state.users.filter((u) => u.id !== action.payload)
        }
      })
      .addCase(restoreUser.fulfilled, (state, action) => {
        const index = state.users.findIndex((u) => u.id === action.payload)
        if (index !== -1) {
          state.users[index].isDeleted = false
        }
      })
      .addCase(hardDeleteUser.fulfilled, (state, action) => {
        state.users = state.users.filter((u) => u.id !== action.payload)
      })
  },
})

export const { setFilter, setSelectedUser, clearError } = usersSlice.actions
export default usersSlice.reducer
