using CovertActionTools.App;

const string title = "CovertAction.Tools";
const int w = 800;
const int h = 600;

var renderWindow = new RenderWindow(title, w, h);
while (renderWindow.IsOpen())
{
    renderWindow.ProcessInput();
    if (!renderWindow.IsOpen())
    {
        break;
    }
    
    //TODO: draw things

    renderWindow.Render();
}