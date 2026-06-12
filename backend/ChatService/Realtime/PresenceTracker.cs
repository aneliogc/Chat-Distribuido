using System.Collections.Concurrent;
using StackExchange.Redis;

namespace ChatService.Realtime;

/// <summary>
/// Rastreia quais usuarios estao online (tem ao menos uma conexao SignalR ativa).
/// Conta conexoes por usuario, pois o mesmo usuario pode ter varios dispositivos/abas
/// e estar conectado em replicas diferentes. O estado fica no Redis (compartilhado
/// entre as replicas); sem Redis, cai para um dicionario em memoria (instancia unica).
/// </summary>
public class PresenceTracker
{
    private const string Key = "presence:counts"; // hash redis: field = userId, value = nº de conexoes
    private readonly IDatabase? _redis;
    private readonly ConcurrentDictionary<string, int> _local = new();

    public PresenceTracker(IConnectionMultiplexer? mux)
    {
        _redis = mux?.GetDatabase();
    }

    /// <summary>Registra uma nova conexao. Retorna true se o usuario passou a ficar online (1a conexao).</summary>
    public async Task<bool> ConnectedAsync(string userId)
    {
        if (_redis is not null)
            return await _redis.HashIncrementAsync(Key, userId, 1) == 1;

        return _local.AddOrUpdate(userId, 1, (_, v) => v + 1) == 1;
    }

    /// <summary>Remove uma conexao. Retorna true se o usuario ficou offline (ultima conexao caiu).</summary>
    public async Task<bool> DisconnectedAsync(string userId)
    {
        if (_redis is not null)
        {
            var count = await _redis.HashIncrementAsync(Key, userId, -1);
            if (count > 0) return false;
            await _redis.HashDeleteAsync(Key, userId); // limpa o field zerado/negativo
            return true;
        }

        var newVal = _local.AddOrUpdate(userId, 0, (_, v) => v - 1);
        if (newVal > 0) return false;
        _local.TryRemove(userId, out _);
        return true;
    }

    /// <summary>Lista os userIds atualmente online.</summary>
    public async Task<string[]> GetOnlineAsync()
    {
        if (_redis is not null)
        {
            var entries = await _redis.HashGetAllAsync(Key);
            return entries.Where(e => (long)e.Value > 0).Select(e => e.Name.ToString()).ToArray();
        }

        return _local.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToArray();
    }
}