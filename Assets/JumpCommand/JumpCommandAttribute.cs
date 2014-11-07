using System;


public class JumpCommandAttribute : Attribute {
	public string mCommand;
	public string mHelp;

	public JumpCommandAttribute(string command, string help = "") {
		mCommand  = command;
		mHelp     = help;
	}
}
