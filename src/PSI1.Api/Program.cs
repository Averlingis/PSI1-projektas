using System.Text;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using PSI1.Api.Data;
using PSI1.Api.Models;


//Creates builder object that is used to configure the app before start
var builder = WebApplication.CreateBuilder(args);

// Add services to the container for class AuthController to handle HTTP req
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize/deserialize enums (e.g. Language) as their string names
        // ("Lithuanian") instead of raw integers, so clients don't need to
        // know the underlying numeric values.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

// Allow the React server to call this API from the browser.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactDev", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

//Finalises config and builds obj
var app = builder.Build();

app.UseCors("AllowReactDev");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// one time used block to add something to db, normal integration of posting quizes is another task
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!db.Quizzes.Any())
    {
        db.Quizzes.Add(new Quiz("Italian food", Category.Food, Language.Italian,
        [
            new Question("Which pasta is long and thin?", [new AnswerOption("Spaghetti", true), new AnswerOption("Penne", false)]),
            new Question("Where is pizza from?", [new AnswerOption("Naples", true), new AnswerOption("Paris", false)]),
            new Question("Main ingredient of pesto?", [new AnswerOption("Basil", true), new AnswerOption("Mint", false)]),
        ]));
        db.SaveChanges();
    }
}

app.Run();