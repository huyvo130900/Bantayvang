import { configureStore } from '@reduxjs/toolkit'
import authReducer from '@/features/auth/slice'
import usersReducer from '@/features/users/slice'
import questionsReducer from '@/features/questions/slice'
import examsReducer from '@/features/exams/slice'

export const store = configureStore({
  reducer: {
    auth: authReducer,
    users: usersReducer,
    questions: questionsReducer,
    exams: examsReducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: false,
    }),
})

export type RootState = ReturnType<typeof store.getState>
export type AppDispatch = typeof store.dispatch
