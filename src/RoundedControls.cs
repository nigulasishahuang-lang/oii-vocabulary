using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;

public static class RoundUi {
 public static GraphicsPath Path(RectangleF box,float radius){var p=new GraphicsPath();float d=Math.Min(radius*2,Math.Min(box.Width,box.Height));if(d<=0)return p;p.AddArc(box.X,box.Y,d,d,180,90);p.AddArc(box.Right-d,box.Y,d,d,270,90);p.AddArc(box.Right-d,box.Bottom-d,d,d,0,90);p.AddArc(box.X,box.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
 public static void Fill(Graphics g,RectangleF box,float radius,Color fill,Color border,int borderSize){g.SmoothingMode=SmoothingMode.AntiAlias;using(var path=Path(box,radius)){using(var brush=new SolidBrush(fill))g.FillPath(brush,path);if(borderSize>0)using(var pen=new Pen(border,borderSize))g.DrawPath(pen,path);}}
 public static void ButtonPaint(Control c,PaintEventArgs e,Color fill,Color border,int borderSize){float scale=e.Graphics.DpiX/96f;e.Graphics.Clear(c.Parent==null?SystemColors.Control:c.Parent.BackColor);Fill(e.Graphics,new RectangleF(.75f,.75f,c.Width-1.5f,c.Height-1.5f),10*scale,fill,border,borderSize);var box=Rectangle.FromLTRB(c.Padding.Left+6,c.Padding.Top+2,c.Width-c.Padding.Right-6,c.Height-c.Padding.Bottom-2);var flags=TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis;var align=c is Button?((Button)c).TextAlign:ContentAlignment.MiddleCenter;flags|=align==ContentAlignment.MiddleLeft?TextFormatFlags.Left:align==ContentAlignment.MiddleRight?TextFormatFlags.Right:TextFormatFlags.HorizontalCenter;TextRenderer.DrawText(e.Graphics,c.Text,c.Font,box,c.Enabled?c.ForeColor:SystemColors.GrayText,flags);}
 public static void Clip(Control c,int radius){if(c.Width<1||c.Height<1)return;using(var path=Path(new RectangleF(0,0,c.Width,c.Height),radius)){var old=c.Region;c.Region=new Region(path);if(old!=null)old.Dispose();}}
}
public class RoundedButton:Button {
 bool hot,pressed;public event PaintEventHandler AfterPaint;
 public RoundedButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);Cursor=Cursors.Hand;}
 protected override void OnMouseEnter(EventArgs e){hot=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hot=false;pressed=false;Invalidate();base.OnMouseLeave(e);}protected override void OnMouseDown(MouseEventArgs e){pressed=true;Invalidate();base.OnMouseDown(e);}protected override void OnMouseUp(MouseEventArgs e){pressed=false;Invalidate();base.OnMouseUp(e);}
 protected override void OnPaint(PaintEventArgs e){Color fill=BackColor;if(Enabled&&hot)fill=FlatAppearance.MouseOverBackColor.IsEmpty?ControlPaint.Light(BackColor,.12f):FlatAppearance.MouseOverBackColor;if(Enabled&&pressed)fill=ControlPaint.Dark(fill,.06f);RoundUi.ButtonPaint(this,e,fill,FlatAppearance.BorderColor,FlatAppearance.BorderSize);if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,Rectangle.Inflate(ClientRectangle,-6,-6),ForeColor,fill);if(AfterPaint!=null)AfterPaint(this,e);}
}
public class RoundedCheckBox:CheckBox {
 public RoundedCheckBox(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);Cursor=Cursors.Hand;}
 protected override void OnPaint(PaintEventArgs e){RoundUi.ButtonPaint(this,e,BackColor,FlatAppearance.BorderColor,FlatAppearance.BorderSize);if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,Rectangle.Inflate(ClientRectangle,-6,-6),ForeColor,BackColor);}
}
public class RoundedCard:TableLayoutPanel {
 public Color Outline=Color.Transparent;
 public RoundedCard(){DoubleBuffered=true;ResizeRedraw=true;}
 protected override void OnPaintBackground(PaintEventArgs e){e.Graphics.Clear(Parent==null?SystemColors.Control:Parent.BackColor);if(Width>1&&Height>1)RoundUi.Fill(e.Graphics,new RectangleF(.75f,.75f,Width-1.5f,Height-1.5f),16*e.Graphics.DpiX/96f,BackColor,Outline,Outline.A==0?0:1);}
}
public class RoundedSearchBox:Panel {
 readonly TextBox input;public Color Accent=Color.FromArgb(33,113,69);
 public RoundedSearchBox(TextBox text){input=text;DoubleBuffered=true;ResizeRedraw=true;BackColor=Color.White;input.BorderStyle=BorderStyle.None;input.Dock=DockStyle.None;input.Margin=Padding.Empty;Controls.Add(input);input.GotFocus+=delegate{Invalidate();};input.LostFocus+=delegate{Invalidate();};input.FontChanged+=delegate{Arrange();};Click+=delegate{input.Focus();};}
 void Arrange(){int inset=(int)(12*DpiRatio);input.SetBounds(inset,Math.Max(2,(Height-input.PreferredHeight)/2),Math.Max(1,Width-inset*2),input.PreferredHeight);}
 float DpiRatio {get{using(var g=CreateGraphics())return g.DpiX/96f;}}
 protected override void OnResize(EventArgs e){base.OnResize(e);if(input!=null)Arrange();}
 protected override void OnPaintBackground(PaintEventArgs e){e.Graphics.Clear(Parent==null?SystemColors.Control:Parent.BackColor);if(Width>1&&Height>1)RoundUi.Fill(e.Graphics,new RectangleF(.75f,.75f,Width-1.5f,Height-1.5f),10*e.Graphics.DpiX/96f,BackColor,input.Focused?Accent:Color.FromArgb(196,218,201),1);}
}
public class RoundedComboBox:ComboBox {
 RoundedDropDown popup;
 public Color Accent=Color.FromArgb(33,113,69);
 public RoundedComboBox(){DrawMode=DrawMode.OwnerDrawFixed;ItemHeight=25;FlatStyle=FlatStyle.Flat;DropDownStyle=ComboBoxStyle.DropDownList;BackColor=Color.White;}
 protected override void OnDrawItem(DrawItemEventArgs e){using(var brush=new SolidBrush(BackColor))e.Graphics.FillRectangle(brush,e.Bounds);if(e.Index<0)return;bool active=(e.State&DrawItemState.Selected)!=0;if(active)RoundUi.Fill(e.Graphics,new RectangleF(e.Bounds.X+4,e.Bounds.Y+2,e.Bounds.Width-8,e.Bounds.Height-4),6,Color.FromArgb((Accent.R+255*4)/5,(Accent.G+255*4)/5,(Accent.B+255*4)/5),Color.Transparent,0);TextRenderer.DrawText(e.Graphics,GetItemText(Items[e.Index]),Font,Rectangle.Inflate(e.Bounds,-12,0),ForeColor,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine);}
 protected override void OnDropDown(EventArgs e){base.OnDropDown(e);if(popup==null)popup=new RoundedDropDown(this);popup.Attach();}
 protected override void OnHandleDestroyed(EventArgs e){if(popup!=null){popup.Dispose();popup=null;}base.OnHandleDestroyed(e);}
 protected override void WndProc(ref Message m){base.WndProc(ref m);if(m.Msg==0x000F&&IsHandleCreated&&Width>1&&Height>1){using(var g=Graphics.FromHwnd(Handle)){float scale=g.DpiX/96f;g.Clear(Parent==null?SystemColors.Control:Parent.BackColor);RoundUi.Fill(g,new RectangleF(.75f,.75f,Width-1.5f,Height-1.5f),10*scale,BackColor,Focused?Accent:Color.FromArgb(196,218,201),1);Color text=Enabled?ForeColor:SystemColors.GrayText;TextRenderer.DrawText(g,Text,Font,new Rectangle((int)(12*scale),0,Math.Max(1,Width-(int)(42*scale)),Height),text,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis);float x=Width-18*scale,y=Height/2f;using(var pen=new Pen(text,1.5f*scale)){pen.StartCap=pen.EndCap=LineCap.Round;g.DrawLines(pen,new[]{new PointF(x-4*scale,y-2*scale),new PointF(x,y+2*scale),new PointF(x+4*scale,y-2*scale)});}}}}
}
sealed class RoundedDropDown:NativeWindow,IDisposable {
 [StructLayout(LayoutKind.Sequential)]struct Rect {public int Left,Top,Right,Bottom;}
 [StructLayout(LayoutKind.Sequential)]struct ComboInfo {public int Size;public Rect Item,Button;public int State;public IntPtr Combo,Edit,List;}
 [DllImport("user32.dll")]static extern bool GetComboBoxInfo(IntPtr combo,ref ComboInfo info);
 [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr window,out Rect rect);
 [DllImport("gdi32.dll")]static extern IntPtr CreateRoundRectRgn(int left,int top,int right,int bottom,int width,int height);
 [DllImport("user32.dll")]static extern int SetWindowRgn(IntPtr window,IntPtr region,bool redraw);
 [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr value);
 readonly RoundedComboBox owner;int width,height;bool shaping;
 public RoundedDropDown(RoundedComboBox combo){owner=combo;}
 public void Attach(){var info=new ComboInfo{Size=Marshal.SizeOf(typeof(ComboInfo))};if(!GetComboBoxInfo(owner.Handle,ref info)||info.List==IntPtr.Zero)return;if(Handle!=info.List){if(Handle!=IntPtr.Zero)ReleaseHandle();AssignHandle(info.List);width=height=0;}Shape();}
 void Shape(){if(shaping||Handle==IntPtr.Zero)return;Rect box;if(!GetWindowRect(Handle,out box))return;int w=box.Right-box.Left,h=box.Bottom-box.Top;if(w<2||h<2||(width==w&&height==h))return;shaping=true;try{width=w;height=h;float scale;using(var g=Graphics.FromHwnd(Handle))scale=g.DpiX/96f;int diameter=(int)(20*scale);IntPtr region=CreateRoundRectRgn(0,0,w+1,h+1,diameter,diameter);if(SetWindowRgn(Handle,region,true)==0)DeleteObject(region);}finally{shaping=false;}}
 protected override void WndProc(ref Message m){base.WndProc(ref m);if(m.Msg==0x0047||m.Msg==0x0005)Shape();if((m.Msg==0x000F||m.Msg==0x0085)&&Handle!=IntPtr.Zero){using(var g=Graphics.FromHwnd(Handle)){float scale=g.DpiX/96f;g.SmoothingMode=SmoothingMode.AntiAlias;using(var path=RoundUi.Path(new RectangleF(.5f,.5f,Math.Max(1,width-3),Math.Max(1,height-3)),9*scale))using(var pen=new Pen(Color.FromArgb(196,218,201)))g.DrawPath(pen,path);}}}
 public void Dispose(){if(Handle!=IntPtr.Zero)ReleaseHandle();}
}
