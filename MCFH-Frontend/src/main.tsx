import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { GoogleOAuthProvider } from '@react-oauth/google'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    {/* TODO: Thay chuỗi bên dưới bằng Client ID thực tế của dự án */}
    <GoogleOAuthProvider clientId="223470488885-ne3j6eapo7hhfogo3cgvh17gero7ldbl.apps.googleusercontent.com">
      <App />
    </GoogleOAuthProvider>
  </StrictMode>,
)