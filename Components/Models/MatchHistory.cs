using final_pro_c.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace final_pro_c.Models
{
    public class MatchHistory
    {
        [Key]
        public long Id { get; set; } = DateTime.UtcNow.Ticks;

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? Player { get; set; }

        [Required]
        public int Level { get; set; }

        [Required, MaxLength(50)]
        public string Enemy { get; set; } = string.Empty;

        [Required, MaxLength(10)]
        public string Result { get; set; } = string.Empty; // "WIN" or "LOSE"

        public string Date { get; set; } = DateTime.Now.ToString("dd/MM/yyyy");
    }
}
