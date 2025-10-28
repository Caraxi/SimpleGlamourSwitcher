using System.Diagnostics.CodeAnalysis;
using Dalamud.Interface.Textures.TextureWraps;
using SimpleGlamourSwitcher.Configuration.Parts;

namespace SimpleGlamourSwitcher.Configuration.ConfigSystem;

public interface IImageProvider {
    
    public static string[] SupportedImageFileTypes = ["png", "jpg", "jpeg"];
    
    public bool IsUsingDefaultImage() => false;
    
    public string Name { get; }
    
    public ImageDetail ImageDetail { get; }
    
    public bool TryGetImage([NotNullWhen(true)] out IDalamudTextureWrap? wrap);

    public IDalamudTextureWrap? GetImage(IDalamudTextureWrap? defaultWrap = null) {
        return TryGetImage(out var wrap) ? wrap : defaultWrap;
    }

    public void SetImage(FileInfo fileInfo);

    public void SetImageDetail(ImageDetail imageDetail);


    
    public void LoadFile(bool fileSelectOk, List<string> filePaths) {
        Plugin.MainWindow.AllowAutoClose = true;
        
        if (!fileSelectOk || filePaths.Count < 1) return;
        var filePath = filePaths[0];
        
        
        PluginLog.Verbose($"Load File: {filePath}");

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Exists) {
            if (fileInfo.Directory != null) {
                PluginConfig.ImageFilePickerLastPath = fileInfo.Directory.FullName;
                PluginConfig.Dirty = true;
            }
            
            SetImage(fileInfo);
        } else {
            PluginLog.Error("File does not exist");
        }
    }

    /*
    public void LoadImage(Image clipImage) {
        var tempDir = Path.Join(PluginInterface.GetPluginConfigDirectory(), "temp");
        if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
        var tempImagePath = Path.Join(tempDir, $"Image_{new Random().Next()}.png");
        
        clipImage.Save(tempImagePath, ImageFormat.Png);
        LoadFile(true, [tempImagePath]);
        Task.Run(async () => {
            await Task.Delay(15000);
            File.Delete(tempImagePath);
        });
    }
    */

    public void LoadImage(IDalamudTextureWrap clipImage) {
        var tempDir = Path.Join(PluginInterface.GetPluginConfigDirectory(), "temp");
        if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
        var tempImagePath = Path.Join(tempDir, $"Image_{new Random().Next()}.png");
        TextureReadbackProvider.SaveToFileAsync(clipImage, TextureReadbackProvider.GetSupportedImageEncoderInfos().First().ContainerGuid, tempImagePath).Wait();
        LoadFile(true, [tempImagePath]);
        Task.Run(async () => {
            await Task.Delay(15000);
            File.Delete(tempImagePath);
        });
    }
}
