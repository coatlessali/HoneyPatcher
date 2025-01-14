using Godot;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using MikuMikuLibrary.Archives;
using MikuMikuLibrary.Archives.CriMw;
using MikuMikuLibrary.IO;
using IniParser;
using IniParser.Model;
using PleOps.XdeltaSharp;
using PleOps.XdeltaSharp.Decoder;

public partial class HoneyPatcher : Node2D
{	
	[Export]
	public AcceptDialog _acceptdialog; // Errors and whatnot
	[Export]
	public FileDialog _usrdirdialog; // Restore USRDIR button
	[Export]
	public Button _install; // Restore USRDIR button
	[Export]
	public Button _restoreusrdir; // Restore USRDIR button
	[Export]
	public Button _modsfolder;
	[Export]
	public Label _progress; // Progress label
	
	string userProfile;
	string psarc;
	string usrdir;
	IniData data;
	
	public override void _Ready()
	{	
		// Dumb hack until I can get around to this
		if (!Engine.IsEditorHint()){
			if (OS.GetName() == "macOS")
				Directory.SetCurrentDirectory(Path.Combine(OS.GetExecutablePath(), "../../../.."));
				// It is officially 12:15 AM on Christmas. I'm tired.
				// Should I run over this liberal, or drive around them?
				// What do you think?
		}
		
		
		// Signal Connection
		_usrdirdialog.DirSelected += OnUsrdirDialog;
		_install.Pressed += OnInstallPressed;
		_restoreusrdir.Pressed += OnRestoreUsrdirPressed;
		_modsfolder.Pressed += OpenModsFolder;
		
		_progress.Text = "Signals connected.";
		
		// Generate default config
		if(!File.Exists("HoneyConfig.ini")){
			File.Copy("HoneyConfig.default.ini", "HoneyConfig.ini");
			_progress.Text = "Created default configuration file.";
		}
		
		// Create mods folder
		if(!Directory.Exists("mods")){
			Directory.CreateDirectory("mods");
			_progress.Text = "Created mods directory.";
		}
		
		// https://github.com/rickyah/ini-parser
		// MIT License
		IniData data = new FileIniDataParser().ReadFile("HoneyConfig.ini");
		usrdir = data["main"]["usrdir"];
		_progress.Text = "loaded HoneyConfig.ini.";
		
		// Define psarc path for each OS
		switch(OS.GetName()){
			case "Windows":
			  psarc = Path.GetFullPath(Path.Combine("bin", "win32", "UnPSARC.exe"));
			  //GD.Print(psarc);
			  break;
			case "macOS":
			  psarc = Path.GetFullPath(Path.Combine("bin", "macosx", "psarc"));
			  //GD.Print(psarc);
			  break;
			case "Linux":
			  psarc = Path.GetFullPath(Path.Combine("bin", "linux", "psarc"));
			  //GD.Print(psarc);
			  break;
			default:
			  ShowError("Error", "This platform is unsupported.");
			  break;
		}
	}

	// Signals
	private void OnUsrdirDialog(string dir){
		usrdir = Path.GetFullPath(dir); // Set usrdir for current session
		IniData data = new FileIniDataParser().ReadFile("HoneyConfig.ini"); // Open config file
		data["main"]["usrdir"] = dir; // Set usrdir
		new FileIniDataParser().WriteFile("HoneyConfig.ini", data); // Write config file
		_progress.Text = "Saved changes.";
	}
	
	// Install mods
	private void OnInstallPressed(){
		// Check for valid stf
		string psarc_path = Path.Combine(usrdir, "rom.psarc");
		if (!File.Exists(psarc_path)){
			ShowError("Error", "rom.psarc could not be found. Please ensure you have a clean copy of Sonic the Fighters if this\nis your first time, or Uninstall mods before proceeding.");
			_progress.Text = "rom.psarc not found.";
			return;
		}
		
		// Make backup if valid stf found and no backup exists
		if(!Directory.Exists("BACKUP")){
			Directory.CreateDirectory("BACKUP");
			_progress.Text = "Created backup directory.";
		}
		CopyFilesRecursively(usrdir, "BACKUP");
		_progress.Text = "Created backup.";
		
		// Extraction on Windows
		if (OS.GetName() == "Windows"){
			string psarc_path_windows_fuck_stupid = "\"" + psarc_path + "\"";
			using Process process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = psarc,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					RedirectStandardInput = true,
					Arguments = psarc_path_windows_fuck_stupid,
					CreateNoWindow = true,
					WorkingDirectory = usrdir,
				}
			};
			
