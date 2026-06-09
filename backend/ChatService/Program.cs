using System.Text;
using ChatService.Config;
using ChatService.Data;
using ChatService.Hubs;
using ChatService.Realtime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---- MongoDB ----
builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("Mongo"));
builder.Services.AddSingleton<MongoContext>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();

// ---- SignalR (com backplane Redis se configurado) ----
var redisConn = builder.Configuration["Redis:ConnectionString"];
var signalR = builder.Services.AddSignalR();
if (!string.IsNullOrWhiteSpace(redisConn))
{
    // Com Redis, varias replicas compartilham as conexoes.
    signalR.AddStackExchangeRedis(redisConn, options =>
        options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("sd-chat"));
}

// Liga cada conexao SignalR ao userId do JWT (para Clients.Users(...)).
builder.Services.AddSingleton<IUserIdProvider, NameUserIdProvider>();

// ---- Autenticacao JWT (mesma chave do AuthService) ----
var jwt = builder.Configuration.GetSection("Jwt");
var secret = jwt["Secret"] ?? throw new InvalidOperationException("Jwt:Secret missing");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // WebSocket nao manda header Authorization: o SignalR envia o token
        // na query string (?access_token=...). Aqui a gente le de la.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub/chat"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ---- CORS (necessario para o app conectar; SignalR exige AllowCredentials) ----
builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p => p
        .SetIsOriginAllowed(_ => true)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Garante a conexao/indices do Mongo no boot (com retry, pois o container
// pode subir antes do MongoDB estar pronto).
for (var i = 1; i <= 10; i++)
{
    try
    {
        _ = app.Services.GetRequiredService<MongoContext>();
        break;
    }
    catch (Exception ex) when (i < 10)
    {
        Console.WriteLine($"[ChatService] Mongo nao pronto (tentativa {i}/10): {ex.Message}");
        Thread.Sleep(2000);
    }
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hub/chat");

app.Run();

public partial class Program { }
