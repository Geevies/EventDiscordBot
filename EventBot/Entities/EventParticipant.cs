using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace EventBot.Entities
{
    public class EventParticipant
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int EventRoleId { get; set; }
        [ForeignKey("EventRoleId")]
        public virtual EventRole Role { get; set; }
        public int EventId { get; set; }
        [ForeignKey("EventId")]
        public virtual Event Event { get; set; }
        public ulong UserId { get; set; }
        public string UserData { get; set; }
    }
}
