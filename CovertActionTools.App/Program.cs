using CovertActionTools.App;
using CovertActionTools.App.Logging;
using CovertActionTools.App.ViewModels;
using CovertActionTools.App.Windows;
using CovertActionTools.Core;
using CovertActionTools.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

const string title = "CovertAction.Tools";
const int w = 1200;
const int h = 800;

//debug flags
#if DEBUG
//start by parsing the default
const bool startWithParsePublishDefault = true;
#endif

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

var sp = container.BuildServiceProvider();
var windows = windowTypes
    .Select(t => (BaseWindow)(sp.GetService(t) ?? throw new Exception("Null window")))
    .ToList();

//debug pre-set options
#if DEBUG
if (startWithParsePublishDefault)
{
    var parsePublishState = sp.GetRequiredService<ParsePublishedState>();
    parsePublishState.Show = true;
    var now = DateTime.Now;
    parsePublishState.SourcePath = Constants.DefaultParseSourcePath;
    var newName = $"package-{now:yyyy-MM-dd_HH-mm-ss}";
    parsePublishState.DestinationPath = Path.Combine(Constants.DefaultParseDestinationPath, newName);
    parsePublishState.Importer = sp.GetRequiredService<IImporterFactory>().Create(true);
    parsePublishState.Importer.StartImport(parsePublishState.SourcePath);
    parsePublishState.Exporter = sp.GetRequiredService<IExporterFactory>().Create();
    parsePublishState.Run = true;
}
#endif

var renderWindow = new RenderWindow(title, w, h);
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