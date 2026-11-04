using System.Windows;
using System.Windows.Input;
using TextBox = System.Windows.Controls.TextBox;
using Keys = System.Windows.Forms.Keys;

namespace MouseClicker.Wpf;

internal sealed class HotkeySettingsWindow : Window
{
    private readonly GlobalHotkeyManager manager;
    private readonly TextBox input;
    private readonly System.Windows.Controls.TextBlock status = DialogLayout.Text(string.Empty);
    private Keys pending;

    internal HotkeySettingsWindow(GlobalHotkeyManager manager)
    {
        this.manager = manager;
        pending = manager.Hotkey;
        var panel = DialogLayout.Initialize(this, Localization.Get("HotkeySettings"));
        panel.Children.Add(DialogLayout.Text(Localization.Get("HotkeyLabel")));
        input = new TextBox { IsReadOnly = true, Text = pending.ToString(), Margin = new Thickness(0, 4, 0, 8) };
        System.Windows.Automation.AutomationProperties.SetName(input, Localization.Get("HotkeyLabel"));
        panel.Children.Add(input);
        panel.Children.Add(DialogLayout.Text(Localization.Get("HotkeyHint")));
        panel.Children.Add(status);
        input.PreviewKeyDown += (_, e) =>
        {
            if (e.Key is Key.Tab or Key.Escape or Key.Enter) return;
            e.Handled = true;
            if (TryGetCapturedKey(e.Key, Keyboard.Modifiers, out var key))
            { pending = key; input.Text = key.ToString(); status.Text = string.Empty; }
            else status.Text = Localization.Get("HotkeyInvalidKey");
        };
        var apply = DialogLayout.Button(Localization.Get("HotkeyApply"), Apply);
        apply.IsDefault = true;
        panel.Children.Add(apply);
        panel.Children.Add(DialogLayout.Button(Localization.Get("CloseButton"), Close, true));
    }

    internal static bool TryGetCapturedKey(Key key, ModifierKeys modifiers, out Keys captured)
    {
        captured = (Keys)KeyInterop.VirtualKeyFromKey(key);
        return modifiers == ModifierKeys.None && GlobalHotkeyManager.SupportedKeys.Contains(captured);
    }

    private void Apply()
    {
        try { manager.SetHotkey(pending); DialogResult = true; }
        catch (Exception ex) { status.Text = string.Format(Localization.Get("SaveConfigError"), ex.Message); }
    }
}
