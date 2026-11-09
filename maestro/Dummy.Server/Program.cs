using Dummy.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
//builder.Services.AddSingleton<DummyAmpService>();

var app = builder.Build();

app.MapGrpcService<ServerDummyService>();
//app.MapGrpcService<DummyAmpService>();

app.Run();
