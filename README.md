<div align="center">

[![LOGO](Ink%20Canvas/Resources/InkCanvas.png?raw=true "LOGO")](# "LOGO")

# Ink-Canvas

  [直接下载](https://github.com/WXRIW/Ink-Canvas/releases/latest "Latest Releases") | [使用指南](https://github.com/WXRIW/Ink-Canvas/blob/master/Manual.md "说明和指南") | [常见问题](https://github.com/WXRIW/Ink-Canvas#FAQ "FAQ")
  
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2FWXRIW%2FInk-Canvas.svg?type=shield)](https://app.fossa.com/projects/git%2Bgithub.com%2FWXRIW%2FInk-Canvas?ref=badge_shield) [![交流群](https://img.shields.io/badge/-%E4%BA%A4%E6%B5%81%E7%BE%A4%20891913576-blue?style=flat&logo=TencentQQ)](https://jq.qq.com/?_wv=1027&k=NvlM1Rgg)  ![GitHub issues](https://img.shields.io/github/issues/WXRIW/Ink-Canvas?logo=github)


  
A fantastic Ink Canvas in WPF/C#, with fantastic support for Seewo Boards.

学校从传统投影仪换成了希沃白板，由于自带的“希沃白板”软件太难用，也没有同类好用的画板软件，所以开发了该画板。  

  [<img src="https://get.microsoft.com/images/zh-CN%20dark.svg" style="width: 200px;"/>](https://www.microsoft.com/store/apps/9NXJFDD97XJ3?cid=ghreadme)
</div>

## 🔧 特性
对 Microsoft PowerPoint 有优化支持（强烈不推荐使用 WPS，会导致 WPS 自己把自己卡住，并且 WPS 对触摸屏的支持实在是差，PPT 翻页点击就行，而不是滑动，也不能放大缩小）  
**笔细的一头写字，反过来粗的一头是橡皮擦。（希沃白板自己并不支持此功能）**  
当然，用手直接擦也是可以的（跟希沃白板一样）  
支持 Active Pen (支持压感)  
对于其他红外线屏也可以提供相似功能，欢迎大家测试！  

## ⚠️ 提示
- 提问前请先读[FAQ](https://github.com/WXRIW/Ink-Canvas#FAQ "FAQ")
- 遇到问题请先尝试自行解决，若无法自行解决，请简单描述你的期望与现实的差异性。如果有必要，请附上复现此问题的操作步骤或错误日志¹ （可适当配图），等待回复。
- 对新功能的有效意见和合理建议，开发者会适时回复并进行开发。Ink Canvas并非商业性质的软件，请勿催促开发者，耐心才能让功能更少BUG、更加稳定。

> 等待是人类的一种智慧

 [1] ：对于长文本，可以使用在线剪贴板 （如 https://pastes.dev/ ），粘贴完毕点击 `SAVE` 后复制链接进行分享
 
## 📗 FAQ
### 在 Windows 10 以下版本系统中部分图标显示为 “□” 怎么办？
[点击下载](https://aka.ms/SegoeFonts "SegoeFonts") SegoeFonts 文件，安装压缩包中 `SegMDL2.ttf` 字体后重启即可解决

### 点击放映后一翻页就闪退？
考虑是由于`Microsoft Office`未激活导致的，请自行激活

### 放映后画板程序不会切换到PPT模式？
如果你曾经安装过`WPS`且在卸载后发现此问题则是由于暂时未确定的问题所导致，可以尝试重新安装WPS
> “您好，关于您反馈的情况我们已经反馈技术同学进一步分析哈，辛苦您可以留意后续WPS版本更新哈~” --回复自WPS客服

另外，处在保护（只读）模式的PPT不会被识别

### **安装后**程序无法正常启动？
请检查你的电脑上是否安装了 `.Net Framework 4.7.2` 或更高版本。若没有，请前往官网下载  
如果仍无法运行，请检查你的电脑上是否安装了 `Microsoft Office`。若没有，请安装后重试

### 我该在何处提出功能需求和错误报告？

1. GitHub Issues

    功能需求：https://github.com/WXRIW/Ink-Canvas/labels/enhancement/new 

    错误报告：https://github.com/WXRIW/Ink-Canvas/labels/bug/new

2. Tencent QQ
    [![交流群](https://img.shields.io/badge/-%E4%BA%A4%E6%B5%81%E7%BE%A4%20891915376-blue?style=flat&logo=TencentQQ)](https://jq.qq.com/?_wv=1027&k=NvlM1Rgg) 

### 大小屏设备交替使用/手指或笔头过大 导致被识别成橡皮怎么办？
点击画板的“设置”按钮并开启`特殊屏幕`选项即可


## 感谢
感谢 [yuwenhui2020](https://github.com/yuwenhui2020) 为 `Ink Canvas 使用说明` 做出的贡献！  
感谢 [CN-Ironegg](https://github.com/CN-Ironegg)、[jiajiaxd](https://github.com/jiajiaxd)、[Kengwang](https://github.com/kengwang)、[Raspberry Kan](https://github.com/Raspberry-Monster) 为本项目贡献代码！  

## License
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2FWXRIW%2FInk-Canvas.svg?type=large)](https://app.fossa.com/projects/git%2Bgithub.com%2FWXRIW%2FInk-Canvas?ref=badge_large)
