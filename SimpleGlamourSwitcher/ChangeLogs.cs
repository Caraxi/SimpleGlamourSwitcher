namespace SimpleGlamourSwitcher;
using static SimpleGlamourSwitcher.UserInterface.Page.ChangeLogPage;

internal static class ChangeLogs {
    internal static void Draw() {
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
