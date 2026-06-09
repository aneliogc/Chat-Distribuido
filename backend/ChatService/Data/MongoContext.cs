using ChatService.Config;
using ChatService.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ChatService.Data;

/// <summary>
/// Abre a conexao com o MongoDB e expoe as colecoes usadas pelo chat.
/// Registrado como singleton: a conexao e reaproveitada por toda a aplicacao.
/// </summary>
public class MongoContext
{
    public IMongoCollection<Conversation> Conversations { get; }
    public IMongoCollection<Message> Messages { get; }

    public MongoContext(IOptions<MongoSettings> options)
    {
        var settings = options.Value;
        var client = new MongoClient(settings.ConnectionString);
        var database = client.GetDatabase(settings.Database);

        Conversations = database.GetCollection<Conversation>("conversations");
        Messages = database.GetCollection<Message>("messages");

        CreateIndexes();
    }

    private void CreateIndexes()
    {
        // Busca de historico: mensagens de uma conversa ordenadas por data.
        var messageIndex = new CreateIndexModel<Message>(
            Builders<Message>.IndexKeys
                .Ascending(m => m.ConversationId)
                .Ascending(m => m.SentAt));
        Messages.Indexes.CreateOne(messageIndex);

        // Busca das conversas de um usuario.
        var convIndex = new CreateIndexModel<Conversation>(
            Builders<Conversation>.IndexKeys.Ascending("Participants.UserId"));
        Conversations.Indexes.CreateOne(convIndex);
    }
}
