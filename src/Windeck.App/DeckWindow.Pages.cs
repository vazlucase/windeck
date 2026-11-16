using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using Microsoft.Win32;
using Windeck.Core;
using Windeck.Native;
using Windeck.Services;
namespace Windeck;
internal sealed partial class DeckWindow
{
    private FrameworkElement BuildHome()
    {
        var grid=new Grid();foreach(var size in new[]{1.25,1d,1d})grid.ColumnDefinitions.Add(new(){Width=new GridLength(size,GridUnitType.Star)});
        var music=Ui.Card(BuildMusic(false));Grid.SetColumn(music,0);grid.Children.Add(music);
        var weather=Ui.Card(BuildWeather());Grid.SetColumn(weather,1);grid.Children.Add(weather);
        var agenda=Ui.Card(Ui.Stack(Ui.Heading("Hoje, no seu ritmo"),Ui.Text(DateTime.Now.ToString("dddd, dd MMM",System.Globalization.CultureInfo.GetCultureInfo("pt-BR")),13,true),BuildReminders(3,DateTime.Today),Ui.Button("Abrir agenda",()=>Navigate(DeckPage.Calendar)),Ui.Button("Notificações do Windows",()=>SystemAction("notifications"))));Grid.SetColumn(agenda,2);grid.Children.Add(agenda);
        if(DeckWidth<850){grid.Children.Clear();var stack=Ui.Stack(music,weather,agenda);return Ui.Scroll(stack);}
        return Ui.Scroll(Ui.Stack(grid,Ui.Row(Ui.Button("Rede e som",()=>Navigate(DeckPage.Controls)),Ui.Button("Bandeja",()=>Navigate(DeckPage.Tray)),Ui.Button("Visão de tarefas",()=>SystemAction("tasks")),Ui.Button("Atividade",()=>ShowNotices())),BuildNoticePreview()));
    }
    private FrameworkElement BuildMusic(bool full)
    {
        var m=runtime.Media.Snapshot;
        mediaImage=new Image{Width=full?140:74,Height=full?140:74,Stretch=Stretch.UniformToFill,HorizontalAlignment=HorizontalAlignment.Left};
        var art=new Border{Child=mediaImage,CornerRadius=new CornerRadius(17),Background=Ui.Brush("#344264"),Padding=new Thickness(2),Width=full?144:78,Height=full?144:78,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,3,0,16)};
        mediaTitle=Ui.Text(m.Title,full?27:19);mediaTitle.FontWeight=FontWeights.SemiBold;mediaTitle.TextWrapping=TextWrapping.Wrap;mediaTitle.MaxHeight=full?82:56;
        mediaArtist=Ui.Text(m.Artist,13,true);mediaArtist.Margin=new Thickness(0,6,0,16);
        var controls=Ui.Row(previousButton=Ui.Icon("\uE892",()=>_ =runtime.Media.CommandAsync("previous"),"Faixa anterior"),playButton=Ui.Icon("\uE768",()=>_=runtime.Media.CommandAsync("play"),"Reproduzir ou pausar"),nextButton=Ui.Icon("\uE893",()=>_=runtime.Media.CommandAsync("next"),"Próxima faixa"));controls.HorizontalAlignment=HorizontalAlignment.Center;
        mediaProgress=new Slider{Minimum=0,Maximum=1,Margin=new Thickness(0,14,0,3)};System.Windows.Automation.AutomationProperties.SetName(mediaProgress,"Posição da música");
        mediaProgress.PreviewMouseLeftButtonUp+=(_,_)=>_ =runtime.Media.CommandAsync("seek",mediaProgress.Value);
        mediaProgress.PreviewKeyUp+=(_,_)=>{if(!updatingSlider)_=runtime.Media.CommandAsync("seek",mediaProgress.Value);};
        mediaTime=Ui.Text("",10,true);
        var panel=Ui.Stack(Ui.Text("AGORA TOCANDO",10,true),art,mediaTitle,mediaArtist,controls,mediaProgress,mediaTime);
        panel.MouseWheel+=(_,e)=>{runtime.Audio.Set(runtime.Audio.Volume+Math.Sign(e.Delta)*.03);UpdateLabels();e.Handled=true;};
        if(full){panel.MaxWidth=620;panel.HorizontalAlignment=HorizontalAlignment.Center;panel.Margin=new Thickness(24);panel.Children.Add(Ui.Text("A música é controlada pela sessão de mídia publicada pelo aplicativo no Windows.",11,true));}
        return panel;
    }
    private FrameworkElement BuildWeather()
    {
        var w=runtime.Weather.Snapshot;
        var panel=Ui.Stack(Ui.Text("TEMPO · SUA PREVISÃO",10,true));
        if(w==null){panel.Children.Add(Ui.Text("☁",46));panel.Children.Add(Ui.Text(runtime.Weather.Status,13,true));panel.Children.Add(Ui.Button("Configurar cidade",()=>Navigate(DeckPage.Settings)));return panel;}
        panel.Children.Add(Ui.Text($"{WeatherService.Glyph(w.Code,w.Day)}  {w.Temperature:0}°",39));
        panel.Children.Add(Ui.Text(w.Place,12,true));panel.Children.Add(Ui.Text(WeatherService.Description(w.Code),16));
        panel.Children.Add(Ui.Text($"{w.Wind:0} km/h · {w.Humidity:0}% de umidade",11,true));
        foreach(var day in w.Forecast){var row=new Grid{Margin=new Thickness(0,7,0,0)};row.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});row.ColumnDefinitions.Add(new(){Width=GridLength.Auto});row.Children.Add(Ui.Text($"{day.Day:ddd}   {WeatherService.Glyph(day.Code)}",12,true));var temp=Ui.Text($"{day.Max:0}°  {day.Min:0}°",12);Grid.SetColumn(temp,1);row.Children.Add(temp);panel.Children.Add(row);}
        panel.Children.Add(Ui.Text(w.Error??$"Open-Meteo · {w.Updated:HH:mm}",10,true));return panel;
    }
    private FrameworkElement BuildReminders(int limit,DateTime? forDate=null)
    {
        var stack=new StackPanel{Margin=new Thickness(0,16,0,12)};
        var list=runtime.Preferences.Reminders.Where(r=>!r.Done&&r.Due.Date==(forDate??selectedDate).Date).OrderBy(r=>r.Due).Take(limit).ToList();
        if(list.Count==0){var empty=Ui.Text("Seu dia tem espaço.\nAdicione um lembrete na agenda.",13,true);empty.TextWrapping=TextWrapping.Wrap;stack.Children.Add(empty);}
        foreach(var item in list){var check=new CheckBox{Content=$"{item.Due:HH:mm}  {item.Text}",ToolTip=item.Text};check.Checked+=(_,_)=>{int i=runtime.Preferences.Reminders.IndexOf(item);if(i>=0)runtime.Preferences.Reminders[i]=item with{Done=true};runtime.Save();ShowPage(interaction.Page);};stack.Children.Add(check);}
        return stack;
    }
    private FrameworkElement BuildCalendarPage()
    {
        var calendar=new StackPanel();
        var caption=Ui.Row(Ui.Icon("\uE76B",()=>{month=month.AddMonths(-1);ShowPage(DeckPage.Calendar);},"Mês anterior"),Ui.Heading(month.ToString("MMMM yyyy",System.Globalization.CultureInfo.GetCultureInfo("pt-BR"))),Ui.Icon("\uE76C",()=>{month=month.AddMonths(1);ShowPage(DeckPage.Calendar);},"Próximo mês"));calendar.Children.Add(caption);
        var grid=new System.Windows.Controls.Primitives.UniformGrid{Columns=7};
        foreach(string name in new[]{"D","S","T","Q","Q","S","S"}){var t=Ui.Text(name,11,true);t.HorizontalAlignment=HorizontalAlignment.Center;t.Margin=new Thickness(8);grid.Children.Add(t);}
        var start=new DateTime(month.Year,month.Month,1);start=start.AddDays(-(int)start.DayOfWeek);
        for(int i=0;i<42;i++){
            var day=start.AddDays(i);var button=Ui.Button(day.Day.ToString(),()=>{selectedDate=day;ShowPage(DeckPage.Calendar);},day.ToLongDateString());button.Margin=new Thickness(3);button.Height=37;
            if(day.Month!=month.Month)button.Opacity=.35;if(day.Date==DateTime.Today)button.BorderBrush=Ui.Brush("#80B7FF");if(day.Date==selectedDate.Date)button.Background=Ui.Brush("#406CBA");grid.Children.Add(button);
        }
        calendar.Children.Add(grid);
        var input=new TextBox{ToolTip="Texto do lembrete"};var time=new TextBox{Text="09:00",Width=90,ToolTip="Horário HH:mm"};
        var add=Ui.Button("Adicionar",()=>{
            if(string.IsNullOrWhiteSpace(input.Text))return;
            if(!TimeSpan.TryParse(time.Text,out var at)||at<TimeSpan.Zero||at>=TimeSpan.FromDays(1)){MessageBox.Show("Use um horário entre 00:00 e 23:59.","Windeck");return;}
            runtime.Preferences.Reminders.Add(new(Guid.NewGuid(),selectedDate.Date+at,input.Text.Trim()));runtime.Save();ShowPage(DeckPage.Calendar);
        });
        var right=Ui.Stack(Ui.Heading(selectedDate.ToString("dd MMMM",System.Globalization.CultureInfo.GetCultureInfo("pt-BR"))),BuildReminders(20),input,Ui.Row(time,add),Ui.Button("Calendário e notificações do Windows",()=>SystemAction("notifications")),Ui.Text("Lembretes locais do Windeck. Calendários de outras contas continuam disponíveis no Windows.",11,true));
        var columns=new Grid();columns.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});columns.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});
        columns.Children.Add(Ui.Card(calendar));var side=Ui.Card(right);Grid.SetColumn(side,1);columns.Children.Add(side);return Ui.Scroll(columns);
    }
    private FrameworkElement BuildControls()
    {
        var sound=Ui.Stack(Ui.Heading("Som"));volumeText=Ui.Text("",22);sound.Children.Add(volumeText);
        volumeSlider=new Slider{Minimum=0,Maximum=1,Margin=new Thickness(0,16,0,10)};System.Windows.Automation.AutomationProperties.SetName(volumeSlider,"Volume geral");volumeSlider.ValueChanged+=(_,_)=>{if(!updatingSlider){runtime.Audio.Set(volumeSlider.Value);UpdateLabels();}};sound.Children.Add(volumeSlider);
        sound.Children.Add(Ui.Button("Ativar / desativar som",()=>{runtime.Audio.ToggleMute();UpdateLabels();}));sound.Children.Add(Ui.Button("Saída e mixer de áudio",()=>Win32.Open("ms-settings:apps-volume")));
        var net=Ui.Stack(Ui.Heading("Conexões"));networkText=Ui.Text("",16);net.Children.Add(networkText);net.Children.Add(Ui.Button("Wi-Fi e rede",()=>Win32.Open("ms-settings:network-wifi")));net.Children.Add(Ui.Button("Bluetooth e dispositivos",()=>Win32.Open("ms-settings:bluetooth")));net.Children.Add(Ui.Button("Controles rápidos do Windows",()=>SystemAction("quick")));
        var battery=Ui.Stack(Ui.Heading("Energia"));batteryText=Ui.Text("",17);battery.Children.Add(batteryText);battery.Children.Add(Ui.Button("Energia e bateria",()=>Win32.Open("ms-settings:powersleep")));battery.Children.Add(Ui.Button("Tela e brilho",()=>Win32.Open("ms-settings:display")));
        var cols=new Grid();for(int i=0;i<3;i++)cols.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});
        foreach(var (panel,i) in new[]{(sound,0),(net,1),(battery,2)}){var card=Ui.Card(panel);Grid.SetColumn(card,i);cols.Children.Add(card);}
        var tools=new WrapPanel();foreach(var (title,key) in new[]{("Notificações","notifications"),("Área de transferência","clipboard"),("Capturar tela","capture"),("Projetar tela","project"),("Transmitir","cast"),("Idioma / teclado","input"),("Emojis","emoji"),("Widgets do Windows","widgets")})tools.Children.Add(Ui.Button(title,()=>SystemAction(key)));
        var power=new WrapPanel();power.Children.Add(Ui.Button("Bloquear",()=>Win32.LockWorkStation()));power.Children.Add(Ui.Button("Gerenciador de tarefas",()=>Win32.Open("taskmgr.exe")));power.Children.Add(Ui.Button("Executar…",()=>ManagedShell.Common.Helpers.ShellHelper.ShowRunDialog("Executar","Digite o nome de um programa, pasta ou documento.")));
        power.Children.Add(Ui.Button("Suspender",()=>ConfirmPower("suspender",ManagedShell.Common.Helpers.PowerHelper.Sleep)));power.Children.Add(Ui.Button("Reiniciar",()=>ConfirmPower("reiniciar",ManagedShell.Common.Helpers.PowerHelper.Reboot)));power.Children.Add(Ui.Button("Desligar",()=>ConfirmPower("desligar",ManagedShell.Common.Helpers.PowerHelper.Shutdown)));power.Children.Add(Ui.Button("Sair da conta",()=>ConfirmPower("sair da conta",ManagedShell.Common.Helpers.ShellHelper.Logoff)));
        return Ui.Scroll(Ui.Stack(cols,Ui.Heading("Ferramentas do sistema"),tools,Ui.Heading("Sua sessão"),power));
    }
    private void ConfirmPower(string label,Action action)
    {if(MessageBox.Show($"Deseja {label} agora?","Windeck",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes){runtime.Desktop.Restore();action();}}
    private FrameworkElement BuildSettings()
    {
        var p=runtime.Preferences;
        var panel=Ui.Stack(Ui.Heading("Do seu jeito"),Ui.Text("Windeck 0.1 · aplicativo nativo para Windows · versão de desenvolvimento",12,true));
        panel.Children.Add(Ui.Row(Ui.Button("Windows 11 · Fluent",()=>{p.Theme="Fluent";runtime.Save();ShowPage(DeckPage.Settings);}),Ui.Button("Apple Dock · escuro",()=>{p.Theme="Dock";runtime.Save();ShowPage(DeckPage.Settings);}))); 
        CheckBox Check(string name,bool value,Action<bool> update){var check=new CheckBox{Content=name,IsChecked=value};check.Checked+=(_,_)=>update(true);check.Unchecked+=(_,_)=>update(false);return check;}
        var replacement=Check("Substituir a barra de tarefas do Windows",runtime.Desktop.Replacing,value=>_=runtime.SetReplacementAsync(value));panel.Children.Add(replacement);
        panel.Children.Add(Check("Olhos e animações suaves",!p.ReduceMotion,value=>{p.ReduceMotion=!value;runtime.Save();}));
        panel.Children.Add(Check("Abrir com movimento rápido para cima",p.Gestures,value=>{p.Gestures=value;runtime.Save();}));
        panel.Children.Add(Check("Ocultar durante jogos e vídeos em tela cheia",p.HideOnFullscreen,value=>{p.HideOnFullscreen=value;runtime.Save();}));
        panel.Children.Add(Check("Reservar espaço no topo para janelas maximizadas",p.ReserveSpace,value=>{p.ReserveSpace=value;runtime.Save();if(runtime.Desktop.Replacing){runtime.Desktop.Restore();_=runtime.SetReplacementAsync(true);}}));
        var speed=new Slider{Minimum=650,Maximum=2400,Value=p.StrikeSpeed,Margin=new Thickness(8),Width=300,HorizontalAlignment=HorizontalAlignment.Left};speed.ValueChanged+=(_,_)=>{p.StrikeSpeed=speed.Value;interaction.StrikeSpeed=p.StrikeSpeed;};speed.PreviewMouseLeftButtonUp+=(_,_)=>runtime.Save();speed.PreviewKeyUp+=(_,_)=>runtime.Save();panel.Children.Add(Ui.Text("Velocidade do gesto · menor valor aumenta a sensibilidade",12,true));panel.Children.Add(speed);
        var city=new TextBox{Text=p.WeatherCity,Width=230,ToolTip="Cidade do clima"};var country=new TextBox{Text=p.WeatherCountry,Width=80,ToolTip="País, por exemplo BR"};
        panel.Children.Add(Ui.Heading("Previsão do tempo"));panel.Children.Add(Ui.Row(city,country,Ui.Button("Salvar cidade",()=>{p.WeatherCity=city.Text.Trim();p.WeatherCountry=country.Text.Trim().ToUpperInvariant();runtime.Save();_=runtime.Weather.RefreshAsync(p.WeatherCity,p.WeatherCountry,true);})));panel.Children.Add(Ui.Text("A cidade é consultada no Open-Meteo. Não usamos detecção de localização.",11,true));
        var autoStart=Check("Iniciar com o Windows",StartupEnabled(),SetStartup);panel.Children.Add(autoStart);
        panel.Children.Add(Ui.Button("Adicionar aplicativo ou atalho aos fixados",()=>{var dialog=new OpenFileDialog{Filter="Aplicativos e atalhos|*.exe;*.lnk;*.url|Todos os arquivos|*.*"};if(dialog.ShowDialog()==true)Pin(Path.GetFileNameWithoutExtension(dialog.FileName),dialog.FileName);}));
        panel.Children.Add(Ui.Row(Ui.Button("Restaurar barra original",()=>{runtime.Desktop.Restore();p.ReplaceTaskbar=false;runtime.Save();ShowPage(DeckPage.Settings);}),Ui.Button("Abrir pasta de configurações",()=>Win32.Open(Recovery.Folder)),Ui.Button("Sair do Windeck",runtime.Quit)));
        var note=Ui.Text("Ctrl+Alt+Espaço abre a cápsula. Ctrl+Alt+Backspace restaura a barra do Windows. Use Alt+arrastar para reordenar os fixados. O Windeck aparece em todos os monitores conectados na inicialização.",12,true);note.TextWrapping=TextWrapping.Wrap;panel.Children.Add(note);return Ui.Scroll(panel);
    }
    private static bool StartupEnabled(){using var key=Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run");return key?.GetValue("Windeck")!=null;}
    private static void SetStartup(bool value){using var key=Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run");if(value)key.SetValue("Windeck",'"'+Environment.ProcessPath!+'"');else key.DeleteValue("Windeck",false);}
    private FrameworkElement BuildNoticePreview()
    {
        var panel=new StackPanel();foreach(var notice in runtime.Notices.Take(2)){var t=Ui.Text($"{notice.Time:HH:mm}  {notice.Title} · {notice.Body}",12,true);t.Margin=new Thickness(10,4,10,0);panel.Children.Add(t);}return panel;
    }
    private void ShowNotices()
    {
        var stack=Ui.Stack(Ui.Heading("Atividade do Windeck"),Ui.Button("Abrir central de notificações do Windows",()=>SystemAction("notifications")),Ui.Text("Lembretes locais e notificações clássicas da bandeja aparecem aqui. As notificações modernas ficam na central do Windows.",12,true));
        if(runtime.Notices.Count==0)stack.Children.Add(Ui.Text("Nenhuma atividade por enquanto.",14,true));
        foreach(var notice in runtime.Notices){var body=Ui.Text(notice.Body,13,true);body.TextWrapping=TextWrapping.Wrap;var entry=Ui.Stack(Ui.Text($"{notice.Title}  ·  {notice.Time:HH:mm}",15),body);if(notice.Activate!=null)entry.Children.Add(Ui.Button("Abrir",notice.Activate));stack.Children.Add(Ui.Card(entry));}
        contentHost.Child=Ui.Scroll(stack);
    }
}
