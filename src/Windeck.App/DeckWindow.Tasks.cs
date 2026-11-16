using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using ManagedShell.WindowsTasks;
using ManagedShell.WindowsTray;
using Windeck.Core;
using Windeck.Native;
using Windeck.Services;
namespace Windeck;
internal sealed partial class DeckWindow
{
    private void RefreshTasks()
    {
        string key=string.Join(",",runtime.Tasks.Select(w=>w.Handle.ToString()))+"|"+string.Join(",",runtime.Preferences.Pins.Select(p=>p.Target));
        if(key==dockKey)return;dockKey=key;taskDock.Children.Clear();dockTransforms.Clear();
        taskDock.Children.Add(Ui.Icon("\uE80F",()=>Navigate(DeckPage.Apps),"Aplicativos · Ctrl+F"));
        taskDock.Children.Add(Ui.Icon("\uE721",()=>SystemAction("search"),"Pesquisa do Windows"));
        var groups=runtime.Tasks.GroupBy(w=>!string.IsNullOrWhiteSpace(w.AppUserModelID)?w.AppUserModelID:(!string.IsNullOrWhiteSpace(w.WinFileName)?w.WinFileName:w.Handle.ToString()),StringComparer.OrdinalIgnoreCase).Select(g=>g.ToArray()).ToList();
        var used=new HashSet<ApplicationWindow[]>();
        foreach(var pin in runtime.Preferences.Pins){
            string resolved=AppCatalog.ResolveTarget(pin.Target);
            var group=groups.FirstOrDefault(g=>!used.Contains(g)&&g.Any(w=>resolved.Equals(w.WinFileName,StringComparison.OrdinalIgnoreCase)||resolved.Equals("shell:AppsFolder\\"+w.AppUserModelID,StringComparison.OrdinalIgnoreCase)));
            if(group==null)taskDock.Children.Add(PinnedButton(pin));
            else{
                used.Add(group);var button=GroupButton(group);
                if(button.ContextMenu!=null)Menu(button.ContextMenu,"Remover dos fixados",()=>{runtime.Preferences.Pins.Remove(pin);runtime.Save();});
                button.PreviewMouseMove+=(_,e)=>{if(e.LeftButton==MouseButtonState.Pressed&&Keyboard.Modifiers==ModifierKeys.Alt)DragDrop.DoDragDrop(button,new DataObject("Windeck.Pin",pin.Target),DragDropEffects.Move);};
                button.PreviewDrop+=(_,e)=>{if(e.Data.GetData("Windeck.Pin") is string moved){int from=runtime.Preferences.Pins.FindIndex(p=>p.Target==moved),to=runtime.Preferences.Pins.IndexOf(pin);if(from>=0&&to>=0&&from!=to){var item=runtime.Preferences.Pins[from];runtime.Preferences.Pins.RemoveAt(from);runtime.Preferences.Pins.Insert(to,item);runtime.Save();}e.Handled=true;}};
                taskDock.Children.Add(button);
            }
        }
        if(groups.Count>used.Count&&runtime.Preferences.Pins.Count>0)taskDock.Children.Add(new Border{Width=1,Height=27,Background=Ui.Brush("#55708090"),Margin=new Thickness(9,0,9,0)});
        foreach(var group in groups.Where(g=>!used.Contains(g)))taskDock.Children.Add(GroupButton(group));
        taskDock.Children.Add(Ui.Icon("\uE7F4",()=>Navigate(DeckPage.Tray),"Bandeja de aplicativos"));
        taskDock.Children.Add(Ui.Icon("\uE787",()=>Navigate(DeckPage.Calendar),"Calendário e lembretes"));
        taskDock.Children.Add(Ui.Icon("\uE8A7",()=>SystemAction("desktop"),"Mostrar área de trabalho"));
    }
    private Button GroupButton(ApplicationWindow[] windows)
    {
        var button=TaskButton(windows.FirstOrDefault(w=>w.State==ApplicationWindow.WindowState.Active)??windows[0]);
        if(windows.Length>1){
            var badge=Ui.Text(windows.Length.ToString(),9);badge.HorizontalAlignment=HorizontalAlignment.Right;badge.VerticalAlignment=VerticalAlignment.Top;if(button.Content is Grid grid)grid.Children.Add(badge);
            button.PreviewMouseLeftButtonUp+=(_,e)=>{
                if(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))return;
                if(Keyboard.Modifiers.HasFlag(ModifierKeys.Control)){int index=Array.FindIndex(windows,w=>w.State==ApplicationWindow.WindowState.Active);windows[(index+1)%windows.Length].BringToFront();interaction.Collapse();}
                else{var menu=new ContextMenu();foreach(var item in windows)Menu(menu,item.Title,()=>{item.BringToFront();interaction.Collapse();});menu.IsOpen=true;}
                e.Handled=true;
            };
        }
        return button;
    }
    private Button PinnedButton(PinnedApp pin)
    {
        var button=Ui.Button("",()=>{AppCatalog.Launch(pin.Target);interaction.Collapse();},pin.Name);
        button.Width=46;button.Height=48;button.Margin=new Thickness(2);button.Padding=new Thickness(7);
        var icon=AppCatalog.GetIcon(pin.Target);button.Content=icon!=null?new Image{Source=icon,Width=30,Height=30}:Ui.Text(pin.Name[..1].ToUpperInvariant(),20);
        var transform=new ScaleTransform(1,1);button.RenderTransform=transform;button.RenderTransformOrigin=new Point(.5,1);dockTransforms.Add((button,transform));
        button.AllowDrop=true;
        button.PreviewMouseMove+=(_,e)=>{if(e.LeftButton==MouseButtonState.Pressed&&Keyboard.Modifiers==ModifierKeys.Alt)DragDrop.DoDragDrop(button,new DataObject("Windeck.Pin",pin.Target),DragDropEffects.Move);};
        button.DragOver+=(_,e)=>{e.Effects=e.Data.GetDataPresent("Windeck.Pin")?DragDropEffects.Move:DragDropEffects.Copy;e.Handled=true;};
        button.Drop+=(_,e)=>{
            if(e.Data.GetData("Windeck.Pin")is string moved){int from=runtime.Preferences.Pins.FindIndex(p=>p.Target==moved),to=runtime.Preferences.Pins.IndexOf(pin);if(from>=0&&to>=0&&from!=to){var item=runtime.Preferences.Pins[from];runtime.Preferences.Pins.RemoveAt(from);runtime.Preferences.Pins.Insert(to,item);runtime.Save();}}
            else if(e.Data.GetData(DataFormats.FileDrop)is string[] files){AppCatalog.Launch(pin.Target,files);interaction.Collapse();}e.Handled=true;
        };
        var menu=new ContextMenu();Menu(menu,"Abrir",()=>AppCatalog.Launch(pin.Target));Menu(menu,"Remover dos fixados",()=>{runtime.Preferences.Pins.Remove(pin);runtime.Save();});button.ContextMenu=menu;
        return button;
    }
    private Button TaskButton(ApplicationWindow window,bool labeled=false)
    {
        var button=Ui.Button("",()=>{
            if(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))AppCatalog.Launch(!string.IsNullOrWhiteSpace(window.AppUserModelID)?"shell:AppsFolder\\"+window.AppUserModelID:window.WinFileName);
            else if(!window.IsMinimized&&(window.State==ApplicationWindow.WindowState.Active||previousForeground==window.Handle))window.Minimize();else window.BringToFront();interaction.Collapse();
        });
        button.DataContext=window;
        var stateStyle=new Style(typeof(Button),(Style)FindResource(typeof(Button)));
        var active=new DataTrigger{Binding=new Binding(nameof(window.State)),Value=ApplicationWindow.WindowState.Active};active.Setters.Add(new Setter(BorderBrushProperty,Ui.Brush("#88B9FF")));active.Setters.Add(new Setter(BorderThicknessProperty,new Thickness(1)));stateStyle.Triggers.Add(active);
        var attention=new DataTrigger{Binding=new Binding(nameof(window.State)),Value=ApplicationWindow.WindowState.Flashing};attention.Setters.Add(new Setter(BackgroundProperty,Ui.Brush("#735527")));stateStyle.Triggers.Add(attention);button.Style=stateStyle;button.SetBinding(ToolTipProperty,new Binding(nameof(window.Title)));
        button.SetBinding(System.Windows.Automation.AutomationProperties.NameProperty,new Binding(nameof(window.Title)));
        button.Width=labeled?245:48;button.Height=labeled?64:48;button.Margin=new Thickness(3);button.Padding=new Thickness(7);
        var grid=new Grid();grid.ColumnDefinitions.Add(new(){Width=labeled?new GridLength(42):new GridLength(1,GridUnitType.Star)});if(labeled)grid.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});
        var icon=new Image{Width=30,Height=30};icon.SetBinding(Image.SourceProperty,new Binding(nameof(window.Icon)));grid.Children.Add(icon);
        if(labeled){var label=Ui.Text("",13);label.SetBinding(TextBlock.TextProperty,new Binding(nameof(window.Title)));Grid.SetColumn(label,1);grid.Children.Add(label);}
        var overlay=new Image{Width=14,Height=14,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Bottom};overlay.SetBinding(Image.SourceProperty,new Binding(nameof(window.OverlayIcon)));grid.Children.Add(overlay);
        var progress=new ProgressBar{Minimum=0,Maximum=100,Height=3,VerticalAlignment=VerticalAlignment.Bottom,Background=Brushes.Transparent,BorderThickness=new Thickness(0),Foreground=Ui.Brush("#80B3FF")};progress.SetBinding(ProgressBar.ValueProperty,new Binding(nameof(window.ProgressValue)));grid.Children.Add(progress);
        button.Content=grid;
        var transform=new ScaleTransform(1,1);button.RenderTransform=transform;button.RenderTransformOrigin=new Point(.5,1);if(!labeled)dockTransforms.Add((button,transform));
        button.MouseEnter+=(_,_)=>{hoverTask=window;hoverTimer.Stop();hoverTimer.Start();};
        button.MouseLeave+=(_,_)=>{hoverTimer.Stop();hoverTask=null;thumbnailDismiss.Start();};
        button.PreviewMouseUp+=(_,e)=>{if(e.ChangedButton==MouseButton.Middle){if(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))window.Close();else AppCatalog.Launch(!string.IsNullOrWhiteSpace(window.AppUserModelID)?"shell:AppsFolder\\"+window.AppUserModelID:window.WinFileName);e.Handled=true;}};
        button.AllowDrop=true;
        DateTime dragStart=DateTime.MinValue;
        button.DragEnter+=(_,_)=>dragStart=DateTime.Now;
        button.DragOver+=(_,e)=>{if(DateTime.Now-dragStart>TimeSpan.FromMilliseconds(550))window.BringToFront();e.Effects=DragDropEffects.Copy;e.Handled=true;};
        button.Drop+=(_,e)=>{if(e.Data.GetData(DataFormats.FileDrop)is string[] files&&!string.IsNullOrWhiteSpace(window.WinFileName)){AppCatalog.Launch(window.WinFileName,files);interaction.Collapse();}e.Handled=true;};
        var menu=new ContextMenu();
        Menu(menu,"Ativar",window.BringToFront);Menu(menu,"Restaurar",window.Restore);Menu(menu,"Minimizar",window.Minimize);Menu(menu,"Maximizar",window.Maximize);Menu(menu,"Mover",window.Move);Menu(menu,"Redimensionar",window.Size);
        string target=!string.IsNullOrWhiteSpace(window.AppUserModelID)?"shell:AppsFolder\\"+window.AppUserModelID:window.WinFileName;
        if(!string.IsNullOrWhiteSpace(target)){Menu(menu,"Fixar no Windeck",()=>Pin(window.WinFileDescription??window.Title,target));Menu(menu,"Abrir nova instância",()=>AppCatalog.Launch(target));}
        if(!string.IsNullOrWhiteSpace(window.AppUserModelID)) {
            var recent=new MenuItem{Header="Documentos recentes"};menu.Items.Add(recent);
            recent.SubmenuOpened+=async(_,_)=>{
                recent.Items.Clear();recent.Items.Add(new MenuItem{Header="Consultando…",IsEnabled=false});
                var documents=await Task.Run(()=>RecentDocuments.Get(window.AppUserModelID));recent.Items.Clear();
                foreach(var (name,path) in documents){var document=new MenuItem{Header=name,ToolTip=path};document.Click+=(_,_)=>Win32.Open(path);recent.Items.Add(document);}
                if(documents.Count==0)recent.Items.Add(new MenuItem{Header="Nenhum documento disponível",IsEnabled=false});
            };
        }
        Menu(menu,"Fechar janela",window.Close);button.ContextMenu=menu;
        return button;
    }
    private static void Menu(ContextMenu menu,string label,Action action){var item=new MenuItem{Header=label};item.Click+=(_,_)=>{try{action();}catch(Exception e){App.Log(e);}};menu.Items.Add(item);}
    private void Pin(string name,string target)
    {if(!runtime.Preferences.Pins.Any(p=>p.Target.Equals(target,StringComparison.OrdinalIgnoreCase))){runtime.Preferences.Pins.Add(new(name,target));runtime.Save();}}
    private FrameworkElement BuildWindows()
    {
        var wrap=new WrapPanel();foreach(var window in runtime.Tasks)wrap.Children.Add(TaskButton(window,true));
        var actions=Ui.Row(Ui.Button("Visão de tarefas",()=>SystemAction("tasks")),Ui.Button("Nova área de trabalho",()=>{interaction.Collapse();Win32.Shortcut(0x5b,0x11,0x44);}),Ui.Button("Área anterior",()=>{interaction.Collapse();Win32.Shortcut(0x5b,0x11,0x25);}),Ui.Button("Próxima área",()=>{interaction.Collapse();Win32.Shortcut(0x5b,0x11,0x27);}));
        return Ui.Scroll(Ui.Stack(Ui.Heading("Seu trabalho, em foco"),actions,wrap,Ui.Text("Passe sobre uma janela para ver a prévia. Botão do meio abre outra instância; Shift+meio fecha. Botão direito mostra as ações.",11,true)));
    }
    private FrameworkElement BuildApps()
    {
        search=new TextBox{Text="",ToolTip="Pesquisar aplicativos e arquivos indexados"};System.Windows.Automation.AutomationProperties.SetName(search,"Pesquisar aplicativos e arquivos");
        appResults=new WrapPanel();searchFiles=new StackPanel();
        search.TextChanged+=(_,_)=>{RenderApps(search.Text);StartFileSearch(search.Text);};
        search.KeyDown+=(_,e)=>{if(e.Key==Key.Enter){var match=runtime.Catalog.FirstOrDefault(a=>a.Name.Contains(search.Text,StringComparison.CurrentCultureIgnoreCase));if(match!=null){AppCatalog.Launch(match.Target);interaction.Collapse();}}};
        var actions=Ui.Row(Ui.Button("Iniciar do Windows",()=>SystemAction("start")),Ui.Button("Pesquisa completa",()=>SystemAction("search")),Ui.Button("Menu avançado · Win+X",()=>SystemAction("winx")));
        var body=Ui.Stack(Ui.Heading("Abra o próximo passo"),search,actions,Ui.Text("APLICATIVOS",11,true),appResults,searchFiles);RenderApps("");return Ui.Scroll(body);
    }
    private void RenderApps(string query)
    {
        if(appResults==null)return;appResults.Children.Clear();
        var apps=runtime.Catalog.Where(a=>a.Name.Contains(query,StringComparison.CurrentCultureIgnoreCase)).Take(string.IsNullOrWhiteSpace(query)?72:40);
        foreach(var app in apps){
            var button=Ui.Button("",()=>{AppCatalog.Launch(app.Target);interaction.Collapse();},app.Name);button.Width=130;button.Height=85;button.Margin=new Thickness(4);button.Padding=new Thickness(6);
            var icon=app.Icon!=null?(FrameworkElement)new Image{Source=app.Icon,Width=30,Height=30}:Ui.Text(app.Name[..1],24);
            icon.HorizontalAlignment=HorizontalAlignment.Center;var name=Ui.Text(app.Name,11);name.HorizontalAlignment=HorizontalAlignment.Center;button.Content=Ui.Stack(icon,name);
            var menu=new ContextMenu();Menu(menu,"Fixar no Windeck",()=>Pin(app.Name,app.Target));button.ContextMenu=menu;appResults.Children.Add(button);
        }
        if(runtime.Catalog.Count==0)appResults.Children.Add(Ui.Text("Carregando aplicativos instalados…",13,true));
    }
    private async void StartFileSearch(string query)
    {
        searchCancel?.Cancel();searchCancel=new();var token=searchCancel.Token;
        if(searchFiles==null)return;searchFiles.Children.Clear();if(query.Trim().Length<2)return;
        try{
            await Task.Delay(280,token);var files=await FileSearch.FindAsync(query,token);
            if(token.IsCancellationRequested||searchFiles==null)return;
            if(files.Count>0)searchFiles.Children.Add(Ui.Heading("Arquivos indexados"));
            foreach(var (name,path) in files)searchFiles.Children.Add(Ui.Button(name,()=>{Win32.Open(path);interaction.Collapse();},path));
        }catch(OperationCanceledException){}catch(Exception e){App.Log(e);if(!token.IsCancellationRequested)searchFiles?.Children.Add(Ui.Text("Índice indisponível. Use Pesquisa completa para abrir a busca do Windows.",11,true));}
    }
    private FrameworkElement BuildTray()
    {
        var panel=Ui.Stack(Ui.Heading("Todos os seus aplicativos, por perto"),Ui.Text("Ícones reais da bandeja. Clique direito abre o menu do próprio aplicativo.",12,true));
        var pinned=new WrapPanel();var others=new WrapPanel();
        foreach(var item in runtime.Desktop.Shell.NotificationArea.PinnedIcons)if(item is NotifyIcon icon)pinned.Children.Add(TrayButton(icon));
        foreach(var item in runtime.Desktop.Shell.NotificationArea.UnpinnedIcons)if(item is NotifyIcon icon)others.Children.Add(TrayButton(icon));
        panel.Children.Add(Ui.Card(pinned));panel.Children.Add(Ui.Text("OUTROS ÍCONES",11,true));panel.Children.Add(Ui.Card(others));
        panel.Children.Add(Ui.Row(Ui.Button("Rede e controles rápidos",()=>SystemAction("quick")),Ui.Button("Notificações do Windows",()=>SystemAction("notifications")),Ui.Button("Idioma e teclado",()=>SystemAction("input"))));
        return Ui.Scroll(panel);
    }
    private Button TrayButton(NotifyIcon icon)
    {
        var button=new Button{DataContext=icon,Width=62,Height=58,Margin=new Thickness(5),Padding=new Thickness(15)};
        var image=new Image();image.SetBinding(Image.SourceProperty,new Binding(nameof(icon.Icon)));button.Content=image;button.SetBinding(ToolTipProperty,new Binding(nameof(icon.Title)));
        button.SetBinding(System.Windows.Automation.AutomationProperties.NameProperty,new Binding(nameof(icon.Title)));
        void Placement(){var p=button.PointToScreen(new Point());icon.Placement=new(){Left=(int)p.X,Top=(int)p.Y,Right=(int)(p.X+button.ActualWidth*scale),Bottom=(int)(p.Y+button.ActualHeight*scale)};PublishTrayPosition();}
        bool skipNextUp=false;
        button.PreviewMouseDown+=(_,e)=>{
            if(e.ChangedButton==MouseButton.Right&&Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)){
                var pinMenu=new ContextMenu();Menu(pinMenu,icon.IsPinned?"Desafixar da bandeja":"Fixar na bandeja",()=>{icon.IsPinned=!icon.IsPinned;runtime.Preferences.PinnedTray=runtime.Desktop.Shell.NotificationArea.PinnedNotifyIcons.ToList();runtime.Save();});pinMenu.IsOpen=true;skipNextUp=true;e.Handled=true;return;
            }
            Placement();icon.IconMouseDown(e.ChangedButton,Win32.MousePoint(),(int)Win32.GetDoubleClickTime());e.Handled=true;
        };
        button.PreviewMouseUp+=(_,e)=>{if(skipNextUp){skipNextUp=false;e.Handled=true;return;}Placement();icon.IconMouseUp(e.ChangedButton,Win32.MousePoint(),(int)Win32.GetDoubleClickTime());e.Handled=true;};
        button.MouseEnter+=(_,_)=>{Placement();icon.IconMouseEnter(Win32.MousePoint());};button.MouseLeave+=(_,_)=>icon.IconMouseLeave(Win32.MousePoint());button.MouseMove+=(_,_)=>icon.IconMouseMove(Win32.MousePoint());
        return button;
    }
}
