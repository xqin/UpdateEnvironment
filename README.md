#系统环境变量自动化配置工具

##系统需求
`Windows` + `.NET Framework 2.0`

##安装方法:
双击build.bat对源码进行编译, 双击编译后的exe文件, 会自动注册 `.env` 的扩展名.


##卸载方法:
打开键盘上的 `ScrollLock` 灯, 然后再双击exe文件会进行反注册 `.env` 的扩展名.


##使用方法:
将下面的代码保存为 `*.env` 文件, 修改里面的配置项为自己所需的, 然后双击该`.env`文件, 配置文件里面指定的环境变量将会被添加至系统的环境变量中, 不需要重启就可以生效.(如果之前有打开`cmd.exe`,关闭并重新打开即可生效)


```xml
<?xml version="1.0" encoding="UTF-8"?>
<Environment>
    <!-- 系统环境变量 -->
	<!--
		{FOLDER}	代表当前文件所在的目录,路径最后面不带 \
		{PATH}		该字符串将被替换为环境变量中相应的值,如果不存在则为空
	-->
	<system>
		<set name="JAVA_HOME" value="{FOLDER}\jdk1.7.0_02" />
		<set name="CLASSPATH" value=".;%JAVA_HOME%\lib\dt.jar;%JAVA_HOME%\lib\tools.jar;" />
		<set name="PATH" value="{PATH};%JAVA_HOME%\bin" />
	</system>
</Environment>
```
>上面的代码,将会在系统环境变量中添加`JAVA_HOME`和`CLASSPATH`这两个环境变量, 并更新`PATH`这个环境变量(因为它的值里有使用它自身`{PATH}`,所以是更新,而不是直接赋值)

###ps:好处,谁用谁知道 ;)