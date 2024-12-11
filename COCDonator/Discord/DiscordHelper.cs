using Discord.WebSocket;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Windows.Threading;
using System.Reflection;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;

namespace COCDonator.Discord
{
  public class DiscordHelper
  {
    private readonly DiscordSocketClient _client;
    private readonly string _botToken;
    private ulong _defaultChannelId;

    public DiscordHelper(string botToken, ulong defaultChannelId)
    {
      _client = new DiscordSocketClient();
      _botToken = botToken;
      _defaultChannelId = defaultChannelId;

      _client.Log += LogAsync;
    }

    public async Task InitializeAsync()
    {
      // Login and start the bot
      await _client.LoginAsync(TokenType.Bot, _botToken);
      await _client.StartAsync();

      // Wait for the bot to be ready
      _client.Ready += () =>
      {
        Console.WriteLine("Bot is connected and ready!");
        return Task.CompletedTask;
      };
    }

    public async Task SendMessageToChannel(string message)
    {
      // Get the default channel
      var channel = _client.GetChannel(_defaultChannelId) as IMessageChannel;

      if (channel == null)
      {
        Console.WriteLine("Channel not found or is not a text channel.");
        return;
      }

      // Send the message
      await channel.SendMessageAsync(message);
    }

    private Task LogAsync(LogMessage log)
    {
      Console.WriteLine(log.ToString());
      return Task.CompletedTask;
    }
  }
}
