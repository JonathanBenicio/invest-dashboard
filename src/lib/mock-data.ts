// Mock data for the investment platform

export interface User {
  id: string;
  name: string;
  email: string;
  avatar?: string;
}

export interface FixedIncomeAsset {
  id: string;
  name: string;
  type: 'CDB' | 'LCI' | 'LCA' | 'Tesouro Direto' | 'Debênture' | 'CRI' | 'CRA';
  institution: string;
  investedValue: number;
  currentValue: number;
  rate: string;
  rateType: 'CDI' | 'IPCA' | 'Prefixado';
  purchaseDate: string;
  maturityDate: string;
  liquidity: 'Diária' | 'No vencimento';
}

export interface VariableIncomeAsset {
  id: string;
  ticker: string;
  name: string;
  type: 'Ação' | 'FII' | 'ETF' | 'BDR';
  sector: string;
  quantity: number;
  averagePrice: number;
  currentPrice: number;
  lastUpdate: string;
}

export interface Dividend {
  id: string;
  ticker: string;
  type: 'Dividendo' | 'JCP' | 'Rendimento';
  value: number;
  paymentDate: string;
  exDate: string;
}

export interface Transaction {
  id: string;
  date: string;
  type: 'Compra' | 'Venda' | 'Aporte' | 'Resgate';
  asset: string;
  quantity?: number;
  price?: number;
  total: number;
}

export const currentUser: User = {
  id: '1',
  name: 'João Silva',
  email: 'joao@email.com',
};

export const fixedIncomeAssets: FixedIncomeAsset[] = [
  {
    id: '1',
    name: 'CDB Banco Inter',
    type: 'CDB',
    institution: 'Banco Inter',
    investedValue: 50000,
    currentValue: 53500,
    rate: '120%',
    rateType: 'CDI',
    purchaseDate: '2024-01-15',
    maturityDate: '2026-01-15',
    liquidity: 'Diária',
  },
  {
    id: '2',
    name: 'Tesouro IPCA+ 2029',
    type: 'Tesouro Direto',
    institution: 'Tesouro Nacional',
    investedValue: 30000,
    currentValue: 32800,
    rate: 'IPCA + 6,5%',
    rateType: 'IPCA',
    purchaseDate: '2023-06-10',
    maturityDate: '2029-05-15',
    liquidity: 'Diária',
  },
  {
    id: '3',
    name: 'LCI Caixa',
    type: 'LCI',
    institution: 'Caixa Econômica',
    investedValue: 25000,
    currentValue: 26200,
    rate: '95%',
    rateType: 'CDI',
    purchaseDate: '2024-03-20',
    maturityDate: '2025-03-20',
    liquidity: 'No vencimento',
  },
  {
    id: '4',
    name: 'LCA BTG',
    type: 'LCA',
    institution: 'BTG Pactual',
    investedValue: 40000,
    currentValue: 41800,
    rate: '98%',
    rateType: 'CDI',
    purchaseDate: '2024-02-01',
    maturityDate: '2025-08-01',
    liquidity: 'No vencimento',
  },
  {
    id: '5',
    name: 'Debênture Energisa',
    type: 'Debênture',
    institution: 'XP Investimentos',
    investedValue: 20000,
    currentValue: 21500,
    rate: 'IPCA + 7,2%',
    rateType: 'IPCA',
    purchaseDate: '2023-09-15',
    maturityDate: '2028-09-15',
    liquidity: 'No vencimento',
  },
];

export const variableIncomeAssets: VariableIncomeAsset[] = [
  {
    id: '1',
    ticker: 'PETR4',
    name: 'Petrobras PN',
    type: 'Ação',
    sector: 'Petróleo e Gás',
    quantity: 200,
    averagePrice: 32.50,
    currentPrice: 38.75,
    lastUpdate: '2024-12-12',
  },
  {
    id: '2',
    ticker: 'VALE3',
    name: 'Vale ON',
    type: 'Ação',
    sector: 'Mineração',
    quantity: 150,
    averagePrice: 68.00,
    currentPrice: 62.30,
    lastUpdate: '2024-12-12',
  },
  {
    id: '3',
    ticker: 'ITUB4',
    name: 'Itaú Unibanco PN',
    type: 'Ação',
    sector: 'Bancos',
    quantity: 300,
    averagePrice: 28.00,
    currentPrice: 32.15,
    lastUpdate: '2024-12-12',
  },
  {
    id: '4',
    ticker: 'HGLG11',
    name: 'CSHG Logística',
    type: 'FII',
    sector: 'Logística',
    quantity: 50,
    averagePrice: 165.00,
    currentPrice: 158.50,
    lastUpdate: '2024-12-12',
  },
  {
    id: '5',
    ticker: 'KNRI11',
    name: 'Kinea Renda Imobiliária',
    type: 'FII',
    sector: 'Híbrido',
    quantity: 80,
    averagePrice: 142.00,
    currentPrice: 138.25,
    lastUpdate: '2024-12-12',
  },
  {
    id: '6',
    ticker: 'BOVA11',
    name: 'iShares Ibovespa',
    type: 'ETF',
    sector: 'Índice',
    quantity: 100,
    averagePrice: 110.00,
    currentPrice: 118.45,
    lastUpdate: '2024-12-12',
  },
  {
    id: '7',
    ticker: 'AAPL34',
    name: 'Apple BDR',
    type: 'BDR',
    sector: 'Tecnologia',
    quantity: 40,
    averagePrice: 52.00,
    currentPrice: 58.90,
    lastUpdate: '2024-12-12',
  },
];

