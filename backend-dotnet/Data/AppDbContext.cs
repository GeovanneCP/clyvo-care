using ClyvoCare.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ClyvoCare.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Pet> Pets { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<LogSaude> LogsSaude { get; set; }
    }
}