			process.Start();
			process.WaitForExit();
			string output = process.StandardOutput.ReadToEnd();
			string error = process.StandardError.ReadToEnd();
			GD.Print(output);
			GD.Print(error);
			string unpacked_dir = Path.Combine(usrdir, "rom_Unpacked");
			string romdir = Path.Combine(usrdir, "rom");
			CopyFilesRecursively(unpacked_dir, romdir);
			Directory.Delete(unpacked_dir, true);
		}
		// Extraction on *nix
		else {
			using Process process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = psarc,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					RedirectStandardInput = true,
					CreateNoWindow = true,
					WorkingDirectory = usrdir,
				}
			};
			// Using ArgumentList.Add() instead of setting Arguments directly was necessary
			// to get this working on macOS. It seems to work on Linux just fine, and
			// if somehow a BSD port ever becomes a thing this will probably help there too,
			// if I had to guess.
			process.StartInfo.ArgumentList.Add("-x");
			process.StartInfo.ArgumentList.Add(psarc_path);
			process.Start();
			process.WaitForExit();
			//string output = process.StandardOutput.ReadToEnd();
			//string error = process.StandardError.ReadToEnd();
			//GD.Print(output);
			//GD.Print(error);
		}
		_progress.Text = "Extracted rom.psarc.";
		File.Delete(psarc_path);
		_progress.Text = "Extracted and removed rom.psarc.";
		FarcUnpack();
		_progress.Text = "Unpacked farc files.";
		ExtractMods();
		_progress.Text = "Extracted mods.";
		FarcPack();
		_progress.Text = "Repacked farc files.";
		ShowError("Success!", "Mods have been installed!");
	}
	
	// Uninstall Mods
	private void OnRestoreUsrdirPressed(){
		// Check if backup exists
		if (!Directory.Exists("BACKUP")){
			ShowError("Error", "No backup found.");
			_progress.Text = "No backup found.";
			return;
		}
		
		// Clear contents of USRDIR
		try{
			// https://stackoverflow.com/questions/1288718/how-to-delete-all-files-and-folders-in-a-directory
			System.IO.DirectoryInfo di = new DirectoryInfo(usrdir);
			foreach (FileInfo file in di.GetFiles()){
				file.Delete(); 
			}
			foreach (DirectoryInfo dir in di.GetDirectories()){
				dir.Delete(true); 
			}
		}
		catch (Exception e){
			ShowError("Exception", e.ToString());
		}
		
		// Restore backup
		try{
			CopyFilesRecursively("BACKUP", usrdir);
		}
		catch (Exception e){
			ShowError("Exception", e.ToString());
		}
		ShowError("Success", "Files restored.");
		_progress.Text = "Restored game files.";
	}

	private void OpenModsFolder(){
		OS.ShellShowInFileManager("mods", true);
	}
	public override void _Process(double delta){
		// GD.Print(usrdir);
	}

	public void ShowError(string title, string text){
		_acceptdialog.Title = title;
		_acceptdialog.DialogText = text;
		_acceptdialog.Show();
	}	
	
	// https://stackoverflow.com/questions/58744/copy-the-entire-contents-of-a-directory-in-c-sharp
	private static void CopyFilesRecursively(string sourcePath, string targetPath){
		//Now Create all of the directories
		foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories)){
			Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
		}
		//Copy all the files & Replaces any files with the same name
		foreach (string newPath in Directory.GetFiles(sourcePath, "*.*",SearchOption.AllDirectories)){
			File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
		}
	}
	
	/* The code for the next two functions was almost entirely ripped from
	Skyth's FarcPack utility, and uses MikuMikuLibrary to unpack/repack game 
	files. It is licensed under the MIT license. Please show him your support,
	https://github.com/blueskythlikesclouds/MikuMikuLibrary */
	
	private void FarcUnpack(){
		string[] farcs =   {"sprite/n_advstf.farc", 
							"sprite/n_cmn.farc", 
							"sprite/n_cmn.farc", 
							"sprite/n_fnt.farc", 
							"sprite/n_info.farc", 
							"sprite/n_stf.farc", 
							"string_array.farc", 
							"sprite/n_advstf/texture.farc", 
							"sprite/n_cmn/texture.farc", 
							"sprite/n_fnt/texture.farc", 
							"sprite/n_info/texture.farc", 
							"sprite/n_stf/texture.farc"};
	
		foreach (string farc in farcs )
		{
			// Set source and destination filename
			string sourceFileName = Path.Combine(usrdir, "rom", farc);
			string destinationFileName = Path.ChangeExtension(sourceFileName, null);

			// Default arguments, don't change, these work with STF
			bool compress = false;
			int alignment = 16;
					
			using (var stream = File.OpenRead(sourceFileName))
			{
				var farcArchive = BinaryFile.Load<FarcArchive>(stream);
				
				Directory.CreateDirectory(destinationFileName);
				
				foreach (string fileName in farcArchive)
				{
					using (var destination = File.Create(Path.Combine(destinationFileName, fileName)))
					using (var source = farcArchive.Open(fileName, EntryStreamMode.OriginalStream))
					source.CopyTo(destination);
				}
			}
		}
	}
	
	private void FarcPack(){
		string[] dirlist = {"sprite/n_advstf/texture", 
							"sprite/n_cmn/texture", 
							"sprite/n_cmn/texture", 
							"sprite/n_fnt/texture", 
							"sprite/n_info/texture", 
							"sprite/n_stf/texture", 
							"string_array", 
							"sprite/n_advstf", 
							"sprite/n_cmn", 
							"sprite/n_fnt", 
							"sprite/n_info", 
							"sprite/n_stf"};
	
		foreach (string dir in dirlist)
		{
			// Set source and destination file name
			string sourceFileName = Path.GetFullPath(Path.Combine(usrdir, "rom", dir));
			string destinationFileName = Path.ChangeExtension(sourceFileName, "farc");;

			// These arguments should never change, as they seem to work fine with STF
			bool compress = false;
			int alignment = 16;
			
			// Modified by me, otherwise it throws access errors if you don't use "using"
			using (var farcArchive = new FarcArchive { IsCompressed = compress, Alignment = alignment }){
			
			if (File.GetAttributes(sourceFileName).HasFlag(FileAttributes.Directory))
			{
				foreach (string filePath in Directory.EnumerateFiles(sourceFileName))
				farcArchive.Add(Path.GetFileName(filePath), filePath);
			}
			else
			{
				farcArchive.Add(Path.GetFileName(sourceFileName), sourceFileName);
			}
			farcArchive.Save(destinationFileName);
			}
		}
	}
	
	private void ExtractMods(){
		string[] files = Directory.GetFiles("mods");
		if (files.Length == 0){
			ShowError("Error", "You don't have any mods!");
			return;
		}
		foreach (string mod in files)
		{
			string modpath = mod;
			string romdir = Path.Combine(usrdir, "rom");
			string stf_rom = Path.Combine(romdir, "stf_rom");
			if (Path.GetExtension(modpath) == ".zip")
				ZipFile.ExtractToDirectory(modpath, romdir, true);
			else
			{
				string patchdest;
				GD.Print(modpath);
				switch(Path.GetExtension(modpath))
				{
					case ".rom_code1":
						patchdest = Path.Combine(stf_rom, "rom_code1.bin");
						break;
					case ".rom_data":
						patchdest = Path.Combine(stf_rom, "rom_data.bin");
						break;
					case ".rom_ep":
						patchdest = Path.Combine(stf_rom, "rom_ep.bin");
						break;
					case ".rom_pol":
						patchdest = Path.Combine(stf_rom, "rom_pol.bin");
						break;
					case ".rom_tex":
						patchdest = Path.Combine(stf_rom, "rom_tex.bin");
						break;
					case ".string_array_en":
						patchdest = Path.Combine(romdir, "string_array", "string_array_en.bin");
						break;
					case ".string_array_jp":
						patchdest = Path.Combine(romdir, "string_array", "string_array_jp.bin");
						break;
					default:
						return;
				}
				
				try{GD.Print(patchdest + modpath);
				using var input = new FileStream(patchdest, FileMode.Open, System.IO.FileAccess.ReadWrite, FileShare.None);
				using var patch = new FileStream(modpath, FileMode.Open);
				using var decoder = new Decoder(input, patch, input);
				decoder.Run();}
				catch(Exception e){GD.Print(e.ToString());}
			}
		}
	}
}
