using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COCDonator.Discord
{
  public class DiscordCommands : InteractionModuleBase<SocketInteractionContext>
  {
    // Constructor to accept MainWindow instance
    public DiscordCommands(MainWindow mainWindow)
    {
    }

    [SlashCommand("addblacklistedplayer", "Add a player to the blacklist")]
    public async Task AddPlayerToBlacklist(string playerTag)
    {
      await RespondAsync("Player: " + playerTag + " is now blacklisted and wont be invited anymore.");
    }
  }
}
