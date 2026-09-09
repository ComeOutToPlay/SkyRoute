using System.Text.Json.Serialization;
using SkyRoute.Application.Services;
using SkyRoute.Infrastructure;
using SkyRoute.WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

const string AllowLocalAngularPolicy = "AllowLocalAngular";
builder.Services.AddCors(options =>
{
    // Without this, every call from the Angular dev server (Phase 6+) fails
    // (docs/02-revision.md's "Dev CORS policy" note).
    options.AddPolicy(AllowLocalAngularPolicy, policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

builder.Services.AddInfrastructure();

// Application services registered as concrete classes — no IFlightSearchService/
// IBookingService interface, since each has exactly one implementation and one consumer
// (docs/02-revision.md's explicit "no unnecessary abstractions" decision).
builder.Services.AddScoped<FlightSearchService>();
builder.Services.AddScoped<BookingService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// HTTP only in dev, to avoid dev-certificate friction (docs/02-revision.md).
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(AllowLocalAngularPolicy);

app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory-based integration tests (Phase 9, if time allows).
public partial class Program;
