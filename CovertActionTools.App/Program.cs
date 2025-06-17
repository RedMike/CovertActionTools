using CovertActionTools.App;
using CovertActionTools.App.Logging;
using CovertActionTools.App.ViewModels;
using CovertActionTools.App.Windows;
using CovertActionTools.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

const string title = "CovertAction.Tools";
const int w = 1200;
const int h = 800;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();

IServiceCollection container = new ServiceCollection();

container.AddSingleton(configuration);

//set up basic console logger
container.AddLogging(o => o
    .AddConfiguration(configuration.GetRequiredSection("Logging"))
    .AddConsole()
);
//also add custom logger
container.AddSingleton<ILoggerProvider, AppLoggerProvider>();

//register core
container.AddCovertActionsTools();
//register all types
var viewModelTypes = AppDomain.CurrentDomain
    .GetAssemblies()
    .Where(x => !x.IsDynamic)
    .SelectMany(a => a.GetTypes()
        .Where(t => t.IsClass &&
            !t.IsAbstract &&
            !t.ContainsGenericParameters &&
            typeof(IViewModel).IsAssignableFrom(t)
        )
    ).ToList();
foreach (var type in viewModelTypes)
{
    container.AddSingleton(type);
}
var windowTypes = AppDomain.CurrentDomain
    .GetAssemblies()
    .Where(x => !x.IsDynamic)
    .SelectMany(a => a.GetTypes()
        .Where(t => t.IsClass &&
                    !t.IsAbstract &&
                    !t.ContainsGenericParameters &&
                    typeof(BaseWindow).IsAssignableFrom(t)
        )
    ).ToList();
foreach (var type in windowTypes)
{
    container.AddSingleton(type);
}

var renderWindow = new RenderWindow(title, w, h);
container.AddSingleton<RenderWindow>(renderWindow);

var sp = container.BuildServiceProvider();
var windows = windowTypes
    .Select(t => (BaseWindow)(sp.GetService(t) ?? throw new Exception("Null window")))
    .ToList();

while (renderWindow.IsOpen())
{
    renderWindow.ProcessInput();
    if (!renderWindow.IsOpen())
    {
        break;
    }

    foreach (var window in windows)
    {
        window.Draw();
    }

    renderWindow.Render();
}