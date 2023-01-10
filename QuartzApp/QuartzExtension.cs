namespace QuartzApp
{
    public static class QuartzExtension
    {
        public static IServiceCollection SetupQuartz(this IServiceCollection services)
        {
            var loggerFactory = services.BuildServiceProvider().GetService<ILoggerFactory>();
            if (loggerFactory != null)
            {
                Quartz.Logging.LogContext.SetCurrentLogProvider(loggerFactory);
            }
            services.AddScoped<TestJob>();
            services.AddSingleton<IQuartzManager, QuartzManager>();
            services.AddHostedService<QuartzHostedService>();
            return services;
        }
    }
}
