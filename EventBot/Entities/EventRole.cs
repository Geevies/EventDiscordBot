using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace EventBot.Entities
{
    public class EventRole
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Emote { get; set; }
        public int MaxParticipants { get; set; }
        public int EventId { get; set; }
        [ForeignKey("EventId")]
        public virtual Event Event { get; set; }
        public virtual ICollection<EventParticipant> Participants { get; set; }
        public int ParticipantCount => Participants == null ? 0 : Participants.Count;
        public int ReamainingOpenings => MaxParticipants < 0 ? 1 : MaxParticipants - ParticipantCount;

        public int SortNumber
        {
            get
            {
                if (MaxParticipants < 0) return int.MaxValue;
                return MaxParticipants;
            }
        }
    }
}
