using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Linq;

namespace EventBot.Entities
{
    public class Event
    {
        public Event()
        {
            Active = true;
            Opened = DateTime.Now;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
        public ulong MessageId { get; set; }
        public ulong MessageChannelId { get; set; }
        public virtual ICollection<EventRole> Roles { get; set; }
        public virtual ICollection<EventParticipant> Participants { get; set; }
        public int ParticipantCount => Participants == null ? 0 : Participants.Count;
        public DateTime Opened { get; set; }
        public virtual EventParticipactionType Type { get; set; }
        public ulong GuildId { get; set; }
        [ForeignKey("GuildId")]
        public virtual GuildConfig Guild { get; set; }
        public int RemainingOpenings => Roles.Sum(r => r.ReamainingOpenings);

        public enum EventParticipactionType
        {
            Unspecified = -1,
            Quick,
            Detailed
        }

    }
}
