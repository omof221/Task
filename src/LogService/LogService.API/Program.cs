using LogService.Application.Extensions;
using LogService.Infrastructure.Extensions;
using Serilog;
using Serilog.Events;
using Shared.Infrastructure.Middleware;

// ── Serilog bootstrap (sadece Console — Seq/ELK bağlantısı henüz yok) ────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog tam konfigürasyonu ─────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, cfg) =>
    {
        var seqUrl     = ctx.Configuration["Seq:Url"]           ?? "http://localhost:5341";
        var elasticUrl = ctx.Configuration["Elastic:Url"]       ?? "http://localhost:9200";
        var elasticIdx = ctx.Configuration["Elastic:IndexName"] ?? "logservice-{0:yyyy.MM}";

        // Her zaman Console'a yaz
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext()
           .WriteTo.Console();

        // Seq ayakta mı? URI geçerli mi? → sadece o zaman ekle
        if (Uri.TryCreate(seqUrl, UriKind.Absolute, out _))
        {
            cfg.WriteTo.Seq(seqUrl, restrictedToMinimumLevel: LogEventLevel.Information);
        }

        // Elasticsearch URI geçerli mi? → sadece o zaman ekle
        if (Uri.TryCreate(elasticUrl, UriKind.Absolute, out _))
        {
            cfg.WriteTo.Elasticsearch(elasticUrl,
                indexFormat: elasticIdx,
                restrictedToMinimumLevel: LogEventLevel.Error);
        }
    });

    // ── DI kayıtları ───────────────────────────────────────────────────────
    var isTesting = builder.Environment.IsEnvironment("Testing");

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(o =>
    {
        o.SwaggerDoc("v1", new() { Title = "LogService API", Version = "v1" });
    });

    builder.Services.AddHealthChecks();

    builder.Services.AddLogApplication();
    builder.Services.AddLogInfrastructure(builder.Configuration, isTesting);

    // ── Pipeline ───────────────────────────────────────────────────────────
    var app = builder.Build();

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LogService v1"));
    }

    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "LogService başlatılamadı.");
}
finally
{
    Log.CloseAndFlush();
}

// WebApplicationFactory için erişilebilir Program sınıfı
public partial class Program { }
