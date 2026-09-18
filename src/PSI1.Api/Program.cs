using Microsoft.EntityFrameworkCore;
using PSI1.Api.Data;


//Creates builder object that is used to configure the app before start
var builder = WebApplication.CreateBuilder(args);

// Add services to the container for class AuthController to handle HTTP req
builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));


//Finalises config and builds obj
var app = builder.Build();

app.MapControllers();

app.Run();