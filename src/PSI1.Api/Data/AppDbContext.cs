using Microsoft.EntityFrameworkCore;

using PSI1.Api.Models;

namespace PSI1.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizScore> QuizScores => Set<QuizScore>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // email must be unique - also lets Register() check for duplicates efficiently
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // a question cannot exist without its quiz. deleting a quiz deletes its questions
        modelBuilder.Entity<Quiz>()
            .HasMany(q => q.Questions)
            .WithOne()
            .HasForeignKey("QuizId")
            .IsRequired();

        // one score per user per quiz. a score must point to an existing quiz
        modelBuilder.Entity<QuizScore>(b =>
        {
            b.HasIndex(s => new { s.UserId, s.QuizId }).IsUnique();
            b.HasOne<Quiz>().WithMany().HasForeignKey(s => s.QuizId);
            b.HasMany<QuestionAnswer>("answerRows").WithOne().HasForeignKey(a => a.QuizScoreId);
        });

        modelBuilder.Entity<QuestionAnswer>()
            .HasIndex(a => new { a.QuizScoreId, a.QuestionIndex })
            .IsUnique();

        // answer options are stored as rows in a child table. a row cannot exist without its question
        modelBuilder.Entity<Question>()
            .HasMany<AnswerOptionRow>("optionRows").WithOne().HasForeignKey("QuestionId")
            .IsRequired();
    }
}