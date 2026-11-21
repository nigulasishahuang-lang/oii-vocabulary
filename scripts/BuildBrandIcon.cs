using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Collections.Generic;

// The oii wordmark is drawn as geometry so it stays legible at taskbar size.
class BuildBrandIcon {
 static GraphicsPath Round(float x,float y,float w,float h,float r) {
  var p=new GraphicsPath();p.AddArc(x,y,r*2,r*2,180,90);p.AddArc(x+w-r*2,y,r*2,r*2,270,90);p.AddArc(x+w-r*2,y+h-r*2,r*2,r*2,0,90);p.AddArc(x,y+h-r*2,r*2,r*2,90,90);p.CloseFigure();return p;
 }
 static Bitmap Render(int size) {
  int hi=size*4;var large=new Bitmap(hi,hi,PixelFormat.Format32bppArgb);
  using(var g=Graphics.FromImage(large)) {
   g.SmoothingMode=SmoothingMode.AntiAlias;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
   g.ScaleTransform(hi/512f,hi/512f);g.Clear(Color.Transparent);
   using(var tile=Round(20,20,472,472,112))using(var fill=new LinearGradientBrush(new Point(20,20),new Point(490,492),Color.FromArgb(29,49,74),Color.FromArgb(16,30,51)))g.FillPath(fill,tile);
   using(var o=new Pen(Color.FromArgb(247,249,245),44)){o.StartCap=o.EndCap=LineCap.Round;g.DrawEllipse(o,85,220,144,144);}
   using(var stem=new SolidBrush(Color.FromArgb(247,249,245))) {
    using(var p=Round(270,218,48,170,24))g.FillPath(stem,p);
    using(var p=Round(365,218,48,170,24))g.FillPath(stem,p);
   }
   using(var mint=new SolidBrush(Color.FromArgb(108,224,184))) {
    g.FillEllipse(mint,266,139,56,56);
    g.FillEllipse(mint,361,139,56,56);
   }
  }
  var result=new Bitmap(size,size,PixelFormat.Format32bppArgb);
  using(var g=Graphics.FromImage(result)){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.CompositingQuality=CompositingQuality.HighQuality;g.DrawImage(large,0,0,size,size);}large.Dispose();return result;
 }
 static void Main(string[] args) {
  string dir=args[0];Directory.CreateDirectory(dir);var sizes=new[]{16,20,24,32,40,48,64,128,256};var data=new List<byte[]>();
  foreach(int s in sizes)using(var bitmap=Render(s))using(var stream=new MemoryStream()){bitmap.Save(stream,ImageFormat.Png);data.Add(stream.ToArray());bitmap.Save(Path.Combine(dir,"oii背词-"+s+".png"),ImageFormat.Png);}
  using(var writer=new BinaryWriter(File.Create(Path.Combine(dir,"oii背词.ico")))) {writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)sizes.Length);int offset=6+16*sizes.Length;for(int i=0;i<sizes.Length;i++){writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)0);writer.Write((byte)0);writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(data[i].Length);writer.Write(offset);offset+=data[i].Length;}foreach(var bytes in data)writer.Write(bytes);}
  using(var bitmap=Render(512))bitmap.Save(Path.Combine(dir,"oii背词.png"),ImageFormat.Png);
 }
}
