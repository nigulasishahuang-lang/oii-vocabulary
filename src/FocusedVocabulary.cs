using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

public class Entry {public string Bank,Word,Meaning,Phonetic,Tier,Common,Example,Translation,ExampleSource;public int Coverage,Count,OtherCount,OtherCoverage;public int FrequencyCount {get{return (Bank=="hard"||Bank=="review"||Bank=="phrases")?Count+OtherCount:Count;}}}
public static class Vocabulary {
 static readonly Dictionary<string,string> TopicNames=new Dictionary<string,string>{{"Nested clauses","嵌套从句"},{"Result participles","分词与结果"},{"Gerund subjects","动名词主语"},{"Contrast and complements","对比与表语"},{"Formal subjects","形式主语"},{"Relative clause reference","定语从句指代"},{"Not whether but whether","并列表语从句"},{"Layered modifiers","多层修饰"},{"What clauses","主语从句"},{"Parallel main clauses","并列主句"},{"Concessive perfect forms","让步与完成式"},{"Apposition and result","同位语与结果"},{"Whose and so that","定语从句与结果"},{"Concession and as if","让步与假设"},{"Complement and whose","表语与定语从句"},{"Comparison and ellipsis","比较与省略"},{"Past possibility","过去可能性"},{"Concession and relatives","让步与定语从句"},{"Reduced relative clauses","简化修饰结构"},{"Reason and contrast","原因与对比"},{"Whereas and participles","对比与分词"},{"So that and whose","结果与定语从句"},{"Parallel relative clauses","并列定语从句"},{"Interrupted coordination","插入式并列"},{"Delayed appositive clause","后置同位语从句"},{"Inversion and contrast","倒装与对比"},{"If and whether","条件与让步"},{"Concession and apposition","让步与同位语"},{"Whether complements","宾语从句"},{"It and result inference","形式主语与推论"},{"Cleft emphasis","强调句"},{"Concession and coordination","让步与并列"},{"Subject agreement","主谓一致"},{"Comparative contrast","比较对照"},{"Concession and passive","让步与被动"},{"Reduced time clauses","时间从句省略"}};
 public static string DisplayTitle(Entry w){if(w.Bank!="sentences")return w.Word;string name;if(!TopicNames.TryGetValue(w.Tier,out name))return w.Word;int dot=w.Word.IndexOf("·");return (dot>=0?w.Word.Substring(0,dot).Trim()+" · ":"")+name;}

 static string Text(BinaryReader r){int n=r.ReadInt32();if(n<0||n>2000000)throw new InvalidDataException();return Encoding.UTF8.GetString(r.ReadBytes(n));}
 public static Entry[] Load(){using(var resource=Assembly.GetExecutingAssembly().GetManifestResourceStream("words.gz"))using(var gz=new GZipStream(resource,CompressionMode.Decompress))using(var r=new BinaryReader(gz)){
  if(r.ReadInt32()!=6)throw new InvalidDataException();int n=r.ReadInt32();if(n<1||n>100000)throw new InvalidDataException();var all=new Entry[n];for(int i=0;i<n;i++)all[i]=new Entry{Bank=Text(r),Word=Text(r),Meaning=Text(r),Phonetic=Text(r),Tier=Text(r),Coverage=r.ReadInt32(),Count=r.ReadInt32(),OtherCount=r.ReadInt32(),OtherCoverage=r.ReadInt32(),Common=Text(r),Example=Text(r),Translation=Text(r),ExampleSource=Text(r)};return all;
 }}
}
public class WordList:Control {
 HashSet<string> unmasteredWords;public HashSet<string> UnmasteredWords {get{return unmasteredWords;}set{if(Object.ReferenceEquals(unmasteredWords,value))return;unmasteredWords=value;Invalidate();}}
 string markedWord="";public string MarkedWord {get{return markedWord;}set{if(markedWord==value)return;markedWord=value??"";Invalidate();}}
 readonly VScrollBar bar=new VScrollBar();Font wordFont=new Font("Segoe UI",12,FontStyle.Bold),metaFont=new Font("Microsoft YaHei UI",9);
 Entry[] rows=new Entry[0];int offset,selected=-1;bool updating;public event EventHandler SelectionChanged;public event EventHandler ScrollChanged;public int PaintedRows;
 public int RowHeight {get{return Math.Max(64,wordFont.Height*2+20);}}
 public Color TextColor=Color.FromArgb(36,65,45),SelectionColor=Color.FromArgb(224,243,228);
 public void Customize(AppearanceSettings a,Color selected){double scroll=ScrollRows;wordFont.Dispose();metaFont.Dispose();wordFont=FontTools.Create(a.Family,a.ListSize,FontStyle.Bold);metaFont=FontTools.Create(a.Family,9,FontStyle.Regular);BackColor=a.Card;TextColor=a.Text;SelectionColor=selected;Measure();ScrollRows=scroll;Invalidate();}
 public int SelectedIndex {get{return selected;}set{int next=rows.Length==0?-1:Math.Max(0,Math.Min(rows.Length-1,value));if(next==selected)return;selected=next;EnsureVisible();Invalidate();if(SelectionChanged!=null)SelectionChanged(this,EventArgs.Empty);}}
 public Entry Current {get{return selected<0?null:rows[selected];}}
 public int Count {get{return rows.Length;}}
 public double ScrollRows {get{return offset/(double)RowHeight;}set{ScrollTo((int)Math.Min(Int32.MaxValue,Math.Max(0,value)*RowHeight));}}
 public int FindWord(string word){return Array.FindIndex(rows,w=>w.Word==word);}
 public WordList(){ImeMode=ImeMode.Disable;SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.UserPaint|ControlStyles.ResizeRedraw|ControlStyles.Selectable,true);Font=new Font("Microsoft YaHei UI",10);BackColor=Color.White;TabStop=true;AccessibleName="左侧词条列表";bar.Dock=DockStyle.Right;Controls.Add(bar);bar.ValueChanged+=delegate{if(!updating){offset=bar.Value;Invalidate();if(ScrollChanged!=null)ScrollChanged(this,EventArgs.Empty);}};}
 public void SetRows(Entry[] value,int initialIndex=0,double initialScrollRows=0){
  rows=value;
  selected=-1;
  offset=(int)Math.Min(Int32.MaxValue,Math.Max(0,initialScrollRows)*RowHeight);
  Measure();
  if(rows.Length>0)SelectedIndex=initialIndex;
  else{Invalidate();if(SelectionChanged!=null)SelectionChanged(this,EventArgs.Empty);}
 }
 void Measure(){updating=true;int view=Math.Max(1,ClientSize.Height);bar.LargeChange=view;bar.SmallChange=RowHeight;bar.Maximum=Math.Max(0,rows.Length*RowHeight-1);offset=Math.Min(offset,Math.Max(0,rows.Length*RowHeight-view));bar.Value=offset;bar.Enabled=rows.Length*RowHeight>view;updating=false;}
 void ScrollTo(int target){int max=Math.Max(0,rows.Length*RowHeight-Math.Max(1,Height));offset=Math.Max(0,Math.Min(max,target));updating=true;bar.Value=offset;updating=false;Invalidate();if(ScrollChanged!=null)ScrollChanged(this,EventArgs.Empty);}
 void EnsureVisible(){if(selected<0)return;int top=selected*RowHeight;if(top<offset)ScrollTo(top);else if(top+RowHeight>offset+Height)ScrollTo(top+RowHeight-Height);}
 protected override void OnResize(EventArgs e){base.OnResize(e);Measure();RoundUi.Clip(this,12);}
 protected override void OnMouseWheel(MouseEventArgs e){ScrollTo(offset-(int)Math.Round(e.Delta*RowHeight/120.0));base.OnMouseWheel(e);}
 protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);Focus();if(e.X<Width-bar.Width){int i=(e.Y+offset)/RowHeight;if(i<rows.Length)SelectedIndex=i;}}
 protected override bool IsInputKey(Keys k){return k==Keys.Up||k==Keys.Down||k==Keys.Home||k==Keys.End||base.IsInputKey(k);}
 protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode==Keys.Up)SelectedIndex--;else if(e.KeyCode==Keys.Down)SelectedIndex++;else if(e.KeyCode==Keys.Home)SelectedIndex=0;else if(e.KeyCode==Keys.End)SelectedIndex=rows.Length-1;else return;e.Handled=true;}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(BackColor);PaintedRows=0;int width=Width-bar.Width;using(var line=new Pen(Color.FromArgb(234,240,234)))for(int i=offset/RowHeight,y=-(offset%RowHeight);i<rows.Length&&y<Height;i++,y+=RowHeight){PaintedRows++;var w=rows[i];bool unmastered=UnmasteredWords!=null&&UnmasteredWords.Contains(w.Word);if(i==selected||unmastered){Color fill=unmastered?(i==selected?Color.FromArgb(249,217,139):Color.FromArgb(255,241,203)):SelectionColor;RoundUi.Fill(g,new RectangleF(5,y+4,width-10,RowHeight-8),10,fill,Color.Transparent,0);}TextRenderer.DrawText(g,Vocabulary.DisplayTitle(w),wordFont,new Rectangle(16,y+10,width-25,RowHeight/2),TextColor,TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);TextRenderer.DrawText(g,(w.Bank=="sentences"?w.Count+"个单词":"出现 "+w.FrequencyCount+" 次"+(unmastered?" · 未过关":""))+(String.Equals(w.Word,MarkedWord,StringComparison.OrdinalIgnoreCase)?" · 复习标记":""),metaFont,new Rectangle(16,y+RowHeight/2+4,width-25,RowHeight/2-8),unmastered?Color.FromArgb(110,73,15):TextColor,TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis);g.DrawLine(line,12,y+RowHeight-1,width-12,y+RowHeight-1);}if(rows.Length==0)TextRenderer.DrawText(g,"暂无符合条件的词条",metaFont,ClientRectangle,Color.Gray,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);}
 protected override void Dispose(bool disposing){if(disposing){wordFont.Dispose();metaFont.Dispose();}base.Dispose(disposing);}
}
public class FocusedWindow:Form,IMessageFilter {
 string pendingReviewRemoval;
 sealed class RemovedReviewWord {
  public string Word;
  public bool WasAdded,WasRemoved,WasHighlighted,WasReviewCheckpoint,WasUnmasteredCheckpoint;
 }
 RemovedReviewWord lastRemoval;
 readonly Timer removalNoticeTimer=new Timer{Interval=3000};
 RoundedCard removalNotice;
 Label removalNoticeText;
 Button undoRemovalButton;
 Panel header;
 Label appTitle;
 Button settingsButton;
 FlowLayoutPanel categoryTabs;
 bool leftAltPending,leftAltCombined;
 bool HandleLeftAlt(int message,Keys key,bool extended,bool repeat,Keys modifiers){bool down=message==0x100||message==0x104,up=message==0x101||message==0x105;if(!down&&!up)return false;bool left=(key==Keys.Menu||key==Keys.LMenu)&&!extended;if(left&&down){if(!repeat){leftAltPending=(modifiers&(Keys.Control|Keys.Shift))==0;leftAltCombined=false;}return leftAltPending;}if(left&&up){bool jump=leftAltPending&&!leftAltCombined;leftAltPending=false;leftAltCombined=false;if(jump){if(String.IsNullOrEmpty(CurrentCheckpoint))status.Text="请先设置当前分类的复习标记。";else JumpReviewCheckpoint();return true;}return false;}if(down&&leftAltPending)leftAltCombined=true;return false;}
 public bool PreFilterMessage(ref Message message){if(message.Msg!=0x100&&message.Msg!=0x101&&message.Msg!=0x104&&message.Msg!=0x105)return false;if(!loaded||!ContainsFocus||Form.ActiveForm!=this||frequency.DroppedDown)return false;long flags=message.LParam.ToInt64();return HandleLeftAlt(message.Msg,(Keys)message.WParam.ToInt32(),(flags&0x1000000L)!=0,(flags&0x40000000L)!=0,Control.ModifierKeys);}
 protected override void OnDeactivate(EventArgs e){leftAltPending=false;leftAltCombined=false;CancelReviewRemoval();base.OnDeactivate(e);}
 public void CheckAltShortcut(){foreach(string category in new[]{"reading","listening","hard","phrases","sentences","review","unmastered"}){Switch(category);frequency.SelectedIndex=0;if(String.IsNullOrEmpty(CurrentCheckpoint)){list.SelectedIndex=Math.Min(2,list.Count-1);MarkReviewCheckpoint();}string marker=CurrentCheckpoint;list.SelectedIndex=list.FindWord(marker);Step(list.SelectedIndex>0?-1:1);string away=list.Current.Word;bool shown=definitions.Checked;FocusWordNavigation();HandleStudyShortcut(Keys.Space);if(definitions.Checked==shown)throw new Exception("space must toggle definitions");HandleStudyShortcut(Keys.Space);if(definitions.Checked!=shown||list.Current.Word!=away)throw new Exception("double space must only toggle definitions");HandleLeftAlt(0x104,Keys.Menu,false,false,Keys.Alt);HandleLeftAlt(0x105,Keys.Menu,false,false,Keys.None);if(list.Current.Word!=marker||definitions.Checked!=shown)throw new Exception("left Alt bookmark jump");Step(list.SelectedIndex>0?-1:1);away=list.Current.Word;HandleLeftAlt(0x104,Keys.Menu,true,false,Keys.Alt);HandleLeftAlt(0x105,Keys.Menu,true,false,Keys.None);if(list.Current.Word!=away)throw new Exception("right Alt must not jump");HandleLeftAlt(0x104,Keys.Menu,false,false,Keys.Alt);HandleLeftAlt(0x104,Keys.Tab,false,false,Keys.Alt);HandleLeftAlt(0x105,Keys.Menu,false,false,Keys.None);if(list.Current.Word!=away)throw new Exception("Alt combination must not jump");wordSearch.Focus();if(HandleStudyShortcut(Keys.Space))throw new Exception("space intercepted search typing");FocusWordNavigation();}SaveBookmark(false);}
 Button exportWordButton;
 string CurrentCheckpoint {get{return bank=="unmastered"?bookmark.UnmasteredCheckpoint:bank=="review"?bookmark.ReviewCheckpoint:bookmark.Get(bank).Checkpoint;}set{if(bank=="unmastered")bookmark.UnmasteredCheckpoint=value;else if(bank=="review")bookmark.ReviewCheckpoint=value;else bookmark.Get(bank).Checkpoint=value;}}
 Button unmasteredTab;Entry[] unmasteredRows=new Entry[0];
 Entry[] CurrentRows(){return bank=="reading"?readRows:bank=="listening"?listenRows:bank=="hard"?hardRows:bank=="phrases"?phraseRows:bank=="sentences"?sentenceRows:bank=="unmastered"?unmasteredRows:reviewRows;}
 Entry[] originalReviewRows=new Entry[0];
 void RefreshUnmasteredRows(){unmasteredRows=originalReviewRows.Where(w=>bookmark.ReviewUnmastered.Contains(w.Word)).ToArray();unmasteredTab.Text="未掌握复习 · "+unmasteredRows.Length.ToString("N0");}

