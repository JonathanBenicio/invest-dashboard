import { useState } from "react"
import { useQueryClient } from "@tanstack/react-query"
import { AlertCircle, CheckCircle, FileSpreadsheet, FileText, Upload } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { usePortfolios } from "@/hooks/use-portfolios"
import { transactionService } from "@/api/services/transaction.service"
import { localDateInputToISOString } from "@/lib/utils"
import { ApiError } from "@/api/errors/api-error"
import { queryKeys } from "@/api/query-keys"
import { useToast } from "@/hooks/use-toast"
import type { RegistrarTransacaoRequest } from "@/api/dtos/transacao.dto"

type ImportedRow = {
  ticker: string
  type: "Buy" | "Sell"
  quantity: number
  unitPrice: number
  fees: number
  transactionDate: string
  modalidadeFiscal?: "Comum" | "DayTrade"
  idempotencyKey: string
  assetClass?: string
  subtype?: string
  issuer?: string
  indexer?: string
  interestRate?: number
  maturityDate?: string
  initialStatementValue?: number
  name?: string
  sector?: string
  rowNumber: number
  status: "pending" | "success" | "error"
  retryable?: boolean
  message?: string
}

const requiredColumns = ["ticker", "type", "quantity", "unitprice", "transactiondate"]

function parseDelimitedText(text: string, delimiter: string): string[][] {
  const rows: string[][] = []
  let row: string[] = []
  let value = ""
  let quoted = false

  for (let index = 0; index < text.length; index += 1) {
    const character = text[index]
    if (character === '"') {
      if (quoted && text[index + 1] === '"') {
        value += '"'
        index += 1
      } else {
        quoted = !quoted
      }
    } else if (!quoted && character === delimiter) {
      row.push(value.trim())
      value = ""
    } else if (!quoted && (character === "\n" || character === "\r")) {
      if (character === "\r" && text[index + 1] === "\n") index += 1
      row.push(value.trim())
      if (row.some(cell => cell.length > 0)) rows.push(row)
      row = []
      value = ""
    } else {
      value += character
    }
  }

  row.push(value.trim())
  if (row.some(cell => cell.length > 0)) rows.push(row)
  if (quoted) throw new Error("Há um campo de texto sem aspas de fechamento.")
  return rows
}

function parseNumber(value: string | undefined): number | undefined {
  if (!value?.trim()) return undefined
  const normalized = value.trim().replace(/[R$\s]/g, "")
  const decimalNormalized = normalized.includes(",")
    ? normalized.replace(/\./g, "").replace(",", ".")
    : normalized
  const number = Number(decimalNormalized)
  return Number.isFinite(number) ? number : undefined
}

function parseDate(value: string | undefined): string | undefined {
  if (!value) return undefined
  const dateParts = value.match(/^(\d{2})\/(\d{2})\/(\d{4})$/)
  const isoDate = /^\d{4}-\d{2}-\d{2}$/.test(value)
    ? value
    : dateParts ? `${dateParts[3]}-${dateParts[2]}-${dateParts[1]}` : undefined
  if (!isoDate) return undefined
  try {
    return localDateInputToISOString(isoDate)
  } catch {
    return undefined
  }
}

function normalizeTransactionType(value: string | undefined): RegistrarTransacaoRequest["tipo"] | undefined {
  const type = value?.trim().toLowerCase()
  if (type === "buy" || type === "compra") return "Buy"
  if (type === "sell" || type === "venda") return "Sell"
  return undefined
}

function normalizeTaxModality(value: string | undefined): "Comum" | "DayTrade" | undefined {
  const modality = value?.trim().toLowerCase().replace(/[\s_-]/g, "")
  if (modality === "comum") return "Comum"
  if (modality === "daytrade") return "DayTrade"
  return undefined
}

