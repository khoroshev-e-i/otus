using Api;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddScoped<DatabaseConnectionProvider>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<DatabaseMigrator>();

var app = builder.Build();

await ApplyMigrations(app.Services);


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();



async Task ApplyMigrations(IServiceProvider appServices)
{
    using var scope = appServices.CreateScope();
    await scope.ServiceProvider.GetService<DatabaseMigrator>().ApplyMigrations();
}