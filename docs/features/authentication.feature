Feature: Autenticação de Usuário com Supabase
  Como um usuário do sistema
  Eu quero me autenticar de forma segura
  Para acessar meu portfólio de investimentos privado

  Scenario: Login bem-sucedido com credenciais válidas
    Given que o usuário possui uma conta registrada no Supabase com email "investidor@teste.com"
    When o usuário tenta fazer login informando a senha correta
    Then o Supabase deve retornar um token JWT de autenticação válido
    And a API .NET deve validar o token com sucesso e permitir o acesso à rota privada "/api/portfolio"

  Scenario: Acesso bloqueado a rotas do Core Domain por falta de autenticação
    Given que o usuário não está logado ou possui um token JWT expirado
    When o usuário faz uma requisição HTTP GET para "/api/portfolio"
    Then a API .NET deve interceptar a chamada via middleware
    And deve retornar o status HTTP 401 Unauthorized
