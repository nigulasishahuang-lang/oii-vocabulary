using System;
using System.IO;

public static class StorageLocations {
 public static readonly string Root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"oii背词数据");
 public static void Initialize(){Directory.CreateDirectory(Root);}
}