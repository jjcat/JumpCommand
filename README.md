JumpCommand
===========

A develop console for Unity let you input command string to execute debug functions.

# Screenshot
![screenshot](http://i.imgur.com/tbmLPVR.png)

# Installation
 
1. Copy JumpCommand directory to your project folder.
2. Create a GameObject in the scene and add a JumpCommand component to it.
3. Run your game and press `~` to open command input.
4. Input some command and press `Enter` to execute.
5. Press `~` again to close command input.

# Registering Custom Commands
JumpCommand can only register static function and class function that inherited from Monobehaviour. If the function's parameter type is not native type(int, float, double, stirng, bool), you will need write Type Converter for you custom parameter type. 

1. Add JumpCommandAttribute to function
2. If the function is Monobehaviour, you need select GameObject at Hierarchy window when you call it.

Here is a example:

```
[JumpCommandAttribute["test","output integer"]]
static public void CommandTest(int i) {
    Debug.Log(i.ToString());
}

```

We have registered a command named `test` with help text "output integer", build-in command `ls` can list all commands and help text. Now input `test 10` as a command we can get a output message `10` in Console.

If you want to change a person's speed from command like `speed 0.5`, add JumpCommandAttribute to AdjustSpeed, and make sure when you input the command you have selected the GameObject you want to invoke.

```
//Person.cs
public class Person :  MonoBehaviour {
    //...
	[JumpCommandAttribute["speed","Change person speed"]]
	public void AdjustSpeed(float value) {
	    speed += value;
	}
	//...
}

```

If the function is static, just add JumpCommandAttribute and call it.
If the function is Monobehaviour class, add JumpCommandAttribute and select the GameObject when you call it.

# Write Type Converter for Custom Parameter Type
Most native data types (int, float, bool) have default type converters, so you don't need to write converts for them. JumpCommand can convert string arguments to proper parameters. If you want to use custom parameter type, you need to write Type Converter for your type. There is a reference [How to: Implement a Type Converter](http://msdn.microsoft.com/en-us/library/ayybcxe5.aspx "How to: Implement a Type Converter") at MSDN show you how to write a converter to convert string to Point class. 

# Contact Author
Feel free to ask me any question.


jjcat@outlook.com	