function parseCsv(text: string): ImportedRow[] {
  const content = text.replace(/^\uFEFF/, "")
  const headerLine = content.split(/\r?\n/, 1)[0] ?? ""
  const delimiter = (headerLine.match(/;/g) ?? []).length > (headerLine.match(/,/g) ?? []).length ? ";" : ","
  const [header, ...records] = parseDelimitedText(content, delimiter)
  if (!header || records.length === 0) throw new Error("O CSV não contém cabeçalho e operações para importar.")

  const columnIndex = new Map(header.map((name, index) => [name.trim().toLowerCase(), index]))
  const missingColumns = requiredColumns.filter(column => !columnIndex.has(column))
  if (missingColumns.length > 0) {
    throw new Error(`Inclua as colunas obrigatórias: ${missingColumns.join(", ")}.`)
  }

  return records.map((record, index) => {
    const value = (column: string) => record[columnIndex.get(column) ?? -1]
    const ticker = value("ticker")?.trim().toUpperCase() ?? ""
    const type = normalizeTransactionType(value("type"))
    const quantity = parseNumber(value("quantity"))
    const unitPrice = parseNumber(value("unitprice"))
    const rawFees = value("fees")
    const fees = rawFees?.trim() ? parseNumber(rawFees) : 0
    const transactionDate = parseDate(value("transactiondate"))
    const assetClass = value("assetclass")?.trim().toUpperCase()
    const rawTaxModality = value("modalidadefiscal") ?? value("taxmodality")
    const modalidadeFiscal = normalizeTaxModality(rawTaxModality)
    const subtype = value("subtype")?.trim().toUpperCase()
    const interestRate = parseNumber(value("interestrate"))
    const maturityDate = parseDate(value("maturitydate"))
    const rawStatementValue = value("initialstatementvalue")
    const initialStatementValue = rawStatementValue?.trim() ? parseNumber(rawStatementValue) : undefined
    const errors: string[] = []

    if (!/^[A-Z0-9.-]{1,20}$/.test(ticker)) errors.push("Ticker inválido.")
    if (!type) errors.push("Tipo deve ser compra/Buy ou venda/Sell.")
    if (!quantity || quantity <= 0) errors.push("Quantidade deve ser maior que zero.")
    if (!unitPrice || unitPrice <= 0) errors.push("Preço unitário deve ser maior que zero.")
    if (fees === undefined) errors.push("Taxas devem ser numéricas.")
    if (fees !== undefined && fees < 0) errors.push("Taxas não podem ser negativas.")
    if (!transactionDate) errors.push("Data deve usar AAAA-MM-DD ou DD/MM/AAAA.")
    const taxRelevantSale = type === "Sell" && !["RENDA_FIXA", "ETF", "BDR", "CRYPTO"].includes(assetClass ?? "")
    if (rawTaxModality?.trim() && !modalidadeFiscal) errors.push("Modalidade fiscal deve ser Comum ou DayTrade.")
    if (taxRelevantSale && !modalidadeFiscal) errors.push("Informe modalidadeFiscal (Comum ou DayTrade) para a venda tributável.")
    if (assetClass && !["ACAO", "STOCK", "FII", "ETF", "BDR", "CRYPTO", "RENDA_FIXA"].includes(assetClass)) {
      errors.push("Classe deve ser ACAO, FII, ETF, BDR, CRYPTO ou RENDA_FIXA.")
    }
    if (initialStatementValue === undefined && rawStatementValue?.trim()) errors.push("Valor do extrato deve ser numérico.")
    if (assetClass === "RENDA_FIXA" && type === "Buy") {
      if (!value("issuer")?.trim()) errors.push("Emissor é obrigatório para compra de renda fixa.")
      if (!subtype) errors.push("Subtipo é obrigatório para compra de renda fixa.")
      if (!value("indexer")?.trim()) errors.push("Indexador é obrigatório para compra de renda fixa.")
      if (interestRate === undefined || interestRate < 0) errors.push("Taxa de juros da renda fixa deve ser informada.")
      if (!maturityDate) errors.push("Vencimento de renda fixa deve usar AAAA-MM-DD ou DD/MM/AAAA.")
      if (initialStatementValue !== undefined && initialStatementValue < 0) errors.push("Valor do extrato não pode ser negativo.")
    }

    return {
      rowNumber: index + 2,
      ticker,
      type: type ?? "Buy",
      quantity: quantity ?? 0,
      unitPrice: unitPrice ?? 0,
      fees: fees ?? 0,
      transactionDate: transactionDate ?? "",
      modalidadeFiscal,
      idempotencyKey: crypto.randomUUID(),
      assetClass: assetClass || undefined,
      subtype,
      issuer: value("issuer")?.trim() || undefined,
      indexer: value("indexer")?.trim().toUpperCase() || undefined,
      interestRate,
      maturityDate,
      initialStatementValue,
      name: value("name")?.trim() || undefined,
      sector: value("sector")?.trim() || undefined,
      status: errors.length === 0 ? "pending" : "error",
      message: errors.join(" ") || undefined,
    }
  })
}

