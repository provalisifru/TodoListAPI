using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TodoList.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace TodoList.Data;

public partial class TodoListContext : DbContext
{
    public TodoListContext(DbContextOptions<TodoListContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TodoList.Models.Task> Tasks { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var guidToBytesConverter = new ValueConverter<Guid, byte[]>(
        guid => guid.ToByteArray(),
        bytes => new Guid(bytes));


        modelBuilder
            .UseCollation("utf8mb3_general_ci")
            .HasCharSet("utf8mb3");

        modelBuilder.Entity<TodoList.Models.Task>(entity =>
        {
            entity.HasKey(e => e.TaskId).HasName("PRIMARY");

            entity.ToTable("tasks");

            entity.HasIndex(e => e.UserId, "userId_idx");

            entity.Property(e => e.TaskId)
           .HasColumnName("taskId")
           .HasColumnType("binary(16)")
           .HasConversion(guidToBytesConverter)
           .ValueGeneratedNever();
            entity.Property(e => e.IsCompleted).HasColumnName("isCompleted");
            entity.Property(e => e.TaskCategory)
                .HasMaxLength(45)
                .HasColumnName("taskCategory");
            entity.Property(e => e.TaskName)
                .HasMaxLength(45)
                .HasColumnName("taskName");
            entity.Property(e => e.TaskDescription)
               .HasMaxLength(100)
               .HasColumnName("taskDescription");
            entity.Property(e => e.UserId).HasColumnName("userId").HasColumnType("binary(16)")
           .HasConversion(guidToBytesConverter)
           .ValueGeneratedNever(); ;
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PRIMARY");

            entity.ToTable("users");

            entity.Property(e => e.UserId)
           .HasColumnName("userId")
           .HasColumnType("binary(16)")
           .HasConversion(guidToBytesConverter)
           .ValueGeneratedNever();
            entity.Property(e => e.Password)
                .HasMaxLength(45)
                .HasColumnName("password");
            entity.Property(e => e.Username)
                .HasMaxLength(45)
                .HasColumnName("username");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
