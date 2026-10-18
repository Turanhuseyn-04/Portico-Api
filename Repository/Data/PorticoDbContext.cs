using Microsoft.EntityFrameworkCore;
using Portico.Domain.Accounts;
using Portico.Domain.Content;
using Portico.Domain.Messages;
using Portico.Domain.Properties;
using Portico.Domain.Reservations;
using Portico.Domain.Platform;

namespace Repository.Data;

public class PorticoDbContext : DbContext
{
    public PorticoDbContext(DbContextOptions<PorticoDbContext> options)
        : base(options)
    {
    }

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<SavedStay> SavedStays => Set<SavedStay>();
    public DbSet<GuestMessage> Messages => Set<GuestMessage>();
    public DbSet<Slide> Slides => Set<Slide>();
    public DbSet<SiteVideo> Videos => Set<SiteVideo>();
    public DbSet<PropertyPhoto> Photos => Set<PropertyPhoto>();
    public DbSet<SiteSetting> Settings => Set<SiteSetting>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Property>(entity =>
        {
            entity.HasKey(property => property.Id);
            entity.Property(property => property.Id).HasMaxLength(40);
            entity.Property(property => property.Name).HasMaxLength(160);
            entity.Property(property => property.Type).HasMaxLength(40);
            entity.Property(property => property.Street).HasMaxLength(160);
            entity.Property(property => property.Area).HasMaxLength(80);
            entity.Property(property => property.City).HasMaxLength(80);
            entity.Property(property => property.Postcode).HasMaxLength(20);
            entity.Property(property => property.Country).HasMaxLength(80);
            entity.Property(property => property.Score).HasPrecision(4, 1);
            entity.Property(property => property.Summary).HasMaxLength(500);
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(room => new { room.PropertyId, room.Id });
            entity.Property(room => room.Id).HasMaxLength(40);
            entity.Property(room => room.PropertyId).HasMaxLength(40);
            entity.Property(room => room.Name).HasMaxLength(160);
            entity.Property(room => room.Image).HasMaxLength(300);
            entity.Property(room => room.Beds).HasMaxLength(120);
            entity.Property(room => room.Price).HasPrecision(10, 2);
            entity.HasOne(room => room.Property)
                .WithMany(property => property.Rooms)
                .HasForeignKey(room => room.PropertyId);
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.Property(review => review.PropertyId).HasMaxLength(40);
            entity.Property(review => review.RoomId).HasMaxLength(40);
            entity.Property(review => review.Author).HasMaxLength(80);
            entity.Property(review => review.Country).HasMaxLength(80);
            entity.Property(review => review.Traveller).HasMaxLength(40);
            entity.Property(review => review.Score).HasPrecision(4, 1);
            entity.Property(review => review.Title).HasMaxLength(160);
            entity.HasOne(review => review.Property)
                .WithMany(property => property.Reviews)
                .HasForeignKey(review => review.PropertyId);
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasIndex(reservation => reservation.Reference).IsUnique();
            entity.Property(reservation => reservation.Reference).HasMaxLength(20);
            entity.Property(reservation => reservation.PropertyId).HasMaxLength(40);
            entity.Property(reservation => reservation.PropertyName).HasMaxLength(160);
            entity.Property(reservation => reservation.City).HasMaxLength(80);
            entity.Property(reservation => reservation.Country).HasMaxLength(80);
            entity.Property(reservation => reservation.Area).HasMaxLength(80);
            entity.Property(reservation => reservation.Image).HasMaxLength(300);
            entity.Property(reservation => reservation.RoomId).HasMaxLength(40);
            entity.Property(reservation => reservation.RoomName).HasMaxLength(160);
            entity.Property(reservation => reservation.Name).HasMaxLength(120);
            entity.Property(reservation => reservation.Email).HasMaxLength(256);
            entity.Property(reservation => reservation.Currency).HasMaxLength(3).HasDefaultValue("EUR");
            entity.Property(reservation => reservation.Status).HasMaxLength(20).HasDefaultValue("Pending");
            entity.HasIndex(reservation => new { reservation.PropertyId, reservation.RoomId, reservation.CheckIn, reservation.CheckOut });
            entity.Property(reservation => reservation.Nightly).HasPrecision(10, 2);
            entity.Property(reservation => reservation.Total).HasPrecision(10, 2);
            entity.Property(reservation => reservation.Refund).HasPrecision(10, 2);
            entity.HasOne<Property>()
                .WithMany()
                .HasForeignKey(reservation => reservation.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasIndex(account => account.Email).IsUnique();
            entity.Property(account => account.Name).HasMaxLength(120);
            entity.Property(account => account.Email).HasMaxLength(256);
            entity.Property(account => account.PasswordHash).HasMaxLength(512);
            entity.Property(account => account.Role).HasMaxLength(20).HasDefaultValue(AccountRoles.Guest);
            entity.Property(account => account.ConfirmTokenHash).HasMaxLength(64);
        });

        modelBuilder.Entity<SiteSetting>(entity =>
        {
            entity.HasKey(setting => setting.Key);
            entity.Property(setting => setting.Key).HasMaxLength(60);
            entity.Property(setting => setting.Value).HasMaxLength(400);
            entity.HasData(SiteSettingKeys.Defaults.Select(pair => new SiteSetting { Key = pair.Key, Value = pair.Value }));
        });

        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.HasIndex(entry => entry.At);
            entity.Property(entry => entry.ActorEmail).HasMaxLength(256);
            entity.Property(entry => entry.ActorRole).HasMaxLength(20);
            entity.Property(entry => entry.Method).HasMaxLength(10);
            entity.Property(entry => entry.Path).HasMaxLength(400);
            entity.Property(entry => entry.Detail).HasMaxLength(400);
        });

        modelBuilder.Entity<SavedStay>(entity =>
        {
            entity.HasIndex(saved => new { saved.AccountId, saved.PropertyId }).IsUnique();
            entity.Property(saved => saved.PropertyId).HasMaxLength(40);
            entity.HasOne(saved => saved.Account)
                .WithMany(account => account.SavedStays)
                .HasForeignKey(saved => saved.AccountId);
            entity.HasOne(saved => saved.Property)
                .WithMany()
                .HasForeignKey(saved => saved.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GuestMessage>(entity =>
        {
            entity.ToTable("Messages");
            entity.Property(message => message.Name).HasMaxLength(120);
            entity.Property(message => message.Email).HasMaxLength(256);
            entity.Property(message => message.Stay).HasMaxLength(160);
            entity.Property(message => message.Body).HasColumnName("Message");
        });

        modelBuilder.Entity<Slide>(entity =>
        {
            entity.Property(slide => slide.City).HasMaxLength(80);
            entity.Property(slide => slide.Country).HasMaxLength(80);
            entity.Property(slide => slide.Image).HasMaxLength(300);
        });

        modelBuilder.Entity<SiteVideo>(entity =>
        {
            entity.ToTable("Videos");
            entity.Property(video => video.Src).HasMaxLength(300);
        });

        modelBuilder.Entity<PropertyPhoto>(entity =>
        {
            entity.Property(photo => photo.PropertyId).HasMaxLength(40);
            entity.Property(photo => photo.Src).HasMaxLength(300);
            entity.Property(photo => photo.Alt).HasMaxLength(200);
            entity.HasOne(photo => photo.Property)
                .WithMany(property => property.Photos)
                .HasForeignKey(photo => photo.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
