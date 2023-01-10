using Quartz;
using Quartz.Logging;
using SecondQuartzTest;
using Serilog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SecondQuartzTest.Data;

var builder = WebApplication.CreateBuilder(args);
//builder.Services.AddDbContext<SecondQuartzTestContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("SecondQuartzTestContext") ?? throw new InvalidOperationException("Connection string 'SecondQuartzTestContext' not found.")));

//builder.Services.AddDbContext<SecondQuartzTestContext>(options =>
//    options.UseMySql(builder.Configuration.GetConnectionString("SecondQuartzTestContext")));

var connectionString =
    builder.Configuration.GetConnectionString(SecondQuartzTestContext.ConnectionString);

builder.Services.AddDbContext<SecondQuartzTestContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));


// Add services to the container.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
// LogProvider.SetCurrentLogProvider(new ConsoleLogProvider());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();




builder.Services.AddQuartz();
builder.Services.AddSingleton<QuartzManager>();
builder.Services.AddSingleton<MyJob>();
builder.Services.AddHostedService<QuartzHostedService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();