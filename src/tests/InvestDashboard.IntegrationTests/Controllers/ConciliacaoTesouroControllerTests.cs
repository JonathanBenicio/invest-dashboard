using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.Application.DTOs.Importacao;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Aggregates.Trading;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;
using Microsoft.Extensions.DependencyInjection;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class ConciliacaoTesouroControllerTests
{
    [Fact]
    public async Task ApresentaCandidatosSemAlterarCarteiraEIsolaOutrosUsuarios()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));
        using var outro = factory.CreateClient();
        outro.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FakeAuthProvider.GenerateJwtForUser(FakeAuthProvider.SecondTestUserId, FakeAuthProvider.SecondTestSessionId, FakeAuthProvider.SecondTestEmail, FakeAuthProvider.SecondTestName));
        var carteiraId = Guid.NewGuid(); var ativoId = Guid.NewGuid(); var loteId = Guid.NewGuid(); var transacaoId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            var carteira = new Carteira(carteiraId, FakeAuthProvider.TestUserId.ToString(), "Carteira sintética");
            var ativo = new RendaFixa(ativoId, "TESTE01", "Título sintético", 20m, DateTime.UtcNow, "PREFIXADO", 10m, DateTime.UtcNow.AddYears(5), "Tesouro");
            var transacao = new Transacao(transacaoId, FakeAuthProvider.TestUserId.ToString(), carteiraId, ativoId, ativo.Ticker, TipoTransacao.Buy, 1m, 20m, 0m, new DateTime(2024, 3, 2, 12, 0, 0, DateTimeKind.Utc));
            carteira.ProcessTransaction(transacao, 20m, TipoAtivo.RendaFixa);
            var movimento = new DadosMovimentoTesouroDto(new DateOnly(2024, 3, 2), "Compra", "Crédito", 1m, 20m, null, 20m, []);
            var previa = new PreviaTesouroDto(carteiraId, "html", "teste", "Título", "2035-01-01", null, [], [new([], movimento)], [], [], true, false, false);
            var lote = new LoteImportacaoTesouro(loteId, carteiraId, FakeAuthProvider.TestUserId, new DateOnly(2024, 3, 3), new string('a', 64), "html", JsonSerializer.Serialize(previa), DateTime.UtcNow);
            db.AddRange(carteira, ativo, transacao, lote);
            await db.SaveChangesAsync();
        }
        var url = $"/api/v1/portfolios/{carteiraId}/tesouro-direto/lotes/{loteId}/conciliacao";
        using var resposta = await client.PostAsJsonAsync(url, new { ativoId });
        resposta.StatusCode.Should().Be(HttpStatusCode.OK, await resposta.Content.ReadAsStringAsync());
        resposta.Headers.CacheControl!.NoStore.Should().BeTrue();
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var dados = json.RootElement.GetProperty("dados");
        dados.GetProperty("podeConfirmar").GetBoolean().Should().BeFalse();
        dados.GetProperty("eventos")[0].GetProperty("candidatos")[0].GetGuid().Should().Be(transacaoId);
        (await outro.PostAsJsonAsync(url, new { ativoId })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.PostAsJsonAsync(url, new { ativoId = Guid.NewGuid() })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            db.Transactions.Count(t => t.CarteiraId == carteiraId).Should().Be(1);
            db.Set<LoteImportacaoTesouro>().Single(l => l.Id == loteId).Estado.Should().Be("Pendente");
        }
    }
}
