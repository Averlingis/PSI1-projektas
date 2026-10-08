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
        // User
        // email must be unique - also lets Register() check for duplicates efficiently
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // Quiz
        modelBuilder.Entity<Quiz>(b =>
        {
            b.Property(q => q.Title).HasMaxLength(Quiz.MaxTitleLength);
            b.Property(q => q.Description).HasMaxLength(Quiz.MaxDescriptionLength);

            // Questions is computed (ordered by Position), so it is not mapped
            b.Ignore(q => q.Questions);

            // a question cannot exist without its quiz. deleting a quiz deletes its questions
            b.HasMany<Question>("questions").WithOne().HasForeignKey("QuizId").IsRequired();
        });

        // Question
        modelBuilder.Entity<Question>(b =>
        {
            b.ToTable("Questions");
            b.Property(q => q.QuestionText).HasMaxLength(Question.MaxQuestionTextLength);

            b.HasIndex("QuizId", nameof(Question.Position)).IsUnique();

            // Options is computed from optionRows, so it is not mapped
            b.Ignore(q => q.Options);

            // answer options are stored as rows in a child table. a row cannot exist without its question
            b.HasMany<AnswerOptionRow>("optionRows").WithOne().HasForeignKey("QuestionId").IsRequired();
            b.Navigation("optionRows").AutoInclude();
        });

        // AnswerOptionRow
        modelBuilder.Entity<AnswerOptionRow>(b =>
        {
            b.ToTable("AnswerOptions");
            b.Property(a => a.OptionText).HasMaxLength(AnswerOption.MaxOptionTextLength);
        });

        // QuizScore
        // one score per user per quiz. a score must point to an existing quiz and user. deleting either deletes the score
        modelBuilder.Entity<QuizScore>(b =>
        {
            b.HasIndex(s => new { s.UserId, s.QuizId }).IsUnique();
            b.HasOne<Quiz>().WithMany().HasForeignKey(s => s.QuizId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne<User>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);

            // Answers is computed from answerRows, so it is not mapped
            b.Ignore(s => s.Answers);

            b.HasMany<QuestionAnswer>("answerRows").WithOne().HasForeignKey(a => a.QuizScoreId);
            b.Navigation("answerRows").AutoInclude();
        });

        // QuestionAnswer
        modelBuilder.Entity<QuestionAnswer>(b =>
        {
            b.ToTable("QuestionAnswers");
            b.HasIndex(a => new { a.QuizScoreId, a.QuestionIndex }).IsUnique();
        });
    }
}