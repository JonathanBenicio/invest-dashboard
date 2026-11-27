import { useRef, useState } from "react";
import {
  ArrowDownLeft,
  ArrowLeft,
  ArrowLeftRight,
  ArrowRight,
  ArrowUpRight,
  BarChart3,
  Bell,
  BriefcaseBusiness,
  CalendarDays,
  Check,
  CheckCheck,
  ChevronDown,
  CircleHelp,
  FileImage,
  FileText,
  LayoutDashboard,
  Landmark,
  LogOut,
  Menu,
  MessageCircle,
  MessageSquareText,
  Moon,
  Paperclip,
  PanelLeftClose,
  Plus,
  Send,
  Settings,
  Sparkles,
  TableProperties,
  TrendingUp,
  UsersRound,
  WalletCards,
  X,
} from "lucide-react";

const initialMessages = [
  {
    id: 1,
    role: "user",
    text: "Como está o Tesouro na carteira?",
    time: "10:24",
  },
  {
    id: 2,
    role: "assistant",
    text: "O Tesouro está presente na carteira em Tesouro IPCA+ 2035, com saldo de R$ 12.540,28, representando 7,89% do patrimônio, na data de 27/11/2026.",
    time: "10:24",
  },
];

const positions = [
  { ticker: "PETR4", name: "Petrobras PN", type: "Renda Variável", quantity: "2.000", price: "37,80", value: "75.600,00", allocation: "47,55%", return: "+19,23%", positive: true },
  { ticker: "VALE3", name: "Vale ON", type: "Renda Variável", quantity: "1.000", price: "59,20", value: "59.200,00", allocation: "37,23%", return: "−8,38%", positive: false },
  { ticker: "TESOURO IPCA+ 2035", name: "Tesouro IPCA+ 2035", type: "Renda Fixa", quantity: "10", price: "1.254,03", value: "12.540,28", allocation: "7,89%", return: "+5,92%", positive: true },
  { ticker: "CDB 100% CDI", name: "CDB 100% CDI", type: "Renda Fixa", quantity: "1", price: "11.669,72", value: "11.669,72", allocation: "7,34%", return: "+6,85%", positive: true },
];

const navigation = [
  { label: "Dashboard", icon: LayoutDashboard },
  { label: "Carteiras", icon: WalletCards, active: true },
  { label: "Grupos e usuários", icon: UsersRound },
  { label: "Renda Fixa", icon: Landmark },
  { label: "Renda Variável", icon: TrendingUp },
  { label: "Simulador", icon: TableProperties },
  { label: "Análise", icon: BarChart3 },
  { label: "Importar operações", icon: ArrowDownLeft },
  { label: "Configurações", icon: Settings },
];

function Brand() {
  return (
    <div className="brand-lockup">
      <span className="brand-mark" aria-hidden="true"><WalletCards size={19} /></span>
      <span><strong>InvestPro</strong><small>Gestão de Carteiras</small></span>
    </div>
  );
}

function Sidebar({ active = "Carteiras", onNavigate, mobileOpen = false, onClose }) {
  return (
    <>
    {mobileOpen && <button aria-label="Fechar menu" className="mobile-scrim" onClick={onClose} type="button" />}
    <aside className={`sidebar ${mobileOpen ? "mobile-open" : ""}`}>
      <div className="sidebar-brand"><Brand /></div>
      <nav aria-label="Menu principal" className="sidebar-nav">
        <span className="nav-heading">Menu Principal</span>
        {navigation.map(({ label, icon: Icon }) => (
          <button
            aria-current={label === active ? "page" : undefined}
            className={`nav-item ${label === active ? "is-active" : ""}`}
            key={label}
            onClick={() => { onNavigate?.(label); onClose?.(); }}
            type="button"
          >
            <Icon size={17} strokeWidth={1.8} />
            <span>{label}</span>
          </button>
        ))}
        <span className="nav-heading nav-heading-admin">Administração</span>
        <button className="nav-item" onClick={() => { onNavigate?.("Taxas e Indicadores"); onClose?.(); }} type="button">
          <span className="percent-icon">%</span><span>Taxas e Indicadores</span>
        </button>
      </nav>
      <div className="sidebar-user">
        <span className="avatar">AD</span>
        <span className="sidebar-user-copy"><strong>Usuário demonstração</strong><small>Conta de exemplo</small></span>
        <button className="icon-button logout-button" aria-label="Sair da demonstração" type="button"><LogOut size={16} /></button>
      </div>
    </aside>
    </>
  );
}

