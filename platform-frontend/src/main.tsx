import React from 'react'
import ReactDOM from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'

import App from './App.tsx'
import './index.css'

// Create QueryClient with industrial-appropriate settings
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // Longer stale time for industrial data that doesn't change frequently
      staleTime: 30000, // 30 seconds
      // Retry failed requests for resilience
      retry: 3,
      retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
    },
    mutations: {
      // Retry mutations for critical operations
      retry: 2,
    },
  },
})

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <BrowserRouter>
      <QueryClientProvider client={queryClient}>
        <App />
        {/* Show React Query DevTools in development */}
        {import.meta.env.DEV && (
          <ReactQueryDevtools 
            initialIsOpen={false} 
            buttonPosition="bottom-left"
          />
        )}
      </QueryClientProvider>
    </BrowserRouter>
  </React.StrictMode>,
)