using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Aggregates.Trading;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class BaseTesouroLedgerTests
{
    [Fact]
    public async Task BaseSubstituiHistoricoAnteriorEReaplicaSomenteMovimentacoesPosteriores()
    {
        using var factory = new CustomWebApplicationFactory();
        await ExecutarAsync(factory);
    }

    [RequiresPostgresFact]
    public async Task BasePersisteEReconstroiComPostgres()
    {
        var connection = Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!;
        await using var database = await IsolatedPostgresDatabase.CreateAsync(connection);
        using var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        await ExecutarAsync(factory);
    }

    private static async Task ExecutarAsync(CustomWebApplicationFactory factory)
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));
        var carteiraId = Guid.NewGuid(); var ativoId = Guid.NewGuid(); var loteId = Guid.NewGuid();
        var antigoId = Guid.NewGuid();
        Guid positionId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            var carteira = new Carteira(carteiraId, FakeAuthProvider.TestUserId.ToString(), "Carteira sintética");
            var ativo = new RendaFixa(ativoId, "TESOUR01", "Título sintético", 10m, DateTime.UtcNow, "PREFIXADO", 10m, DateTime.UtcNow.AddYears(5), "Tesouro");
            var antigo = new Transacao(antigoId, FakeAuthProvider.TestUserId.ToString(), carteiraId, ativoId, ativo.Ticker,
                TipoTransacao.Buy, 2m, 10m, 0m, new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc));
            carteira.ProcessTransaction(antigo, 10m, TipoAtivo.RendaFixa);
            positionId = carteira.Positions.Single().Id;
            var avaliacaoPosterior = new ValuacaoPosicao(Guid.NewGuid(), positionId,
                new DateTime(2024, 1, 4, 12, 0, 0, DateTimeKind.Utc), 20m, 5m,
                new DateTime(2024, 1, 4, 12, 0, 0, DateTimeKind.Utc));
            var lote = new LoteImportacaoTesouro(loteId, carteiraId, FakeAuthProvider.TestUserId, new DateOnly(2024, 1, 2), new string('a', 64), "html", "{}", DateTime.UtcNow);
            var saldo = new BasePosicaoTesouro(Guid.NewGuid(), loteId, carteiraId, ativoId, lote.DataReferencia, 5m, 50m, 12m);
            saldo.Ativar();
            db.AddRange(carteira, ativo, antigo, lote, saldo, avaliacaoPosterior);
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsJsonAsync("/api/v1/transactions", new
        {
            carteiraId, ativoId, ticker = "TESOUR01", tipo = "Buy", classeAtivo = "RENDA_FIXA",
            quantidade = 1m, precoUnitario = 15m, taxas = 0m, dataTransacao = new DateTime(2024, 1, 3, 12, 0, 0, DateTimeKind.Utc), chaveIdempotencia = Guid.NewGuid()
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            var position = await db.AssetPositions.SingleAsync(p => p.CarteiraId == carteiraId && p.AtivoId == ativoId);
            position.Quantity.Should().Be(6m);
            position.TotalCost.Should().Be(65m);
            position.CurrentPrice.Should().Be(20m);
            (await db.Transactions.CountAsync(t => t.CarteiraId == carteiraId)).Should().Be(2);
        }

        // Mesmo fora de dias com transações, uma base ainda sem reconstrução
        // histórica não pode produzir um total zero marcado como completo.
        using (var historicoResponse = await client.GetAsync($"/api/v1/portfolios/{carteiraId}/history?dataDe=2024-02-01&dataAte=2024-02-02"))
        {
            historicoResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            using var historico = JsonDocument.Parse(await historicoResponse.Content.ReadAsStringAsync());
            var pontos = historico.RootElement.GetProperty("dados").EnumerateArray().ToArray();
            pontos.Should().HaveCount(2);
            foreach (var ponto in pontos)
            {
                ponto.GetProperty("estaCompleto").GetBoolean().Should().BeFalse();
                ponto.GetProperty("valorTotal").ValueKind.Should().Be(JsonValueKind.Null);
                ponto.GetProperty("tickersAusentes").EnumerateArray().Select(item => item.GetString()).Should().Contain("TESOUR01");
            }
        }

        var historica = await client.PostAsJsonAsync("/api/v1/transactions", new
        {
            carteiraId, ativoId, ticker = "TESOUR01", tipo = "Buy", classeAtivo = "RENDA_FIXA",
            quantidade = 1m, precoUnitario = 15m, taxas = 0m, dataTransacao = new DateTime(2024, 1, 2, 12, 0, 0, DateTimeKind.Utc), chaveIdempotencia = Guid.NewGuid()
        });
        historica.StatusCode.Should().Be(HttpStatusCode.Conflict);
        // 02:00 UTC do dia seguinte ainda é 23h no dia da base no Brasil.
        var limiteFuso = await client.PostAsJsonAsync("/api/v1/transactions", new
        {
            carteiraId, ativoId, ticker = "TESOUR01", tipo = "Buy", classeAtivo = "RENDA_FIXA",
            quantidade = 1m, precoUnitario = 15m, taxas = 0m, dataTransacao = new DateTime(2024, 1, 3, 2, 0, 0, DateTimeKind.Utc), chaveIdempotencia = Guid.NewGuid()
        });
        limiteFuso.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var semFuso = await client.PostAsJsonAsync("/api/v1/transactions", new
        {
            carteiraId, ativoId, ticker = "TESOUR01", tipo = "Buy", classeAtivo = "RENDA_FIXA",
            quantidade = 1m, precoUnitario = 15m, taxas = 0m, dataTransacao = "2024-01-03T12:00:00", chaveIdempotencia = Guid.NewGuid()
        });
        semFuso.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.DeleteAsync($"/api/v1/transactions/{antigoId}")).StatusCode.Should().Be(HttpStatusCode.Conflict);
        var edicao = await client.PatchAsJsonAsync($"/api/v1/transactions/{antigoId}", new
        {
            tipo = "Buy", quantidade = 3m, precoUnitario = 10m, taxas = 0m,
            dataTransacao = new DateTime(2024, 1, 3, 12, 0, 0, DateTimeKind.Utc)
        });
        edicao.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await client.DeleteAsync($"/api/v1/investments/{positionId}")).StatusCode.Should().Be(HttpStatusCode.Conflict);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            (await db.Transactions.SingleAsync(t => t.Id == antigoId)).Quantity.Should().Be(2m);
            (await db.AssetPositions.SingleAsync(p => p.CarteiraId == carteiraId && p.AtivoId == ativoId)).Quantity.Should().Be(6m);
        }
    }
}
