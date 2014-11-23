using System;

public class JumpCommandRegister : Attribute {
	public string command;
	public string help;
	public string gameObjFullName;

	public JumpCommandRegister(string command, string help = "", string gameObjFullName = "") {
		this.command     = command;
		this.help        = help;
		this.gameObjFullName = gameObjFullName;
	}
}
