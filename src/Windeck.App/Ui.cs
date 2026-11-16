using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
namespace Windeck;
internal static class Ui
{
    public static SolidColorBrush Brush(string hex)=>new((Color)ColorConverter.ConvertFromString(hex));
    public static TextBlock Text(string value,double size=14,bool muted=false)
    {
        var t=new TextBlock{Text=value,FontSize=size,TextTrimming=TextTrimming.CharacterEllipsis,VerticalAlignment=VerticalAlignment.Center};
        t.SetResourceReference(TextBlock.ForegroundProperty,muted?"Muted":"Text");return t;
    }
    public static Button Button(string label,Action action,string? tooltip=null)
    {
        var button=new Button{Content=label,ToolTip=tooltip??label};
        System.Windows.Automation.AutomationProperties.SetName(button,tooltip??label);
        button.Click+=(_,_)=>{try{action();}catch(Exception e){App.Log(e);MessageBox.Show(e.Message,"Windeck");}};
        return button;
    }
    public static Button Icon(string glyph,Action action,string label)
    {var b=Button(glyph,action,label);b.FontFamily=new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets");b.FontSize=17;b.Width=38;b.Height=38;b.Padding=new Thickness(0);return b;}
    public static Border Card(UIElement child,Thickness? padding=null)
    {var b=new Border{Child=child,CornerRadius=new CornerRadius(21),Padding=padding??new Thickness(20),Margin=new Thickness(5),BorderThickness=new Thickness(1)};b.SetResourceReference(Border.BackgroundProperty,"Card");b.SetResourceReference(Border.BorderBrushProperty,"Stroke");return b;}
    public static StackPanel Stack(params UIElement[] children){var s=new StackPanel();foreach(var c in children)s.Children.Add(c);return s;}
    public static StackPanel Row(params UIElement[] children){var s=Stack(children);s.Orientation=Orientation.Horizontal;return s;}
    public static TextBlock Heading(string title){var t=Text(title,20);t.FontWeight=FontWeights.SemiBold;t.Margin=new Thickness(4,2,4,14);return t;}
    public static ScrollViewer Scroll(UIElement child)=>new(){Content=child,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,CanContentScroll=false};
    public static void Theme(ResourceDictionary resources,string theme)
    {
        if(SystemParameters.HighContrast){
            resources["Surface"]=SystemColors.WindowBrush;resources["Card"]=SystemColors.ControlBrush;
            resources["Stroke"]=SystemColors.WindowTextBrush;resources["Text"]=SystemColors.WindowTextBrush;
            resources["Muted"]=SystemColors.GrayTextBrush;resources["Accent"]=SystemColors.HighlightBrush;resources["Hover"]=SystemColors.HighlightBrush;return;
        }
        bool dock=theme=="Dock";
        resources["Surface"]=Brush(dock?"#F509090B":"#F2182232");
        resources["Card"]=Brush(dock?"#FF202023":"#FF242F43");
        resources["Stroke"]=Brush(dock?"#FF36363C":"#FF3D4D67");
        resources["Text"]=Brush("#FFF4F6FB");resources["Muted"]=Brush(dock?"#FFAAAAB2":"#FF9DAEC8");
        resources["Accent"]=Brush(dock?"#FFB7A0FF":"#FF8FB9FF");resources["Hover"]=Brush(dock?"#FF383441":"#FF36465C");
    }
    public static void InstallStyles(ResourceDictionary resources)
    {
        Theme(resources,"Fluent");
        const string styles="""
        <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
          <Style TargetType="TextBlock"><Setter Property="Foreground" Value="{DynamicResource Text}"/></Style>
          <Style TargetType="Button">
            <Setter Property="Foreground" Value="{DynamicResource Text}"/><Setter Property="Background" Value="Transparent"/><Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Padding" Value="12,9"/><Setter Property="Margin" Value="3"/><Setter Property="Cursor" Value="Hand"/><Setter Property="FontSize" Value="13"/>
            <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="Button"><Border x:Name="B" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="12" Padding="{TemplateBinding Padding}"><ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/></Border><ControlTemplate.Triggers><Trigger Property="IsMouseOver" Value="True"><Setter TargetName="B" Property="Background" Value="{DynamicResource Hover}"/></Trigger><Trigger Property="IsKeyboardFocused" Value="True"><Setter TargetName="B" Property="BorderBrush" Value="{DynamicResource Accent}"/><Setter TargetName="B" Property="BorderThickness" Value="1"/></Trigger><Trigger Property="IsEnabled" Value="False"><Setter Property="Opacity" Value="0.4"/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
          </Style>
          <Style TargetType="TextBox"><Setter Property="Background" Value="{DynamicResource Card}"/><Setter Property="Foreground" Value="{DynamicResource Text}"/><Setter Property="CaretBrush" Value="{DynamicResource Accent}"/><Setter Property="BorderBrush" Value="{DynamicResource Stroke}"/><Setter Property="Padding" Value="12,10"/><Setter Property="FontSize" Value="14"/><Setter Property="Margin" Value="4"/></Style>
          <Style TargetType="CheckBox"><Setter Property="Foreground" Value="{DynamicResource Text}"/><Setter Property="Margin" Value="6,9"/><Setter Property="FontSize" Value="14"/></Style>
          <Style TargetType="ToolTip"><Setter Property="Background" Value="#FF242A35"/><Setter Property="Foreground" Value="White"/><Setter Property="BorderBrush" Value="#FF47536A"/><Setter Property="Padding" Value="10,6"/></Style>
          <Style TargetType="ContextMenu"><Setter Property="Background" Value="#FF252B37"/><Setter Property="Foreground" Value="White"/><Setter Property="BorderBrush" Value="#FF485365"/></Style>
          <Style TargetType="MenuItem"><Setter Property="Foreground" Value="White"/></Style>
        </ResourceDictionary>
        """;
        resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Parse(styles));
    }
}
