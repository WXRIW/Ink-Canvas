# Ink-Canvas
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2FWXRIW%2FInk-Canvas.svg?type=shield)](https://app.fossa.com/projects/git%2Bgithub.com%2FWXRIW%2FInk-Canvas?ref=badge_shield)

A fantastic Ink Canvas in WPF/C#, with fantastic support for Seewo Boards.

学校从传统投影仪换成了希沃白板，由于自带的“希沃白板”软件太难用，也没有同类好用的画板软件，所以开发了该画板。

## 特性
对 Microsoft PowerPoint 有优化支持（强烈不推荐使用 WPS，会导致 WPS 自己把自己卡住，并且 WPS 对触摸屏的支持实在是差，PPT 翻页点击就行，而不是滑动，也不能放大缩小）  
**笔细的一头写字，反过来粗的一头是橡皮擦。（希沃白板自己并不支持此功能）**  
当然，用手直接擦也是可以的（跟希沃白板一样）  
支持 Active Pen (支持压感)  
对于其他红外线屏也可以提供相似功能，欢迎大家测试！  

##目录
[1.介绍](#introduce)
[2.用法](#useage)
[3.项目](#item)

##Ink Canvas是什么? <span id='introduce'></span>
Ink Canvas是一个用于授课等场景的笔迹软件
###Ink Canvas模式


* PPT模式
    *  在ppt模式下支持画笔、橡皮擦、图形工具、快捷翻页按键、换页换笔迹等功能
    *  自动保存PPT笔迹，下次打开笔迹仍然存在，并且随时可修改。
    *  隐藏PPT页面可以提示重新显示
* 画板模式（黑板模式）
    *  在画板模式下有着一整个类似希沃白板一样的画板
    *  支持添加新页面和页面切换
    *  支持多指书写：黑板模式界面左下角人像图标为切换按钮
* 屏幕画笔模式
    *  在屏幕画笔模式下可以显示原屏幕内容的同时将鼠标调为画笔书写授课笔迹

###Ink Canvas功能
* 截屏：任意模式模式下（包括鼠标）下点击相机图标截图并自动保存到user/picture/Ink Canvas
* 自动查杀希沃部分软件
* 单个墨迹选中后缩放
* 全屏幕笔迹双指手势缩放（旋转和拖动也是双指手势）
* 图形绘制
 多条平行线。带焦点的和不带焦点的椭圆、双曲线。
 正圆、虚线圆、圆柱、圆锥、长方体
 坐标系（数轴，平面坐标系，空间坐标系）
 直线，虚线
* 墨迹转图形，目前可实现智能识别圆、三角形、特殊四边形
  自动转换为规范图形。可自动识别同心圆，相切圆，可自动识别球的截面圆
* 墨迹回放，从头到尾重新播放墨迹绘制过程的连续动画
* 抽奖：导入名单后选择抽取人数即刻开始抽奖
* 倒计时：分钟倒计时

##用法
 <span id='useage'></span>
 
##1

###项目 <span id='item'></span>

* GIThub上的[Ink Canvas](https://github.com/WXRIW/Ink-Canvas)

## License
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2FWXRIW%2FInk-Canvas.svg?type=large)](https://app.fossa.com/projects/git%2Bgithub.com%2FWXRIW%2FInk-Canvas?ref=badge_large)
