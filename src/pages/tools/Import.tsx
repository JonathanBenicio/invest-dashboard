import { useState } from "react";
import { Upload, FileSpreadsheet, CheckCircle, AlertCircle, FileText, Building2 } from "lucide-react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Progress } from "@/components/ui/progress";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { useToast } from "@/hooks/use-toast";

interface ImportedRow {
  ticker: string;
  quantity: number;
  price: number;
  date: string;
  status: 'success' | 'error' | 'pending';
  message?: string;
}

export default function Import() {
  const [isDragging, setIsDragging] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [uploadProgress, setUploadProgress] = useState(0);
  const [importedData, setImportedData] = useState<ImportedRow[]>([]);
  const { toast } = useToast();

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(true);
  };

  const handleDragLeave = () => {
    setIsDragging(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
    const files = Array.from(e.dataTransfer.files);
    handleFiles(files);
  };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files ? Array.from(e.target.files) : [];
    handleFiles(files);
  };

  const handleFiles = async (files: File[]) => {
    const file = files[0];
    if (!file) return;

    const validTypes = ['.csv', '.xlsx', '.xls'];
    const isValid = validTypes.some(type => file.name.toLowerCase().endsWith(type));

    if (!isValid) {
      toast({
        title: "Formato inválido",
        description: "Por favor, envie um arquivo CSV ou Excel (.xlsx, .xls)",
        variant: "destructive",
      });
      return;
    }

    setIsUploading(true);
    setUploadProgress(0);

    // Simulate upload and processing
    for (let i = 0; i <= 100; i += 10) {
      await new Promise(resolve => setTimeout(resolve, 200));
      setUploadProgress(i);
    }

    // Simulate imported data
    const mockData: ImportedRow[] = [
      { ticker: 'PETR4', quantity: 100, price: 35.50, date: '2024-12-01', status: 'success' },
      { ticker: 'VALE3', quantity: 50, price: 65.00, date: '2024-12-02', status: 'success' },
      { ticker: 'ITUB4', quantity: 200, price: 30.25, date: '2024-12-03', status: 'success' },
      { ticker: 'INVALID', quantity: 30, price: 10.00, date: '2024-12-04', status: 'error', message: 'Ticker não encontrado' },
      { ticker: 'BBDC4', quantity: 150, price: 14.80, date: '2024-12-05', status: 'pending' },
    ];

    setImportedData(mockData);
    setIsUploading(false);

    toast({
      title: "Arquivo processado",
      description: `${mockData.length} registros encontrados. Revise antes de confirmar.`,
    });
  };

  const confirmImport = () => {
    const successCount = importedData.filter(row => row.status === 'success').length;
    toast({
      title: "Importação concluída",
      description: `${successCount} registros importados com sucesso.`,
    });
    setImportedData([]);
  };

  const brokers = [
    { name: 'Clear', status: 'Em breve', icon: Building2 },
    { name: 'XP Investimentos', status: 'Em breve', icon: Building2 },
    { name: 'Rico', status: 'Em breve', icon: Building2 },
    { name: 'NuInvest', status: 'Em breve', icon: Building2 },
    { name: 'BTG Pactual', status: 'Em breve', icon: Building2 },
    { name: 'Inter', status: 'Em breve', icon: Building2 },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">Importar Dados</h1>
        <p className="text-muted-foreground">Importe seus dados via arquivo ou conecte sua corretora</p>
      </div>

      <Tabs defaultValue="file" className="space-y-6">
        <TabsList>
          <TabsTrigger value="file">Importar Arquivo</TabsTrigger>
          <TabsTrigger value="brokers">Corretoras</TabsTrigger>
        </TabsList>

        <TabsContent value="file" className="space-y-6">
          {/* Upload Area */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <FileSpreadsheet className="h-5 w-5" />
                Upload de Arquivo
              </CardTitle>
              <CardDescription>
                Arraste um arquivo CSV ou Excel com suas operações
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div
                className={`border-2 border-dashed rounded-lg p-8 text-center transition-colors ${
                  isDragging 
                    ? 'border-primary bg-primary/5' 
                    : 'border-border hover:border-primary/50'
                }`}
                onDragOver={handleDragOver}
                onDragLeave={handleDragLeave}
                onDrop={handleDrop}
              >
                {isUploading ? (
                  <div className="space-y-4">
                    <div className="flex items-center justify-center">
                      <div className="h-12 w-12 rounded-full bg-primary/10 flex items-center justify-center">
                        <Upload className="h-6 w-6 text-primary animate-pulse" />
                      </div>
                    </div>
                    <div className="space-y-2">
                      <p className="text-sm text-muted-foreground">Processando arquivo...</p>
                      <Progress value={uploadProgress} className="w-64 mx-auto" />
                    </div>
                  </div>
                ) : (
                  <div className="space-y-4">
                    <div className="flex items-center justify-center">
                      <div className="h-12 w-12 rounded-full bg-muted flex items-center justify-center">
                        <Upload className="h-6 w-6 text-muted-foreground" />
                      </div>
                    </div>
                    <div className="space-y-2">
                      <p className="text-sm font-medium">
                        Arraste um arquivo aqui ou{" "}
                        <label className="text-primary cursor-pointer hover:underline">
                          clique para selecionar
                          <input
                            type="file"
                            accept=".csv,.xlsx,.xls"
                            className="hidden"
                            onChange={handleFileSelect}
                          />
                        </label>
                      </p>
                      <p className="text-xs text-muted-foreground">
                        Formatos aceitos: CSV, Excel (.xlsx, .xls)
                      </p>
                    </div>
                  </div>
                )}
              </div>

              {/* Template Download */}
              <div className="mt-4 flex items-center justify-center gap-2 text-sm text-muted-foreground">
                <FileText className="h-4 w-4" />
                <span>Precisa de ajuda?</span>
                <Button variant="link" className="p-0 h-auto text-primary">
                  Baixe nosso modelo de importação
                </Button>
              </div>
            </CardContent>
          </Card>

          {/* Preview Table */}
          {importedData.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle>Preview dos Dados</CardTitle>
                <CardDescription>
                  Revise os dados antes de confirmar a importação
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="rounded-lg border">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Status</TableHead>
                        <TableHead>Ticker</TableHead>
                        <TableHead className="text-right">Quantidade</TableHead>
                        <TableHead className="text-right">Preço</TableHead>
                        <TableHead>Data</TableHead>
                        <TableHead>Observação</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {importedData.map((row, index) => (
                        <TableRow key={index}>
                          <TableCell>
                            {row.status === 'success' && (
                              <CheckCircle className="h-4 w-4 text-success" />
                            )}
                            {row.status === 'error' && (
                              <AlertCircle className="h-4 w-4 text-destructive" />
                            )}
                            {row.status === 'pending' && (
                              <div className="h-4 w-4 rounded-full border-2 border-warning" />
                            )}
                          </TableCell>
                          <TableCell className="font-medium">{row.ticker}</TableCell>
                          <TableCell className="text-right">{row.quantity}</TableCell>
                          <TableCell className="text-right">R$ {row.price.toFixed(2)}</TableCell>
                          <TableCell>{row.date}</TableCell>
                          <TableCell>
                            {row.message && (
                              <span className="text-xs text-destructive">{row.message}</span>
                            )}
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
                <div className="flex justify-end gap-4 mt-4">
                  <Button variant="outline" onClick={() => setImportedData([])}>
                    Cancelar
                  </Button>
                  <Button onClick={confirmImport}>
                    Confirmar Importação
                  </Button>
                </div>
              </CardContent>
            </Card>
          )}
        </TabsContent>

        <TabsContent value="brokers">
          <Card>
            <CardHeader>
              <CardTitle>Integração com Corretoras</CardTitle>
              <CardDescription>
                Conecte sua conta para importar automaticamente suas operações
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                {brokers.map((broker) => (
                  <Card key={broker.name} className="relative overflow-hidden">
                    <CardContent className="p-6">
                      <div className="flex items-center justify-between">
                        <div className="flex items-center gap-3">
                          <div className="h-10 w-10 rounded-lg bg-muted flex items-center justify-center">
                            <broker.icon className="h-5 w-5 text-muted-foreground" />
                          </div>
                          <div>
                            <p className="font-medium">{broker.name}</p>
                            <Badge variant="secondary" className="text-xs">
                              {broker.status}
                            </Badge>
                          </div>
                        </div>
                      </div>
                    </CardContent>
                    <div className="absolute inset-0 bg-muted/50 flex items-center justify-center">
                      <Badge variant="outline" className="bg-background">Em desenvolvimento</Badge>
                    </div>
                  </Card>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
