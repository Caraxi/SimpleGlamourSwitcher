namespace SimpleGlamourSwitcher;
using static SimpleGlamourSwitcher.UserInterface.Page.ChangeLogPage;

internal static class ChangeLogs {
    internal static void Draw() {
        ChangelogFor("1.3.0.0", () => {
            Change("Added 'Generic' option.");
            Change("Generic items load a set of mods and can be configured to have a unique or shared 'Identifier'.", 1);
            Change("Added ability to link non-outfit files to outfits, applying their mod settings.");
            Change("Added ability to sort folders, either alphabetically or manually.");
            Change("Added ability to clone outfits into another character.");
            Change("Improved the ability to edit the appearance options from within Simple Glamour Switcher.");
            Change("Added ability to assign and automatically detect mods for Face, Face Paint, Height, and Tail.");
            Change("Added ability to assign mods to Body Type and Skin Colour.");
            Change("Added options to disable automatic mod detection for individual slots.");
        });
        ChangelogFor("1.2.0.0", () => {
            Change("Added ability to switch minions, along with associated mods.");
            Change("Added ability to switch emotes, along with associated mods.");
            Change("Fixed some issues with the UI.");
        });
        ChangelogFor("1.1.6.0", () => {
            Change("Added ability to configure display of Character List images.");
            Change("Added ability to edit the colours of image displays.");
            Change("Added ability to modify weapon visibility with outfits.");
            Change("Added ability to modify viera ear visibility with outfits.");
            Change("Fixed full screen display mode on UI scales other than 100%");
        });
        ChangelogFor("1.1.5.0", () => {
            Change("Added ability to enable or disable Customize+ templates when equipping outfits.");
            Change("Added gridline options for outfit screenshots");
        });
        ChangelogFor("1.1.4.0", () => {
            Change("Added ability to take automatically cropped screenshots from the outfit creator.");
            Change("Fixed issue causing custom material colours on glasses to be lost.");
        });
        ChangelogFor("1.1.3.2", () => {
            Change("Added images when hovering outfit links.");
            Change("Fixed display of hairstyle mods in outfit editor.");
        });
        ChangelogFor("1.1.3.0", () => {
            Change("Added ability to automatically use commands when switching outfits.");
        });
        ChangelogFor("1.1.2.0", () => {
            Change("Added ability to clone outfits.");
        });
        ChangelogFor("1.1.0.0", () => {
            Change("Added ability to apply outfits when switching gearsets.");
            Change("Configure within the 'Automations' menu.", 1);
            Change("Added ability to apply other outfits before or after a specific outfit.");
            Change("Configure within individual outfits under the 'Outfit Links' submenu.", 1);
            Change("Added option to allow the use of the hotkey inside GPose.");
        });
        ChangelogFor("1.0.0.12", () => {
            Change("Added ability to edit some properties of appearance in saved outfits.");
            Change("The remaining properties will be editable in a future version.", 1);
            Change("When I'm not lazy", 2);
        });
        ChangelogFor("1.0.0.11", () => {
            Change("Will now detect missing mods assigned to an outfit.");
            Change("Added ability to update a mod assigned on an item, maintaining the associated configuration .");
        });
        ChangelogFor("1.0.0.10", () => {
            Change("Added option to set image sizes for the root outfit folder.");
            Change("Added ability to adjust padding around images.");
        });
        ChangelogFor("1.0.0.9", () => {
            Change("Added protections for invalid items in equipment slots.");
        });
        ChangelogFor("1.0.0.8", () => {
            Change("Added ability to change selected dyes on items.");
            Change("Added ability to change selected items.");
            Change("Added ability to edit selected mods on items.");
            Change("Improved icon display for 'Nothing' items.");
        });
    }
}
