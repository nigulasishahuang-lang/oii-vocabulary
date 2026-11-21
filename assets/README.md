# 品牌资源

深蓝底、白色 oii 字样和薄荷绿圆点的新 Logo 由项目的几何绘图代码生成。PNG 用于文档及预览，ICO 包含常用 Windows 图标尺寸。

在项目目录使用 Windows PowerShell 可重新生成：

```powershell
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
New-Item -ItemType Directory -Force dist | Out-Null
& $csc /nologo /codepage:65001 /reference:System.Drawing.dll /out:dist\BuildBrandIcon.exe scripts\BuildBrandIcon.cs
if ($LASTEXITCODE -eq 0) { & .\dist\BuildBrandIcon.exe assets }
```
