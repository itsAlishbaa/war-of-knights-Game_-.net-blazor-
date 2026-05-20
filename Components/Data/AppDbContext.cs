//using final_pro_c.Mode ls;
//using IronCrusadeBlazor.Models;
//using Microsoft.EntityFrameworkCore;
//using System.Reflection.Emit;
  
//namespace IronCrusa deBlazor.Data
//{
//    public class App DbContext : DbContext
//    {
//        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

//        public DbSet<User> Users => Set<User>();
//        public DbSet<MatchHistory> Matches => Set<MatchHistory>();

//        protected override void OnModelCreating(ModelBuilder modelBuilder)
//        {
//            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
//            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
//        }
//    }
//}
