using Kot.MongoDB.Migrations;
using Kot.MongoDB.Migrations.DI;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMongoMigrations(
    builder.Configuration.GetConnectionString("Mongo"),
    new MigrationOptions(builder.Configuration["DbName"]),
    x => x.LoadMigrationsFromCurrentDomain());

builder.Host.UseSerilog((_, logging) =>
{
    logging.MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Warning)
        .WriteTo.File("log.txt")
        .WriteTo.Console(Serilog.Events.LogEventLevel.Information);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

var migrator = app.Services.GetRequiredService<IMigrator>();
await migrator.MigrateAsync();

app.Run();
