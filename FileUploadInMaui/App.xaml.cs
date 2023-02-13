namespace FileUploadInMaui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new AppShell();
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        var window = base.CreateWindow(activationState);
#if WINDOWS
        // from https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/windows?view=net-maui-7.0
		window.Width = 720;
		window.Height= 600;
#endif
        return window;
    }
}
