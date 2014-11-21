using System;

public class JumpCommandRegister : Attribute {
	public string command;
	public string help;
	public string gameObjName;

	public JumpCommandRegister(string command, string help = "", string gameObjName = "") {
		this.command     = command;
		this.help        = help;
		this.gameObjName = gameObjName;
	}
}
