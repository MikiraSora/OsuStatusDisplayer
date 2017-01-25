##屙屎状态显示
----------------------------------
####注意:本软件需要和osuSync程序配套使用，或者您可以通过TCP来向7582发送指定格式的数据。本程序默认在此端口提取数据并解析出内容，再显示。


#####发送的内容格式:  <字符1><内容1> <字符2><内容1> <字符3><内容1>，字符和内容成一组，每组空格分开,如
a54 d87 q65789
c44 tMomo - MikiraSora dInsane h100 c200
<br>
现在支持的参数字符有:<br>
*	a :  Acc<br>
*	b : BeatmapId<br>
*	s : BeatmapSetId<br>
*	h : Hp<br>
*	c : Combo<br>
*	t : Tiitle(其实是Aritist - Tittle)<br>
*	d : DifficultName<br>
*	m : Mods (值是一串数字，Flag Enum)<br>
<br>
```C#
[Flags]
public enum Mods
{
	None = 0,
    NoFail = 1 << 0,
    Easy = 1 << 1,
	Hidden = 1 << 3,
    HardRock = 1 << 4,
    SuddenDeath = 1 << 5,
    DoubleTime = 1 << 6,
    Relax = 1 << 7,
    HalfTime = 1 << 8,
    Nightcore = 1 << 9,
    Flashlight = 1 << 10,
    Autoplay = 1 << 11,
    SpunOut = 1 << 12,
    Relax2 = 1 << 13,
    Perfect = 1 << 14,
    Key1 = 1 << 26,
    Key3 = 1 << 27,
	Key2 = 1 << 28,
	Key4 = 1 << 15,
    Key5 = 1 << 16,
    Key6 = 1 << 17,
    Key7 = 1 << 18,
    Key8 = 1 << 19,
    Key9 = 1 << 24,
    KeyCoop = 1 << 25,
    FadeIn = 1 << 20,
    Random = 1 << 21,
    Cinema = 1 << 22,
    Target = 1 << 23,
}
/*
	然后你可以这么用
	Mods mods=Mods.HardRock|Mods.NoFail;
	int flagValue=(int)mods;
	...
*/
```


####[示例代码](http://git.oschina.net/remilia/osuSync/blob/dpdev/OtherPlugins/OutputSever/OsuStatusOutputSever.cs)