 TableLayoutPanel sidebar,reviewCheckpointPanel,reviewActionsPanel;Label reviewCheckpointLabel;Button markReviewPosition,jumpReviewPosition,unmasteredReviewAction;
 public Action ReadyForCheck;
 public void CheckStudyFeatures(){
  if(bank=="unmastered"){var savedCollection=bookmark.Get("unmastered");if(list.Count!=unmasteredRows.Length||list.Current==null||list.Current.Word!=savedCollection.Word||definitions.Checked!=savedCollection.Definitions)throw new Exception("reopened unmastered collection position");}
  if(bookmark.ReviewUnmastered.Count>0){Switch("review");foreach(string word in bookmark.ReviewUnmastered.Where(IsReviewed))if(list.UnmasteredWords==null||!list.UnmasteredWords.Contains(word)||!BookmarkStore.Load().ReviewUnmastered.Contains(word))throw new Exception("reopened unmastered highlights");}
  if(!String.IsNullOrEmpty(bookmark.ReviewCheckpoint)){string savedCheckpoint=bookmark.ReviewCheckpoint;Switch("review");JumpReviewCheckpoint();if(list.Current==null||list.Current.Word!=savedCheckpoint||list.MarkedWord!=savedCheckpoint)throw new Exception("reopened manual review checkpoint");}
  if(bank=="sentences"&&renderedSentence!=null&&definitions.Checked){double previous;if(bookmark.SentenceScroll.TryGetValue(renderedSentence,out previous)&&previous>0&&Math.Abs(ReadingScroll.Get(meaning).Y/DeviceDpiScale()-previous)>4)throw new Exception("reopened sentence scroll");}
  Switch("reading");frequency.SelectedIndex=0;wordSearch.Text="research";JumpToWord();bookmark.ReviewAdded.Remove("research");bookmark.ReviewRemoved.Remove("research");RebuildReview();int before=reviewRows.Length;list.Focus();if(!HandleStudyShortcut(Keys.F)||!IsReviewed("research")||reviewRows.Length!=before+1)throw new Exception("F add review");HandleStudyShortcut(Keys.F);if(reviewRows.Length!=before+1)throw new Exception("duplicate F");var disk=BookmarkStore.Load();if(!disk.ReviewAdded.Contains("research"))throw new Exception("review persistence");Switch("listening");frequency.SelectedIndex=0;wordSearch.Text="research";JumpToWord();list.Focus();HandleStudyShortcut(Keys.F);if(reviewRows.Length!=before+1)throw new Exception("cross-category duplicate");wordSearch.Focus();if(HandleStudyShortcut(Keys.F))throw new Exception("search consumes F");Switch("review");if(frequency.SelectedIndex!=0||list.Current.Word!="research")throw new Exception("review landing");ToggleReview();if(IsReviewed("research")||BookmarkStore.Load().ReviewAdded.Contains("research"))throw new Exception("review remove");Switch("reading");frequency.SelectedIndex=0;wordSearch.Text="sense";JumpToWord();ToggleReview();if(IsReviewed("sense")||!BookmarkStore.Load().ReviewRemoved.Contains("sense"))throw new Exception("demo review removal");AddCurrentReview();if(!IsReviewed("sense"))throw new Exception("demo review restore");wordSearch.Text="research";JumpToWord();list.Focus();HandleStudyShortcut(Keys.F);
  Switch("sentences");list.SelectedIndex=0;definitions.Checked=true;RenderWord();ReadingScroll.Set(meaning,new Point(0,280));CaptureSentenceScroll();string key=list.Current.Word;double scroll=bookmark.SentenceScroll[key];if(scroll<=0)throw new Exception("sentence did not scroll");Switch("reading");Switch("sentences");if(Math.Abs(ReadingScroll.Get(meaning).Y/DeviceDpiScale()-scroll)>4)throw new Exception("sentence scroll switch");definitions.Checked=false;definitions.Checked=true;if(Math.Abs(ReadingScroll.Get(meaning).Y/DeviceDpiScale()-scroll)>4)throw new Exception("sentence scroll toggle");SaveBookmark(false);if(Math.Abs(BookmarkStore.Load().SentenceScroll[key]-scroll)>.1)throw new Exception("sentence scroll persistence");CheckReviewCheckpointFeatures();CheckUnmasteredReviewFeatures();CheckUnmasteredCollection();CheckAltShortcut();CheckReviewRemovalPosition();
 }

 void CheckReviewRemovalPosition(){
  Switch("review");
  frequency.SelectedIndex=1;
  var visible=Filter(reviewRows,Thresholds[frequency.SelectedIndex]);
  int removedIndex=Math.Min(80,visible.Length-2);
  if(removedIndex<1)throw new Exception("review removal test needs a successor");
  list.SelectedIndex=removedIndex;
  string successor=visible[removedIndex+1].Word;
  double scroll=list.ScrollRows;
  string removed=visible[removedIndex].Word;
  reviewAction.PerformClick();
  if(!IsReviewed(removed)||list.Count!=visible.Length||reviewAction.Text!="确认移除")
   throw new Exception("first click must ask for confirmation without removing a word");
  Step(1);
  Step(-1);
  if(reviewAction.Text=="确认移除")throw new Exception("changing words must cancel removal confirmation");
  reviewAction.PerformClick();
  if(!IsReviewed(removed))throw new Exception("returning to the same word must require confirmation again");
  reviewAction.PerformClick();
  if(list.SelectedIndex!=removedIndex||list.Current.Word!=successor||frequency.SelectedIndex!=1||Math.Abs(list.ScrollRows-scroll)>1)
   throw new Exception("removing a review word must keep its filtered position and select the successor");
  var saved=BookmarkStore.Load().Get("review");
  if(saved.Word!=successor||saved.SelectedIndex!=removedIndex)throw new Exception("successor position not saved");
  if(!removalNotice.Visible||removalNoticeText.Text!="删除成功 · "+removed||removalNoticeTimer.Interval!=3000)
   throw new Exception("removal notice did not open for three seconds");
  CheckHeaderLayout();
  WindowState=FormWindowState.Maximized;
  PerformLayout();
  CheckHeaderLayout();
  WindowState=FormWindowState.Normal;
  PerformLayout();
  undoRemovalButton.PerformClick();
  if(!IsReviewed(removed)||list.FindWord(removed)!=removedIndex||list.Current.Word!=successor||removalNotice.Visible)
   throw new Exception("undo must restore original list order and preserve the current word");

  visible=Filter(reviewRows,Thresholds[frequency.SelectedIndex]);
  string predecessor=visible[visible.Length-2].Word;
  list.SelectedIndex=list.Count-1;
  reviewAction.PerformClick();
  reviewAction.PerformClick();
  if(list.SelectedIndex!=list.Count-1||list.Current.Word!=predecessor)
   throw new Exception("removing the last review word must select its predecessor");
  var deadline=Stopwatch.StartNew();
  while(removalNotice.Visible&&deadline.ElapsedMilliseconds<4000){Application.DoEvents();System.Threading.Thread.Sleep(15);}
  if(removalNotice.Visible||lastRemoval!=null||deadline.ElapsedMilliseconds<2800)
   throw new Exception("removal notice did not expire after three seconds");

  const string personalWord="research";
  Switch("reading");
  wordSearch.Text=personalWord;
  JumpToWord();
  AddCurrentReview();
  Switch("review");
  list.SelectedIndex=list.FindWord(personalWord);
  if(list.Current.Word!=personalWord||!bookmark.ReviewAdded.Contains(personalWord))throw new Exception("undo personal word test setup");
  bookmark.ReviewUnmastered.Add(personalWord);
  bookmark.ReviewCheckpoint=bookmark.UnmasteredCheckpoint=personalWord;
  reviewAction.PerformClick();
  reviewAction.PerformClick();
  if(IsReviewed(personalWord)||bookmark.ReviewUnmastered.Contains(personalWord))throw new Exception("personal word was not removed");
  undoRemovalButton.PerformClick();
  var restored=BookmarkStore.Load();
  if(!restored.ReviewAdded.Contains(personalWord)||!restored.ReviewUnmastered.Contains(personalWord)||
     restored.ReviewCheckpoint!=personalWord||restored.UnmasteredCheckpoint!=personalWord)
   throw new Exception("undo must save the restored word, highlight and both checkpoints");
 }
 void CheckHeaderLayout(){
  foreach(var control in new Control[]{appTitle,removalNotice,settingsButton,unmasteredTab})
   if(!header.ClientRectangle.Contains(control.Bounds)||control.Bottom>categoryTabs.Top)
    throw new Exception("header control overlaps the category tabs");
  if(!removalNotice.ClientRectangle.Contains(undoRemovalButton.Bounds)||
     undoRemovalButton.Height<undoRemovalButton.Font.Height||
     removalNoticeText.Height<removalNoticeText.Font.Height||
     undoRemovalButton.Left<removalNoticeText.Right)
   throw new Exception("notice text and undo button must fit side by side");
 }

