using System;

public class JumpCommandRegister : Attribute {
	public string mCommand;
	public string mHelp;

	public JumpCommandRegister(string command, string help = "") {
		mCommand  = command;
		mHelp     = help;
	}
}
