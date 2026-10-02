import { useEffect, useState } from 'react'
import { useNavigate } from '@tanstack/react-router'
import { Button } from '@/components/ui/button'
import { useAuthStore } from '@/store/authStore'

export default function AcceptInvitation() {
  const navigate = useNavigate()
  const acceptInvitation = useAuthStore(state => state.acceptInvitation)
  const [error, setError] = useState<string | null>(null)
  const [isAccepting, setIsAccepting] = useState(true)

  useEffect(() => {
    const url = new URL(window.location.href)
    const hash = new URLSearchParams(url.hash.replace(/^#/, ''))
    const grupoId = url.searchParams.get('grupoId')
    const conviteId = url.searchParams.get('conviteId')
    const tokenHash = url.searchParams.get('token_hash') ?? hash.get('token_hash')
    const accessToken = url.searchParams.get('access_token') ?? hash.get('access_token')
    if (!grupoId || !conviteId || (!tokenHash && !accessToken)) {
      setError('O link de convite está incompleto. Peça um novo convite ao administrador do grupo.')
      setIsAccepting(false)
      return
    }
    void acceptInvitation({ grupoId, conviteId, tokenHash: tokenHash ?? undefined, tokenSupabase: accessToken ?? undefined })
      .then(() => navigate({ to: '/carteiras', replace: true }))
      .catch(reason => setError(reason instanceof Error ? reason.message : 'Não foi possível aceitar o convite.'))
      .finally(() => setIsAccepting(false))
  }, [acceptInvitation, navigate])

  return (
    <main className="flex min-h-screen items-center justify-center p-6">
      <section className="w-full max-w-md space-y-4 rounded-xl border bg-card p-6 text-center shadow-sm">
        <h1 className="text-xl font-semibold">Convite para grupo</h1>
        {isAccepting ? <p className="text-muted-foreground">Validando convite…</p> : error ? (
          <><p role="alert" className="text-sm text-destructive">{error}</p><Button onClick={() => navigate({ to: '/login' })}>Entrar na conta</Button></>
        ) : <p className="text-muted-foreground">Convite aceito. Abrindo suas carteiras…</p>}
      </section>
    </main>
  )
}
