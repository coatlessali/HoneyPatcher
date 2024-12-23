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
		/* userProfile = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
		// if we're on macOS we can actually assume with confidence where the files will be
		if(OperatingSystem.IsMacOS())
		{
			defaultUsrdir = Path.Combine(userProfile, "Library/Application Support/rpcs3/dev_hdd0/game/NPUB30927/USRDIR");
		}
		else
		{
			defaultUsrdir = userProfile;
		} */
		
		// Signal Connection
		_usrdirdialog.DirSelected += OnUsrdirDialog;
		_install.Pressed += OnInstallPressed;
		_restoreusrdir.Pressed += OnRestoreUsrdirPressed;
		
		GD.Print("1");
		
		if(!File.Exists("HoneyConfig.ini"))
		{
			File.Copy("HoneyConfig.default.ini", "HoneyConfig.ini");
		}
		IniData data = new FileIniDataParser().ReadFile("HoneyConfig.ini");
		usrdir = data["main"]["usrdir"];
		
		// https://github.com/rickyah/ini-parser
		// MIT License
		GD.Print("this is here to tell me if it failed or not");
	}

	// Signals
	private void OnUsrdirDialog(string dir){
		GD.Print("pressed");
		// usrdir = path;
		GD.Print(dir);
	}
	private void OnInstallPressed(){
		GD.Print("pressed");
	}
	private void OnRestoreUsrdirPressed(){
		GD.Print("pressed");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// GD.Print(usrdir);
	}
	
	
}
