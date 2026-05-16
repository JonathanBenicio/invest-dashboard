Feature: Gestão de Transações e Portfólio Core
  Como um investidor
  Eu quero gerenciar minhas operações financeiras (compras e vendas)
  Para que o sistema consolide a rentabilidade da minha carteira automaticamente

  Scenario: Registro bem-sucedido de uma compra de ativos
    Given que o investidor autenticado não possui o ativo "AAPL34" em custódia
    When o investidor envia uma requisição para adicionar uma transação de COMPRA de 100 cotas de "AAPL34" a R$ 50,00 cada
    Then o Domínio deve validar a operação e registrar a transação com sucesso
    And a Posição de Ativos do usuário deve ser atualizada para refletir 100 cotas de "AAPL34"
    And o custo médio do ativo "AAPL34" deve ser calculado como R$ 50,00

  Scenario: Recálculo de patrimônio em tempo real (SignalR)
    Given que o investidor possui ativos na carteira e está com o Dashboard aberto
    And o serviço de background (Worker) recebe uma nova cotação de mercado para "AAPL34" de R$ 55,00
    When o backend processa essa nova cotação
    Then um evento de atualização deve ser disparado via SignalR Hub para o Frontend
    And o Dashboard do investidor deve atualizar o saldo patrimonial instantaneamente sem precisar recarregar a página
