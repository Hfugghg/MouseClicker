using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace MouseClicker.Wpf;

/// <summary>仅作用于常规对话框，避免影响需要透明背景的屏幕工具。</summary>
internal static class DialogTheme
{
    internal static void Apply(Window window)
    {
        // 与主窗口保持一致；该 WPF API 目前仍标记为实验性。
#pragma warning disable WPF0001
        window.ThemeMode = ThemeMode.Light;
#pragma warning restore WPF0001
        window.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 246, 251));
        window.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59));
        window.FontFamily = new System.Windows.Media.FontFamily("Segoe UI, Microsoft YaHei UI");
        window.FontSize = 14;
        window.Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Parse(Resources));
    }

    private const string Resources = """
        <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
            <Style TargetType="Button">
                <Setter Property="Background" Value="White"/>
                <Setter Property="Foreground" Value="#26364D"/>
                <Setter Property="BorderBrush" Value="#D6DFEB"/>
                <Setter Property="BorderThickness" Value="1"/>
                <Setter Property="Padding" Value="16,9"/>
                <Setter Property="MinHeight" Value="38"/>
                <Setter Property="HorizontalContentAlignment" Value="Center"/>
                <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="Button">
                    <Border x:Name="Surface" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="8" Padding="{TemplateBinding Padding}">
                        <ContentPresenter HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}" VerticalAlignment="Center" RecognizesAccessKey="True"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Surface" Property="BorderBrush" Value="#2563EB"/></Trigger>
                        <Trigger Property="IsPressed" Value="True"><Setter TargetName="Surface" Property="Opacity" Value="0.75"/></Trigger>
                        <Trigger Property="IsKeyboardFocused" Value="True"><Setter TargetName="Surface" Property="BorderBrush" Value="#2563EB"/><Setter TargetName="Surface" Property="BorderThickness" Value="2"/></Trigger>
                        <Trigger Property="IsDefault" Value="True"><Setter Property="Background" Value="#2563EB"/><Setter Property="Foreground" Value="White"/><Setter Property="BorderBrush" Value="#2563EB"/></Trigger>
                        <Trigger Property="IsEnabled" Value="False"><Setter TargetName="Surface" Property="Opacity" Value="0.45"/></Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate></Setter.Value></Setter>
            </Style>
            <Style TargetType="TextBox"><Setter Property="Padding" Value="10,8"/><Setter Property="MinHeight" Value="36"/><Setter Property="BorderBrush" Value="#D6DFEB"/><Setter Property="Background" Value="White"/><Setter Property="VerticalContentAlignment" Value="Center"/></Style>
            <Style TargetType="ComboBox"><Setter Property="Padding" Value="10,7"/><Setter Property="MinHeight" Value="36"/><Setter Property="BorderBrush" Value="#D6DFEB"/></Style>
            <Style TargetType="CheckBox"><Setter Property="Margin" Value="0,7,0,7"/><Setter Property="VerticalContentAlignment" Value="Center"/></Style>
            <Style TargetType="ListBox"><Setter Property="Background" Value="White"/><Setter Property="BorderBrush" Value="#D6DFEB"/><Setter Property="Padding" Value="6"/></Style>
            <Style TargetType="ListBoxItem"><Setter Property="Padding" Value="10,10"/><Setter Property="Margin" Value="0,2"/></Style>
        </ResourceDictionary>
        """;
}