function ProviderBadge() {
  return <div aria-label="Provedor ativo: Ollama local" className="provider-badge"><span className="provider-orb"><span /></span><span>Ollama · Local</span><span className="provider-status">Ativo</span></div>;
}

function PortfolioTable() {
  return (
    <section aria-labelledby="positions-heading" className="panel positions-panel">
      <div className="section-heading">
        <div><h2 id="positions-heading">Posições atuais</h2><p>Valores ilustrativos para esta demonstração</p></div>
        <button className="secondary-button export-button" type="button"><ArrowDownLeft size={15} /> Exportar <ChevronDown size={14} /></button>
      </div>
      <p className="table-scroll-hint"><ArrowLeftRight size={13} /> Role a tabela horizontalmente para ver todas as colunas.</p>
      <div aria-label="Posições de demonstração" className="table-scroll" role="region" tabIndex={0}>
        <table className="positions-table">
          <thead><tr><th>Ativo</th><th>Tipo</th><th>Quantidade</th><th>Preço (R$)</th><th>Valor (R$)</th><th>Alocação</th><th>Rentabilidade</th></tr></thead>
          <tbody>{positions.map((position) => (
            <tr key={position.ticker}>
              <td><span className="ticker-pill">{position.ticker}</span><small>{position.name}</small></td>
              <td>{position.type}</td><td>{position.quantity}</td><td>{position.price}</td><td>{position.value}</td><td>{position.allocation}</td>
              <td className={position.positive ? "positive-value" : "negative-value"}>{position.return}</td>
            </tr>
          ))}</tbody>
          <tfoot><tr><th colSpan="4">Total da carteira</th><th>R$ 159.010,00</th><th>100,00%</th><th className="positive-value">+6,47%</th></tr></tfoot>
        </table>
      </div>
      <p className="table-footnote"><CircleHelp size={13} /> Posições e valores de demonstração, sem vínculo a uma conta real.</p>
    </section>
  );
}

function SummaryCards() {
  return (
    <div className="summary-grid" aria-label="Resumo da carteira de demonstração">
      <section className="summary-card"><span>Patrimônio total</span><strong>R$ 159.010,00</strong><small>Valor ilustrativo da carteira</small></section>
      <section className="summary-card"><span>Rentabilidade</span><strong className="positive-value">R$ 9.660,00</strong><small className="positive-value"><TrendingUp size={13} /> +6,47%</small></section>
      <section className="summary-card"><span>Maior alta <ArrowUpRight size={14} className="positive-value" /></span><strong>PETR4</strong><small className="positive-value">+19,23%</small></section>
      <section className="summary-card"><span>Maior baixa <ArrowDownLeft size={14} className="negative-value" /></span><strong>VALE3</strong><small className="negative-value">−8,38%</small></section>
    </div>
  );
}

function StatPanels() {
  return (
    <div className="stat-grid" aria-label="Resumo visual demonstrativo">
      <section className="panel stat-panel"><div className="section-heading"><div><h2>Histórico da carteira</h2><p>Evolução do patrimônio ilustrativo</p></div></div><div className="history-sample"><span>R$ 165 mil</span><span className="history-line" aria-hidden="true" /><div><span>Mai/26</span><span>Jul/26</span><span>Set/26</span><span>Nov/26</span></div></div></section>
      <section className="panel stat-panel"><div className="section-heading"><div><h2>Alocação por tipo</h2><p>Distribuição ilustrativa</p></div></div><div className="allocation-list"><div><span className="allocation-dot fixed-dot" /><span>Renda Fixa</span><strong>R$ 24.209,99</strong><small>15,21%</small></div><div><span className="allocation-dot variable-dot" /><span>Renda Variável</span><strong>R$ 134.800,01</strong><small>84,79%</small></div></div></section>
    </div>
  );
}

function PreviewCard({ fileName, status, onReview }) {
  return (
    <section aria-labelledby="preview-title" className={`preview-card ${status === "reviewed" ? "preview-reviewed" : ""}`}>
      <span className="preview-icon"><FileImage size={19} /></span>
      <div className="preview-copy">
        <h3 id="preview-title">Prévia de análise do Tesouro</h3>
        <p>{fileName || "Resumo da posição, rentabilidade e próximos passos."}</p>
        {status === "reviewed" && <span className="reviewed-state"><Check size={13} /> Revisão demonstrativa concluída</span>}
      </div>
      <button className="review-link-button" data-review-trigger="true" onClick={onReview} type="button">{status === "reviewed" ? "Abrir prévia" : "Revisar prévia do Tesouro"}<ArrowRight size={14} /></button>
    </section>
  );
}

