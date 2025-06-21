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
    private readonly PendingEditorWorldState _pendingState;

    public SelectedWorldWindow(ILogger<SelectedWorldWindow> logger, MainEditorState mainEditorState, RenderWindow renderWindow, PendingEditorWorldState pendingState)
    {
        _logger = logger;
        _mainEditorState = mainEditorState;
        _renderWindow = renderWindow;
        _pendingState = pendingState;
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
        ImGui.Begin("World",
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoNav | 
            ImGuiWindowFlags.NoCollapse);
        
        if (_mainEditorState.LoadedPackage != null)
        {
            var model = _mainEditorState.LoadedPackage;
            DrawWorldWindow(model, key);
        }
        else
        {
            ImGui.Text("Something went wrong, no package loaded..");
        }

        ImGui.End();
    }

    private void DrawWorldWindow(PackageModel model, int key)
    {
        if (!model.Worlds.ContainsKey(key))
        {
            ImGui.Text("Something went wrong, missing world");
            return;
        }
        var world = ImGuiExtensions.PendingSaveChanges(_pendingState, key.ToString(),
            () => model.Worlds[key].Clone(),
            (data) =>
            {
                model.Worlds[key] = data;
                _mainEditorState.RecordChange();
                if (model.Index.WorldChanges.Add(key))
                {
                    model.Index.WorldIncluded.Add(key);
                }
            });
        
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
        
        DrawSharedMetadataEditor(world.Metadata, () => { _pendingState.RecordChange(); });

        var windowSize = ImGui.GetContentRegionAvail();
        var cursorPos = ImGui.GetCursorPos();

        ImGui.BeginChild("Editor Fields", new Vector2(windowSize.X / 2.0f, windowSize.Y - cursorPos.Y));

        if (ImGui.CollapsingHeader("Cities"))
        {
            ImGui.SetNextItemWidth(200.0f);
            if (ImGui.Button("Add City"))
            {
                if (world.Cities.Count < 16)
                {   
                    world.Cities.Add(new WorldModel.City()
                    {
                        Name = "X",
                        Country = "X",
                        MapX = 0,
                        MapY = 0,
                        Unknown1 = 0,
                        Unknown2 = 0
                    });
                    _pendingState.RecordChange();
                }
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
                    _pendingState.RecordChange();
                }

                var country = city.Country;
                var origCountry = country;
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100.0f);
                ImGui.InputText($"Country {i}", ref country, 12);
                if (country != origCountry)
                {
                    city.Country = country;
                    _pendingState.RecordChange();
                }

                var x = city.MapX;
                var origX = x;
                ImGui.SetNextItemWidth(100.0f);
                ImGui.InputInt($"X {i}", ref x);
                if (x != origX)
                {
                    city.MapX = x;
                    _pendingState.RecordChange();
                }
                
                var y = city.MapY;
                var origY = y;
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100.0f);
                ImGui.InputInt($"Y {i}", ref y);
                if (y != origY)
                {
                    city.MapY = y;
                    _pendingState.RecordChange();
                }
                
                var u1 = city.Unknown1;
                var origU1 = u1;
                ImGui.SetNextItemWidth(100.0f);
                ImGui.InputInt($"U1 {i}", ref u1);
                if (u1 != origU1)
                {
                    city.Unknown1 = u1;
                    _pendingState.RecordChange();
                }
                
                var u2 = city.Unknown2;
                var origU2 = u2;
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100.0f);
                ImGui.InputInt($"U2 {i}", ref u2);
                if (u2 != origU2)
                {
                    city.Unknown2 = u2;
                    _pendingState.RecordChange();
                }

                ImGui.Separator();
            }
        }

        if (ImGui.CollapsingHeader("Organisations"))
        {
            ImGui.SetNextItemWidth(200.0f);
            if (ImGui.Button("Add Organization"))
            {
                if (world.Organisations.Count < 16)
                {   
                    world.Organisations.Add(new WorldModel.Organisation()
                    {
                        ShortName = "X",
                        LongName = "X",
                        Unknown1 = 0,
                        Unknown2 = 0,
                        UniqueId = 0xFF,
                        Unknown3 = 0,
                        Unknown4 = 0
                    });
                    _pendingState.RecordChange();
                }
            }

            if (world.Organisations.Count >= 16)
            {
                ImGui.SameLine();
                ImGui.Text("Already have 16 orgs, cannot add more");
            }
            
            var i = 0;
            foreach (var org in world.Organisations)
            {
                i++;
                var shortName = org.ShortName;
                var origShortName = shortName;
                ImGui.SetNextItemWidth(100.0f);
                ImGui.InputText($"Short Name {i}", ref shortName, 6);
                if (shortName != origShortName)
                {
                    org.ShortName = shortName;
                    _pendingState.RecordChange();
                }
                
                var longName = org.LongName;
                var origLongName = longName;
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100.0f);
                ImGui.InputText($"Long Name {i}", ref longName, 20);
                if (longName != origLongName)
                {
                    org.LongName = longName;
                    _pendingState.RecordChange();
                }

                var u1 = org.Unknown1;
                var origU1 = u1;
                ImGui.SetNextItemWidth(100.0f);
                ImGui.InputInt($"U1 {i}", ref u1);
                if (u1 != origU1)
                {
                    org.Unknown1 = u1;
                    _pendingState.RecordChange();
                }
                
                var u2 = org.Unknown2;
                var origU2 = u2;
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100.0f);
                ImGui.InputInt($"U2 {i}", ref u2);
                if (u2 != origU2)
                {
                    org.Unknown2 = u2;
                    _pendingState.RecordChange();
                }
                
                var u3 = org.Unknown3;
                var origU3 = u3;
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100.0f);
                ImGui.InputInt($"U3 {i}", ref u3);
                if (u3 != origU3)
                {
                    org.Unknown3 = u3;
                    _pendingState.RecordChange();
                }
                
                var u4 = org.Unknown4;
                var origU4 = u4;
                ImGui.SetNextItemWidth(100.0f);
                ImGui.InputInt($"U4 {i}", ref u4);
                if (u4 != origU4)
                {
                    org.Unknown4 = u4;
                    _pendingState.RecordChange();
                }
                
                var uniqueId = org.UniqueId;
                var origUniqueId = uniqueId;
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100.0f);
                ImGui.InputInt($"ID {i}", ref uniqueId);
                if (uniqueId != origUniqueId)
                {
                    org.UniqueId = uniqueId;
                    _pendingState.RecordChange();
                }

                var allowMastermind = org.AllowMastermind;
                var origAllowMastermind = allowMastermind;
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100.0f);
                ImGui.Checkbox($"Allow MM {i}", ref allowMastermind);
                if (allowMastermind != origAllowMastermind)
                {
                    if (allowMastermind)
                    {
                        org.UniqueId = 0x01;
                        _pendingState.RecordChange();
                    }
                    else
                    {
                        org.UniqueId = 0xFF;
                        _pendingState.RecordChange();
                    }
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
            var imageId = $"world_preview_{imageKey}";
            var texture = _renderWindow.RenderImage(RenderWindow.RenderType.Image, imageId, image.Image.Data.Width, image.Image.Data.Height,
                image.Image.VgaImageData);
            ImGui.Image(texture, new Vector2(image.Image.Data.Width, image.Image.Data.Height));
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