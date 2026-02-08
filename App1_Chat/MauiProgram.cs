using Microsoft.Extensions.Logging;

namespace App1_Chat
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            // constructor del MauiApp, donde se configuran las fuentes y otras opciones de la aplicación como que arranque en app.xaml.cs
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
