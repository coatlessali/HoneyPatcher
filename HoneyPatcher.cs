using Godot;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MikuMikuLibrary;
using IniParser;
using IniParser.Model;

public partial class HoneyPatcher : Node2D
{	
	[Export]
	public FileDialog _usrdirdialog; // Restore USRDIR button
	[Export]
	public Button _install; // Restore USRDIR button
	[Export]
	public Button _restoreusrdir; // Restore USRDIR button
	
	string userProfile;
	// string defaultUsrdir;
	string usrdir;
	IniData data;
	
	public override void _Ready()
	{		
		
		// Signal Connection
		_usrdirdialog.DirSelected += OnUsrdirDialog;
		_install.Pressed += OnInstallPressed;
		_restoreusrdir.Pressed += OnRestoreUsrdirPressed;
		
		if(!File.Exists("HoneyConfig.ini"))
		{
			File.Copy("HoneyConfig.default.ini", "HoneyConfig.ini");
		}
		
		// https://github.com/rickyah/ini-parser
		// MIT License
		IniData data = new FileIniDataParser().ReadFile("HoneyConfig.ini");
		usrdir = data["main"]["usrdir"];
		
		
		GD.Print("it didn't fail??? peak....");
	}

	// Signals
	private void OnUsrdirDialog(string dir){
		usrdir = dir; // Set usrdir for current session
		IniData data = new FileIniDataParser().ReadFile("HoneyConfig.ini"); // Open config file
		data["main"]["usrdir"] = dir; // Set usrdir
		new FileIniDataParser().WriteFile("HoneyConfig.ini", data); // Write config file
	}
	private void OnInstallPressed(){
		GD.Print("todo: install button");
	}
	private void OnRestoreUsrdirPressed(){
		GD.Print("todo: uninstall button");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// GD.Print(usrdir);
	}
	
	
}
