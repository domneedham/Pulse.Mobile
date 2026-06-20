using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Nalu;
using Pulse.Services;
using Pulse.Services.Api;
using Pulse.Services.Auth;
using Pulse.Services.Logging;
using Pulse.ViewModels;
using Pulse.Views;
using Pulse.Views.Onboarding;

namespace Pulse;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();

        // Strip the native text-field border from every Entry on iOS.
#if IOS
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping(
            nameof(IEntry),
            (handler, _) => handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None);

        // DatePicker renders as a UITextField on iOS with its own border/background, which double-boxes
        // it inside our InputField Border. Strip them so it sits flush like a plain field.
        Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping(
            nameof(IDatePicker),
            (handler, _) =>
            {
                handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
                handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
            });

        // Disable the iOS left-edge swipe-back gesture app-wide. Every page hides the
        // nav bar and drives navigation through its own in-page back/close buttons (which
        // go through Nalu). The native interactive pop gesture bypasses Nalu's stack, so a
        // swipe can pop the Shell out from under it and leave a blank page — so we turn it off.
        Microsoft.Maui.Handlers.PageHandler.Mapper.AppendToMapping(
            "DisableInteractivePopGesture",
            (handler, _) =>
            {
                var navController = (handler as IPlatformViewHandler)?.ViewController?.NavigationController;
                if (navController?.InteractivePopGestureRecognizer is { } gesture)
                {
                    gesture.Enabled = false;
                }
            });
#endif

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseNaluLayouts()
            .UseNaluTabBar()
            .UseNaluNavigation<App>(nav => nav
                .AddPage<StartupViewModel, StartupView>()
                .AddPage<SignInViewModel, SignInView>()
                .AddPage<SignUpViewModel, SignUpView>()
                .AddPage<ProfileSetupViewModel, ProfileSetupView>()
                .AddPage<ResetPasswordViewModel, ResetPasswordView>()
                .AddPage<HomeViewModel, HomeView>()
                .AddPage<EditProfileViewModel, EditProfileView>()
                .AddPage<AppSettingsViewModel, AppSettingsView>()
                .AddPage<HelpSupportViewModel, HelpSupportView>()
                .AddPage<PromptSheetViewModel, PromptSheetView>()
            )
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("materialdesignicons-webfont.ttf", "mdi");
                fonts.AddFont("NotoSerif-Thin.ttf", "SerifThin");
                fonts.AddFont("NotoSerif-ThinItalic.ttf", "SerifThinItalic");
                fonts.AddFont("NotoSerif-ExtraLight.ttf", "SerifExtraLight");
                fonts.AddFont("NotoSerif-ExtraLightItalic.ttf", "SerifExtraLightItalic");
                fonts.AddFont("NotoSerif-Light.ttf", "SerifLight");
                fonts.AddFont("NotoSerif-LightItalic.ttf", "SerifLightItalic");
                fonts.AddFont("NotoSerif-Regular.ttf", "SerifRegular");
                fonts.AddFont("NotoSerif-Italic.ttf", "SerifItalic");
                fonts.AddFont("NotoSerif-Medium.ttf", "SerifMedium");
                fonts.AddFont("NotoSerif-MediumItalic.ttf", "SerifMediumItalic");
                fonts.AddFont("NotoSerif-SemiBold.ttf", "SerifSemiBold");
                fonts.AddFont("NotoSerif-SemiBoldItalic.ttf", "SerifSemiBoldItalic");
                fonts.AddFont("NotoSerif-Bold.ttf", "SerifBold");
                fonts.AddFont("NotoSerif-BoldItalic.ttf", "SerifBoldItalic");
                fonts.AddFont("NotoSerif-ExtraBold.ttf", "SerifExtraBold");
                fonts.AddFont("NotoSerif-ExtraBoldItalic.ttf", "SerifExtraBoldItalic");
                fonts.AddFont("NotoSerif-Black.ttf", "SerifBlack");
                fonts.AddFont("NotoSerif-BlackItalic.ttf", "SerifBlackItalic");
                fonts.AddFont("Nunito-ExtraLight.ttf", "SansExtraLight");
                fonts.AddFont("Nunito-Light.ttf", "SansLight");
                fonts.AddFont("Nunito-Regular.ttf", "SansRegular");
                fonts.AddFont("Nunito-Medium.ttf", "SansMedium");
                fonts.AddFont("Nunito-SemiBold.ttf", "SansSemiBold");
                fonts.AddFont("Nunito-Bold.ttf", "SansBold");
                fonts.AddFont("Nunito-ExtraBold.ttf", "SansExtraBold");
                fonts.AddFont("Nunito-Black.ttf", "SansBlack");
                fonts.AddFont("Nunito-Italic.ttf", "SansItalic");
                fonts.AddFont("Nunito-LightItalic.ttf", "SansLightItalic");
                fonts.AddFont("Nunito-MediumItalic.ttf", "SansMediumItalic");
                fonts.AddFont("Nunito-SemiBoldItalic.ttf", "SansSemiBoldItalic");
                fonts.AddFont("Nunito-BoldItalic.ttf", "SansBoldItalic");
            });

        // Infrastructure
        builder.Services.AddSingleton<IAuthService, AuthService>();

        // The API client gets its HttpClient from IHttpClientFactory: base address set here, with an
        // AuthHandler that attaches the bearer token and handles the 401-refresh-retry for every call.
        builder.Services.AddTransient<AuthHandler>();
        builder.Services.AddHttpClient<IPulseApiClient, PulseApiClient>(client =>
                client.BaseAddress = new Uri($"{AppConfig.ApiBaseUrl}/api/"))
            .AddHttpMessageHandler<AuthHandler>();

        // App services
        builder.Services.AddSingleton<IAlertService, AlertService>();
        builder.Services.AddSingleton<ILogService, LogService>();
        builder.Services.AddSingleton<IThemeService, ThemeService>();
        builder.Services.AddSingleton<HapticService>();
        builder.Services.AddSingleton<UserSession>();

        // File logging: one shared LogStore feeds the provider (so all ILogger<T> output lands in
        // a rolling file) and ILogService (so the file can be viewed / shared / emailed in-app).
        var logStore = new LogStore();
        builder.Services.AddSingleton(logStore);
        builder.Logging.AddProvider(new FileLoggerProvider(logStore, LogLevel.Information));
        builder.Logging.SetMinimumLevel(LogLevel.Information);

#if DEBUG
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif

        return builder.Build();
    }
}
