using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EventBot.Entities
{
    public class GuildConfig
    {
        [Key]
        public ulong GuildId { get; set; }
        public string Prefix { get; set; }
        public ulong EventRoleConfirmationChannelId { get; set; }
        public ulong ParticipantRoleId { get; set; }
        public virtual ICollection<Event> Events { get; set; }

    }
}
