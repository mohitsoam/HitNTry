using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using poc_1.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace poc_1
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddSingleton<WeatherForecastService>();

            //// Add plugin dependencies
            //services.AddSingleton<IDbConnection>(sp =>
            //{
            //    var config = sp.GetRequiredService<IConfiguration>();
            //    return new SqlConnection(config["Database:ConnectionString"]);
            //});

            Batteries.Init(); // ← IMPORTANT: add this FIRST

            services.AddSingleton<IDbConnection>(sp =>
            {
                // Shared in-memory SQLite DB
                var conn = new SqliteConnection("Data Source=:memory:;Cache=Shared");

                conn.Open(); // Must remain open to keep in-memory DB alive

                // Optional: Create sample schema & data
                using var cmd = conn.CreateCommand();
                cmd.CommandText =
                @"
        CREATE TABLE IF NOT EXISTS Users (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL
        );

        INSERT INTO Users (Name) VALUES ('Alice'), ('Bob'), ('Charlie');
    ";
                cmd.ExecuteNonQuery();

                return conn;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
