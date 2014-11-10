using System;

public class JumpCommandRegister : Attribute {
	public string command;
	public string help;

	public JumpCommandRegister(string command, string help = "") {
		this.command  = command;
		this.help     = help;
	}
}
