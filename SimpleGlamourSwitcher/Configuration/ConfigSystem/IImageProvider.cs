using System.Diagnostics.CodeAnalysis;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Textures.TextureWraps;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.UserInterface.Windows;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace SimpleGlamourSwitcher.Configuration.ConfigSystem;

public interface IImageProvider {
    
    public static string[] SupportedImageFileTypes = ["png", "jpg", "jpeg", "webp"];
    
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

    public bool TryGetImageFileInfo([NotNullWhen(true)] out FileInfo? fileInfo);
    
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

    public void LoadImage(AnimatedScreenshotRecording animatedScreenshot) {
        Task.Run(async () => {
            animatedScreenshot.StopRecording();
            var activeNotification = NotificationManager.AddNotification(new Notification() {
                Content = "Waiting for completion...",
                Title = "Simple Glamour Switcher - Screenshot",
                Progress = 0f,
                InitialDuration = TimeSpan.FromSeconds(10),
                UserDismissable = false,
            });

            if (!await animatedScreenshot.WaitFinishedRecording(TimeSpan.FromSeconds(10))) {
                activeNotification.Content = $"Timed Out.";
                activeNotification.HardExpiry = DateTime.Now + TimeSpan.FromSeconds(5);
                activeNotification.Progress = 1;
                activeNotification.Icon = INotificationIcon.From(FontAwesomeIcon.Exclamation);
                return;
            }
            
            activeNotification.Content = $"Saving...";
            activeNotification.HardExpiry = DateTime.Now + TimeSpan.FromSeconds(30);
            
            var tempDir = Path.Join(PluginInterface.GetPluginConfigDirectory(), "temp");
            if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
            var tempImagePath = Path.Join(tempDir, $"Image_{new Random().Next()}.webp");
            var size = animatedScreenshot.Size;
            var image = new Image<Rgba32>((int) size.X, (int) size.Y);
            for (var i = 0; i < animatedScreenshot.Frames.Count; i++) {
                var f =  animatedScreenshot.Frames[i];
                var nextFrame = i >= animatedScreenshot.Frames.Count - 1 ? animatedScreenshot.Frames[0] : animatedScreenshot.Frames[i + 1];
                using var stream = new MemoryStream();
                await TextureReadbackProvider.SaveToStreamAsync(f.TextureWrap, TextureReadbackProvider.GetSupportedImageEncoderInfos().First().ContainerGuid, stream, null, false, true);
                stream.Position = 0;
                var frameImage = await Image.LoadAsync(stream);
                var frame = image.Frames.AddFrame(frameImage.Frames[0]);
                frame.Metadata.GetWebpMetadata().FrameDelay = (uint)nextFrame.TimeSinceLastFrame;
                activeNotification.Content = $"Generating... [{i+1} /  {animatedScreenshot.Frames.Count}]";
                activeNotification.HardExpiry = DateTime.Now + TimeSpan.FromSeconds(5);
                activeNotification.Progress = i / (float)animatedScreenshot.Frames.Count;
            }
            
            image.Frames.RemoveFrame(0);
            animatedScreenshot.Dispose();
            activeNotification.Content = $"Saving...";
            activeNotification.HardExpiry = DateTime.Now + TimeSpan.FromSeconds(30);
            await image.SaveAsWebpAsync(tempImagePath, new WebpEncoder()
            {
                FileFormat = PluginConfig.AnimatedImageConfiguration.UseLosslessCompression ? WebpFileFormatType.Lossless : WebpFileFormatType.Lossy,
                Quality = PluginConfig.AnimatedImageConfiguration.CompressionQuality,
                Method = PluginConfig.AnimatedImageConfiguration.EncodingMethod,
            });
            activeNotification.Content = $"Saving...";
            activeNotification.HardExpiry = DateTime.Now + TimeSpan.FromSeconds(30);
            await Framework.RunOnTick(() => {
                LoadFile(true, [tempImagePath]);
            });
            activeNotification.Content = $"Saved";
            activeNotification.HardExpiry = DateTime.Now + TimeSpan.FromSeconds(3);
            await Task.Delay(15000);
            File.Delete(tempImagePath);
        });
    }
}
