//Creates builder object that is used to configure the app before start
var builder = WebApplication.CreateBuilder(args);

// Add services to the container for class AuthController to handle HTTP req
builder.Services.AddControllers();

//Finalises config and builds obj
var app = builder.Build();

app.MapControllers();

app.Run();