function MessageBubble({ message }) {
  const isUser = message.role === "user";
  return (
    <article className={`message-row ${isUser ? "message-user" : "message-assistant"}`}>
      {!isUser && <span className="assistant-avatar" aria-hidden="true"><Sparkles size={17} /></span>}
      <div className="message-content">
        <div className="message-bubble"><p>{message.text}</p>{message.balance && <div className="inline-balance"><span><small>Saldo observado</small><strong>R$ 12.540,28</strong></span><i /><span><small>Data de referência</small><strong><CalendarDays size={14} /> 27/11/2026</strong></span></div>}</div>
        <span className="message-time">{message.time}{isUser && <CheckCheck size={13} aria-label="Enviada" />}</span>
      </div>
    </article>
  );
}

function AttachmentCard({ fileName, onReview }) {
  return <PreviewCard fileName={fileName} onReview={onReview} status="pending" />;
}

function QuickAssistant({ messages, isSending, onSend, onCancel, onOpenFull, onFile, onReview, uploadedFile, reviewed }) {
  const [draft, setDraft] = useState("");
  const endRef = useRef(null);

  const submit = (event) => {
    event.preventDefault();
    const value = draft.trim();
    if (!value || isSending) return;
    onSend(value);
    setDraft("");
    window.setTimeout(() => {
      endRef.current?.scrollIntoView({ block: "nearest", behavior: "smooth" });
    }, 450);
  };

  return (
    <aside aria-label="Consulta rápida ao assistente" className="quick-assistant">
      <header className="quick-header">
        <span className="assistant-brand-icon" aria-hidden="true"><Sparkles size={19} /></span>
        <div className="quick-title"><h2>Consulta rápida</h2><p>Tire dúvidas e consulte esta carteira.</p></div>
        <ProviderBadge />
      </header>
      <div aria-live="polite" aria-relevant="additions text" className="quick-thread" role="log">
        {messages.map((message) => <MessageBubble key={message.id} message={message} />)}
        {uploadedFile && <AttachmentCard fileName={uploadedFile} onReview={onReview} />}
        {reviewed && <span className="thread-review-state"><Check size={13} /> Prévia revisada na demonstração</span>}
        {isSending && <div className="typing-indicator" aria-label="O assistente está respondendo"><span /><span /><span /><button onClick={onCancel} type="button">Interromper</button></div>}
        <div ref={endRef} />
      </div>
      <div className="quick-footer">
        <div className="continue-hint"><MessageCircle size={16} /><span>A conversa pode continuar no assistente completo, com mais contexto e histórico.</span></div>
        <button className="open-full-link" onClick={onOpenFull} type="button">Abrir assistente completo <ArrowUpRight size={15} /></button>
        <form className="composer" onSubmit={submit}>
          <label className="visually-hidden" htmlFor="quick-message">Sua pergunta</label>
          <textarea id="quick-message" onChange={(event) => setDraft(event.target.value)} onKeyDown={(event) => { if (event.key === "Enter" && !event.shiftKey) { event.preventDefault(); submit(event); } }} placeholder="Digite sua pergunta ou solicitação..." rows="2" value={draft} />
          <button className="send-button" aria-label={isSending ? "Interromper resposta" : "Enviar pergunta"} onClick={isSending ? onCancel : undefined} type={isSending ? "button" : "submit"}>{isSending ? <PanelLeftClose size={18} /> : <Send size={18} />}</button>
        </form>
        <div className="composer-meta"><label className="file-button"><Paperclip size={14} /> Anexar print do Tesouro<input accept="image/png,image/jpeg,image/webp" onChange={onFile} type="file" /></label><span>PNG, JPEG ou WebP · até 10 MB</span></div>
      </div>
    </aside>
  );
}

