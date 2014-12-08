#![](http://i.imgur.com/rBXIi4Q.png)JumpCommand

JumpCommand 能在Unity中输入命令行来进行函数调用。

##截屏

![image](https://camo.githubusercontent.com/5072e4e11db963129193428bbb89af3d0fa50e4c/687474703a2f2f692e696d6775722e636f6d2f74626d4c5056522e706e67)

##安装

1. 拷贝 *JumpCommand* 文件夹到你的工程目录下。
2. 在场景中添加一个 *GameObject* ，并在它上面添加上 *JumpCommandGUI* 组件。
3. 运行游戏，按 `~` 键打开命令输入界面。
4. 输入命令并按 `Enter` 键运行命令，按 `↑` 或 `↓` 键获取历史命令。
5. 再次按 `~` 键关闭命令输入界面。

##自定义命令

为函数注册一条命令，你需要

1. 为函数添加 *JumpCommandRegister* 。
2. 如果该函数是 *Monohehaviour* 的函数，你需要在调用该命令的时候在 Hierarchy 窗口中选择该 *GameObject* 。

下面的例子为一个静态函数注册了一条命令 `test` 。


```csharp
[JumpCommandRegister("test","output integer")]
static public void CommandTest(int i) {
    Debug.Log(i.ToString());
}
```

在这个例子中我们注册了一条命令 `test` 和它的帮助信息 `“output integer”` ，内置命令 `ls` 会列出所有的命令和帮助信息。现在输入 `test 10` 我们可以在 Console 窗口中看到输出的消息`10`。


下面的例子为一个 MonoBehaviour 类的 AdjustSpeed 函数注册了一条命令 `speed` 。

```csharp
//Person.cs
public class Person :  MonoBehaviour {
    //...
    [JumpCommandRegister("speed","Change person speed")]
    public void AdjustSpeed(float value) {
        speed += value;
    }
    //...
}
```

现在你可以在游戏运行的时候输入一条命令 `speed 0.5` 来改变一个人物的速度，确保当你输入命令的时候 Hierarchy 窗口里选择中带有 *Person* 组件的那个 *GameObject* 。

JumpCommand 现在只能注册静态函数和从 *Monohehaviour* 继承的类的成员函数。如果函数的参数类型不是内置类型 (int, float, double, string, bool, enum) ，你将需要为该类型编写 Type Converter。

如果调用的函数是静态函数，只需要添加 JumpCommandRegister 。如果该函数是 *Monobehaviour* 的函数，添加 *JumpCommandRegister* 并在调用命令的时候在Hierarchy选择该 *GameObject* 。

## 为自定义参数类型编写 Type Converter

大部分的内置类型 (int, float, double, string, bool, enum)都不需要编写转换类。 JumpCommand 能正确的把字符串转换成相应的参数。如果你想要使用自定义参数类型，你需要为该类型编写 Type Converter 。这篇 MSDN 上的[参考文章](http://msdn.microsoft.com/en-us/library/ayybcxe5.aspx)演示了如何编写转换类将string转换为Point。

## 联系作者

问我任何问题。

jjcat@outlook.com
