using System;
using System.ComponentModel.DataAnnotations;

namespace final_pro_c.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; } = Environment.TickCount;

        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty; // Simple Base64 Simulation

        public string CreatedAt { get; set; } = DateTime.Now.ToString("dd/MM/yyyy");
        public string LastLogin { get; set; } = DateTime.Now.ToString("g");

        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
        public int Streak { get; set; } = 0;
        public int BestStreak { get; set; } = 0;
        public int DragonKills { get; set; } = 0;
    }
}