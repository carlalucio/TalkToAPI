using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TalkToAPI.V1.Models;

namespace TalkToAPI.Database
{
    public class TalkToContext : IdentityDbContext <ApplicationUser>
    {
        public TalkToContext(DbContextOptions<TalkToContext> options) : base(options)
        {

        }
        public DbSet<Mensagem> Mensagem { get; set; }  
        public DbSet<Token> Tokens { get; set; }  
    }
}
