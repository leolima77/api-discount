using ApiDiscount.Database;
using ApiDiscount.Database.Interfaces;
using ApiDiscount.Database.Repositories;
using ApiDiscount.Helpers;
using ApiDiscount.Services.Interfaces;
using ApiDiscount.Services.Repositories;
using Serilog;

namespace ApiDiscount
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // initiallize serilog
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            // create builder
            var builder = WebApplication.CreateBuilder(args);

            // write to console
            builder.Host.UseSerilog((ctx, sp, cfg) =>
                cfg.ReadFrom.Configuration(ctx.Configuration)
                   .Enrich.FromLogContext()
                   .Enrich.WithProperty("Env", ctx.HostingEnvironment.EnvironmentName)
                   .WriteTo.Console());

            // Add services to the container.
            builder.Services.AddGrpc();

            // add service routing
            builder.Services.AddRouting();

            // get connection string from configuration
            var cs = builder.Configuration["ConnectionStrings:Postgres"]!;

            // adding dapper to context
            builder.Services.AddSingleton(new DapperContext(cs));

            // testing connection with postgres
            builder.Services.AddHealthChecks().AddNpgSql(cs, name: "postgres");

            builder.Services.AddScoped<IDiscountCodeRepository, DiscountCodeRepository>();
            builder.Services.AddScoped<IDiscountCodeService, DiscountCodeService>();
            builder.Services.AddSingleton<CodeGenerator>();

#if DEBUG
            builder.Services.AddGrpcReflection();
#endif

            var app = builder.Build();


            // adding health check
            app.MapHealthChecks("/health");

            // Configure the HTTP request pipeline.
            app.MapGrpcService<ApiDiscount.Services.Grpc.DiscountServiceGrpc>();

            // adding graceful shutdown for prevent issues on pod scale down or scale up
            app.Lifetime.ApplicationStopping.Register(() =>
            {
                Serilog.Log.Information("Stopping application.");
            });

            app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

            // run application
            app.Run();
        }
    }
}