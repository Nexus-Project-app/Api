using Microsoft.EntityFrameworkCore;

namespace Nexus.Context.Context
{
    public class NexusDbContext : DbContext
    {
        public NexusDbContext(DbContextOptions<NexusDbContext> options)
            : base(options)
        {
            // Permet de régler l'erreur "timestamp without time zone" .
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }
    }
}