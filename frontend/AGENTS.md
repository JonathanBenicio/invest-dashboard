# Frontend — Invest Dashboard

- Aplicação React 19 + Vite, TypeScript, Tailwind 3 e componentes shadcn-ui. Use `bun` conforme `package.json`.
- Rotas são definidas programaticamente em `src/router.tsx`; não introduza convenções de App Router ou Server Components do Next.js. Preserve a base de GitHub Pages e Capacitor configurada em `vite.config.ts`.
- Reutilize componentes em `src/components/ui`, variáveis do tema em `src/index.css` e padrões já existentes; preserve responsividade, semântica, acessibilidade e suporte ao tema escuro. Cores de estado: emerald para sucesso, amber para alerta, rose para erro e azul para informação.
- Zustand representa estado do cliente; TanStack Query e o cliente em `src/api/` apoiam dados remotos. Confirme o padrão da funcionalidade antes de adicionar outra camada ou biblioteca.
- Para transformações compartilhadas de payloads em dados de UI, centralize a lógica em um mapeador próximo do domínio e trate nulos conforme o contrato. Evite duplicar o mapeamento em componentes, hooks e stores; não esconda dados ausentes com valores financeiros inventados.
- MSW só é ativado quando `VITE_USE_MSW=true` em `src/main.tsx`. Build normal: `bun run build`. Se um cenário explicitamente usar o worker e ele precisar ser gerado, execute `bunx msw init public --save`; isso pode alterar arquivos do projeto. O workflow atual de Pages também executa esse comando mesmo com MSW desativado.
- Valide com os comandos pertinentes de `bun run lint`, `bun run build` e `bun run test`. Testes Playwright podem precisar de servidor, browsers ou serviços adicionais; relate limites reais.