 public void CheckReviewCheckpointFeatures(){
  if(list.ImeMode!=ImeMode.Disable||meaning.ImeMode!=ImeMode.Disable||wordSearch.ImeMode==ImeMode.Disable)throw new Exception("IME must be disabled only for read-only study controls");
  foreach(var entry in new[]{Tuple.Create("research",Keys.OemPeriod),Tuple.Create("hypothesis",Keys.Decimal)}){Switch("reading");frequency.SelectedIndex=0;wordSearch.Text=entry.Item1;JumpToWord();bookmark.ReviewAdded.Remove(entry.Item1);bookmark.ReviewRemoved.Remove(entry.Item1);RebuildReview();int count=reviewRows.Length;list.Focus();if(!HandleStudyShortcut(entry.Item2)||!IsReviewed(entry.Item1)||reviewRows.Length!=count+1)throw new Exception("period/decimal add failed");HandleStudyShortcut(entry.Item2);if(reviewRows.Length!=count+1)throw new Exception("period duplicated review");}
  Switch("reading");frequency.SelectedIndex=0;wordSearch.Text="evidence";JumpToWord();bookmark.ReviewAdded.Remove("evidence");RebuildReview();int before=reviewRows.Length;list.Focus();if(!HandleReviewPunctuation('。')||!IsReviewed("evidence")||reviewRows.Length!=before+1)throw new Exception("Chinese period add failed");wordSearch.Focus();if(HandleStudyShortcut(Keys.OemPeriod)||HandleStudyShortcut(Keys.Decimal)||HandleReviewPunctuation('.')||HandleReviewPunctuation('。'))throw new Exception("punctuation intercepted search typing");list.Focus();if(HandleStudyShortcut(Keys.Control|Keys.OemPeriod)||HandleStudyShortcut(Keys.Shift|Keys.OemPeriod))throw new Exception("modified period intercepted");
  int totalBefore=reviewRows.Length;Switch("sentences");list.Focus();HandleStudyShortcut(Keys.OemPeriod);if(HandleReviewPunctuation('。')||reviewRows.Length!=totalBefore)throw new Exception("sentence added to word review");
  bookmark.ReviewCheckpoint="";Switch("review");frequency.SelectedIndex=0;var target=reviewRows.First(w=>w.FrequencyCount<100);list.SelectedIndex=list.FindWord(target.Word);markReviewPosition.PerformClick();if(bookmark.ReviewCheckpoint!=target.Word||BookmarkStore.Load().ReviewCheckpoint!=target.Word||list.MarkedWord!=target.Word)throw new Exception("manual checkpoint not persisted");Step(1);if(bookmark.ReviewCheckpoint!=target.Word)throw new Exception("manual checkpoint followed browsing");frequency.SelectedIndex=6;if(list.FindWord(target.Word)>=0)throw new Exception("checkpoint test filter setup");jumpReviewPosition.PerformClick();if(frequency.SelectedIndex!=0||list.Current.Word!=target.Word)throw new Exception("checkpoint hidden by frequency filter");
  wordSearch.Focus();wordSearch.Text="focus-test";jumpReviewPosition.PerformClick();Application.DoEvents();int checkpointIndex=list.SelectedIndex;if(wordSearch.ContainsFocus||!list.ContainsFocus||!HandleStudyShortcut(Keys.D)||list.SelectedIndex!=checkpointIndex+1||wordSearch.Text!="")throw new Exception("D after returning to checkpoint must navigate instead of type");if(!HandleStudyShortcut(Keys.A)||list.Current.Word!=target.Word)throw new Exception("A after returning to checkpoint");wordSearch.Focus();wordSearch.Text="d";if(HandleStudyShortcut(Keys.D)||HandleStudyShortcut(Keys.A)||wordSearch.Text!="d")throw new Exception("search must retain normal typing after checkpoint jump");wordSearch.Clear();FocusWordNavigation();
  Switch("reading");Switch("review");if(list.MarkedWord!=target.Word)throw new Exception("checkpoint lost switching categories");var other=reviewRows.First(w=>w.Word!=target.Word);list.SelectedIndex=list.FindWord(other.Word);markReviewPosition.PerformClick();if(bookmark.ReviewCheckpoint!=other.Word||list.MarkedWord!=other.Word)throw new Exception("checkpoint did not move");markReviewPosition.PerformClick();if(BookmarkStore.Load().ReviewCheckpoint!=""||list.MarkedWord!="")throw new Exception("checkpoint clear failed");markReviewPosition.PerformClick();ToggleReview();if(bookmark.ReviewCheckpoint!=""||BookmarkStore.Load().ReviewCheckpoint!="")throw new Exception("removed word kept checkpoint");
  Switch("reading");frequency.SelectedIndex=0;wordSearch.Text=other.Word;JumpToWord();AddCurrentReview();Switch("review");frequency.SelectedIndex=0;list.SelectedIndex=list.FindWord(target.Word);markReviewPosition.PerformClick();Step(1);SaveBookmark(false);if(BookmarkStore.Load().ReviewCheckpoint!=target.Word)throw new Exception("saved browsing overwrote manual checkpoint");
 }
 public void CheckUnmasteredReviewFeatures(){
  Switch("review");frequency.SelectedIndex=0;bookmark.ReviewUnmastered.Clear();list.Invalidate();var first=reviewRows.First(w=>w.Word==bookmark.ReviewCheckpoint);var second=reviewRows.First(w=>w.Word!=first.Word);int count=reviewRows.Length;string checkpoint=bookmark.ReviewCheckpoint;
  list.SelectedIndex=list.FindWord(first.Word);unmasteredReviewAction.PerformClick();list.SelectedIndex=list.FindWord(second.Word);unmasteredReviewAction.PerformClick();if(!BookmarkStore.Load().ReviewUnmastered.SetEquals(new[]{first.Word,second.Word})||reviewRows.Length!=count||bookmark.ReviewCheckpoint!=checkpoint)throw new Exception("independent multiple unmastered highlights");Step(1);frequency.SelectedIndex=6;frequency.SelectedIndex=0;JumpReviewCheckpoint();if(list.UnmasteredWords==null||!list.UnmasteredWords.SetEquals(new[]{first.Word,second.Word})||list.Current.Word!=first.Word)throw new Exception("highlights lost browsing or filtering");
  Switch("reading");ToggleUnmasteredReview();if(unmasteredReviewAction.Visible||list.UnmasteredWords!=null||bookmark.ReviewUnmastered.Count!=2)throw new Exception("highlights leaked outside review");Switch("review");list.SelectedIndex=list.FindWord(first.Word);unmasteredReviewAction.PerformClick();if(BookmarkStore.Load().ReviewUnmastered.Contains(first.Word)||!BookmarkStore.Load().ReviewUnmastered.Contains(second.Word)||bookmark.ReviewCheckpoint!=checkpoint)throw new Exception("unmark changed another highlight or position");unmasteredReviewAction.PerformClick();ToggleReview();if(bookmark.ReviewUnmastered.Contains(first.Word)||!bookmark.ReviewUnmastered.Contains(second.Word)||BookmarkStore.Load().ReviewUnmastered.Contains(first.Word))throw new Exception("removed word kept unmastered highlight");
  Switch("reading");frequency.SelectedIndex=0;wordSearch.Text=first.Word;JumpToWord();AddCurrentReview();Switch("review");frequency.SelectedIndex=0;list.SelectedIndex=list.FindWord(first.Word);markReviewPosition.PerformClick();unmasteredReviewAction.PerformClick();SaveBookmark(false);if(!BookmarkStore.Load().ReviewUnmastered.SetEquals(new[]{first.Word,second.Word})||BookmarkStore.Load().ReviewCheckpoint!=first.Word||reviewRows.Length!=count)throw new Exception("highlights and position did not persist together");
 }
 public void CheckUnmasteredCollection(){
  var marked=reviewRows.Where(w=>bookmark.ReviewUnmastered.Contains(w.Word)).ToArray();if(marked.Length!=2)throw new Exception("collection test setup");string first=marked[0].Word,second=marked[1].Word;int total=reviewRows.Length;unmasteredTab.PerformClick();if(bank!="unmastered"||list.Count!=2||frequency.SelectedIndex!=0||list.FindWord(first)<0||list.FindWord(second)<0||!position.Text.StartsWith("未掌握复习"))throw new Exception("unmastered collection contents");wordSearch.Text=second.ToUpperInvariant();JumpToWord();if(list.Current.Word!=second)throw new Exception("collection search");unmasteredReviewAction.PerformClick();if(list.Count!=1||list.Current.Word!=first||!IsReviewed(second)||reviewRows.Length!=total)throw new Exception("unmark did not leave collection only");unmasteredReviewAction.PerformClick();Step(1);Step(-1);if(list.Count!=0||list.Current!=null||unmasteredReviewAction.Enabled||!meaning.Text.Contains("自动收集"))throw new Exception("empty unmastered collection");
  Switch("review");frequency.SelectedIndex=0;foreach(string word in new[]{first,second}){list.SelectedIndex=list.FindWord(word);unmasteredReviewAction.PerformClick();}unmasteredTab.PerformClick();list.SelectedIndex=list.FindWord(first);ToggleReview();if(list.Count!=1||list.Current.Word!=second||IsReviewed(first)||bookmark.ReviewUnmastered.Contains(first))throw new Exception("collection review removal");Switch("reading");frequency.SelectedIndex=0;wordSearch.Text=first;JumpToWord();AddCurrentReview();Switch("review");frequency.SelectedIndex=0;list.SelectedIndex=list.FindWord(first);if(bookmark.ReviewCheckpoint!=first)markReviewPosition.PerformClick();unmasteredReviewAction.PerformClick();unmasteredTab.PerformClick();wordSearch.Text=second;JumpToWord();definitions.Checked=false;SaveBookmark(false);Switch("reading");unmasteredTab.PerformClick();if(list.Count!=2||list.Current.Word!=second||definitions.Checked||reviewRows.Length!=total)throw new Exception("independent collection bookmark");SaveBookmark(false);var disk=BookmarkStore.Load();if(disk.ActiveBank!="unmastered"||disk.Get("unmastered").Word!=second||!disk.ReviewUnmastered.SetEquals(new[]{first,second}))throw new Exception("unmastered collection persistence");string mainMark=bookmark.ReviewCheckpoint;markReviewPosition.PerformClick();if(bookmark.UnmasteredCheckpoint!=second||bookmark.ReviewCheckpoint!=mainMark)throw new Exception("independent unmastered checkpoint");Step(-1);wordSearch.Focus();jumpReviewPosition.PerformClick();if(list.Current.Word!=second||wordSearch.ContainsFocus||BookmarkStore.Load().UnmasteredCheckpoint!=second)throw new Exception("unmastered checkpoint jump/persistence");
 }
 public void CheckAuditMode(){Switch("reading");frequency.SelectedIndex=0;wordSearch.Text="research";JumpToWord();if(list.Current.Word!="research")throw new Exception("audit search");frequency.SelectedIndex=2;if(list.Current.Word!="research")throw new Exception("frequency reset selection");frequency.SelectedIndex=6;if(list.Count==0)throw new Exception("100 threshold");wordSearch.Text="in contrast";Switch("phrases");frequency.SelectedIndex=4;wordSearch.Text="IN  CONTRAST";JumpToWord();if(list.Current.Word!="in contrast"||frequency.SelectedIndex!=0)throw new Exception("hidden phrase search");wordSearch.Text="not-a-real-word-xyz";string before=list.Current.Word;JumpToWord();if(list.Current.Word!=before)throw new Exception("missing search moved selection");foreach(string category in new[]{"reading","listening","hard","phrases","sentences","review"}){Switch(category);if(list.Count==0)throw new Exception("empty category: "+category);int start=list.SelectedIndex;Step(1);if(list.SelectedIndex!=Math.Min(start+1,list.Count-1))throw new Exception("next step");list.SelectedIndex=0;Step(-1);if(list.SelectedIndex!=0)throw new Exception("first boundary");list.SelectedIndex=list.Count-1;Step(1);if(list.SelectedIndex!=list.Count-1)throw new Exception("last boundary");}CheckSentenceMode();}

