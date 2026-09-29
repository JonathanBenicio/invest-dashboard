import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useAuthStore } from '@/store/authStore'

export default function Settings() {
  const user = useAuthStore(state => state.user)

  return (
    <div className="mx-auto w-full max-w-3xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">Configurações</h1>
        <p className="text-muted-foreground">Informações da conta autenticada</p>
      </div>

      <Card>
        <CardHeader>
          <div className="flex items-center gap-4">
            <Avatar className="h-16 w-16">
              <AvatarImage src={user?.avatar} alt={user?.name ?? 'Usuário'} />
              <AvatarFallback>{user?.name.split(' ').map(part => part[0]).join('') || 'U'}</AvatarFallback>
            </Avatar>
            <div>
              <CardTitle>Perfil</CardTitle>
              <CardDescription>Dados retornados pelo provedor de identidade</CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="profile-name">Nome</Label>
            <Input id="profile-name" value={user?.name ?? ''} readOnly />
          </div>
          <div className="space-y-2">
            <Label htmlFor="profile-email">E-mail</Label>
            <Input id="profile-email" type="email" value={user?.email ?? ''} readOnly />
          </div>
          <div className="space-y-2">
            <Label htmlFor="profile-role">Perfil de acesso</Label>
            <Input id="profile-role" value={user?.role ?? ''} readOnly />
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
