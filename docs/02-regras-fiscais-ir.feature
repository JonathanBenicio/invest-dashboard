# language: pt
Funcionalidade: Apuração e Cálculos Fiscais (Imposto de Renda)
  Para facilitar minha declaração fiscal
  Como um investidor do mercado brasileiro
  Eu quero que o sistema calcule deduções e isenções de forma automática

  Cenário: Isenção de imposto de renda em vendas de ações abaixo de R$ 20.000 no mês
    Dado que o usuário possui posições em Ações Brasileiras
    Quando o usuário registra uma transação de "Venda" de Ações com lucro
    E o volume total de vendas de ações do usuário no mês atual é de "R$ 15.000,00"
    Então o sistema não deve reter ou cobrar Imposto de Renda (Alíquota = 0%)
    E o lucro líquido gerado deve ser igual ao lucro bruto

  Cenário: Cobrança de IR sobre ganho de capital em ações acima de R$ 20.000 no mês
    Dado que o usuário registrou vendas de Ações que somam "R$ 25.000,00" no mês atual
    Quando a transação atual resulta em um lucro bruto de "R$ 2.000,00"
    Então o sistema deve aplicar a alíquota padrão de IR (15% para Swing Trade)
    E registrar um Imposto Devido de "R$ 300,00"
    E atualizar o saldo considerando o desconto aplicável

  Cenário: Cobrança de IR de Fundos Imobiliários sem isenção mensal
    Dado que o usuário possui posições no tipo "Fundo Imobiliário" (FII)
    Quando o usuário registra uma transação de "Venda" de FII com lucro bruto de "R$ 500,00"
    E o volume total de vendas no mês é irrelevante para a regra do FII
    Então o sistema deve aplicar a alíquota de 20%
    E registrar um Imposto Devido de "R$ 100,00"