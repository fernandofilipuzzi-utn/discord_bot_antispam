using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace AntiSpamBot
{
    public class Program
    {
        private DiscordSocketClient _client;
       
        private readonly string _token; 

        #region restricciones
        private readonly int _maxFileSize = 10 * 1024 * 1024; // 10MB
        private readonly List<string> _blockedExtensions = new List<string>
        {
            ".exe", ".bat", ".cmd", ".sh", ".js", ".vbs", ".ps1", ".scr", ".com", ".pif"
        };

        private readonly List<string> _suspiciousExtensions = new List<string>
        {
            ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp" 
        };
        
        int CountsSuspiciousPatternsMax = 3;

        // identificación de patrones sospechosos
        private readonly List<string> _suspiciousPatterns = new List<string>
        {
            @"photo_\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2}\.jpg",         
        };

        private readonly List<string> _countsSuspiciousPatterns = new List<string>
        {
            @"image\.png"
        };

        #endregion
        
        public Program()
        {
            //_token = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN"); // Nombre de tu variable de entorno
            var configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
               .Build();

            _token = configuration.GetSection("Discord:Token").Value;

            if (string.IsNullOrWhiteSpace(_token))
            {
                Console.WriteLine("¡Error! La variable de entorno 'DISCORD_BOT_TOKEN' no se encontró o está vacía.");
                Console.WriteLine("Por favor, establece la variable de entorno antes de ejecutar el bot.");
                Environment.Exit(1); // Sale del programa si no hay token
            }
        }

        // seguimiento de actividad del usuario
        private readonly Dictionary<ulong, UserActivity> _userActivity = new Dictionary<ulong, UserActivity>();
        
        public static Task Main(string[] args) => new Program().MainAsync();
        
        public async Task MainAsync()
        {
            var config = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent // Add MessageContent here
            };
            _client = new DiscordSocketClient(config);
            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.MessageReceived += MessageReceivedAsync;

            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();

            await Task.Delay(-1);
        }
        
        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }
        
        private Task ReadyAsync()
        {
            Console.WriteLine($"{_client.CurrentUser} está conectado!");
            return Task.CompletedTask;
        }
        
        // tratamiento de mensajes
        private async Task MessageReceivedAsync(SocketMessage message)
        {
            // ignora mensajes de bots
            if (message.Author.IsBot) return;

            var userMessage = message as SocketUserMessage;
            if (userMessage == null) return;

            var user = message.Author;
            var channel = message.Channel;

            // verificar si es spam 
            if (await IsSpamMessage(userMessage))
            {
                await HandleSpamMessage(userMessage);
                return;
            }

            // seguimiento de actividad del usuario
            await TrackUserActivity(user.Id, userMessage);
        }
        
        private async Task<bool> IsSpamMessage(SocketUserMessage message)
        {
            // tiene archivos adjuntos 
            if (message.Attachments.Count == 0) return false;

            // analizando archivos adjuntos
            int umbralSuspicious = 0;
            foreach (var attachment in message.Attachments)
            {
                Console.WriteLine($"Archivo adjunto: {attachment.Filename}, Tamaño: {attachment.Size} bytes");
                // verificar tamaño
                if (attachment.Size > _maxFileSize)
                {
                    await LogSuspiciousActivity(message, $"Archivo muy grande: {attachment.Size} bytes");
                    return true;
                }

                #region extraer extension
                var extension = Path.GetExtension(attachment.Filename).ToLower();
                #endregion

                #region verifica en archivos con ciertas extensiones si tienen patrones sospechosos
                if (_suspiciousExtensions.Contains(extension))
                {
                    foreach (var pattern in _suspiciousPatterns)
                    {
                        if (Regex.IsMatch(attachment.Filename, pattern, RegexOptions.IgnoreCase))
                        {
                            await LogSuspiciousActivity(message, $"Patrón sospechoso detectado: {attachment.Filename}");
                            return true;
                        }
                    }
                }
                #endregion

                #region verificar archivos con extensiones a bloquear
                if (_blockedExtensions.Contains(extension))
                {
                    await LogSuspiciousActivity(message, $"Patrón sospechoso detectado: {attachment.Filename}");
                    return true;
                }
                #endregion

                #region actualiza cantidad de adjuntos sospechosos
                foreach (var pattern in _countsSuspiciousPatterns)
                {
                    if (Regex.IsMatch(attachment.Filename, pattern, RegexOptions.IgnoreCase))
                    {
                        umbralSuspicious++;
                        break;
                    }
                }               
                #endregion 
            }

            #region verifica si supera el umbral de archivos sospechosos
            if (umbralSuspicious >= CountsSuspiciousPatternsMax)
            {
                await LogSuspiciousActivity(message, $"Demasiados archivos sospechosos: {umbralSuspicious}");
                return true;
            }
            #endregion

            return false;
        }
        
        private async Task TrackUserActivity(ulong userId, SocketUserMessage message)
        {
            if (!_userActivity.ContainsKey(userId))
            {
                _userActivity[userId] = new UserActivity();
            }
            
            var activity = _userActivity[userId];
            var now = DateTime.Now;
            
            // Limpiar actividad antigua (últimos 5 minutos)
            activity.RecentMessages = activity.RecentMessages
                .Where(m => (now - m.Timestamp).TotalMinutes < 5)
                .ToList();
            
            // Agregar mensaje actual
            activity.RecentMessages.Add(new MessageInfo
            {
                Timestamp = now,
                HasAttachments = message.Attachments.Count > 0,
                AttachmentCount = message.Attachments.Count
            });
            
            // Verificar si es spam por volumen
            var recentAttachments = activity.RecentMessages.Count(m => m.HasAttachments);
            if (recentAttachments > 5) // Más de 5 mensajes con archivos en 5 minutos
            {
                await HandleSpamUser(message, "Spam por volumen de archivos");
            }
        }
        
        private async Task HandleSpamMessage(SocketUserMessage message)
        {
            try
            {
                // Eliminar el mensaje
                await message.DeleteAsync();
                
                // Enviar advertencia al usuario
                var embed = new EmbedBuilder()
                    .WithTitle("⚠️ Mensaje Bloqueado")
                    .WithDescription($"{message.Author.Mention}, tu mensaje ha sido eliminado por contener archivos no permitidos.")
                    .WithColor(Color.Orange)
                    .WithTimestamp(DateTimeOffset.Now)
                    .Build();
                
                await message.Channel.SendMessageAsync(embed: embed);
                
                // Log para moderadores
                await LogToModerationChannel(message, "Mensaje spam eliminado");
                
                // Incrementar contador de infracciones
                await IncrementUserViolations(message.Author.Id, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al manejar mensaje spam: {ex.Message}");
            }
        }
        
        private async Task HandleSpamUser(SocketUserMessage message, string reason)
        {
            try
            {
                var guild = (message.Channel as SocketGuildChannel)?.Guild;
                if (guild == null) return;
                
                var user = guild.GetUser(message.Author.Id);
                if (user == null) return;
                
                // Mutear usuario temporalmente
                await user.SetTimeOutAsync(TimeSpan.FromMinutes(10));
                
                var embed = new EmbedBuilder()
                    .WithTitle("🔇 Usuario Muteado")
                    .WithDescription($"{user.Mention} ha sido muteado por 10 minutos.\nRazón: {reason}")
                    .WithColor(Color.Red)
                    .WithTimestamp(DateTimeOffset.Now)
                    .Build();
                
                await message.Channel.SendMessageAsync(embed: embed);
                
                await LogToModerationChannel(message, $"Usuario muteado: {reason}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al mutear usuario: {ex.Message}");
            }
        }
        
        private async Task IncrementUserViolations(ulong userId, SocketUserMessage message)
        {
            if (!_userActivity.ContainsKey(userId))
            {
                _userActivity[userId] = new UserActivity();
            }
            
            var activity = _userActivity[userId];
            activity.ViolationCount++;
            
            // Acciones escaladas
            switch (activity.ViolationCount)
            {
                case 3:
                    await HandleSpamUser(message, "3 infracciones - Mute temporal");
                    break;
                case 5:
                    await BanUser(message, "5 infracciones - Ban automático");
                    break;
            }
        }
        
        private async Task BanUser(SocketUserMessage message, string reason)
        {
            try
            {
                var guild = (message.Channel as SocketGuildChannel)?.Guild;
                if (guild == null) return;
                
                var user = guild.GetUser(message.Author.Id);
                if (user == null) return;
                
                await guild.AddBanAsync(user.Id, reason: reason);
                
                var embed = new EmbedBuilder()
                    .WithTitle("🔨 Usuario Baneado")
                    .WithDescription($"{user.Mention} ha sido baneado del servidor.\nRazón: {reason}")
                    .WithColor(Color.DarkRed)
                    .WithTimestamp(DateTimeOffset.Now)
                    .Build();
                
                await message.Channel.SendMessageAsync(embed: embed);
                
                await LogToModerationChannel(message, $"Usuario baneado: {reason}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al banear usuario: {ex.Message}");
            }
        }
        
        private async Task LogSuspiciousActivity(SocketUserMessage message, string details)
        {
            Console.WriteLine($"[SPAM DETECTADO] Usuario: {message.Author.Username} | {details}");
            await LogToModerationChannel(message, details);
        }
        
        private async Task LogToModerationChannel(SocketUserMessage message, string details)
        {
            // Buscar canal de moderación (opcional)
            var guild = (message.Channel as SocketGuildChannel)?.Guild;
            if (guild == null) return;
            
            var modChannel = guild.TextChannels.FirstOrDefault(c => 
                c.Name.ToLower().Contains("mod") || 
                c.Name.ToLower().Contains("log"));
            
            if (modChannel == null) return;
            
            var embed = new EmbedBuilder()
                .WithTitle("📋 Log de Moderación")
                .WithDescription(details)
                .AddField("Usuario", message.Author.Mention, true)
                .AddField("Canal", message.Channel.Name, true)
                .AddField("Hora", DateTime.Now.ToString("HH:mm:ss"), true)
                .WithColor(Color.Blue)
                .WithTimestamp(DateTimeOffset.Now)
                .Build();
            
            await modChannel.SendMessageAsync(embed: embed);
        }
    }
    
    public class UserActivity
    {
        public List<MessageInfo> RecentMessages { get; set; } = new List<MessageInfo>();
        public int ViolationCount { get; set; } = 0;
    }
    
    public class MessageInfo
    {
        public DateTime Timestamp { get; set; }
        public bool HasAttachments { get; set; }
        public int AttachmentCount { get; set; }
    }
}