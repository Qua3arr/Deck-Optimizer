using Microsoft.EntityFrameworkCore;
using DeckOptimizer.Domain.Entities;
using System.Reflection.PortableExecutable;

namespace DeckOptimizer.Infrastructure
{
    internal class AppDbContext : DbContext
    {
        public DbSet<Card> Cards { get; set; }
        public DbSet<Characteristic> Characteristics { get; set; }
        public DbSet<CharacteristicValue> CharacteristicValues { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Настройка составного ключа для связующей таблицы
            modelBuilder.Entity<CharacteristicValue>()
                .HasKey(cv => new { cv.CardId, cv.CharacteristicId });

            //Связь "Многие-ко-многим" через CharacteristicValue
            modelBuilder.Entity<CharacteristicValue>()
                .HasOne(cv => cv.Card)
                .WithMany(c => c.CharacteristicValues)
                .HasForeignKey(cv => cv.CardId);

            modelBuilder.Entity<CharacteristicValue>()
                .HasOne(cv => cv.Characteristic)
                .WithMany()
                .HasForeignKey(cv => cv.CharacteristicId);
        }
    }
}