function FullAssistant({ messages, isSending, onSend, onCancel, onBack, onReview, reviewed, uploadedFile }) {
  const [draft, setDraft] = useState("");
  const submit = (event) => {
    event.preventDefault();
    if (!draft.trim() || isSending) return;
    onSend(draft.trim());
    setDraft("");
  };
  return (
    <section className="full-assistant-page">
      <header className="full-chat-header"><button className="back-button" onClick={onBack} type="button"><ArrowLeft size={16} /> Voltar à carteira</button><div><span className="assistant-brand-icon" aria-hidden="true"><Sparkles size={18} /></span><span><strong>Assistente de investimentos</strong><small>Carteira de demonstração</small></span></div><ProviderBadge /></header>
      <div className="full-chat-body">
        <div aria-live="polite" className="full-thread" role="log">{messages.map((message) => <MessageBubble key={message.id} message={message} />)}{uploadedFile && <AttachmentCard fileName={uploadedFile} onReview={onReview} />}{reviewed && <span className="thread-review-state"><Check size={13} /> Prévia revisada nesta demonstração</span>}{isSending && <div className="typing-indicator"><span /><span /><span /><button onClick={onCancel} type="button">Interromper</button></div>}</div>
        <form className="full-composer" onSubmit={submit}><label className="visually-hidden" htmlFor="full-message">Sua mensagem</label><textarea id="full-message" onChange={(event) => setDraft(event.target.value)} onKeyDown={(event) => { if (event.key === "Enter" && !event.shiftKey) { event.preventDefault(); submit(event); } }} placeholder="Pergunte sobre a carteira, posições ou resultados..." rows="2" value={draft} /><button className="send-button" aria-label={isSending ? "Interromper resposta" : "Enviar mensagem"} onClick={isSending ? onCancel : undefined} type={isSending ? "button" : "submit"}>{isSending ? <PanelLeftClose size={18} /> : <Send size={18} />}</button></form>
        <p className="demo-note">Protótipo interativo com dados sintéticos. Nenhuma consulta externa é realizada.</p>
      </div>
    </section>
  );
}

function ReviewSurface({ fileName, onBack, onMarkReviewed, reviewed }) {
  return (
    <main className="review-stage">
      <article aria-labelledby="review-heading" className="review-panel">
        <div className="review-topline"><button className="back-button" onClick={onBack} type="button"><ArrowLeft size={16} /> Voltar à conversa</button><span className="demo-tag">Demonstração</span></div>
        <div className="review-heading"><span className="assistant-brand-icon"><FileText size={18} /></span><div><h1 id="review-heading">Revisar prévia do Tesouro</h1><p>Confira a leitura antes de seguir para a importação.</p></div></div>
        <div className="file-summary"><FileImage size={18} /><span><strong>{fileName || "tesouro-prefixado-2032.png"}</strong><small>Arquivo local · usado somente nesta demonstração</small></span></div>
        <div className="review-fields">
          <div className="review-field"><span>Título identificado</span><strong>Tesouro IPCA+ 2040</strong><small>Exemplo sintético. Compatibilidade precisa ser revisada.</small></div>
          <div className="review-field"><span>Saldo exibido</span><strong>R$ 83.472,18</strong><small>Exemplo sintético. Natureza não especificada no print.</small></div>
          <div className="review-field"><span>Data do saldo</span><strong>18/11/2026</strong><small>Data sintética de referência da demonstração.</small></div>
          <div className="review-field"><span>Período de rentabilidade</span><strong>01/03/2026 a 17/11/2026</strong><small>Período sintético, independente da data do saldo.</small></div>
          <div className="review-field"><span>Rentabilidade exibida</span><strong>R$ 1.208,44 · +1,48%</strong><small>Valor sintético de demonstração. Não recalcula o saldo.</small></div>
        </div>
        <div className="review-alert"><CircleHelp size={18} /><p>O print mostra “Saldo” sem dizer se o valor é bruto ou líquido. Para gravar uma avaliação, o Invest Dashboard pedirá que você confirme essa informação e a posição correspondente.</p></div>
        <div className={`review-confirmation ${reviewed ? "confirmation-done" : ""}`} aria-live="polite">{reviewed ? <><Check size={17} /><span>Esta prévia foi marcada como revisada neste protótipo. Nenhuma posição foi alterada.</span></> : <><span>O próximo passo abre a revisão na importação do dashboard.</span><button className="primary-button" onClick={onMarkReviewed} type="button">Marcar prévia como revisada</button></>}</div>
      </article>
    </main>
  );
}

