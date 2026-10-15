using System;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Errors;
using ExpenseHub.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ExpenseHub.Api;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não configurada.");

        builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
        builder.Services.AddSingleton(TimeProvider.System);

        builder.Services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
            });

        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<ExpenseHubExceptionHandler>();
        builder.Services.AddOpenApi();

        builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();
        builder.Services.AddScoped<IExpenseService, ExpenseService>();

        WebApplication app = builder.Build();

        await DatabaseInitializer.InitializeAsync(app.Services, CancellationToken.None);

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseAuthorization();

        app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
            .WithName("GetHealth");
        app.MapControllers();

        await app.RunAsync();
    }
}
