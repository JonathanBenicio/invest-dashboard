using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace InvestDashboard.IntegrationTests.Setup;

internal sealed class TesouroSnapshotConfirmationReadBarrier : DbCommandInterceptor
{
    private const int MaxWaitSeconds = 20;
    private readonly TaskCompletionSource<bool> _participantesProntos =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _liberar =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _participantesEsperados;
    private int _participantes;
    private int _armada;

    public void Armar(int participantesEsperados)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(participantesEsperados);
        _participantesEsperados = participantesEsperados;
        Volatile.Write(ref _armada, 1);
    }

    public Task EsperarParticipantesAsync(CancellationToken cancellationToken) =>
        _participantesProntos.Task.WaitAsync(TimeSpan.FromSeconds(MaxWaitSeconds), cancellationToken);

    public void Liberar() => _liberar.TrySetResult(true);

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command,
        CommandExecutedEventData eventData, DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _armada) == 0 ||
            !command.CommandText.Contains("treasury_position_bases", StringComparison.OrdinalIgnoreCase))
            return result;

        var participante = Interlocked.Increment(ref _participantes);
        if (participante <= _participantesEsperados)
        {
            if (participante == _participantesEsperados)
                _participantesProntos.TrySetResult(true);

            await _liberar.Task.WaitAsync(TimeSpan.FromSeconds(MaxWaitSeconds), cancellationToken);
        }

        return result;
    }
}
