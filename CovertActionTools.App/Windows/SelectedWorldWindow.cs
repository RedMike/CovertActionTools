using System.Numerics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Models;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class SelectedWorldWindow : BaseWindow
{
    private readonly ILogger<SelectedWorldWindow> _logger;
    private readonly MainEditorState _mainEditorState;
    private readonly RenderWindow _renderWindow;

    public SelectedWorldWindow(ILogger<SelectedWorldWindow> logger, MainEditorState mainEditorState, RenderWindow renderWindow)
    {
        _logger = logger;
        _mainEditorState = mainEditorState;
        _renderWindow = renderWindow;
    }

    public override void Draw()
    {
        if (!_mainEditorState.IsPackageLoaded)
        {
            return;
        }

        if (_mainEditorState.SelectedItem == null ||
            _mainEditorState.SelectedItem.Value.type != MainEditorState.ItemType.World)
        {
            return;
        }

        var key = int.Parse(_mainEditorState.SelectedItem.Value.id);
        
        var screenSize = ImGui.GetMainViewport().Size;
        var initialPos = new Vector2(300.0f, 20.0f);
        var initialSize = new Vector2(screenSize.X - 300.0f, screenSize.Y - 200.0f);
        ImGui.SetNextWindowSize(initialSize);
        ImGui.SetNextWindowPos(initialPos);
        ImGui.Begin($"World {key}", //TODO: change label but not ID to prevent unfocusing
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoNav | 
            ImGuiWindowFlags.NoCollapse);
        
        if (_mainEditorState.LoadedPackage != null)
        {
            var model = _mainEditorState.LoadedPackage;
            if (model.Worlds.TryGetValue(key, out var world))
            {
                DrawWorldWindow(model, world);
            }
            else
            {
                ImGui.Text("Something went wrong, world is missing..");
            }
        }
        else
        {
            ImGui.Text("Something went wrong, no package loaded..");
        }

        ImGui.End();
    }

    private void DrawWorldWindow(PackageModel model, WorldModel world)
    {
        //TODO: keep a pending model and have a save button?
        var origId = world.Id;
        var id = origId;
        ImGui.SetNextItemWidth(100.0f);
        ImGui.InputInt("ID", ref id);
        if (id != origId)
        {
            if (id < 0 || id > 2)
            {
                ImGui.SameLine();
                ImGui.Text("Only world 0-2 are supported");
            }
            else if (model.Worlds.ContainsKey(id))
            {
                ImGui.SameLine();
                ImGui.Text("Key already taken");
            }
            else
            {
                //TODO: change ID?    
            }
        }

        var origName = world.ExtraData.Name;
        var name = origName;
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200.0f);
        ImGui.InputText("Name", ref name, 64);
        if (name != origName)
        {
            world.ExtraData.Name = name;
        }
        
        var windowSize = ImGui.GetContentRegionAvail();
        var origComment = world.ExtraData.Comment;
        var comment = origComment;
        ImGui.InputTextMultiline("Comment", ref comment, 2048, new Vector2(windowSize.X, 50.0f));
        if (comment != origComment)
        {
            world.ExtraData.Comment = comment;
        }

        var cursorPos = ImGui.GetCursorPos();

        ImGui.BeginChild("Editor Fields", new Vector2(windowSize.X / 2.0f, windowSize.Y - cursorPos.Y));

        if (ImGui.CollapsingHeader("Cities"))
        {
            ImGui.SetNextItemWidth(200.0f);
            if (ImGui.Button("Add City"))
            {
                //TODO: add city
            }

            if (world.Cities.Count >= 16)
            {
                ImGui.SameLine();
                ImGui.Text("Already have 16 cities, cannot add more");
            }

            var i = 0;
            foreach (var city in world.Cities)
            {
                i++;
                var cityName = city.Name;
                var origCityName = cityName;
                ImGui.SetNextItemWidth(100.0f);
                ImGui.InputText($"Name {i}", ref cityName, 12);
                if (cityName != origCityName)
                {
                    city.Name = cityName;
                }

                var country = city.Country;
                var origCountry = country;
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100.0f);
                ImGui.InputText($"Country {i}", ref country, 12);
                if (country != origCountry)
                {
                    city.Country = country;
                }

                var x = city.MapX;
                var origX = x;
                ImGui.SetNextItemWidth(100.0f);
                ImGui.InputInt($"X {i}", ref x);
                if (x != origX)
                {
                    city.MapX = x;
                }
                
                var y = city.MapY;
                var origY = y;
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100.0f);
                ImGui.InputInt($"Y {i}", ref y);
                if (y != origY)
                {
                    city.MapY = y;
                }
                
                var u1 = city.Unknown1;
                var origU1 = u1;
                ImGui.SetNextItemWidth(100.0f);
                ImGui.InputInt($"U1 {i}", ref u1);
                if (u1 != origU1)
                {
                    city.Unknown1 = u1;
                }
                
                var u2 = city.Unknown2;
                var origU2 = u2;
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100.0f);
                ImGui.InputInt($"U2 {i}", ref u2);
                if (u2 != origU2)
                {
                    city.Unknown2 = u2;
                }

                ImGui.Separator();
            }
        }
        ImGui.EndChild();
        ImGui.SetCursorPos(new Vector2(cursorPos.X + windowSize.X / 2.0f, cursorPos.Y));
        ImGui.BeginChild("Preview", new Vector2(windowSize.X / 2.0f, windowSize.Y - cursorPos.Y), true);

        var pos = ImGui.GetCursorPos();
        var imageKey = "EUROPE";
        if (world.Id == 1)
        {
            imageKey = "AFRICA";
        } else if (world.Id == 2)
        {
            imageKey = "CENTRAL";
        }

        if (model.SimpleImages.TryGetValue(imageKey, out var image))
        {
            var imageId = $"image_preview_{imageKey}";
            //TODO: cache?
            var texture = _renderWindow.RenderImage(RenderWindow.RenderType.Image, imageId, image.Width, image.Height,
                image.VgaImageData);
            ImGui.Image(texture, new Vector2(image.Width, image.Height));
        }
        else
        {
            ImGui.Text($"No matching image for: {imageKey}");
        }

        foreach (var city in world.Cities)
        {
            ImGui.SetCursorPos(pos + new Vector2(city.MapX, city.MapY - 10.0f));
            ImGui.Text($".{city.Name}");
        }
        
        ImGui.EndChild();
    }
}