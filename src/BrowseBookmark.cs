using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Globalization;
using System.Collections.Generic;

public class BankBookmark {
 public int Frequency=1,SelectedIndex;
 public string Word="",Checkpoint="";
 public double ScrollRows;
 public bool Definitions=true;
}
public class BrowseBookmark {
 public string ActiveBank="reading",ReviewCheckpoint="",UnmasteredCheckpoint="";
 public HashSet<string> ReviewAdded=new HashSet<string>(StringComparer.OrdinalIgnoreCase),ReviewRemoved=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 public HashSet<string> ReviewUnmastered=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 public readonly List<string> ReviewOrder=new List<string>();
 public Dictionary<string,double> SentenceScroll=new Dictionary<string,double>();
 public Dictionary<string,BankBookmark> Banks=new Dictionary<string,BankBookmark>();
 public BankBookmark Get(string bank){BankBookmark b;if(!Banks.TryGetValue(bank,out b)){b=new BankBookmark();if(bank=="review"||bank=="unmastered"||bank=="sentences")b.Frequency=0;Banks[bank]=b;}return b;}
}
public static class BookmarkStore {
 public static string PathName=Path.Combine(StorageLocations.Root,"词库浏览存档.xml");
 static BrowseBookmark Read(string path){
  using(var reader=XmlReader.Create(path,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null})){
   var root=XElement.Load(reader);if(root.Name!="vocabulary-bookmark")throw new InvalidDataException();
   var state=new BrowseBookmark();state.UnmasteredCheckpoint=(string)root.Attribute("unmastered-checkpoint")??"";state.ReviewCheckpoint=(string)root.Attribute("review-checkpoint")??"";string active=(string)root.Attribute("active");state.ActiveBank=active=="listening"||active=="hard"||active=="review"||active=="unmastered"||active=="phrases"||active=="sentences"?active:"reading";
   foreach(var el in root.Elements("bank")){
    string name=(string)el.Attribute("name");if(name!="reading"&&name!="listening"&&name!="hard"&&name!="review"&&name!="unmastered"&&name!="phrases"&&name!="sentences")continue;
    int f=(int?)el.Attribute("frequency")??1;int selected=(int?)el.Attribute("selected")??0;
    double scroll=(double?)el.Attribute("scroll")??0;if(Double.IsNaN(scroll)||Double.IsInfinity(scroll))scroll=0;
    state.Banks[name]=new BankBookmark{Frequency=f>=0&&f<7?f:1,SelectedIndex=Math.Max(0,selected),Checkpoint=(string)el.Attribute("checkpoint")??"",Word=(string)el.Attribute("word")??"",ScrollRows=Math.Max(0,scroll),Definitions=(bool?)el.Attribute("definitions")??true};
   }
   foreach(var el in root.Elements("review-added")){string word=((string)el.Attribute("word")??"").Trim();if(word.Length>0&&word.Length<200)state.ReviewAdded.Add(word);}
   foreach(var el in root.Elements("review-removed")){string word=((string)el.Attribute("word")??"").Trim();if(word.Length>0&&word.Length<200)state.ReviewRemoved.Add(word);}
   foreach(var el in root.Elements("review-unmastered")){string word=((string)el.Attribute("word")??"").Trim();if(word.Length>0&&word.Length<200)state.ReviewUnmastered.Add(word);}
   foreach(var el in root.Elements("sentence-scroll")){string word=(string)el.Attribute("word");double y=(double?)el.Attribute("y")??0;if(!String.IsNullOrEmpty(word)&&!Double.IsNaN(y)&&!Double.IsInfinity(y))state.SentenceScroll[word]=Math.Max(0,Math.Min(65535,y));}
   ReadReviewOrder(root,state);
   return state;
  }
 }
 public static BrowseBookmark Load(){return Load(PathName);}
 public static BrowseBookmark Load(string path){BrowseBookmark state;try{state=Read(path);}catch{try{state=Read(path+".bak");}catch{state=new BrowseBookmark();}}ReadReviewList(path,state);return state;}
 public static string ReviewPath(string path){return Path.Combine(Path.GetDirectoryName(path),Path.GetFileNameWithoutExtension(path)+"_待复习.xml");}
 static XElement ReadReviewRoot(string path){using(var reader=XmlReader.Create(path,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null})){var root=XElement.Load(reader);if(root.Name!="review-list")throw new InvalidDataException();return root;}}
 static void ReadReviewOrder(XElement root,BrowseBookmark state){
  var order=root.Element("review-order");
  if(order==null)return;
  state.ReviewOrder.Clear();
  state.ReviewOrder.AddRange(order.Elements("word").Select(el=>el.Value.Trim())
   .Where(word=>word.Length>0&&word.Length<200).Distinct(StringComparer.OrdinalIgnoreCase));
 }
 static void WriteReviewOrder(XElement root,BrowseBookmark state){
  if(state.ReviewOrder.Count>0)root.Add(new XElement("review-order",state.ReviewOrder.Select(word=>new XElement("word",word))));
 }
 static void ReadReviewList(string path,BrowseBookmark state){XElement root;string file=ReviewPath(path);try{root=ReadReviewRoot(file);}catch{try{root=ReadReviewRoot(file+".bak");}catch{return;}}var uc=root.Attribute("unmastered-checkpoint");if(uc!=null)state.UnmasteredCheckpoint=uc.Value;var checkpoint=root.Attribute("checkpoint");if(checkpoint!=null)state.ReviewCheckpoint=checkpoint.Value;int version;bool hasHighlights=(Int32.TryParse((string)root.Attribute("version"),out version)&&version>=2)||root.Elements("unmastered").Any();if(hasHighlights)state.ReviewUnmastered.Clear();state.ReviewAdded.Clear();state.ReviewRemoved.Clear();foreach(var el in root.Elements()){string word=((string)el.Attribute("word")??"").Trim();if(word.Length==0||word.Length>=200)continue;if(el.Name=="added")state.ReviewAdded.Add(word);if(el.Name=="removed")state.ReviewRemoved.Add(word);if(el.Name=="unmastered")state.ReviewUnmastered.Add(word);}ReadReviewOrder(root,state);}
 static void SaveReviewList(string path,BrowseBookmark state){string file=ReviewPath(path);var root=new XElement("review-list",new XAttribute("version",2),new XAttribute("checkpoint",state.ReviewCheckpoint??""),new XAttribute("unmastered-checkpoint",state.UnmasteredCheckpoint??""));foreach(string word in state.ReviewAdded.OrderBy(w=>w))root.Add(new XElement("added",new XAttribute("word",word)));foreach(string word in state.ReviewRemoved.OrderBy(w=>w))root.Add(new XElement("removed",new XAttribute("word",word)));foreach(string word in state.ReviewUnmastered.OrderBy(w=>w))root.Add(new XElement("unmastered",new XAttribute("word",word)));WriteReviewOrder(root,state);string temp=file+".tmp";root.Save(temp);if(File.Exists(file)){bool valid=false;try{ReadReviewRoot(file);valid=true;}catch{}File.Replace(temp,file,valid?file+".bak":null);}else File.Move(temp,file);}
 public static void Save(BrowseBookmark state){Save(PathName,state);}
 public static void Save(string path,BrowseBookmark state){
  Directory.CreateDirectory(Path.GetDirectoryName(path));var root=new XElement("vocabulary-bookmark",new XAttribute("active",state.ActiveBank),new XAttribute("review-checkpoint",state.ReviewCheckpoint??""),new XAttribute("unmastered-checkpoint",state.UnmasteredCheckpoint??""));
  foreach(var kv in state.Banks){var b=kv.Value;root.Add(new XElement("bank",new XAttribute("name",kv.Key),new XAttribute("frequency",b.Frequency),new XAttribute("selected",b.SelectedIndex),new XAttribute("word",b.Word??""),new XAttribute("checkpoint",b.Checkpoint??""),new XAttribute("scroll",b.ScrollRows),new XAttribute("definitions",b.Definitions)));}
  foreach(string word in state.ReviewAdded.OrderBy(w=>w))root.Add(new XElement("review-added",new XAttribute("word",word)));
  foreach(string word in state.ReviewRemoved.OrderBy(w=>w))root.Add(new XElement("review-removed",new XAttribute("word",word)));
  foreach(string word in state.ReviewUnmastered.OrderBy(w=>w))root.Add(new XElement("review-unmastered",new XAttribute("word",word)));
  foreach(var item in state.SentenceScroll)root.Add(new XElement("sentence-scroll",new XAttribute("word",item.Key),new XAttribute("y",item.Value)));
  // The review list has its own atomic file, independent of browsing progress.
  WriteReviewOrder(root,state);
  SaveReviewList(path,state);
  string temp=path+".tmp";root.Save(temp);if(File.Exists(path)){bool valid=false;try{Read(path);valid=true;}catch{}File.Replace(temp,path,valid?path+".bak":null);}else File.Move(temp,path);
 }
 public static void Test(){
  string path=Path.Combine(Path.GetTempPath(),"vocab-bookmark-"+Guid.NewGuid()+".xml");
  var state=new BrowseBookmark{ActiveBank="listening",ReviewCheckpoint="research",UnmasteredCheckpoint="hypothesis"};state.ReviewUnmastered.UnionWith(new[]{"research","hypothesis"});state.ReviewAdded.Add("research");state.ReviewOrder.AddRange(new[]{"habitat","platform","research"});state.ReviewRemoved.Add("sense");state.SentenceScroll["S01 · Nested clauses"]=125.5;state.Banks["reading"]=new BankBookmark{Frequency=3,SelectedIndex=37,Word="research",ScrollRows=30.25,Definitions=false};state.Banks["listening"]=new BankBookmark{Frequency=2,SelectedIndex=81,Word="acquire",ScrollRows=76.5,Definitions=true};state.Banks["hard"]=new BankBookmark{Word="cognitive",Frequency=4};state.Banks["sentences"]=new BankBookmark{Word="S01 · Nested clauses",Frequency=0,Definitions=false};state.Banks["phrases"]=new BankBookmark{Word="account for",Frequency=2};state.Banks["unmastered"]=new BankBookmark{Word="research",Frequency=0,Definitions=false};state.Banks["review"]=new BankBookmark{Word="adapt",Frequency=0,Definitions=false};Save(path,state);
  var read=Load(path);if(read.UnmasteredCheckpoint!="hypothesis")throw new Exception("未掌握复习标记未保存");if(read.Get("unmastered").Word!="research"||read.Get("unmastered").Definitions||read.Get("unmastered").Frequency!=0)throw new Exception("未掌握复习分类存档异常");if(!read.ReviewUnmastered.SetEquals(new[]{"research","hypothesis"}))throw new Exception("未掌握高亮未保存");if(read.ReviewCheckpoint!="research")throw new Exception("手动复习标记未保存");if(!read.ReviewAdded.Contains("RESEARCH")||!read.ReviewRemoved.Contains("sense")||read.SentenceScroll["S01 · Nested clauses"]!=125.5)throw new Exception("复习清单或精读滚动位置未保存");if(read.Get("sentences").Word!="S01 · Nested clauses"||read.Get("sentences").Definitions)throw new Exception("句子存档异常");if(read.Get("phrases").Word!="account for"||read.Get("phrases").Frequency!=2)throw new Exception("短语存档异常");if(read.Get("review").Word!="adapt"||read.Get("review").Definitions)throw new Exception("待复习存档异常");if(read.Get("hard").Word!="cognitive")throw new Exception("难词库存档异常");if(read.ActiveBank!="listening"||read.Get("reading").Word!="research"||read.Get("reading").Definitions||read.Get("reading").Frequency!=3||read.Get("reading").ScrollRows!=30.25||read.Get("listening").SelectedIndex!=81)throw new Exception("浏览存档往返异常");
  Save(path,state);File.WriteAllText(path,"broken");if(Load(path).Get("listening").Word!="acquire")throw new Exception("备用存档恢复异常");read.ActiveBank="sentences";Save(path,read);if(Load(path).ActiveBank!="sentences")throw new Exception("句子分类恢复异常");File.Delete(path);File.Delete(path+".bak");var independent=Load(path);if(!independent.ReviewOrder.SequenceEqual(state.ReviewOrder))throw new Exception("复习顺序未从独立存档恢复");if(independent.UnmasteredCheckpoint!="hypothesis")throw new Exception("未掌握复习独立标记未恢复");if(!independent.ReviewUnmastered.SetEquals(new[]{"research","hypothesis"}))throw new Exception("独立存档未恢复高亮");if(independent.ReviewCheckpoint!="research")throw new Exception("独立存档未恢复手动标记");if(!independent.ReviewAdded.Contains("research")||!independent.ReviewRemoved.Contains("sense"))throw new Exception("浏览存档丢失后待复习清单未恢复");File.WriteAllText(ReviewPath(path),"broken");if(!Load(path).ReviewAdded.Contains("research"))throw new Exception("待复习独立备用恢复异常");var removed=Load(path);if(removed.ReviewUnmastered.Count!=2)throw new Exception("独立备用未恢复高亮");removed.ReviewUnmastered.Remove("research");if(removed.ReviewCheckpoint!="research")throw new Exception("独立备用未恢复标记");removed.ReviewCheckpoint="";removed.UnmasteredCheckpoint="";removed.ReviewAdded.Remove("research");removed.ReviewRemoved.Add("research");Save(path,removed);if(!Load(path).ReviewUnmastered.SetEquals(new[]{"hypothesis"}))throw new Exception("取消高亮未独立保存");removed.ReviewUnmastered.Clear();Save(path,removed);if(Load(path).ReviewUnmastered.Count!=0)throw new Exception("取消所有高亮后重新出现");if(Load(path).ReviewCheckpoint!="")throw new Exception("取消标记后重新出现");if(Load(path).ReviewAdded.Contains("research")||!Load(path).ReviewRemoved.Contains("research"))throw new Exception("移除后被重复恢复");File.Delete(path);foreach(string file in new[]{path+".bak",ReviewPath(path),ReviewPath(path)+".bak"})if(File.Exists(file))File.Delete(file);
 }
}
