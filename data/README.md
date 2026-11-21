# 演示词库

原始条目保存在 `scripts/build_demo_data.py`，包括为本项目编写的释义、例句、译文和句子结构说明。阅读、听力、高频难词、进阶短语、长难句、待复习共 6 类。频次和覆盖数是演示数值，待复习的初始条目是演示用的预设内容。

构建时只读取生成脚本中的条目，生成 `data/words.gz` 并嵌入可执行文件。生成脚本使用 Python 标准库，可以独立运行：

```powershell
python .\scripts\build_demo_data.py
```

压缩包解压后是版本号与条数两个小端 32 位整数，随后逐条存放 5 个字符串、4 个小端 32 位整数、4 个字符串。每个字符串使用一个小端 32 位字节长度，随后是 UTF-8 内容。读取实现见 `src/FocusedVocabulary.cs` 的 `Vocabulary.Load`。