export const dividends: Dividend[] = [
  { id: '1', ticker: 'PETR4', type: 'Dividendo', value: 1.25, paymentDate: '2024-12-15', exDate: '2024-11-28' },
  { id: '2', ticker: 'ITUB4', type: 'JCP', value: 0.45, paymentDate: '2024-12-20', exDate: '2024-12-01' },
  { id: '3', ticker: 'HGLG11', type: 'Rendimento', value: 1.10, paymentDate: '2024-12-10', exDate: '2024-11-30' },
  { id: '4', ticker: 'KNRI11', type: 'Rendimento', value: 0.92, paymentDate: '2024-12-10', exDate: '2024-11-30' },
  { id: '5', ticker: 'VALE3', type: 'Dividendo', value: 2.10, paymentDate: '2024-11-25', exDate: '2024-11-10' },
  { id: '6', ticker: 'PETR4', type: 'Dividendo', value: 1.15, paymentDate: '2024-11-15', exDate: '2024-10-28' },
];

export const transactions: Transaction[] = [
  { id: '1', date: '2024-12-10', type: 'Compra', asset: 'PETR4', quantity: 50, price: 37.80, total: 1890 },
  { id: '2', date: '2024-12-08', type: 'Aporte', asset: 'CDB Banco Inter', total: 10000 },
  { id: '3', date: '2024-12-05', type: 'Venda', asset: 'MGLU3', quantity: 100, price: 12.50, total: 1250 },
  { id: '4', date: '2024-12-01', type: 'Compra', asset: 'HGLG11', quantity: 10, price: 160.00, total: 1600 },
  { id: '5', date: '2024-11-28', type: 'Aporte', asset: 'Tesouro IPCA+ 2029', total: 5000 },
];

export const portfolioHistory = [
  { month: 'Jul', value: 280000, benchmark: 275000 },
  { month: 'Ago', value: 295000, benchmark: 282000 },
  { month: 'Set', value: 305000, benchmark: 290000 },
  { month: 'Out', value: 315000, benchmark: 298000 },
  { month: 'Nov', value: 328000, benchmark: 305000 },
  { month: 'Dez', value: 342500, benchmark: 312000 },
];

export const allocationData = [
  { name: 'Renda Fixa', value: 175800, color: 'hsl(220, 70%, 50%)' },
  { name: 'Ações', value: 89500, color: 'hsl(145, 65%, 42%)' },
  { name: 'FIIs', value: 42985, color: 'hsl(38, 92%, 50%)' },
  { name: 'ETFs', value: 11845, color: 'hsl(280, 65%, 55%)' },
  { name: 'BDRs', value: 2356, color: 'hsl(200, 85%, 50%)' },
];

export const sectorAllocation = [
  { name: 'Bancos', value: 25 },
  { name: 'Petróleo e Gás', value: 20 },
  { name: 'Mineração', value: 15 },
  { name: 'Logística', value: 12 },
  { name: 'Tecnologia', value: 10 },
  { name: 'Outros', value: 18 },
];

export const monthlyDividends = [
  { month: 'Jul', received: 850, projected: 900 },
  { month: 'Ago', received: 920, projected: 900 },
  { month: 'Set', received: 780, projected: 850 },
  { month: 'Out', received: 1050, projected: 950 },
  { month: 'Nov', received: 890, projected: 900 },
  { month: 'Dez', received: 0, projected: 1100 },
];

// Helper functions
export function calculateTotalPortfolio() {
  const fixedTotal = fixedIncomeAssets.reduce((acc, asset) => acc + asset.currentValue, 0);
  const variableTotal = variableIncomeAssets.reduce((acc, asset) => acc + (asset.currentPrice * asset.quantity), 0);
  return fixedTotal + variableTotal;
}

export function calculateTotalInvested() {
  const fixedTotal = fixedIncomeAssets.reduce((acc, asset) => acc + asset.investedValue, 0);
  const variableTotal = variableIncomeAssets.reduce((acc, asset) => acc + (asset.averagePrice * asset.quantity), 0);
  return fixedTotal + variableTotal;
}

export function calculateTotalProfit() {
  return calculateTotalPortfolio() - calculateTotalInvested();
}

export function calculateProfitPercentage() {
  return ((calculateTotalProfit() / calculateTotalInvested()) * 100).toFixed(2);
}

export function formatCurrency(value: number): string {
  return new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency: 'BRL',
  }).format(value);
}

export function formatPercentage(value: number): string {
  return `${value >= 0 ? '+' : ''}${value.toFixed(2)}%`;
}

export function formatDate(dateString: string): string {
  return new Date(dateString).toLocaleDateString('pt-BR');
}
