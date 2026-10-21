using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
namespace SchoolApp;
public static class Ui
{
 public static readonly Brush Green=new SolidColorBrush(Color.FromRgb(231,250,191));
 public static readonly Brush Accent=new SolidColorBrush(Color.FromRgb(4,160,255));
 public static void Setup(Window w,string title){w.Title=title;w.FontFamily=new FontFamily("Comic Sans MS");w.FontSize=14;w.Background=Brushes.White;w.Icon=Picture("Assets/school_logo.ico");w.WindowStartupLocation=WindowStartupLocation.CenterOwner;}
 public static BitmapImage? Picture(string p){try{if(string.IsNullOrWhiteSpace(p))return null;var b=new BitmapImage();b.BeginInit();b.UriSource=new Uri(Path.IsPathRooted(p)?p:Path.Combine(AppContext.BaseDirectory,p));b.CacheOption=BitmapCacheOption.OnLoad;b.EndInit();b.Freeze();return b;}catch{return null;}}
 public static StackPanel Header(string title){var s=new StackPanel{Orientation=Orientation.Horizontal,Background=Green,Margin=new Thickness(0,0,0,12)};s.Children.Add(new Image{Source=Picture("Assets/school_logo.png"),Width=64,Height=64,Margin=new Thickness(12)});s.Children.Add(new TextBlock{Text=title,FontSize=23,VerticalAlignment=VerticalAlignment.Center});return s;}
 public static Button Button(string text,Action action){var b=new Button{Content=text,Padding=new Thickness(12,7,12,7),Margin=new Thickness(4),Background=Accent,Foreground=Brushes.White};b.Click+=(_,_)=>Guard(action);return b;}
 public static void Guard(Action action){try{action();}catch(Exception e){MessageBox.Show(e.Message,"Ошибка",MessageBoxButton.OK,MessageBoxImage.Warning);}}
 public static TextBox Field(Panel panel,string label,string value=""){panel.Children.Add(new TextBlock{Text=label,Margin=new Thickness(4,8,4,2)});var t=new TextBox{Text=value,Margin=new Thickness(4),Padding=new Thickness(6)};panel.Children.Add(t);return t;}
}
