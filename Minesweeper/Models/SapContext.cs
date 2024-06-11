using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Minesweeper.Models;

public partial class SapContext : DbContext
{
    public SapContext()
    {
    }

    public SapContext(DbContextOptions<SapContext> options)
        : base(options)
    {
    }

    public virtual DbSet<FieldForUser> FieldForUsers { get; set; }

    public virtual DbSet<GameInfoResponse> GameInfoResponses { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=ngknn.ru;Port=5442;Database=sap;Username=33P;Password=12345");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FieldForUser>(entity =>
        {
            entity.HasKey(e => e.GameId).HasName("field_for_user_pk");

            entity.ToTable("field_for_user");

            entity.Property(e => e.GameId)
                .ValueGeneratedNever()
                .HasColumnName("game_id");
            entity.Property(e => e.Field)
                .HasColumnType("character varying[]")
                .HasColumnName("field");
        });

        modelBuilder.Entity<GameInfoResponse>(entity =>
        {
            entity.HasKey(e => e.GameId).HasName("game_info_response_pk");

            entity.ToTable("game_info_response");

            entity.Property(e => e.GameId)
                .ValueGeneratedNever()
                .HasColumnName("game_id");
            entity.Property(e => e.Completed).HasColumnName("completed");
            entity.Property(e => e.Field)
                .HasColumnType("character varying[]")
                .HasColumnName("field");
            entity.Property(e => e.Height).HasColumnName("height");
            entity.Property(e => e.MinesCount).HasColumnName("mines_count");
            entity.Property(e => e.Width).HasColumnName("width");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
