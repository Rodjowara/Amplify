using Microsoft.Extensions.DependencyInjection;

namespace Maestro.Desktop.ViewModels;

// TODO: separate file
public static class MaestroServices
{
    public static IServiceProvider ServiceProvider { get; set; } = null!;
}

public class BaseViewModel
{
    protected static T GetRequiredService<T>() where T : notnull
    {
        var provider = MaestroServices.ServiceProvider;
        return provider.GetRequiredService<T>();
    }
}