function downloadTemplate() {
  const template = "ticker;type;quantity;unitPrice;fees;transactionDate;assetClass;name;sector;issuer;subtype;indexer;interestRate;maturityDate;initialStatementValue;modalidadeFiscal\nPETR4;Buy;100;35,50;0;2026-09-29;ACAO;Petrobras;Petróleo e Gás;;;;;;\nRFPETR4;Buy;1000;1;0;2026-09-29;RENDA_FIXA;CDB;;Banco de teste;CDB;CDI;110;2028-01-15;1010\n"
  const url = URL.createObjectURL(new Blob([template], { type: "text/csv;charset=utf-8" }))
  const link = document.createElement("a")
  link.href = url
  link.download = "modelo-operacoes.csv"
  link.click()
  URL.revokeObjectURL(url)
}

export default function Import() {
  const [isDragging, setIsDragging] = useState(false)
  const [isReading, setIsReading] = useState(false)
  const [isImporting, setIsImporting] = useState(false)
  const [selectedPortfolioId, setSelectedPortfolioId] = useState("")
  const [importedData, setImportedData] = useState<ImportedRow[]>([])
  const [fileError, setFileError] = useState("")
  const queryClient = useQueryClient()
  const { data: portfoliosResponse, isLoading: isLoadingPortfolios, isError: isPortfolioError, refetch: refetchPortfolios } = usePortfolios({ pagina: 1, itensPorPagina: 100 })
  const portfolios = portfoliosResponse?.dados ?? []
  const { toast } = useToast()

  const handleFiles = async (files: File[]) => {
    const file = files[0]
    if (!file) return
    if (!file.name.toLowerCase().endsWith(".csv")) {
      setFileError("Nesta versão, importe arquivos CSV. Planilhas XLS/XLSX ainda não são processadas.")
      setImportedData([])
      return
    }

    setIsReading(true)
    setFileError("")
    try {
      const rows = parseCsv(await file.text())
      setImportedData(rows)
      toast({ title: "CSV processado", description: `${rows.length} linhas encontradas. Revise antes de confirmar.` })
    } catch (error) {
      setImportedData([])
      setFileError(error instanceof Error ? error.message : "Não foi possível ler o CSV.")
    } finally {
      setIsReading(false)
    }
  }

  const confirmImport = async () => {
    if (!selectedPortfolioId) {
      toast({ title: "Selecione a carteira", description: "Escolha a carteira que receberá as operações.", variant: "destructive" })
      return
    }
    const pendingRows = importedData.filter(row => row.status === "pending")
    if (pendingRows.length === 0) return

    setIsImporting(true)
    const updatedRows = [...importedData]
    for (const row of pendingRows) {
      const index = updatedRows.findIndex(item => item.rowNumber === row.rowNumber)
      try {
        await transactionService.create({
          carteiraId: selectedPortfolioId,
          ticker: row.ticker,
          tipo: row.type,
          quantidade: row.quantity,
          precoUnitario: row.unitPrice,
          taxas: row.fees,
          modalidadeFiscal: row.modalidadeFiscal,
          dataTransacao: row.transactionDate,
          chaveIdempotencia: row.idempotencyKey,
          classeAtivo: row.assetClass,
          subtipo: row.subtype,
          emissor: row.issuer,
          indexador: row.indexer,
          taxaJuros: row.interestRate,
          dataVencimento: row.maturityDate,
          valorInicialExtrato: row.initialStatementValue,
          nome: row.name,
          setor: row.sector,
        })
        updatedRows[index] = { ...row, status: "success", message: "Operação registrada." }
      } catch (error) {
        const retryable = !(error instanceof ApiError) || error.status === 0 || error.status === 408 || error.status === 429 || error.status >= 500
        updatedRows[index] = {
          ...row,
          status: "error",
          retryable,
          message: retryable
            ? "Falha de conexão/servidor. Tente novamente; a chave de idempotência será reutilizada."
            : error.message,
        }
      }
      setImportedData([...updatedRows])
    }
    setIsImporting(false)
    const succeeded = updatedRows.filter(row => row.status === "success").length
    const failed = updatedRows.filter(row => row.status === "error").length
    if (succeeded > 0) {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.investments.all }),
        queryClient.invalidateQueries({ queryKey: queryKeys.portfolios.all }),
      ])
    }
    const successMessage = `${succeeded} ${succeeded === 1 ? "operação registrada" : "operações registradas"}`
    const failureMessage = `${failed} ${failed === 1 ? "linha precisa" : "linhas precisam"} de correção`
    toast({
      title: failed === 0 ? "Importação concluída" : "Importação parcial",
      description: failed === 0 ? successMessage : `${successMessage}; ${failureMessage}.`,
      variant: failed === 0 ? undefined : "destructive",
    })
  }

  const validRows = importedData.filter(row => row.status === "pending").length

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">Importar operações</h1>
        <p className="text-muted-foreground">Leia um CSV, confira cada linha e envie as operações à carteira selecionada.</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2"><FileSpreadsheet className="h-5 w-5" />Arquivo CSV</CardTitle>
          <CardDescription>
            Colunas obrigatórias: ticker, type, quantity, unitPrice e transactionDate. Para novas compras de renda fixa, informe issuer, subtype, indexer, interestRate e maturityDate. Aceita delimitadores vírgula ou ponto e vírgula.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-2">
            <label htmlFor="import-portfolio" className="text-sm font-medium">Carteira de destino</label>
            <Select value={selectedPortfolioId} onValueChange={setSelectedPortfolioId} disabled={isLoadingPortfolios || portfolios.length === 0}>
              <SelectTrigger id="import-portfolio"><SelectValue placeholder={isLoadingPortfolios ? "Carregando carteiras..." : "Selecione a carteira"} /></SelectTrigger>
              <SelectContent>{portfolios.map(portfolio => <SelectItem key={portfolio.id} value={portfolio.id}>{portfolio.nome}</SelectItem>)}</SelectContent>
            </Select>
          </div>

          <div
            className={`rounded-lg border-2 border-dashed p-8 text-center transition-colors ${isDragging ? "border-primary bg-primary/5" : "border-border hover:border-primary/50"}`}
            onDragOver={event => { event.preventDefault(); setIsDragging(true) }}
            onDragLeave={() => setIsDragging(false)}
            onDrop={event => { event.preventDefault(); setIsDragging(false); void handleFiles(Array.from(event.dataTransfer.files)) }}
          >
            <Upload className="mx-auto mb-3 h-8 w-8 text-muted-foreground" />
            <p className="text-sm font-medium">{isReading ? "Lendo arquivo..." : "Arraste um CSV ou selecione no dispositivo"}</p>
            <input
              id="import-csv"
              type="file"
              accept=".csv,text/csv"
              className="mt-4 max-w-full text-sm"
              aria-label="Arquivo CSV"
              disabled={isReading || isImporting}
              onChange={event => { void handleFiles(Array.from(event.target.files ?? [])); event.target.value = "" }}
            />
          </div>

          {fileError && <p role="alert" className="text-sm text-destructive">{fileError}</p>}
          {isPortfolioError && <p role="alert" className="text-sm text-destructive">Não foi possível carregar as carteiras. <Button type="button" variant="link" className="h-auto p-0" onClick={() => void refetchPortfolios()}>Tentar novamente</Button></p>}
          {portfolios.length === 0 && !isLoadingPortfolios && !isPortfolioError && <p role="status" className="text-sm text-amber-700 dark:text-amber-300">Crie uma carteira antes de importar operações.</p>}

          <div className="flex flex-wrap items-center justify-center gap-2 text-sm text-muted-foreground">
            <FileText className="h-4 w-4" />
            <span>Precisa do formato esperado?</span>
            <Button variant="link" className="h-auto p-0" onClick={downloadTemplate}>Baixe o modelo CSV</Button>
          </div>
        </CardContent>
      </Card>

      {importedData.length > 0 && <Card>
        <CardHeader>
          <CardTitle>Prévia do CSV</CardTitle>
          <CardDescription>{validRows} linhas válidas aguardando envio; linhas inválidas são ignoradas.</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="overflow-x-auto rounded-lg border">
            <Table>
              <TableHeader><TableRow><TableHead>Status</TableHead><TableHead>Linha</TableHead><TableHead>Ticker</TableHead><TableHead>Tipo</TableHead><TableHead>Modalidade fiscal</TableHead><TableHead className="text-right">Quantidade</TableHead><TableHead className="text-right">Preço</TableHead><TableHead>Data</TableHead><TableHead>Resultado</TableHead></TableRow></TableHeader>
              <TableBody>{importedData.map(row => <TableRow key={row.rowNumber}>
                <TableCell>
                  {row.status === "success" ? <CheckCircle aria-label="Importado" className="h-4 w-4 text-success" />
                    : row.status === "error" ? <AlertCircle aria-label="Erro" className="h-4 w-4 text-destructive" />
                      : <Badge variant="outline">Pendente</Badge>}
                </TableCell>
                <TableCell>{row.rowNumber}</TableCell><TableCell className="font-medium">{row.ticker || "—"}</TableCell>
                <TableCell>{row.type}</TableCell><TableCell>{row.type === "Sell" ? row.modalidadeFiscal ?? "Obrigatória" : "—"}</TableCell><TableCell className="text-right">{row.quantity}</TableCell>
                <TableCell className="text-right">{row.unitPrice.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}</TableCell>
                <TableCell>{row.transactionDate ? new Date(row.transactionDate).toLocaleDateString("pt-BR") : "—"}</TableCell>
                <TableCell className={row.status === "error" ? "text-destructive" : "text-muted-foreground"}>
                  <div className="flex flex-col items-start gap-1">
                    <span>{row.message ?? ""}</span>
                    {row.status === "error" && row.retryable && <Button
                      type="button"
                      variant="link"
                      className="h-auto p-0 text-sm"
                      disabled={isImporting}
                      onClick={() => setImportedData(rows => rows.map(item => item.rowNumber === row.rowNumber
                        ? { ...item, status: "pending", retryable: false, message: undefined }
                        : item))}
                    >Tentar novamente</Button>}
                  </div>
                </TableCell>
              </TableRow>)}</TableBody>
            </Table>
          </div>
          <div className="mt-4 flex justify-end gap-3">
            <Button variant="outline" onClick={() => { setImportedData([]); setFileError("") }} disabled={isImporting}>Limpar</Button>
            <Button onClick={() => void confirmImport()} disabled={isImporting || validRows === 0 || !selectedPortfolioId}>
              {isImporting ? "Enviando operações..." : `Importar ${validRows} operações`}
            </Button>
          </div>
        </CardContent>
      </Card>}

      <Card>
        <CardHeader><CardTitle>Conexões com corretoras</CardTitle><CardDescription>Conexões diretas ainda não estão disponíveis.</CardDescription></CardHeader>
        <CardContent className="text-sm text-muted-foreground">Use o CSV para registrar operações. Conectores serão adicionados quando houver suporte de API com autenticação segura.</CardContent>
      </Card>
    </div>
  )
}