 public void CheckSentenceMode(){Switch("sentences");definitions.Checked=true;RenderWord();if(list.Count<2||frequency.Enabled||!meaning.Text.Contains("句子主干")||!meaning.Text.Contains("结构拆解"))throw new Exception("Sentence page failed");string original=list.Current.Example;definitions.Checked=false;if(meaning.Text!=original)throw new Exception("Sentence hide failed");definitions.Checked=true;if(!meaning.Text.Contains(original)||!meaning.Text.Contains(list.Current.Translation))throw new Exception("Sentence show failed");wordSearch.Text="lasting benefits";JumpToWord();if(!list.Current.Example.Contains("lasting benefits"))throw new Exception("Sentence search failed");string selected=list.Current.Word;Switch("reading");if(!frequency.Enabled||list.Count==0)throw new Exception("Return to words failed");Switch("sentences");if(list.Current.Word!=selected)throw new Exception("Sentence bookmark failed");if(!SaveBookmark(false)||BookmarkStore.Load().ActiveBank!="sentences")throw new Exception("Sentence persistence failed");}

 readonly Dictionary<string,Font> uiFonts=new Dictionary<string,Font>();AppearanceSettings appearance=AppearanceStore.Load();TableLayoutPanel body,card;bool testProfile;
 Color SelectedColour {get{return Color.FromArgb((appearance.Accent.R+255*4)/5,(appearance.Accent.G+255*4)/5,(appearance.Accent.B+255*4)/5);}}
 TextBox wordSearch;Label searchFeedback;Button reviewAction;Button reading,listening,hardTab,phraseTab,sentenceTab,reviewTab,previous,next;ComboBox frequency;CheckBox definitions;Label shortcutHint;Label filterLabel,total,title,phonetic,statistics,position,status,sourceNote;RichTextBox meaning;WordList list;Entry[] readRows,listenRows,hardRows,phraseRows,sentenceRows,reviewRows,sampleReviewRows;readonly Dictionary<string,Entry> reviewSource=new Dictionary<string,Entry>(StringComparer.OrdinalIgnoreCase);string renderedSentence;bool renderedSentenceChinese,renderingText;bool loaded,updatingFrequency;string bank="reading";Font exampleFont=new Font("Segoe UI",14),translationFont=new Font("Microsoft YaHei",15),sourceFont=new Font("Microsoft YaHei UI",10),starFont=new Font("Microsoft YaHei",19,FontStyle.Bold);Font posHeading=new Font("Microsoft YaHei UI",14,FontStyle.Bold);Font titleLarge=new Font("Segoe UI",29,FontStyle.Bold),titleSmall=new Font("Segoe UI",21,FontStyle.Bold);
 public static int[] Thresholds={0,2,5,10,20,50,100};
 public static Entry[] Filter(Entry[] rows,int threshold){return rows.Where(w=>w.FrequencyCount>=threshold).ToArray();}
 readonly BrowseBookmark bookmark;readonly Timer bookmarkTimer=new Timer{Interval=500};bool restoring;
 public FocusedWindow(bool useTestProfile=false){SuspendLayout();testProfile=useTestProfile;if(testProfile)appearance.DataDirectory=Path.GetDirectoryName(BookmarkStore.PathName);else BookmarkStore.PathName=Path.Combine(appearance.DataDirectory,"词库浏览存档.xml");bookmark=BookmarkStore.Load();bank=bookmark.ActiveBank;bookmarkTimer.Tick+=delegate{bookmarkTimer.Stop();SaveBookmark(false);};Text="oii背词";StartPosition=FormStartPosition.CenterScreen;ClientSize=new Size(1080,740);MinimumSize=new Size(940,620);AutoScaleDimensions=new SizeF(96F,96F);AutoScaleMode=AutoScaleMode.Dpi;KeyPreview=true;Font=new Font("Microsoft YaHei UI",10);BackColor=Color.FromArgb(245,248,243);try{Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);}catch{}
  var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(24,18,24,14)};root.RowStyles.Add(new RowStyle(SizeType.Absolute,100));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,30));Controls.Add(root);
  header=new Panel{Dock=DockStyle.Fill};
  appTitle=L("oii背词",23,true);appTitle.Dock=DockStyle.None;
  header.Controls.Add(appTitle);
  BuildRemovalNotice();
  settingsButton=B("设置",delegate{OpenSettings();});header.Controls.Add(settingsButton);
  unmasteredTab=B("未掌握复习 · 0",delegate{Switch("unmastered");});
  unmasteredTab.AccessibleName="未掌握复习";header.Controls.Add(unmasteredTab);
  categoryTabs=new FlowLayoutPanel{WrapContents=false};
  reading=B("阅读词库",delegate{Switch("reading");});listening=B("听力词库",delegate{Switch("listening");});reading.Width=listening.Width=128;categoryTabs.Controls.Add(reading);categoryTabs.Controls.Add(listening);hardTab=B("高频难词",delegate{Switch("hard");});hardTab.Width=128;categoryTabs.Controls.Add(hardTab);phraseTab=B("进阶短语",delegate{Switch("phrases");});phraseTab.Width=128;categoryTabs.Controls.Add(phraseTab);sentenceTab=B("长难句",delegate{Switch("sentences");});sentenceTab.Width=128;categoryTabs.Controls.Add(sentenceTab);reviewTab=B("待复习词",delegate{Switch("review");});reviewTab.Width=128;categoryTabs.Controls.Add(reviewTab);
  header.Controls.Add(categoryTabs);
  header.Resize+=delegate{LayoutHeader();};
  root.Controls.Add(header,0,0);
  body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,300));body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));root.Controls.Add(body,0,1);
  var left=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=7,Padding=new Padding(0,0,18,0)};foreach(int h in new[]{28,43,44,25,30,0})left.RowStyles.Add(new RowStyle(SizeType.Absolute,h));left.RowStyles.Add(new RowStyle(SizeType.Percent,100));filterLabel=L("按正文总出现次数筛选",10,false);left.Controls.Add(filterLabel,0,0);frequency=new RoundedComboBox{Dock=DockStyle.Top,DropDownStyle=ComboBoxStyle.DropDownList,AccessibleName="出现频次筛选"};frequency.Items.AddRange(new object[]{"全部词汇","2次及以上","5次及以上","10次及以上","20次及以上","50次及以上","100次及以上"});frequency.SelectedIndex=1;left.Controls.Add(frequency,0,1);total=L("正在加载…",9,false);left.Controls.Add(total,0,4);var searchBar=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};searchBar.RowStyles.Add(new RowStyle(SizeType.Percent,100));searchBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));searchBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,72));wordSearch=new TextBox{Dock=DockStyle.Fill,AccessibleName="检索当前分类词汇或句子",Margin=new Padding(0,6,6,0)};var searchFrame=new RoundedSearchBox(wordSearch){Dock=DockStyle.Fill,Margin=new Padding(0,2,6,4)};searchBar.Controls.Add(searchFrame,0,0);var jump=B("跳转",delegate{JumpToWord();});jump.Dock=DockStyle.Fill;jump.Margin=new Padding(0,2,0,4);searchBar.Controls.Add(jump,1,0);left.Controls.Add(searchBar,0,2);searchFeedback=L("输入词汇或短语，回车跳转",9,false);left.Controls.Add(searchFeedback,0,3);list=new WordList{Dock=DockStyle.Fill};BuildReviewCheckpoint(left);left.Controls.Add(list,0,6);body.Controls.Add(left,0,0);
  card=new RoundedCard{Dock=DockStyle.Fill,BackColor=Color.White,Padding=new Padding(28,20,26,18),ColumnCount=1,RowCount=9};foreach(int h in new[]{52,78,36,16,12})card.RowStyles.Add(new RowStyle(SizeType.Absolute,h));card.RowStyles.Add(new RowStyle(SizeType.Percent,100));card.RowStyles.Add(new RowStyle(SizeType.Absolute,70));card.RowStyles.Add(new RowStyle(SizeType.Absolute,28));card.RowStyles.Add(new RowStyle(SizeType.Absolute,50));body.Controls.Add(card,1,0);
  var cardTop=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};cardTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,35));cardTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,65));position=L("",10,false);position.TextAlign=ContentAlignment.TopLeft;position.Padding=new Padding(0,5,0,0);cardTop.Controls.Add(position,0,0);statistics=L("",10,false);statistics.TextAlign=ContentAlignment.TopRight;statistics.Padding=new Padding(0,5,0,0);cardTop.Controls.Add(statistics,1,0);card.Controls.Add(cardTop,0,0);title=L("正在加载词库…",29,true);title.Font=titleLarge;title.AutoEllipsis=true;var titleRow=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};titleRow.RowStyles.Add(new RowStyle(SizeType.Percent,100));titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,138));titleRow.Controls.Add(title,0,0);reviewAction=B("＋待复习 [. / F]",delegate{RequestReviewToggle();});reviewAction.LostFocus+=delegate{CancelReviewRemoval();};reviewAction.Width=138;reviewAction.Height=34;reviewAction.Font=new Font(Font.FontFamily,9);reviewAction.Anchor=AnchorStyles.Right|AnchorStyles.Top;reviewAction.Margin=new Padding(0,12,0,0);reviewAction.AccessibleName="加入或移除待复习";reviewActionsPanel=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=Padding.Empty};reviewActionsPanel.RowStyles.Add(new RowStyle(SizeType.Percent,100));reviewActionsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute,0));reviewActionsPanel.Controls.Add(reviewAction,0,0);unmasteredReviewAction=B("标为仍未掌握",delegate{ToggleUnmasteredReview();});unmasteredReviewAction.Width=138;unmasteredReviewAction.Height=32;unmasteredReviewAction.Font=new Font(Font.FontFamily,9);unmasteredReviewAction.Margin=new Padding(0,2,0,0);unmasteredReviewAction.AccessibleName="标记或取消仍未掌握";unmasteredReviewAction.Visible=false;unmasteredReviewAction.Dock=DockStyle.Fill;reviewActionsPanel.Controls.Add(unmasteredReviewAction,0,1);titleRow.Controls.Add(reviewActionsPanel,1,0);card.Controls.Add(titleRow,0,1);phonetic=L("",12,false);card.Controls.Add(phonetic,0,2);
  definitions=new RoundedCheckBox{Text="显示中文",Checked=true,Appearance=Appearance.Button,FlatStyle=FlatStyle.Flat,TextAlign=ContentAlignment.MiddleCenter,Width=175,Height=50,Font=new Font(Font.FontFamily,12),BackColor=Color.FromArgb(220,241,223),ForeColor=Color.FromArgb(39,100,63),Margin=new Padding(0,2,0,0),AccessibleName="显示中文"};definitions.FlatAppearance.BorderColor=Color.FromArgb(196,218,201);meaning=new RichTextBox{Dock=DockStyle.Fill,ReadOnly=true,ImeMode=ImeMode.Disable,BorderStyle=BorderStyle.None,BackColor=Color.White,ForeColor=Color.FromArgb(23,36,28),Font=new Font("Microsoft YaHei",19),DetectUrls=false,AccessibleName="释义与精读内容",Margin=new Padding(20,0,0,0)};card.Controls.Add(meaning,0,5);var nav=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};previous=B("上一条",delegate{Step(-1);});next=B("下一条",delegate{Step(1);});previous.Width=next.Width=148;previous.Height=next.Height=50;previous.Font=next.Font=new Font(Font.FontFamily,12);nav.Controls.Add(previous);nav.Controls.Add(next);nav.Controls.Add(definitions);card.Controls.Add(nav,0,6);shortcutHint=L("A / ↑ 上一条 · D / ↓ 下一条 · 空格 显隐中文 · . / F 加入待复习",9,false);card.Controls.Add(shortcutHint,0,7);sourceNote=L("",10,false);sourceNote.Font=sourceFont;sourceNote.TextAlign=ContentAlignment.BottomRight;sourceNote.AccessibleName="来源与学习说明";card.Controls.Add(sourceNote,0,8);
  status=L("正在加载词库…",9,false);root.Controls.Add(status,0,2);frequency.SelectedIndexChanged+=delegate{Apply();};list.SelectionChanged+=delegate{CancelReviewRemoval();RenderWord();ScheduleSave();};list.ScrollChanged+=delegate{ScheduleSave();};meaning.VScroll+=delegate{if(!renderingText){CaptureSentenceScroll();ScheduleSave();}};Application.AddMessageFilter(this);definitions.CheckedChanged+=delegate{definitions.BackColor=definitions.Checked?SelectedColour:appearance.Card;RenderWord();ScheduleSave();};ResumeLayout(true);
 }
 bool IsReviewed(string word){return reviewRows!=null&&reviewRows.Any(w=>String.Equals(w.Word,word,StringComparison.OrdinalIgnoreCase));}
 Entry ReviewCopy(Entry source){bool listening=source.Bank=="listening";return new Entry{Bank="review",Word=source.Word,Meaning=source.Meaning,Phonetic=source.Phonetic,Tier=source.Tier,Common=source.Common,Example=source.Example,Translation=source.Translation,ExampleSource=source.ExampleSource,Count=listening?source.OtherCount:source.Count,Coverage=listening?source.OtherCoverage:source.Coverage,OtherCount=listening?source.Count:source.OtherCount,OtherCoverage=listening?source.Coverage:source.OtherCoverage};}
 void RebuildReview(){
  var combined=new List<Entry>();
  var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  foreach(var entry in sampleReviewRows)
   if(!bookmark.ReviewRemoved.Contains(entry.Word)&&seen.Add(entry.Word))combined.Add(entry);
  foreach(string word in bookmark.ReviewAdded.OrderBy(w=>w)){
   Entry source;
   if(seen.Add(word)&&reviewSource.TryGetValue(word,out source))combined.Add(ReviewCopy(source));
  }
  originalReviewRows=combined.ToArray();
  bool hadOrder=bookmark.ReviewOrder.Count>0;
  reviewRows=OrderReviewWords(originalReviewRows,bookmark.ReviewOrder);
  if(!hadOrder&&bookmark.ReviewOrder.Count>0){
   var saved=bookmark.Get("review");
   int index=Array.FindIndex(Filter(reviewRows,Thresholds[saved.Frequency]),w=>w.Word==saved.Word);
   if(index>=0){
    double relativeRow=Math.Max(0,saved.SelectedIndex-saved.ScrollRows);
    saved.ScrollRows=Math.Max(0,index-relativeRow);
    saved.SelectedIndex=index;
   }
   reviewOrderNeedsSave=true;
  }
  reviewTab.Text="待复习词 · "+reviewRows.Length.ToString("N0");
  RefreshUnmasteredRows();
 }
 bool reviewOrderNeedsSave;
 static Entry[] OrderReviewWords(Entry[] rows,List<string> savedOrder){
  // Keep removed words in the order so undo or re-adding can restore their old positions.
  var known=new HashSet<string>(savedOrder,StringComparer.OrdinalIgnoreCase);
  foreach(var row in rows)if(known.Add(row.Word))savedOrder.Add(row.Word);
  var byWord=rows.ToDictionary(w=>w.Word,StringComparer.OrdinalIgnoreCase);
  return savedOrder.Where(byWord.ContainsKey).Select(word=>byWord[word]).ToArray();
 }
 public static void CheckReviewOrder(){
  var entries=new[]{"habitat","platform","conservation","evidence"}.Select(word=>new Entry{Word=word}).ToArray();
  var savedOrder=new List<string>();
  if(!OrderReviewWords(entries,savedOrder).SequenceEqual(entries))
   throw new Exception("first launch must preserve the supplied order");
  var removed=entries[1];
  if(!OrderReviewWords(entries.Where(entry=>entry!=removed).ToArray(),savedOrder).SequenceEqual(entries.Where(entry=>entry!=removed))||
     !OrderReviewWords(entries,savedOrder).SequenceEqual(entries))
   throw new Exception("removal and undo must preserve review order");
  var added=new Entry{Word="appointment"};
  if(!OrderReviewWords(new[]{added}.Concat(entries).ToArray(),savedOrder).SequenceEqual(entries.Concat(new[]{added})))
   throw new Exception("new words must be appended to the saved review order");
 }
 void RequestReviewToggle(){
  if(!loaded||list.Current==null||bank=="sentences")return;
  string word=list.Current.Word;
  if(IsReviewed(word)&&pendingReviewRemoval!=word){
   pendingReviewRemoval=word;
   UpdateReviewAction();
   return;
  }
  pendingReviewRemoval=null;
  ToggleReview();
 }
 void CancelReviewRemoval(){
  if(pendingReviewRemoval==null)return;
  pendingReviewRemoval=null;
  UpdateReviewAction();
 }
 void UpdateReviewAction(){
  var entry=list.Current;
  reviewAction.Visible=bank!="sentences";
  reviewAction.Enabled=entry!=null;
  if(entry==null)return;
  bool inReview=IsReviewed(entry.Word);
  bool confirming=inReview&&pendingReviewRemoval==entry.Word;
  reviewAction.Text=confirming?"确认移除":inReview?"已加入 · 移除":"＋待复习 [. / F]";
  reviewAction.AccessibleName=confirming?"确认移除当前待复习词":"加入或移除待复习";
  reviewAction.BackColor=confirming?Color.FromArgb(255,226,185):inReview?SelectedColour:appearance.Card;
  reviewAction.ForeColor=confirming?Color.FromArgb(110,57,18):appearance.Accent;
 }
 void AddCurrentReview(){if(!loaded||list.Current==null||bank=="sentences")return;string word=list.Current.Word;if(IsReviewed(word)){status.Text="“"+word+"”已在待复习中，不会重复添加。";return;}bookmark.ReviewRemoved.Remove(word);if(!sampleReviewRows.Any(w=>String.Equals(w.Word,word,StringComparison.OrdinalIgnoreCase)))bookmark.ReviewAdded.Add(word);RebuildReview();var reviewPosition=bookmark.Get("review");reviewPosition.Frequency=0;reviewPosition.Word=word;reviewPosition.SelectedIndex=Array.FindIndex(reviewRows,w=>String.Equals(w.Word,word,StringComparison.OrdinalIgnoreCase));reviewPosition.ScrollRows=Math.Max(0,reviewPosition.SelectedIndex-2);reviewAction.Text="已加入 · 移除";reviewAction.BackColor=SelectedColour;status.Text="已加入待复习："+word;SaveBookmark(false);}
 void LayoutHeader(){
  if(categoryTabs==null)return;
  float scale=DeviceDpiScale();
  int width=header.ClientSize.Width,height=header.ClientSize.Height;
  int tabsHeight=Math.Min(height,(int)Math.Ceiling(43*scale));
  int topHeight=Math.Max(0,height-tabsHeight-(int)Math.Ceiling(7*scale));
  int gap=(int)Math.Ceiling(12*scale);
  int collectionWidth=(int)Math.Ceiling(160*scale),settingsWidth=(int)Math.Ceiling(78*scale);
  int collectionX=Math.Max(0,width-collectionWidth);
  int settingsX=Math.Max(0,collectionX-gap-settingsWidth);
  int titleWidth=Math.Min((int)Math.Ceiling(184*scale),settingsX);
  // Use the space allocated to the header so a wide notice cannot push it over the tabs.
  appTitle.SetBounds(0,0,titleWidth,topHeight);
  removalNotice.SetBounds(titleWidth,0,Math.Max(0,settingsX-gap-titleWidth),topHeight);
  settingsButton.SetBounds(settingsX,0,settingsWidth,topHeight);
  unmasteredTab.SetBounds(collectionX,0,collectionWidth,topHeight);
  categoryTabs.SetBounds(0,height-tabsHeight,width,tabsHeight);
 }
 void BuildRemovalNotice(){
  var green=Color.FromArgb(26,83,53);
  removalNotice=new RoundedCard{ColumnCount=2,RowCount=1,Visible=false,
   Padding=new Padding(12,4,5,4),
   BackColor=Color.FromArgb(230,244,235),Outline=Color.FromArgb(181,218,194)};
  removalNotice.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
  removalNotice.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,72));
  removalNotice.RowStyles.Add(new RowStyle(SizeType.Percent,100));
  removalNoticeText=L("",10,true);
  removalNoticeText.ForeColor=green;
  removalNoticeText.AutoEllipsis=true;
  undoRemovalButton=B("撤销",delegate{UndoReviewRemoval();});
  undoRemovalButton.Dock=DockStyle.Fill;
  undoRemovalButton.Margin=Padding.Empty;
  undoRemovalButton.BackColor=Color.White;
  undoRemovalButton.ForeColor=green;
  undoRemovalButton.AccessibleName="撤销上一次移除";
  removalNotice.Controls.Add(removalNoticeText,0,0);
  removalNotice.Controls.Add(undoRemovalButton,1,0);
  header.Controls.Add(removalNotice);
  removalNoticeTimer.Tick+=delegate{DismissRemovalNotice();};
 }
 void DismissRemovalNotice(){
  removalNoticeTimer.Stop();
  lastRemoval=null;
  if(undoRemovalButton.Focused)FocusWordNavigation();
  removalNotice.Visible=false;
 }
 void RefreshReviewAfterMembershipChange(){
  RebuildReview();
  if(bank=="review"||bank=="unmastered")Apply(keepRemovalPosition:true);
  else RenderWord();
 }
 void ToggleReview(){
  if(!loaded||list.Current==null||bank=="sentences")return;
  string word=list.Current.Word;
  if(!IsReviewed(word)){AddCurrentReview();return;}
  CaptureBookmark();
  var removed=new RemovedReviewWord{
   Word=word,WasAdded=bookmark.ReviewAdded.Contains(word),WasRemoved=bookmark.ReviewRemoved.Contains(word),
   WasHighlighted=bookmark.ReviewUnmastered.Contains(word),
   WasReviewCheckpoint=String.Equals(bookmark.ReviewCheckpoint,word,StringComparison.OrdinalIgnoreCase),
   WasUnmasteredCheckpoint=String.Equals(bookmark.UnmasteredCheckpoint,word,StringComparison.OrdinalIgnoreCase)
  };
  if(removed.WasReviewCheckpoint)bookmark.ReviewCheckpoint="";
  if(removed.WasUnmasteredCheckpoint)bookmark.UnmasteredCheckpoint="";
  bookmark.ReviewUnmastered.Remove(word);
  bookmark.ReviewAdded.Remove(word);
  if(sampleReviewRows.Any(w=>String.Equals(w.Word,word,StringComparison.OrdinalIgnoreCase)))bookmark.ReviewRemoved.Add(word);
  RefreshReviewAfterMembershipChange();
  DismissRemovalNotice();
  if(!SaveBookmark(false))return;
  lastRemoval=removed;
  removalNoticeText.Text="删除成功 · "+word;
  removalNotice.Visible=true;
  removalNoticeTimer.Start();
 }
 void UndoReviewRemoval(){
  var removed=lastRemoval;
  if(removed==null)return;
  CancelReviewRemoval();
  string word=removed.Word;
  if(removed.WasAdded)bookmark.ReviewAdded.Add(word);
  if(removed.WasRemoved)bookmark.ReviewRemoved.Add(word);
  else bookmark.ReviewRemoved.Remove(word);
  if(removed.WasHighlighted)bookmark.ReviewUnmastered.Add(word);
  if(removed.WasReviewCheckpoint&&String.IsNullOrEmpty(bookmark.ReviewCheckpoint))bookmark.ReviewCheckpoint=word;
  if(removed.WasUnmasteredCheckpoint&&String.IsNullOrEmpty(bookmark.UnmasteredCheckpoint))bookmark.UnmasteredCheckpoint=word;
  RefreshReviewAfterMembershipChange();
  if(!SaveBookmark(false))return;
  DismissRemovalNotice();
  status.Text="已撤销移除："+word;
  FocusWordNavigation();
 }
 void BuildReviewCheckpoint(TableLayoutPanel left){sidebar=left;reviewCheckpointPanel=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=3,Margin=new Padding(0,0,0,6),Visible=false};reviewCheckpointPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));reviewCheckpointPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));reviewCheckpointPanel.RowStyles.Add(new RowStyle(SizeType.Absolute,25));reviewCheckpointPanel.RowStyles.Add(new RowStyle(SizeType.Absolute,38));reviewCheckpointPanel.RowStyles.Add(new RowStyle(SizeType.Percent,100));reviewCheckpointLabel=L("复习标记：未设置",9,false);reviewCheckpointLabel.AutoEllipsis=true;reviewCheckpointPanel.Controls.Add(reviewCheckpointLabel,0,0);reviewCheckpointPanel.SetColumnSpan(reviewCheckpointLabel,2);markReviewPosition=B("标记到这里",delegate{MarkReviewCheckpoint();});jumpReviewPosition=B("回到标记",delegate{JumpReviewCheckpoint();});markReviewPosition.AccessibleName="标记或取消当前复习位置";jumpReviewPosition.AccessibleName="回到手动标记的复习位置";foreach(var button in new[]{markReviewPosition,jumpReviewPosition}){button.Dock=DockStyle.Fill;button.Font=new Font(Font.FontFamily,9);button.Margin=new Padding(0,1,6,1);}jumpReviewPosition.Margin=new Padding(0,1,0,1);reviewCheckpointPanel.Controls.Add(markReviewPosition,0,1);reviewCheckpointPanel.Controls.Add(jumpReviewPosition,1,1);exportWordButton=B("导出为 Word",delegate{ExportCurrentWords();});exportWordButton.Dock=DockStyle.Fill;exportWordButton.Margin=new Padding(0,3,0,0);exportWordButton.Visible=false;reviewCheckpointPanel.Controls.Add(exportWordButton,0,2);reviewCheckpointPanel.SetColumnSpan(exportWordButton,2);left.Controls.Add(reviewCheckpointPanel,0,5);}
 void UpdateReviewCheckpoint(){bool visible=true;exportWordButton.Visible=bank=="unmastered";exportWordButton.Enabled=list.Count>0;float height=visible?(bank=="unmastered"?108:70)*DeviceDpiScale():0;if(sidebar.RowStyles[5].Height!=height)sidebar.RowStyles[5].Height=height;reviewCheckpointPanel.Visible=visible;list.MarkedWord=visible?CurrentCheckpoint:"";bool marked=!String.IsNullOrEmpty(CurrentCheckpoint)&&CurrentRows().Any(w=>String.Equals(w.Word,CurrentCheckpoint,StringComparison.OrdinalIgnoreCase));reviewCheckpointLabel.Text=marked?"复习标记："+Vocabulary.DisplayTitle(CurrentRows().First(w=>String.Equals(w.Word,CurrentCheckpoint,StringComparison.OrdinalIgnoreCase))):"复习标记：未设置";markReviewPosition.Enabled=visible&&list.Current!=null;markReviewPosition.Text=list.Current!=null&&String.Equals(list.Current.Word,CurrentCheckpoint,StringComparison.OrdinalIgnoreCase)?"取消此标记":"标记到这里";jumpReviewPosition.Enabled=marked;}
 void MarkReviewCheckpoint(){if(!loaded||list.Current==null)return;string word=list.Current.Word;bool clear=String.Equals(word,CurrentCheckpoint,StringComparison.OrdinalIgnoreCase);CurrentCheckpoint=clear?"":word;UpdateReviewCheckpoint();if(SaveBookmark(false))status.Text=clear?"已取消复习标记。":"已标记复习位置："+word+"；继续浏览不会改变此标记。";}
 void UpdateUnmasteredReview(){bool visible=bank=="review"||bank=="unmastered";unmasteredTab.BackColor=bank=="unmastered"?SelectedColour:appearance.Card;list.UnmasteredWords=visible?bookmark.ReviewUnmastered:null;unmasteredReviewAction.Visible=visible;reviewActionsPanel.RowStyles[1].Height=visible?36*DeviceDpiScale():0;unmasteredReviewAction.Enabled=visible&&list.Current!=null;bool marked=visible&&list.Current!=null&&bookmark.ReviewUnmastered.Contains(list.Current.Word);unmasteredReviewAction.Text=marked?"仍未掌握 · 取消":"标为仍未掌握";unmasteredReviewAction.BackColor=marked?Color.FromArgb(255,231,168):appearance.Card;unmasteredReviewAction.ForeColor=marked?Color.FromArgb(110,73,15):appearance.Accent;reviewAction.Dock=visible?DockStyle.Fill:DockStyle.None;reviewAction.Height=visible?32:34;reviewAction.Margin=visible?new Padding(0,3,0,2):new Padding(0,12,0,0);}
 void ToggleUnmasteredReview(){if(!loaded||(bank!="review"&&bank!="unmastered")||list.Current==null)return;CaptureBookmark();string word=list.Current.Word;bool remove=bookmark.ReviewUnmastered.Contains(word);if(remove){bookmark.ReviewUnmastered.Remove(word);if(String.Equals(bookmark.UnmasteredCheckpoint,word,StringComparison.OrdinalIgnoreCase))bookmark.UnmasteredCheckpoint="";}else bookmark.ReviewUnmastered.Add(word);RefreshUnmasteredRows();if(bank=="unmastered")Apply(keepRemovalPosition:remove);UpdateUnmasteredReview();list.Invalidate();if(SaveBookmark(false))status.Text=remove?"已取消“仍未掌握”高亮："+word:"已标为仍未掌握："+word+"；列表将持续高亮，可再次点击取消。";}
 void FocusWordNavigation(){if(list==null||!list.CanFocus)return;ActiveControl=list;list.Select();list.Focus();}
 void ExportCurrentWords(){if(!loaded)return;var rows=Filter(CurrentRows(),Thresholds[Math.Max(0,frequency.SelectedIndex)]);if(rows.Length==0){status.Text="当前没有可导出的词汇。";return;}using(var dialog=new SaveFileDialog{Title="导出当前词汇为 Word",Filter="Word 文档 (*.docx)|*.docx",DefaultExt="docx",AddExtension=true,OverwritePrompt=true,FileName="未掌握复习词汇_"+DateTime.Now.ToString("yyyyMMdd")+".docx"}){if(dialog.ShowDialog(this)!=DialogResult.OK)return;try{WordExporter.Save(dialog.FileName,rows,"未掌握复习词汇");status.Text="已导出 "+rows.Length+" 个词到："+dialog.FileName;}catch(Exception ex){MessageBox.Show(this,"导出未完成："+ex.Message,"导出 Word");}}}
 void JumpReviewCheckpoint(){if(!loaded||String.IsNullOrEmpty(CurrentCheckpoint))return;string word=CurrentCheckpoint;if(!CurrentRows().Any(w=>String.Equals(w.Word,word,StringComparison.OrdinalIgnoreCase))){status.Text="标记的词条已不在当前分类中，请重新标记。";return;}wordSearch.Clear();bool reset=list.FindWord(word)<0;if(reset)frequency.SelectedIndex=0;list.SelectedIndex=list.FindWord(word);FocusWordNavigation();if(SaveBookmark(false))status.Text="已回到复习标记："+word+(reset?"，频次筛选已改为全部。":"。");}
 bool HandleReviewPunctuation(char character){if(character!='.'&&character!='。')return false;if(!loaded||wordSearch.ContainsFocus||frequency.DroppedDown||bank=="sentences")return false;AddCurrentReview();return true;}
 protected override void OnKeyPress(KeyPressEventArgs e){if(HandleReviewPunctuation(e.KeyChar)){e.Handled=true;return;}base.OnKeyPress(e);}
 void CaptureSentenceScroll(){if(renderingText||renderedSentence==null||!renderedSentenceChinese||meaning==null||!meaning.IsHandleCreated)return;bookmark.SentenceScroll[renderedSentence]=ReadingScroll.Get(meaning).Y/DeviceDpiScale();}
 void RestoreSentenceScroll(string word){double y;if(!definitions.Checked||!bookmark.SentenceScroll.TryGetValue(word,out y))return;ReadingScroll.Set(meaning,new Point(0,(int)Math.Round(y*DeviceDpiScale())));if(!IsHandleCreated||IsDisposed)return;try{BeginInvoke((Action)delegate{if(IsDisposed||renderedSentence!=word||!definitions.Checked)return;bool before=renderingText;renderingText=true;try{ReadingScroll.Set(meaning,new Point(0,(int)Math.Round(y*DeviceDpiScale())));}finally{renderingText=before;}});}catch(InvalidOperationException){}}
 void OpenSettings(){using(var dialog=new AppearanceDialog(appearance,delegate(AppearanceSettings next){CaptureBookmark();string target=Path.Combine(next.DataDirectory,"词库浏览存档.xml");Directory.CreateDirectory(next.DataDirectory);string probe=Path.Combine(next.DataDirectory,".write-"+Guid.NewGuid().ToString("N"));using(File.Create(probe)){}File.Delete(probe);BookmarkStore.Save(target,bookmark);AppearanceStore.Save(next);BookmarkStore.PathName=target;appearance=next.Copy();ApplyAppearance();})){dialog.ShowDialog(this);}}
 Font CachedUiFont(float size,FontStyle style){string key=appearance.Family+"|"+size+"|"+style;Font result;if(!uiFonts.TryGetValue(key,out result)){result=FontTools.Create(appearance.Family,size,style);uiFonts[key]=result;}return result;}
 void StyleTree(Control parent){foreach(Control c in parent.Controls){if(c is RoundedSearchBox){var field=(RoundedSearchBox)c;field.BackColor=appearance.Card;field.Accent=appearance.Accent;field.Invalidate();}if(c is TextBox||c is RoundedComboBox){c.BackColor=appearance.Card;c.ForeColor=appearance.Text;c.Font=CachedUiFont(10,FontStyle.Regular);if(c is RoundedComboBox){var combo=(RoundedComboBox)c;combo.Accent=appearance.Accent;combo.ItemHeight=Math.Max(24,combo.Font.Height+12);}c.Invalidate();}if(c is Label&&c!=removalNoticeText){c.ForeColor=appearance.Text;if(c.Font.FontFamily.Name!=appearance.Family)c.Font=CachedUiFont(c.Font.Size,c.Font.Style);}if(c is Button&&c!=undoRemovalButton){c.ForeColor=appearance.Accent;c.BackColor=appearance.Card;if(c.Font.FontFamily.Name!=appearance.Family)c.Font=CachedUiFont(c.Font.Size,c.Font.Style);}StyleTree(c);}}
 void ApplyAppearance(){CaptureSentenceScroll();bool previousRendering=renderingText;renderingText=true;try{SuspendLayout();MinimumSize=new Size(Math.Max(940,appearance.Sidebar+630),620);BackColor=appearance.Background;StyleTree(this);card.BackColor=appearance.Card;meaning.BackColor=appearance.Card;meaning.ForeColor=appearance.Text;meaning.Font=CachedUiFont(appearance.MeaningSize,FontStyle.Regular);exampleFont.Dispose();translationFont.Dispose();starFont.Dispose();posHeading.Dispose();titleLarge.Dispose();titleSmall.Dispose();sourceFont.Dispose();exampleFont=FontTools.Create(appearance.Family,appearance.ExampleSize,FontStyle.Regular);translationFont=FontTools.Create(appearance.Family,appearance.TranslationSize,FontStyle.Regular);starFont=FontTools.Create(appearance.Family,appearance.MeaningSize,FontStyle.Bold);posHeading=FontTools.Create(appearance.Family,Math.Max(11,appearance.MeaningSize-5),FontStyle.Bold);titleLarge=FontTools.Create(appearance.Family,appearance.WordSize,FontStyle.Bold);titleSmall=FontTools.Create(appearance.Family,Math.Max(18,appearance.WordSize-8),FontStyle.Bold);sourceFont=FontTools.Create(appearance.Family,10,FontStyle.Regular);sourceNote.Font=sourceFont;title.Font=titleLarge;phonetic.Font=CachedUiFont(12,FontStyle.Regular);body.ColumnStyles[0].Width=appearance.Sidebar*DeviceDpiScale();meaning.Margin=new Padding((int)(appearance.Inset*DeviceDpiScale()),0,0,0);previous.Height=next.Height=definitions.Height=(int)(appearance.ButtonHeight*DeviceDpiScale());card.RowStyles[1].Height=Math.Max(78,titleLarge.Height+12)*DeviceDpiScale();card.RowStyles[6].Height=(appearance.ButtonHeight+20)*DeviceDpiScale();list.Customize(appearance,SelectedColour);reading.BackColor=bank=="reading"?SelectedColour:appearance.Card;listening.BackColor=bank=="listening"?SelectedColour:appearance.Card;hardTab.BackColor=bank=="hard"?SelectedColour:appearance.Card;phraseTab.BackColor=bank=="phrases"?SelectedColour:appearance.Card;sentenceTab.BackColor=bank=="sentences"?SelectedColour:appearance.Card;reviewTab.BackColor=bank=="review"?SelectedColour:appearance.Card;definitions.BackColor=definitions.Checked?SelectedColour:appearance.Card;definitions.ForeColor=appearance.Accent;RenderWord();ResumeLayout(true);}finally{renderingText=previousRendering;}}
 float DeviceDpiScale(){return IsHandleCreated?CreateGraphicsDpi()/96f:1f;}
 float CreateGraphicsDpi(){using(var g=CreateGraphics())return g.DpiX;}
 Label L(string text,float size,bool bold){return new Label{Text=text,Font=new Font(Font.FontFamily,size,bold?FontStyle.Bold:FontStyle.Regular),ForeColor=bold?Color.FromArgb(32,65,45):Color.FromArgb(78,96,84),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Margin=Padding.Empty};}
 Button B(string text,EventHandler action){var b=new RoundedButton{Text=text,Width=130,Height=36,FlatStyle=FlatStyle.Flat,BackColor=Color.White,ForeColor=Color.FromArgb(39,100,63),Margin=new Padding(0,2,12,0)};b.FlatAppearance.BorderColor=Color.FromArgb(196,218,201);b.Click+=action;return b;}
 protected override void OnShown(EventArgs e){base.OnShown(e);LayoutHeader();FontCatalog.Warm();Task.Factory.StartNew(()=>Vocabulary.Load()).ContinueWith(t=>{if(IsDisposed||!IsHandleCreated)return;try{BeginInvoke((Action)delegate{if(IsDisposed||Disposing)return;if(t.IsFaulted){status.Text="词库加载失败："+t.Exception.GetBaseException().Message;return;}readRows=t.Result.Where(w=>w.Bank=="reading").ToArray();listenRows=t.Result.Where(w=>w.Bank=="listening").ToArray();hardRows=t.Result.Where(w=>w.Bank=="hard").ToArray();hardTab.Text="高频难词 · "+hardRows.Length.ToString("N0");phraseRows=t.Result.Where(w=>w.Bank=="phrases").ToArray();phraseTab.Text="进阶短语 · "+phraseRows.Length.ToString("N0");sentenceRows=t.Result.Where(w=>w.Bank=="sentences").ToArray();sentenceTab.Text="长难句 · "+sentenceRows.Length.ToString("N0");sampleReviewRows=t.Result.Where(w=>w.Bank=="review").ToArray();foreach(var entry in t.Result.Where(w=>w.Bank=="reading"||w.Bank=="listening"||w.Bank=="hard"||w.Bank=="phrases")){if(!reviewSource.ContainsKey(entry.Word)||entry.Bank=="reading")reviewSource[entry.Word]=entry;}foreach(var entry in sampleReviewRows)if(!reviewSource.ContainsKey(entry.Word))reviewSource[entry.Word]=entry;RebuildReview();loaded=true;reading.Text="阅读词库 · "+readRows.Length.ToString("N0");listening.Text="听力词库 · "+listenRows.Length.ToString("N0");RestoreBank();ApplyAppearance();if(reviewOrderNeedsSave&&SaveBookmark(false))reviewOrderNeedsSave=false;if(ReadyForCheck!=null)ReadyForCheck();});}catch(InvalidOperationException){} });}
 void Switch(string value){CancelReviewRemoval();if(!loaded){bank=value;return;}CaptureBookmark();bank=value;RestoreBank();ScheduleSave();}
 void RestoreBank(){restoring=true;wordSearch.Clear();searchFeedback.Text=bank=="sentences"?"输入句子关键词，回车查找":"输入词汇或短语，回车跳转";var saved=bookmark.Get(bank);frequency.SelectedIndex=bank=="sentences"?0:saved.Frequency;definitions.Checked=saved.Definitions;reading.BackColor=bank=="reading"?SelectedColour:appearance.Card;listening.BackColor=bank=="listening"?SelectedColour:appearance.Card;hardTab.BackColor=bank=="hard"?SelectedColour:appearance.Card;phraseTab.BackColor=bank=="phrases"?SelectedColour:appearance.Card;sentenceTab.BackColor=bank=="sentences"?SelectedColour:appearance.Card;reviewTab.BackColor=bank=="review"?SelectedColour:appearance.Card;Apply();int selected=list.FindWord(saved.Word);if(selected<0)selected=saved.SelectedIndex;list.SelectedIndex=selected;list.ScrollRows=saved.ScrollRows;RenderWord();restoring=false;}
 void CaptureBookmark(){if(!loaded||restoring)return;CaptureSentenceScroll();var saved=bookmark.Get(bank);saved.Frequency=frequency.SelectedIndex;saved.Definitions=definitions.Checked;saved.Word=list.Current==null?"":list.Current.Word;saved.SelectedIndex=Math.Max(0,list.SelectedIndex);saved.ScrollRows=list.ScrollRows;bookmark.ActiveBank=bank;}
 void ScheduleSave(){if(!loaded||restoring)return;bookmarkTimer.Stop();bookmarkTimer.Start();}
 bool SaveBookmark(bool closing){if(!loaded)return true;CaptureBookmark();try{BookmarkStore.Save(bookmark);return true;}catch(Exception ex){if(closing)MessageBox.Show(this,"本次进度未能保存："+ex.Message,"存档提醒");else status.Text="进度暂未保存："+ex.Message;return false;}}

 void Apply(bool keepRemovalPosition=false){if(!loaded||updatingFrequency)return;var rows=CurrentRows();frequency.Enabled=bank!="sentences";filterLabel.Text=bank=="sentences"?"精选阅读长难句":"按正文总出现次数筛选";string allLabel=bank=="sentences"?"全部句子":"全部词汇";if(!String.Equals(frequency.Items[0] as string,allLabel)){updatingFrequency=true;try{frequency.Items[0]=allLabel;}finally{updatingFrequency=false;}}
  string currentWord=list.Current==null?null:list.Current.Word;
  var visibleRows=bank=="sentences"?rows:Filter(rows,Thresholds[Math.Max(0,frequency.SelectedIndex)]);
  int retained=Array.FindIndex(visibleRows,w=>w.Word==currentWord);
  // In a filtered list, the successor shifts into the removed row's index.
  int selected=retained>=0?retained:(keepRemovalPosition?list.SelectedIndex:0);
  list.SetRows(visibleRows,selected,keepRemovalPosition?list.ScrollRows:0);
  total.Text=list.Count.ToString("N0")+" / "+rows.Length.ToString("N0")+(bank=="phrases"?" 条短语":bank=="sentences"?" 条句子":" 条词汇");status.Text="词频为演示数值，可用来体验筛选。浏览位置自动保存。";if(bank=="hard")status.Text="高频难词：按阅读与听力的演示频次之和筛选。";if(bank=="phrases")status.Text="进阶短语：按阅读与听力的演示频次之和筛选。";if(bank=="sentences")status.Text="长难句精读：原句、译文、主干、结构拆解与重点表达。可检索关键词，浏览位置自动保存。";if(bank=="review")status.Text="待复习词：可添加词条、设置复习标记并检索跳转。";if(bank=="unmastered")status.Text="未掌握复习：自动收集高亮的词；取消高亮后从这里移出，仍保留在待复习词中。";ScheduleSave();}
 public static int NavigationDelta(Keys key){return key==Keys.Up||key==Keys.Left||key==Keys.A?-1:key==Keys.Down||key==Keys.Right||key==Keys.D?1:0;}
 void JumpToWord(){if(!loaded)return;string query=System.Text.RegularExpressions.Regex.Replace(wordSearch.Text.Trim(),@"\s+"," ");if(query.Length==0){searchFeedback.Text="请输入词汇、短语或句子关键词";return;}var rows=CurrentRows();var found=rows.FirstOrDefault(w=>String.Equals(w.Word,query,StringComparison.OrdinalIgnoreCase)||(bank=="sentences"&&(w.Example.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0||w.ExampleSource.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0||Vocabulary.DisplayTitle(w).IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0)));if(found==null){searchFeedback.Text="当前分类未找到，请检查拼写";return;}bool reset=list.FindWord(found.Word)<0;if(reset)frequency.SelectedIndex=0;list.SelectedIndex=list.FindWord(found.Word);searchFeedback.Text=reset?"已跳转，频次筛选已改为全部":"已跳转到："+found.Word;FocusWordNavigation();ScheduleSave();}
 bool HandleStudyShortcut(Keys keyData){if(keyData==Keys.Escape&&pendingReviewRemoval!=null){CancelReviewRemoval();return true;}if(wordSearch!=null&&wordSearch.ContainsFocus){if(keyData==Keys.Enter){JumpToWord();return true;}return false;}if(!loaded||(frequency!=null&&frequency.DroppedDown))return false;if(keyData==Keys.F||keyData==Keys.OemPeriod||keyData==Keys.Decimal){AddCurrentReview();return true;}if(keyData==Keys.Space){definitions.Checked=!definitions.Checked;return true;}int delta=NavigationDelta(keyData);if(delta==0)return false;Step(delta);return true;}
 protected override bool ProcessCmdKey(ref Message msg,Keys keyData){if(keyData==Keys.Space&&(msg.Msg==0x100||msg.Msg==0x104)&&(msg.LParam.ToInt64()&0x40000000L)!=0&&!wordSearch.ContainsFocus&&!frequency.DroppedDown)return true;if(HandleStudyShortcut(keyData))return true;return base.ProcessCmdKey(ref msg,keyData);}
 protected override void OnKeyDown(KeyEventArgs e){if(HandleStudyShortcut(e.KeyData)){e.Handled=true;e.SuppressKeyPress=true;return;}base.OnKeyDown(e);}
 void Step(int delta){if(!loaded||list.Count==0)return;list.SelectedIndex+=delta;}
 public static string FormatMeaning(string text){
  var labels=new Dictionary<string,string>{{"名词","n."},{"名词（复数）","n. (pl.)"},{"动词","v."},{"及物动词","vt."},{"不及物动词","vi."},{"形容词","adj."},{"副词","adv."},{"介词","prep."},{"连词","conj."},{"代词","pron."},{"数词","num."},{"感叹词","interj."},{"助动词","aux."},{"冠词","art."},{"缩写","abbr."},{"固定表达","phr."},{"常见释义",""}};
  return System.Text.RegularExpressions.Regex.Replace(text??"",@"【([^】]+)】\s*",m=>{string label;return labels.TryGetValue(m.Groups[1].Value,out label)?(label.Length==0?"":label+"  "):m.Groups[1].Value+"  ";});
 }
 void RenderWord(){CaptureSentenceScroll();bool previousRendering=renderingText;renderingText=true;renderedSentence=null;try{RenderWordCore();}finally{renderingText=previousRendering;}}
 void RenderWordCore(){UpdateReviewCheckpoint();UpdateUnmasteredReview();shortcutHint.Text=bank=="sentences"?"A / ↑ 上一条 · D / ↓ 下一条 · 空格 显隐中文":"A / ↑ 上一条 · D / ↓ 下一条 · 空格 显隐中文 · . / F 加入待复习";shortcutHint.Text="A / D 翻条 · 空格 显隐中文 · 左Alt 回到标记"+(bank=="sentences"?"":" · . / F 加入待复习");float scale=DeviceDpiScale();card.RowStyles[1].Height=(bank=="sentences"?48:Math.Max(78,titleLarge.Height+12))*scale;card.RowStyles[2].Height=(bank=="sentences"?0:36)*scale;card.RowStyles[3].Height=(bank=="sentences"?8:16)*scale;card.RowStyles[4].Height=(bank=="sentences"?0:12)*scale;var w=list.Current;UpdateReviewAction();previous.Enabled=w!=null&&list.SelectedIndex>0;next.Enabled=w!=null&&list.SelectedIndex+1<list.Count;if(w==null){title.Text=loaded?(bank=="review"&&reviewRows.Length==0?"尚未添加待复习词":bank=="unmastered"&&unmasteredRows.Length==0?"尚未标记未掌握词":"暂无符合条件的词条"):"正在加载词库…";phonetic.Text=statistics.Text=meaning.Text=position.Text=sourceNote.Text="";if(loaded&&bank=="review"&&reviewRows.Length==0)meaning.Text="你整理的待复习词会显示在这里。";if(loaded&&bank=="unmastered"&&unmasteredRows.Length==0)meaning.Text="在待复习词中点击“标为仍未掌握”，标记的词会自动收集到这里。";return;}position.Text=(bank=="reading"?"阅读词库":bank=="listening"?"听力词库":bank=="hard"?"高频难词":bank=="phrases"?"进阶短语":bank=="sentences"?"长难句":bank=="unmastered"?"未掌握复习":"待复习词")+" · "+(list.SelectedIndex+1)+" / "+list.Count;title.Text=Vocabulary.DisplayTitle(w);title.Font=bank=="sentences"||w.Word.Length>23?titleSmall:titleLarge;if(bank=="sentences"){phonetic.Text="";statistics.Text=w.Count+"个单词 · 精读";RenderSentence(w);return;}phonetic.Text=!appearance.ShowPhonetic||String.IsNullOrWhiteSpace(w.Phonetic)?"":"/"+w.Phonetic+"/";int readingCount=bank=="listening"?w.OtherCount:w.Count,readCoverage=bank=="listening"?w.OtherCoverage:w.Coverage;int listenCount=bank=="listening"?w.Count:w.OtherCount,listenCoverage=bank=="listening"?w.Coverage:w.OtherCoverage;statistics.Text=!appearance.ShowCounts?"":"阅读正文："+readingCount+"次 · "+readCoverage+"组材料\n听力原文："+listenCount+"次 · "+listenCoverage+"组材料";RenderDefinition(w);}
 void RenderSentence(Entry w){sourceNote.Text="来源："+w.ExampleSource+"\n主干为简化表达，结构拆解用于辅助精读。";string original=w.Example;meaning.Text=original+(definitions.Checked?"\n\n译文\n"+w.Translation+"\n\n"+w.Meaning.Replace("Main clause\n","句子主干\n").Replace("Structure\n","结构拆解\n").Replace("Useful expressions\n","重点表达\n"):"");meaning.SelectAll();meaning.SelectionFont=translationFont;meaning.SelectionColor=appearance.Text;meaning.Select(0,original.Length);meaning.SelectionFont=CachedUiFont(Math.Max(16,appearance.ExampleSize),FontStyle.Regular);foreach(System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(meaning.Text,@"(?m)^(译文|句子主干|结构拆解|重点表达)$")){meaning.Select(match.Index,match.Length);meaning.SelectionFont=posHeading;meaning.SelectionColor=appearance.Accent;}meaning.Select(0,0);meaning.ScrollToCaret();renderedSentence=w.Word;renderedSentenceChinese=definitions.Checked;RestoreSentenceScroll(w.Word);}
 void RenderDefinition(Entry w){
  sourceNote.Text="来源："+(w.ExampleSource.StartsWith("教学补充")?"教学补充例句":w.ExampleSource)+"\n词频和覆盖数用于演示筛选。";
  if(!definitions.Checked){meaning.Text="中文释义已隐藏";return;}
  string gloss=FormatMeaning(w.Meaning).Replace("\n\n","\n");string example=appearance.ShowExamples?"例句\n"+w.Example+"\n译文\n"+w.Translation:"";
  meaning.Text=gloss+(appearance.ShowExamples?"\n\n"+example:"");meaning.SelectAll();meaning.SelectionFont=meaning.Font;meaning.SelectionColor=meaning.ForeColor;
  if(appearance.ShowExamples){int exampleStart=gloss.Length+2;meaning.Select(exampleStart,example.Length);meaning.SelectionFont=exampleFont;int translationStart=exampleStart+"例句\n".Length+w.Example.Length+"\n译文\n".Length;meaning.Select(translationStart,w.Translation.Length);meaning.SelectionFont=translationFont;}
  foreach(System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(gloss,@"(?m)^(?:n\.\s*\(pl\.\)|vt\.|vi\.|adj\.|adv\.|prep\.|conj\.|pron\.|num\.|interj\.|aux\.|art\.|abbr\.|phr\.|n\.|v\.)")){meaning.Select(m.Index,m.Length);meaning.SelectionFont=posHeading;meaning.SelectionColor=appearance.Accent;}
  foreach(System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(gloss,@"★\s*[^,，;；\n]+")){meaning.Select(m.Index,m.Length);meaning.SelectionFont=starFont;meaning.SelectionColor=appearance.Accent;}
  meaning.Select(0,0);meaning.ScrollToCaret();
 }
 protected override void OnFormClosing(FormClosingEventArgs e){bookmarkTimer.Stop();if(!SaveBookmark(true)){e.Cancel=true;return;}base.OnFormClosing(e);}
 protected override void OnFormClosed(FormClosedEventArgs e){Application.RemoveMessageFilter(this);removalNoticeTimer.Dispose();bookmarkTimer.Dispose();exampleFont.Dispose();translationFont.Dispose();sourceFont.Dispose();starFont.Dispose();titleLarge.Dispose();titleSmall.Dispose();posHeading.Dispose();foreach(var f in uiFonts.Values)f.Dispose();base.OnFormClosed(e);}
}
static class Program {
 [STAThread]static int Main(string[] args){
  if(args.Contains("--self-test")){string output=args.Length>1?args[1]:Path.Combine(Path.GetTempPath(),"oii-self-test.txt");try{var rows=Vocabulary.Load();string[] banks={"reading","listening","hard","phrases","sentences","review"};foreach(string bank in banks)if(!rows.Any(w=>w.Bank==bank))throw new Exception("Missing sample bank: "+bank);if(rows.Any(w=>String.IsNullOrWhiteSpace(w.Word)||String.IsNullOrWhiteSpace(w.Meaning)))throw new Exception("Blank sample entry");if(rows.Select(w=>w.Bank+"|"+w.Word).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=rows.Length)throw new Exception("Duplicate sample key");if(FocusedWindow.NavigationDelta(Keys.Up)!=-1||FocusedWindow.NavigationDelta(Keys.Down)!=1||FocusedWindow.NavigationDelta(Keys.A)!=-1||FocusedWindow.NavigationDelta(Keys.D)!=1)throw new Exception("Navigation mapping");BookmarkStore.Test();FocusedWindow.CheckReviewOrder();WordExporter.Test();File.WriteAllText(output,"PASS: sample vocabulary loads; six banks present; keys unique; navigation, bookmark persistence and Word export checks passed.");return 0;}catch(Exception ex){File.WriteAllText(output,"FAIL: "+ex);return 1;}}
  string mode=args.FirstOrDefault()??"";
  bool automatedUi=mode=="--sentence-ui-test"||mode=="--audit-ui-test"||mode=="--study-ui-test";
  bool closeEarly=mode=="--close-early-test";
  bool testProfile=automatedUi||closeEarly||mode=="--ui-test";
  string report=args.Length>1?args[1]:Path.Combine(Path.GetTempPath(),"sentence-ui-check.txt");
  if(testProfile){
   string testDirectory=args.Length>2?Path.GetFullPath(args[2]):Path.Combine(Path.GetTempPath(),"OiiVocabularyQA",Guid.NewGuid().ToString("N"));
   BookmarkStore.PathName=Path.Combine(testDirectory,"bookmark.xml");
   AppearanceStore.FileName=Path.Combine(testDirectory,"settings.xml");
  }
  bool created;
  using(var mutex=new System.Threading.Mutex(true,testProfile?@"Local\OiiVocabularyQA":@"Local\OiiVocabulary2026",out created)){
   if(!created){
    if(testProfile)return 1;
    MessageBox.Show("oii背词已打开，请切换到现有窗口。");
    return 0;
   }
   if(!testProfile)StorageLocations.Initialize();
   Application.EnableVisualStyles();
   Application.SetCompatibleTextRenderingDefault(false);
   using(var window=new FocusedWindow(testProfile)){
    if(closeEarly)window.Shown+=delegate{window.Close();};
    if(automatedUi)window.ReadyForCheck=delegate{
     try{
      if(mode=="--study-ui-test")window.CheckStudyFeatures();
      else if(mode=="--audit-ui-test")window.CheckAuditMode();
      else window.CheckSentenceMode();
      File.WriteAllText(report,"PASS: "+mode);
     }catch(Exception ex){
      Environment.ExitCode=1;
      File.WriteAllText(report,"FAIL: "+ex);
     }finally{window.Close();}
    };
    Application.Run(window);
   }
   return Environment.ExitCode;
  }
 }
}
