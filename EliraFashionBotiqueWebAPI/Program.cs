using EliraFashionBotiqueWebAPI.Services;
using EliraFashionBotiqueWebAPI.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// QuestPDF requires a license declaration before generating any PDFs (free Community license)
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

builder.Services.AddScoped<EliraFashionBotiqueWebAPI.Data.DapperContext>();
builder.Services.AddScoped<IReportService, ReportService>();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ===== CORS: allow the MVC frontend (running on localhost:7248) to call this API =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://localhost:7248")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS middleware must be added BEFORE Authorization/MapControllers
app.UseCors("AllowFrontend");

app.UseAuthorization();
app.MapControllers();
app.Run();