using System.Windows;
using TextBox = System.Windows.Controls.TextBox;

namespace MouseClicker.Wpf;

internal sealed class LabelPromptWindow : Window
{
    private readonly TextBox input;
    internal string Label => input.Text.Trim();

    internal LabelPromptWindow(string defaultLabel)
    {
        var panel = DialogLayout.Initialize(this, Localization.Get("SetLabelTitle"), 420);
        panel.Children.Add(DialogLayout.Text(Localization.Get("SetLabelPrompt")));
        input = new TextBox { Text = defaultLabel, Margin = new Thickness(0, 4, 0, 8) };
        System.Windows.Automation.AutomationProperties.SetName(input, Localization.Get("SetLabelPrompt"));
        panel.Children.Add(input);
        var accept = DialogLayout.Button(Localization.Get("ConditionSave"), () => { if (Label.Length > 0) DialogResult = true; });
        accept.IsDefault = true;
        panel.Children.Add(accept);
        panel.Children.Add(DialogLayout.Button(Localization.Get("ConditionCancel"), Close, true));
        Loaded += (_, _) => { input.Focus(); input.SelectAll(); };
    }
}