export function App() {
  const [view, setView] = useState("portfolio");
  const [reviewOrigin, setReviewOrigin] = useState("portfolio");
  const [messages, setMessages] = useState(initialMessages);
  const [isSending, setIsSending] = useState(false);
  const [uploadedFile, setUploadedFile] = useState("print-tesouro-demo.png");
  const [reviewed, setReviewed] = useState(false);
  const [activeNav, setActiveNav] = useState("Carteiras");
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const timeouts = useRef([]);

  const sendMessage = (text) => {
    setMessages((current) => [...current, { id: Date.now(), role: "user", text, time: "Agora" }]);
    setIsSending(true);
    const delay = window.setTimeout(() => {
      const response = /rentabilidade|rendimento|retorno/i.test(text)
        ? "Posso consultar o resultado registrado e mostrar a data de referência. O valor líquido depende das informações disponíveis para esta posição."
        : /compar|carteir|tesouro|saldo/i.test(text)
          ? "Na carteira de demonstração, o Tesouro IPCA+ 2035 tem saldo observado de R$ 12.540,28 na referência de 27/11/2026. Este valor é sintético e serve apenas para demonstrar a conversa."
          : "Entendi. Posso ajudar a consultar as posições demonstrativas ou preparar um print do Tesouro para revisão na importação.";
      setMessages((current) => [...current, { id: Date.now() + 1, role: "assistant", text: response, time: "Agora", balance: /compar|carteir|tesouro|saldo/i.test(text) }]);
      setIsSending(false);
    }, 900);
    timeouts.current.push(delay);
  };

  const cancelReply = () => {
    timeouts.current.forEach(window.clearTimeout);
    timeouts.current = [];
    setIsSending(false);
    setMessages((current) => [...current, { id: Date.now() + 2, role: "assistant", text: "Resposta interrompida. Você pode continuar quando quiser.", time: "Agora" }]);
  };

  const selectFile = (event) => {
    const file = event.target.files?.[0];
    if (!file) return;
    const allowedTypes = ["image/png", "image/jpeg", "image/webp"];
    if (!allowedTypes.includes(file.type) || file.size > 10 * 1024 * 1024) {
      setMessages((current) => [...current, { id: Date.now(), role: "assistant", text: "Escolha uma imagem PNG, JPEG ou WebP de até 10 MB.", time: "Agora" }]);
      event.target.value = "";
      return;
    }
    setUploadedFile(file.name);
    setReviewed(false);
    setMessages((current) => [...current, { id: Date.now(), role: "assistant", text: "Arquivo recebido no protótipo. Confira a prévia sintética e as datas antes de avançar.", time: "Agora" }]);
  };

  const openReview = (origin) => {
    setReviewOrigin(origin);
    setView("review");
  };

  const returnFromReview = () => {
    setView(reviewOrigin);
    window.setTimeout(() => document.querySelector("[data-review-trigger='true']")?.focus(), 50);
  };

  return (
    <div className="prototype-shell">
      <Sidebar active={activeNav} onNavigate={(label) => label === "Carteiras" ? (setView("portfolio"), setActiveNav(label)) : setActiveNav(label)} mobileOpen={mobileMenuOpen} onClose={() => setMobileMenuOpen(false)} />
      <div className="app-main">
        <header className="app-topbar"><button aria-label="Abrir menu" aria-expanded={mobileMenuOpen} className="icon-button menu-button" onClick={() => setMobileMenuOpen(true)} type="button"><Menu size={19} /></button><div className="topbar-right"><span className="demo-tag">Dados de demonstração</span><button aria-label="Tema escuro" className="icon-button" type="button"><Moon size={17} /></button></div></header>
        {view === "review" ? <ReviewSurface fileName={uploadedFile} onBack={returnFromReview} onMarkReviewed={() => setReviewed(true)} reviewed={reviewed} /> : view === "full" ? <FullAssistant messages={messages} isSending={isSending} onSend={sendMessage} onCancel={cancelReply} onBack={() => setView("portfolio")} onReview={() => openReview("full")} reviewed={reviewed} uploadedFile={uploadedFile} /> : (
          <main className="portfolio-layout">
            <section className="portfolio-content">
              <div className="breadcrumb"><span>Carteiras</span><ArrowRight size={13} /><strong>Carteira demonstração</strong></div>
              <header className="portfolio-heading"><div className="portfolio-title"><span className="portfolio-mark"><BriefcaseBusiness size={19} /></span><div><h1>Carteira demonstração</h1><p>Detalhes, posições e desempenho ilustrativo.</p></div></div><button className="open-full-button" onClick={() => { setView("full"); setActiveNav("Carteiras"); }} type="button"><MessageCircle size={17} /> Abrir conversa completa <ArrowUpRight size={15} /></button></header>
              <SummaryCards />
              <nav aria-label="Seções da carteira" className="portfolio-tabs"><button className="tab-active" type="button">Posições</button><button type="button">Evolução</button><button type="button">Composição</button><button type="button">Movimentações</button><button type="button">Indicadores</button></nav>
              <PortfolioTable />
              <StatPanels />
            </section>
            <QuickAssistant messages={messages} isSending={isSending} onSend={sendMessage} onCancel={cancelReply} onOpenFull={() => { setView("full"); setActiveNav("Carteiras"); }} onFile={selectFile} onReview={() => openReview("portfolio")} uploadedFile={uploadedFile} reviewed={reviewed} />
          </main>
        )}
      </div>
      <span className="sr-status" aria-live="polite">{isSending ? "O assistente está respondendo" : ""}</span>
    </div>
  );
}
