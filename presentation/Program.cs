using System.Text.Json.Serialization;

// Builder
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
builder.Services.AddControllers().AddJsonOptions(
    o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Application
var app = builder.Build();
app.UseWebSockets();

// Endpoints
app.MapGet("/", () => "Hello, this is Presentation Processing Server of Tao Slide Tot Nghiep. Please read the docs for API usages.");

app.MapHealthChecks("/health");

app.Run();
