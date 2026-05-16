import { useEffect, useRef } from 'react'
import { useRouter, useLocation } from '@tanstack/react-router'
import { useSidebar } from '@/components/ui/sidebar'
import { Capacitor } from '@capacitor/core'

// List of root routes where swipe-back should be disabled
const ROOT_ROUTES = [
  '/dashboard',
  '/carteiras',
  '/renda-fixa',
  '/renda-variavel',
  '/simulador',
  '/importar',
  '/analise',
  '/chat',
  '/taxas',
  '/configuracoes',
  '/admin/usuarios'
]

export function useSwipeBack() {
  const router = useRouter()
  const location = useLocation()
  const { openMobile, isMobile } = useSidebar()

  // Refs to track touch coordinates
  const touchStartRef = useRef<{ x: number, y: number } | null>(null)

  useEffect(() => {
    // If on iOS, rely on native swipe gesture configured in capacitor.config.ts
    // to avoid double-handling or conflicts.
    if (Capacitor.getPlatform() === 'ios') return

    // Only enable on mobile/touch devices if needed, but the events are touch-only anyway.

    const handleTouchStart = (e: TouchEvent) => {
      // Only single touch
      if (e.touches.length !== 1) return

      const touch = e.touches[0]

      // Only start if touch is near the left edge (e.g., 40px)
      if (touch.clientX < 40) {
        touchStartRef.current = { x: touch.clientX, y: touch.clientY }
      } else {
        touchStartRef.current = null
      }
    }

    const handleTouchEnd = (e: TouchEvent) => {
      if (!touchStartRef.current) return

      const touch = e.changedTouches[0]
      const deltaX = touch.clientX - touchStartRef.current.x
      const deltaY = touch.clientY - touchStartRef.current.y

      // Reset
      touchStartRef.current = null

      // Conditions for Swipe Back:
      // 1. Swipe must be rightward (deltaX > 0)
      // 2. Swipe distance must be significant (e.g., > 60px)
      // 3. Swipe must be mostly horizontal (abs(deltaY) < deltaX)
      if (deltaX > 60 && Math.abs(deltaY) < deltaX) {

        // 4. Check if Sidebar is open on mobile. If open, Sidebar handles gestures (or we shouldn't navigate underneath)
        if (isMobile && openMobile) return

        // 5. Check if we are on a root route. If so, don't go back.
        if (ROOT_ROUTES.includes(location.pathname)) return

        // 6. Check if history allows back (optional, but window.history.back handles it safely usually)
        // Using window.history.back() to simulate native back button
        window.history.back()
      }
    }

    // Add event listeners with passive: false if we wanted to preventDefault, but we don't need to block scrolling usually.
    // Actually, horizontal swipe might conflict with horizontal scrolling elements.
    // Since we only listen to the LEFT EDGE, conflict is minimal unless there's a carousel starting at 0px.
    document.addEventListener('touchstart', handleTouchStart, { passive: true })
    document.addEventListener('touchend', handleTouchEnd, { passive: true })

    return () => {
      document.removeEventListener('touchstart', handleTouchStart)
      document.removeEventListener('touchend', handleTouchEnd)
    }
  }, [location.pathname, isMobile, openMobile, router])
}
