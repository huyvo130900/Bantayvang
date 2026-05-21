import { Provider } from 'react-redux'
import { store } from './store'
import { AppRouter } from './router'

export function AppProviders() {
  return (
    <Provider store={store}>
      <AppRouter />
    </Provider>
  )
}
