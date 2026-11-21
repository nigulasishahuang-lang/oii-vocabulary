using System;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Drawing.Text;

public static class FontTools {
 public static string DefaultFamily {
  get {
   try{using(var family=new FontFamily("Microsoft YaHei UI"))return family.Name;}
   catch(ArgumentException){return SystemFonts.MessageBoxFont.FontFamily.Name;}
  }
 }
 public static Font Create(string name,float size,FontStyle style){using(var family=new FontFamily(name)){FontStyle actual=style;if(!family.IsStyleAvailable(actual)){actual=new[]{FontStyle.Regular,FontStyle.Bold,FontStyle.Italic,FontStyle.Bold|FontStyle.Italic}.First(family.IsStyleAvailable);}return new Font(family,size,actual);}}
}

public static class FontCatalog {
 static readonly Lazy<Task<string[]>> pending=new Lazy<Task<string[]>>(()=>Task.Factory.StartNew(()=>{using(var fonts=new InstalledFontCollection()){var families=fonts.Families;try{return families.Select(f=>f.Name).Distinct().OrderBy(n=>n).ToArray();}finally{foreach(var family in families)family.Dispose();}}}));
 public static Task<string[]> Names {get{return pending.Value;}}
 public static void Warm(){var task=Names;}
}

public class AppearanceSettings {
 public string Family=FontTools.DefaultFamily,DataDirectory=StorageLocations.Root;
 public int WordSize=29,MeaningSize=19,ExampleSize=14,TranslationSize=15,ListSize=12,Sidebar=300,Inset=20,ButtonHeight=50;
 public Color Background=Color.FromArgb(245,248,243),Card=Color.White,Text=Color.FromArgb(23,36,28),Accent=Color.FromArgb(33,113,69);
 public bool ShowPhonetic=true,ShowExamples=true,ShowCounts=true;
 public AppearanceSettings Copy(){return (AppearanceSettings)MemberwiseClone();}
}
public static class AppearanceStore {
 public static string FileName=Path.Combine(StorageLocations.Root,"界面设置.xml");
 public static AppearanceSettings Load(){try{return Read(FileName);}catch{try{return Read(FileName+".bak");}catch{return new AppearanceSettings();}}}
 internal static AppearanceSettings Read(string path){var root=XElement.Load(path);var s=new AppearanceSettings();foreach(var field in typeof(AppearanceSettings).GetFields()){var a=root.Attribute(field.Name);if(a==null)continue;object value=field.FieldType==typeof(int)?(object)int.Parse(a.Value):field.FieldType==typeof(bool)?(object)bool.Parse(a.Value):field.FieldType==typeof(Color)?(object)Color.FromArgb(int.Parse(a.Value)):a.Value;field.SetValue(s,value);}Validate(s);return s;}
 public static void Validate(AppearanceSettings s){if(!Path.IsPathRooted(s.DataDirectory))throw new Exception("请选择完整的缓存文件夹路径");try{using(var family=new FontFamily(s.Family)){} }catch(ArgumentException){s.Family=FontTools.DefaultFamily;}if(s.WordSize<18||s.WordSize>42||s.MeaningSize<12||s.MeaningSize>28||s.ExampleSize<10||s.ExampleSize>22||s.TranslationSize<10||s.TranslationSize>24||s.ListSize<10||s.ListSize>18||s.Sidebar<260||s.Sidebar>380||s.Inset<0||s.Inset>40||s.ButtonHeight<40||s.ButtonHeight>64)throw new Exception("设置数值超出允许范围");}
 public static void Save(AppearanceSettings s){Validate(s);var root=new XElement("appearance");foreach(var f in typeof(AppearanceSettings).GetFields()){object v=f.GetValue(s);root.SetAttributeValue(f.Name,v is Color?((Color)v).ToArgb().ToString():v.ToString());}Directory.CreateDirectory(Path.GetDirectoryName(FileName));string temp=FileName+".tmp";root.Save(temp);if(File.Exists(FileName)){bool valid=false;try{Read(FileName);valid=true;}catch{}File.Replace(temp,FileName,valid?FileName+".bak":null);}else File.Move(temp,FileName);}
}
