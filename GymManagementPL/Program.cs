using GymManagementPL.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGymApplication(builder.Configuration, builder.Environment);

var app = builder.Build();

await app.MigrateAndSeedAsync();
app.UseGymPipeline();

app.Run();

// Exposed for integration tests.
public partial class Program;
