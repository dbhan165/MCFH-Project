import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { GoogleOAuthProvider } from '@react-oauth/google'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    {/* TODO: Thay chuỗi bên dưới bằng Client ID thực tế của dự án */}
    <GoogleOAuthProvider clientId="371030743086-5v6dfo6bgiaag8tergce1ca0gqr88ji7.apps.googleusercontent.com">
      <App />
    </GoogleOAuthProvider>
  </StrictMode>,
)