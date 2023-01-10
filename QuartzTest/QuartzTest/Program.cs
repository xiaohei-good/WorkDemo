using Quartz;
using QuartzTest;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// await builder.InitialQuartz();
builder.Services.AddHostedService<HostedService>();
builder.Services.AddSingleton<QuartzManager>();

// builder.Services.AddQuartzHostedService(q =>
// {
//     q.WaitForJobsToComplete = true;
//     q.AwaitApplicationStarted = true;
// });
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