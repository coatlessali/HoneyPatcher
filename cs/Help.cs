using Godot;
using System;

public partial class Help : Button{
	public override void _Ready(){
		Pressed += HelpPressed;
	}
	private void HelpPressed(){
		OS.ShellOpen("https://github.com/coatlessali/HoneyPatcher/wiki/Install-&-Usage-Guide");
	}
}
