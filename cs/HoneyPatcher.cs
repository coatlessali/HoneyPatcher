using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using SonicAudioLib;
using SonicAudioLib.CriMw;
using SonicAudioLib.IO;
using SonicAudioLib.Archives;
using MikuMikuLibrary.Databases;
using MikuMikuLibrary.Archives;
using MikuMikuLibrary.IO;
using Newtonsoft.Json;
using DatabaseConverter;
using AcbEditor;
using LibSTF;
using IniParser;
using IniParser.Model;
using UnPSARC;

public partial class HoneyPatcher : Node2D
{	
	[Export] public AcceptDialog _acceptdialog; // Errors and whatnot
	[Export] public FileDialog _usrdirdialog; // Restore USRDIR button
	[Export] public Button _selectusrdir; 
	[Export] public Button _install; // Restore USRDIR button
	[Export] public Button _restoreusrdir; // Restore USRDIR button
	[Export] public Button _modsfolder; // Opens mods folder, doesn't currently work on my setup for some reason
	[Export] public Button _genpatches; // Generate Patches button
	[Export] public RichTextLabel _progress; // Progress label
	[Export] public Label _game; // game label
	[Export] public LineEdit _patchname; // Name of patch
	[Export] public Button _patchesfolder; // Opens patches folder
	[Export] public PopupMenu _gameselector; // Selects a game
	[Export] public AudioStreamPlayer _confirm;
	[Export] public AudioStreamPlayer _back;
	[Export] public AudioStreamPlayer _select;
	[Export] public AudioStreamPlayer _stfa;
	[Export] public AudioStreamPlayer _fva;
	[Export] public AudioStreamPlayer _vf2a;
	[Export] public AudioStreamPlayer _omga;
	[Export] public CheckButton _logoskip;
	[Export] public CheckButton _cleanup;
	[Export] public MenuBar _gamebutton;
	
	// This is an absolute war crime and I'm open to suggestions for fixing this garbage
	byte[] ddscomp = {0x07, 0x10, 0x00, 0x00};
	byte[] d5comp = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
							0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
							0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
							0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 
							0x44, 0x58, 0x54, 0x35, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
							0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
							0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
	byte[] nocomp = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
							0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
							0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
							0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x41, 0x00, 0x00, 0x00, 
							0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0xFF, 0x00, 0x00, 
							0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x02, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
							0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
	
	// Static directories
	string modsDir = ProjectSettings.GlobalizePath("user://mods");
	string workbenchDir = ProjectSettings.GlobalizePath("user://workbench");
	string backupDir = ProjectSettings.GlobalizePath("user://BACKUP");
	string honeyConfig = ProjectSettings.GlobalizePath("user://HoneyConfig.ini");
	string honeyLog = ProjectSettings.GlobalizePath("user://HoneyLog.txt");
	string elf = ProjectSettings.GlobalizePath("user://EBOOT.bin"); // modified bin
	
	// model 2 game file lists for patch creation
	string[] stf_roms = {"rom_code1.bin", "rom_data.bin", "rom_ep.bin", "rom_pol.bin", "rom_tex.bin", "string_array_en.bin", "string_array_jp.bin"};
	string[] fv_roms = {"rom_code1.bin", "rom_code2.bin", "rom_data.bin", "rom_ep1.bin", "rom_ep2.bin", "rom_pol.bin", "rom_tex.bin", "string_array_en.bin", "string_array_jp.bin"};
	string[] vf2_roms = {"ic12_13.bin", "ic12_15.bin", "rom_data.bin", "rom_pol.bin", "rom_tex.bin", "string_array_en.bin", "string_array_jp.bin"};
	string[] omg_roms = {"farc_tex.bin", "rom_code.bin", "rom_data.bin", "rom_pol.bin", "rom_tex.bin", "string_array_en.bin", "string_array_jp.bin", "string_array2_en.bin", "string_array2_jp.bin"};
	string[] gamesList = {"stf", "vf2", "fv", "omg"};
	
	string usrdir = ".";
	string patchname = "Default";
	string game = "stf";
	string pretty_game = "Sonic the Fighters";
	string log;
	string modsStr;
	
	byte loglevel = 2;
	
	bool nomods = false;
	bool logoskip = false;
	bool cleanup = true;
	bool gemsSfx = false;
	
	public override void _Ready(){	
		
		// Signal Connection
		_usrdirdialog.DirSelected += OnUsrdirDialog;
		_install.Pressed += OnInstallPressed;
		_restoreusrdir.Pressed += OnRestoreUsrdirPressed;
		_modsfolder.Pressed += OpenModsFolder;
		_genpatches.Pressed += CreatePatches;
		_patchesfolder.Pressed += OpenPatchesFolder;
		_gameselector.IdPressed += GameSelector;
		_logoskip.Toggled += ToggleLogoskip;
		_cleanup.Toggled += ToggleCleanup;
		
		/* Enable portable mode because people kept asking for it. */
		if (File.Exists("portable.txt")){
			HoneyLog(3, "Found portable.txt. Enabling portable mode.");
			modsDir = "mods";
			workbenchDir = "workbench";
			backupDir = "BACKUP";
			honeyConfig = "HoneyConfig.ini";
			honeyLog = "HoneyLog.txt";
			elf = "EBOOT.bin";
		}
		
		LoadConfig();
		File.Create(honeyLog).Close();
		
		string[] essentialDirs = {modsDir, workbenchDir, Path.Combine(workbenchDir, "original"), Path.Combine(workbenchDir, "modified"), Path.Combine(workbenchDir, "patches")};
		foreach (string h in essentialDirs){
			try{
				Directory.CreateDirectory(h);
				HoneyLog(4, $"Created directory {h}.");
			}
			catch (Exception e){
				HoneyLog(1, $"Failed to create directory {h}");
				HoneyLog(1, e.ToString(), true);
			}
		}
		foreach (string h in gamesList){
			Directory.CreateDirectory(Path.Combine(modsDir, h));
			HoneyLog(4, $"Created directory {Path.Combine(modsDir, h)}.");
			string[] thing = {"original", "modified", "patches"};
			foreach (string thingy in thing){
				Directory.CreateDirectory(Path.Combine(workbenchDir, thingy, h));
				HoneyLog(4, $"Created directory {Path.Combine(workbenchDir, thingy, h)}.");
			}
		}
		
		/* Easter egg. */
		if (gemsSfx){
			_confirm.Stream = (AudioStream)GD.Load(ProjectSettings.GlobalizePath("res://assets/sounds/gems_confirm.ogg"));
			_back.Stream = (AudioStream)GD.Load(ProjectSettings.GlobalizePath("res://assets/sounds/gems_back.ogg"));
			_select.Stream = (AudioStream)GD.Load(ProjectSettings.GlobalizePath("res://assets/sounds/gems_select.ogg"));
		}
		
		/* Migration code has been removed from this spot. Please just use V7 if you need this. */
	}

	private void OnUsrdirDialog(string dir){
		usrdir = Path.GetFullPath(dir); // Set usrdir for current session
		IniData data = new FileIniDataParser().ReadFile(honeyConfig); // Open config file
		data["main"][$"{game}usrdir"] = dir; // Set usrdir
		new FileIniDataParser().WriteFile(honeyConfig, data); // Write config file
		HoneyLog(3, "Saved changes.");
	}
	
	private void UpdateGame(){
		switch (game){
			case "stf": pretty_game = "Sonic the Fighters"; break;
			case "vf2": pretty_game = "Virtua Fighter 2"; break;
			case "fv": pretty_game = "Fighting Vipers"; break;
			case "omg": pretty_game = "Cyber Troopers Virtual-On: Operation Moongate"; break;
		}
	}
	
	private void GameSelector(long id){
		IniData data = new FileIniDataParser().ReadFile(honeyConfig); // Open config file
		switch (id){
			case 0: game = "stf"; break;
			case 1: game = "vf2"; break;
			case 2: game = "fv"; break;
			case 3: game = "omg"; break;
		}
		UpdateGame();
		data["main"]["game"] = game;
		new FileIniDataParser().WriteFile(honeyConfig, data); // Write config file
		usrdir = data["main"][$"{game}usrdir"];
		HoneyLog(3, $"Changed game: {pretty_game}.");
	}
	
	private void EnableButtons(){
		_install.Disabled = false;
		_restoreusrdir.Disabled = false;
		_genpatches.Disabled = false;
		_gamebutton.Visible = true;
		_logoskip.Disabled = false;
		_selectusrdir.Disabled = false;
	}
	
	private void DisableButtons(){
		_install.Disabled = true;
		_restoreusrdir.Disabled = true;
		_genpatches.Disabled = true;
		_gamebutton.Visible = false;
		_logoskip.Disabled = true;
		_selectusrdir.Disabled = true;
	}
	
	private async void OnInstallPressed(){
		// disable buttons
		DisableButtons();
		HoneyLog(4, "Disabled Menu buttons.");
		/* Check if usrdir is set. */
		if (usrdir == "."){
			_back.Play();
			ShowError("Error", "USRDIR is unset. Please select a USRDIR.");
			HoneyLog(1, "usrdir is unset.");
			EnableButtons();
			HoneyLog(4, "Enabled Menu buttons.");
			return;
		}
		/* Check for clean copy of game w/ rom.psarc still intact */
		string psarc_path = Path.Combine(usrdir, "rom.psarc");
		if (!File.Exists(psarc_path)){
			_back.Play();
			ShowError("Error", $"rom.psarc could not be found. Please ensure you have a clean copy of {pretty_game} if this\nis your first time, or Uninstall mods before proceeding.");
			HoneyLog(1, $"rom.psarc not found at {psarc_path}.");
			EnableButtons();
			HoneyLog(4, "Enabled Menu buttons.");
			return;
		}
		
		try{
			// Runs the below function
			await Task.Run(() => { InstallAsync(); });
			string[] success = { "Success!", "Mods have been installed!" };
			string[] success2 = { "Success?", "No mods were found, but I extracted rom.psarc and unpacked your game files for you anyways." };
			if (nomods){
				success = success2;
			}
			GameSound();
			ShowError(success[0], success[1]);
		}
		catch (Exception e){
			HoneyLog(1, $"Failed to install mods. See HoneyLog.txt for more information.");
			HoneyLog(1, e.ToString(), true);
		}
		finally
		{
			EnableButtons();
			HoneyLog(4, $"Enabled Menu Buttons.");
		}
	}
	
	/* Install mods */
	private void InstallAsync(){
		/* Make backup if valid stf found and no backup exists */
		string psarc_path = Path.Combine(usrdir, "rom.psarc");
		string gameBackupDir = Path.Combine(backupDir, game);
		try{
			Directory.CreateDirectory(gameBackupDir);
			CopyFilesRecursively(usrdir, gameBackupDir);
			HoneyLog(3, "Created backup.");
		}
		catch (Exception e){
			HoneyLog(1, "There was an issue creating a backup. Check HoneyLog.txt for more details.");
			HoneyLog(1, e.ToString(), true);
		}
		
		/* This gets AcbEditor working. */
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		
		/* Extract rom.psarc - used UnPSARC by NoobInCoding as a base, stripped it down,
		   and turned it into a DLL. It's honestly still really bloated and could do with
		   a bit more cleanup. */
		try{
			PsarcThing.UnpackArchiveFile(psarc_path, Path.Combine(usrdir, "rom"));
			HoneyLog(3, "Extracted rom.psarc");
		}
		catch (Exception e){
			HoneyLog(1, "There was an issue extracting rom.psarc. Check HoneyLog.txt for more details.");
			HoneyLog(1, e.ToString(), true);
		}
		try{
			File.Delete(psarc_path);
			HoneyLog(4, "Removed rom.psarc.");
		}
		catch (Exception e){
			HoneyLog(1, "There was an issue removing rom.psarc. Check HoneyLog.txt for more details.");
			HoneyLog(1, e.ToString(), true);
		}
		FarcUnpack();
		UnpackAcb();
		DbToXml();
		ExtractMods();
		ApplyPatches();
		InjectModels(); // LibSTF by Bekzii
		DDSFixHeader();
		InjectModsStr();
		XmlToDb();
		FarcPack();
		PackAcb();
		CleanUp();
		LogoSkip();
		
		
	}
	
	private async void OnRestoreUsrdirPressed(){
		/* Disable buttons */
		DisableButtons();
		HoneyLog(4, "Disabled Menu Buttons.");
		/* Check if backup exists */
		if (!Directory.Exists(Path.Combine(backupDir, game))){
			_back.Play();
			ShowError("Error", "No backup found.");
			HoneyLog(1, "No backup found.");
			EnableButtons();
			HoneyLog(4, "Enabled Menu buttons.");
			return;
		}
		try{
			// Runs the below function
			await Task.Run(() => { RestoreAsync(); });
			GameSound();
			ShowError("Success", "Files restored.");
			HoneyLog(3, "Restored game files.");
		}
		catch (Exception e){
			HoneyLog(1, $"Failed to restore game files. See HoneyLog.txt for more information.");
			HoneyLog(1, e.ToString(), true);
		}
		finally
		{
			EnableButtons();
			HoneyLog(4, $"Enabled Menu Buttons.");
		}
	}
	
	/* Uninstall Mods */
	private void RestoreAsync(){
		/* Clear contents of USRDIR */
		try{
			Directory.Delete(usrdir, true);
			HoneyLog(4, $"Deleted {usrdir}.");
			Directory.CreateDirectory(usrdir);
			HoneyLog(4, $"Created {usrdir}.");
			HoneyLog(3, "Wiped game files.");
		}
		catch (Exception e){
			_back.Play();
			HoneyLog(1, "Error restoring files. See HoneyLog.txt for more details.");
			HoneyLog(1, e.ToString(), true);;
			return;
		}
		
		/* Restore backup */
		try{
			CopyFilesRecursively(Path.Combine(backupDir, game), usrdir);
			HoneyLog(3, "Restored game files.");
		}
		catch (Exception e){
			_back.Play();
			HoneyLog(1, "Error restoring files. See HoneyLog.txt for more details.");
			HoneyLog(1, e.ToString(), true);
			return;
		}
	}

	private void OpenModsFolder(){
		OS.ShellOpen(Path.Combine(modsDir, game));
	}
	
	private void OpenPatchesFolder(){
		OS.ShellOpen(workbenchDir);
	}
	
	private void CreatePatches(){
		if (_patchname.Text != "")
		  patchname = _patchname.Text;
		List<string> files = new List<string>();
		List<string> roms = new List<string>();
		switch (game){
			case "stf": roms = stf_roms.ToList(); break;
			case "vf2": roms = vf2_roms.ToList(); break;
			case "fv": roms = fv_roms.ToList(); break;
			case "omg": roms = omg_roms.ToList(); break;
		}
		
		foreach (string rawhm in roms){
			if (!File.Exists(Path.Combine(workbenchDir, "original", game, rawhm))){
				HoneyLog(1, $"Couldn't find {rawhm}.");
				_back.Play();
				ShowError("Error", "Original " + rawhm + "not found.");
				return;
			}
			else{
				files.Add(rawhm);
			}
		}
		foreach (string filename in files){
			if (!File.Exists(Path.Combine(workbenchDir, "modified", game, filename))){
				HoneyLog(2, $"Did not find modified {filename}. If this is intentional/no changes were made, please ignore this message.");
				continue;
			}
			uint count = 0;
			List<string> locations = new List<string>();
			List<byte> changes = new List<byte>();
			string patchextension = Path.GetFileNameWithoutExtension(Path.Combine(workbenchDir, "original", game, filename));
			byte[] original = File.ReadAllBytes(Path.Combine(workbenchDir, "original", game, filename));
			byte[] modified = File.ReadAllBytes(Path.Combine(workbenchDir, "modified", game, filename));
			foreach (byte b in original){
				if (b != modified[count]){
					locations.Add(count.ToString());
					changes.Add(modified[count]);
				}
				count++;
			}
			string patch = Path.Combine(workbenchDir, "patches", game, patchname + "." + patchextension);
			string patchloc = patch + ".loc";
			File.WriteAllBytes(patch, changes.ToArray());
			HoneyLog(4, $"Created {patch}");
			File.WriteAllLines(patchloc, locations.ToArray());
			HoneyLog(4, $"Created {patchloc}");
		}
		HoneyLog(3, $"Created patches.");
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
	files. It is licensed under the MIT license. Please show him your support.
	https://github.com/blueskythlikesclouds/MikuMikuLibrary */
	
	private void FarcUnpack(){
		string romdir = Path.Combine(usrdir, "rom");
		List<string> unpacked = new List<string>();
		
		for (int i = 0; i < 2; i++){
			/* get all farc files, we need to do this twice hence the for loop */
			string[] farcs = Directory.GetFiles(romdir, "*.farc", SearchOption.AllDirectories);
			HoneyLog(4, farcs.Length.ToString());
			foreach (string farc in farcs ){
				if (unpacked.Contains(farc)){
					HoneyLog(4, $"{farc} already unpacked.");
					continue;
				}
				unpacked.Add(farc);
				string sourceFileName = farc;
				try{
					/* Set source and destination filename */
					string destinationFileName = Path.ChangeExtension(sourceFileName, null);
				
					using (var stream = File.OpenRead(sourceFileName)){
						var farcArchive = BinaryFile.Load<FarcArchive>(stream);
					
						Directory.CreateDirectory(destinationFileName);
					
						foreach (string fileName in farcArchive){
							using (var destination = File.Create(Path.Combine(destinationFileName, fileName)))
							using (var source = farcArchive.Open(fileName, EntryStreamMode.OriginalStream))
							source.CopyTo(destination);
						}
					}
					HoneyLog(4, $"Unpacked {sourceFileName}.");
				}
				catch (Exception e){
					HoneyLog(1, $"{sourceFileName} could not be unpacked.");
					HoneyLog(1, e.ToString(), true);
				}
			}
		}
		HoneyLog(3, "Unpacked farc files.");
	}
	
	private void FarcPack(){
		string romdir = Path.Combine(usrdir, "rom");
		string[] farcs = Directory.GetFiles(romdir, "*.farc", SearchOption.AllDirectories);
		Array.Reverse(farcs);
	
		foreach (string farc in farcs)
		{
			string sourceFileName = farc.Replace(".farc", String.Empty);
			try{
				/* Set source and destination file name */
				string destinationFileName = Path.ChangeExtension(sourceFileName, "farc");;
				
				/* Modified by me, otherwise it throws access errors if you don't use "using" */
				using (var farcArchive = new FarcArchive { IsCompressed = false, Alignment = 16 }){
				
					if (File.GetAttributes(sourceFileName).HasFlag(FileAttributes.Directory)){
						foreach (string filePath in Directory.EnumerateFiles(sourceFileName))
						farcArchive.Add(Path.GetFileName(filePath), filePath);
					}
					else
						farcArchive.Add(Path.GetFileName(sourceFileName), sourceFileName);
					farcArchive.Save(destinationFileName);
				}
				/* Cleanup */
				try{
					if (cleanup){
						Directory.Delete(sourceFileName, true);
						HoneyLog(4, $"{sourceFileName} deleted.");
					}
				}
				catch (Exception e)
				{
					HoneyLog(1, $"Failed to delete {sourceFileName}. Check HoneyLog.txt for more details.");
					HoneyLog(1, e.ToString(), true);
				}
			}
			catch (Exception e){
				if (!Directory.Exists(sourceFileName))
					HoneyLog(4, $"{sourceFileName} does not exist. Skipping.");
				else{
					HoneyLog(1, $"{sourceFileName} could not be repacked. See HoneyLog.txt for more details.");
					HoneyLog(1, e.ToString(), true);
				}
			}
		}
		HoneyLog(3, "Repacked farc files.");
	}
	
	private void ExtractMods(){
		modsStr = "Mods installed:\n";
		string[] files = Directory.GetFiles(Path.Combine(modsDir, game));
		Array.Sort(files);
		switch (files.Length){
			case 0: nomods = true; return;
			default: nomods = false; break;
		}
		
		foreach (string mod in files){
			string modpath = mod;
			string romdir = Path.Combine(usrdir, "rom");
			string stf_rom = Path.Combine(romdir, $"{game}_rom");
			try{
				if (Path.GetExtension(modpath) == ".zip"){
					using (ZipArchive archive = ZipFile.Open(modpath, ZipArchiveMode.Update)){
						try{
							ZipArchiveEntry entry = archive.GetEntry("name.txt");
							using (var reader = new StreamReader(entry.Open())){
								string contents = reader.ReadToEnd();
								modsStr += contents;
							}
						}
						catch{
							modsStr += $"{Path.GetFileNameWithoutExtension(modpath)}\n";
						}
					}
					ZipFile.ExtractToDirectory(modpath, romdir, true);
					HoneyLog(4, $"Extracted {modpath}.");
				}
			}
			catch (Exception e){
				HoneyLog(1, "There was an issue extracting your mods. Check HoneyLog.txt for more information.");
				HoneyLog(1, e.ToString(), true);
			}
		}
		HoneyLog(3, "Extracted mods.");
	}
	
	private void ApplyPatches(){
		/* Get list of files in rom folder */
		string[] files = Directory.GetFiles(Path.Combine(usrdir, "rom"));
		/* Apply in alphabetical order */
		Array.Sort(files);
		foreach (string mod in files){
			string modpath = mod; // patch
			string romdir = Path.Combine(usrdir, "rom");
			string stf_rom = Path.Combine(romdir, $"{game}_rom");
			string patchdest; // file to be patched
			switch(Path.GetExtension(modpath)){
				/* Check the file extension, which should be the name of the file you want to patch */
				case ".rom_code": patchdest = Path.Combine(stf_rom, "rom_code.bin"); break;
				case ".rom_code1": patchdest = Path.Combine(stf_rom, "rom_code1.bin"); break;
				case ".rom_code2": patchdest = Path.Combine(stf_rom, "rom_code2.bin"); break;
				case ".rom_cop": patchdest = Path.Combine(stf_rom, "rom_cop.bin"); break;
				case ".rom_data": patchdest = Path.Combine(stf_rom, "rom_data.bin"); break;
				case ".rom_ep": patchdest = Path.Combine(stf_rom, "rom_ep.bin"); break;
				case ".rom_ep1": patchdest = Path.Combine(stf_rom, "rom_ep1.bin"); break;
				case ".rom_ep2": patchdest = Path.Combine(stf_rom, "rom_ep1.bin"); break;
				case ".rom_pol": patchdest = Path.Combine(stf_rom, "rom_pol.bin"); break;
				case ".rom_tex": patchdest = Path.Combine(stf_rom, "rom_tex.bin"); break;
				case ".ic12_13": patchdest = Path.Combine(stf_rom, "ic12_13.bin"); break;
				case ".ic12_15": patchdest = Path.Combine(stf_rom, "ic12_15.bin"); break;
				
				// BREAKING: no longer byte patching this bc it makes no sense
				case ".string_array_en": patchdest = Path.Combine(romdir, "string_array", "string_array_en.bin"); break;
				case ".string_array2_en": patchdest = Path.Combine(romdir, "string_array", "string_array2_en.bin"); break;
				case ".string_array_jp": patchdest = Path.Combine(romdir, "string_array", "string_array_jp.bin"); break;
				case ".string_array2_jp": patchdest = Path.Combine(romdir, "string_array", "string_array2_jp.bin"); break;
				default: continue;
			}
			byte[] original = File.ReadAllBytes(patchdest);
			byte[] changes = File.ReadAllBytes(modpath);
			string[] locations = File.ReadAllLines(modpath+".loc");
			uint inc = 0;
			try
			{
				using (FileStream fs = File.Open(patchdest, FileMode.Open, System.IO.FileAccess.ReadWrite, FileShare.ReadWrite)){
					foreach (string i in locations){
						long loc = Int64.Parse(i);
						fs.Seek(loc, SeekOrigin.Begin);
						fs.WriteByte(changes[inc]);
						inc++;
					}
				}
			}
			catch (Exception e)
			{
				HoneyLog(1, "Failed to apply patches to one or more files. Check HoneyLog.txt for more information.");
				HoneyLog(1, e.ToString(), true);
			}
		}
		HoneyLog(3, "Applied patches.");
	}
	
	private void InjectModels(){
		if (game != "stf"){
			HoneyLog(3, "Game is not Sonic the Fighters. Skipping model injection.");
			return;
		}
		string[] files = Directory.GetFiles(Path.Combine(usrdir, "rom"));
		Array.Sort(files);
		foreach (string model in files){
			if (Path.GetExtension(model) != ".stfmdl")
				continue;
			string fileName = Path.GetFileName(model);
			/* get the first 4 digits of the filename to be the id */
			fileName = fileName.Substring(0, 4);
			int modelId;
			/* attempt parsing */
			if (!Int32.TryParse(fileName, out modelId)){
				HoneyLog(2, $"Filename of {model} is invalid - skipping.");
				continue;
			}
			/* remove extension */
			string modelName = Path.GetFileNameWithoutExtension(model);
			/* read filepath */
			modelName = Path.Combine(usrdir, "rom", modelName);
			ModelInject.AddModel(modelId, modelName);
		}
		try{
			ModelInject.Verbose = true;
			ModelInject.InjectModels(Path.Combine(usrdir, "rom", "stf_rom"));
			HoneyLog(3, "Injected models for Sonic the Fighters.");
		}
		catch (Exception e){
			HoneyLog(1, "There was an error injecting models. See HoneyLog.txt for more details.");
			HoneyLog(1, e.ToString(), true);
		}
	}
	
	private void UnpackAcb(){
		/* AcbEditor by Skyth - did you know the upstream build literally can't run without a console? */
		string[] AcbFile = {Path.Combine(usrdir, "rom", "sound", $"{game}_all.acb")};
		try{
			AcbEditorThing.AcbEdit(AcbFile);
			HoneyLog(3, "Unpacked ACB file.");
		}
		catch (Exception e){
			HoneyLog(1, $"There was an issue extracting {game}_all.acb. Check HoneyLog.txt for more details.");
			HoneyLog(1, e.ToString(), true);
		}
	}

	private void PackAcb(){
		string[] AcbFolder = {Path.Combine(usrdir, "rom", "sound", $"{game}_all")};
		try{
			AcbEditorThing.AcbEdit(AcbFolder);
			HoneyLog(3, "Repacked ACB file.");
		}
		catch (Exception e){
			HoneyLog(1, $"There was an issue repacking {game}_all.acb. Check HoneyLog.txt for more details.");
			HoneyLog(1, e.ToString(), true);
		}
		/* Cleanup */
		try{
			if (cleanup){
				Directory.Delete(AcbFolder[0], true);
				HoneyLog(4, "Cleaned up ACB Folder");
			}
		}
		catch (Exception e){
			HoneyLog(1, $"There was an issue deleting {game}_all.acb. Check HoneyLog.txt for more details.");
			HoneyLog(1, e.ToString(), true);
		}
	}
	
	private void DbToXml(){
		/* DatabaseConverter by Skyth - did you know the upstream build will fail due to invalid xml characters? */
		string stringArrayDir = Path.Combine(usrdir, "rom", "string_array");
		string[] stringArrays = {Path.Combine(stringArrayDir, "string_array_en.bin"), Path.Combine(stringArrayDir, "string_array2_en.bin"), Path.Combine(stringArrayDir, "string_array_jp.bin"), Path.Combine(stringArrayDir, "string_array2_jp.bin")};
		string[] dbFile = new string[1];
		try{
			foreach (string stringArray in stringArrays){
				if (!File.Exists(stringArray)){
					HoneyLog(4, $"{stringArray} not found. Skipping.");
					continue;
				}
				dbFile[0] = stringArray;
				DBConverter.Convert(dbFile);
				HoneyLog(4, $"Converted {stringArray} to XML.");
			}
			HoneyLog(3, "Converted string DBs to XML.");
		}
		catch (Exception e){
			HoneyLog(1, "There was a problem converting one of your string arrays to XML. Check HoneyLog.txt for more information.");
			HoneyLog(1, e.ToString(), true);
		}
	}
	
	private void XmlToDb(){
		string stringArrayDir = Path.Combine(usrdir, "rom", "string_array");
		string[] stringArrays = {Path.Combine(stringArrayDir, "string_array_en.xml"), Path.Combine(stringArrayDir, "string_array2_en.xml"), Path.Combine(stringArrayDir, "string_array_jp.xml"), Path.Combine(stringArrayDir, "string_array2_jp.xml")};
		string[] dbFile = new string[1];
		try{
			foreach (string stringArray in stringArrays){
				if (!File.Exists(stringArray)){
					HoneyLog(4, $"{stringArray} not found. Skipping.");
					continue;
				}
				dbFile[0] = stringArray;
				DBConverter.Convert(dbFile);
				HoneyLog(4, $"Converted {stringArray} to DB.");
			}
			HoneyLog(3, "Converted string XMLs to DBs.");
		}
		catch (Exception e){
			HoneyLog(1, "There was an issue converting your string XMLs to DB format. Check HoneyLog.txt for more information.");
			HoneyLog(1, e.ToString(), true);
		}
	}
	
	private void InjectModsStr(){
		string stringArrayEnPath = Path.Combine(usrdir, "rom", "string_array", "string_array_en.xml");
		string stringArrayEn = File.ReadAllText(stringArrayEnPath);
		stringArrayEn = stringArrayEn.Replace("Font Design by FONTWORKS Inc.\n", String.Empty);
		stringArrayEn = stringArrayEn.Replace("The typefaces included herein are solely developed\nby DynaComware.\n", String.Empty);
		stringArrayEn = stringArrayEn.Replace("”PlayStation” is a registered trademark\nof Sony Computer Entertainment Inc.\n", modsStr);
		if (logoskip){
			stringArrayEn = stringArrayEn.Replace("A very small percentage of people may experience a seizure when exposed to certain visual images, including flashing lights or patterns that may appear in video games. If you or any of your relatives have a history of seizures or epilepsy, consult a doctor before playing.", String.Empty);
			HoneyLog(4, "Emptied epilepsy warning.");
		}
		else{
			stringArrayEn = stringArrayEn.Replace("A very small percentage of people may experience a seizure when exposed to certain visual images, including flashing lights or patterns that may appear in video games. If you or any of your relatives have a history of seizures or epilepsy, consult a doctor before playing.", "This game was patched with HoneyPatcher. HoneyPatcher is free software, with source code and help available at https://github.com/coatlessali/HoneyPatcher.");
			HoneyLog(4, "Replaced epilepsy warning with HoneyPatcher notice.");
		}
		try{
			File.WriteAllText(stringArrayEnPath, stringArrayEn);
			HoneyLog(3, "Injected mod list into string_array_en.xml.");
		}
		catch (Exception e){
			HoneyLog(1, "There was an issue saving string_array_en.xml. Check HoneyLog.txt for more details.");
			HoneyLog(1, e.ToString(), true);
		}
	}
	
	private void DDSFixHeader(){
		string[] ddsList = Directory.EnumerateFiles(usrdir, "*.dds", SearchOption.AllDirectories).ToArray();
		foreach (string dds in ddsList){
			using (FileStream fs = File.Open(dds, FileMode.Open, System.IO.FileAccess.ReadWrite, FileShare.ReadWrite)){
				byte[] headerbytes = new byte[3];
				fs.Read(headerbytes, 0, 3);
				string header = System.Text.Encoding.UTF8.GetString(headerbytes, 0, 3);
				const string valid = "DDS";
				if (header != valid){
					HoneyLog(2, $"DDS file {dds} has invalid header magic. Skipping.");
					continue;
				}
				fs.Seek(8, SeekOrigin.Begin);
				fs.Write(ddscomp);
				if (Path.GetFileName(dds).Contains("d5comp")){
					fs.Seek(20, SeekOrigin.Begin);
					fs.Write(d5comp);
				}
				else if (Path.GetFileName(dds).Contains("nocomp")){
					fs.Seek(20, SeekOrigin.Begin);
					fs.Write(nocomp);
				}
				else
					HoneyLog(2, $"Could not determine compression type of file {dds}. Skipping.");
			}
		}
		HoneyLog(3, "Sanitized DDS headers.");
	}
	
	private void GameSound(){
		switch (game){
			case "stf": _stfa.Play(); break;
			case "fv": _fva.Play(); break;
			case "vf2": _vf2a.Play(); break;
			case "omg": _omga.Play(); break;
		}
	}
	
	private void LoadConfig(){
		/* https://github.com/rickyah/ini-parser */
		/* Migrates config from V5 to V6 */
		if(!File.Exists(honeyConfig)){
			string defaultConfig = "[main]\nlogoskip = false\nstfusrdir = .\nvf2usrdir = .\n fvusrdir = .\n omgusrdir = .\ngame = stf\nloglevel = 2\ngemsSfx = false\ncleanup = true\nusrdir = migrated";
			try{
				File.WriteAllText(honeyConfig, defaultConfig);
				HoneyLog(3, "Created default configuration file.");
			}
			catch (Exception e){
				GD.Print(e.ToString());
				HoneyLog(1, "There was an issue creating the configuration. See HoneyLog.txt for more details.");
				HoneyLog(1, e.ToString(), true);
			}
		}
		IniData data = new FileIniDataParser().ReadFile(honeyConfig);
		try{
			if (data["main"]["usrdir"] != "migrated"){
				data["main"]["stfusrdir"] = data["main"]["usrdir"];
				data["main"]["vf2usrdir"] = ".";
				data["main"]["fvusrdir"] = ".";
				data["main"]["omgusrdir"] = ".";
				data["main"]["usrdir"] = "migrated";
				data["main"]["game"] = "stf";
				data["main"]["loglevel"] = "2";
				HoneyLog(3, "Migrated old config.");
				new FileIniDataParser().WriteFile(honeyConfig, data);
			}
		}
		catch{
			HoneyLog(4, "Skipping migration.", false);
		}
		
		try{
			game = data["main"]["game"];
		}
		catch{
			HoneyLog(2, "game not found in INI. Setting to default.");
			data["main"]["game"] = game;
		}
		try{
			usrdir = data["main"][$"{game}usrdir"];
		}
		catch{
			HoneyLog(2, $"{game}usrdir not found in INI. Setting to default.");
			data["main"][$"{game}usrdir"] =  ".";
		}
		try{
			loglevel = Byte.Parse(data["main"]["loglevel"]);
		}
		catch{
			HoneyLog(2, "loglevel not found in INI. Setting to default.");
			data["main"]["loglevel"] = loglevel.ToString();
		}
		try{
			logoskip = Boolean.Parse(data["main"]["logoskip"]);
		}
		catch{
			HoneyLog(2, "logoskip not found in INI. Setting to default.");
			data["main"]["logoskip"] =  "false";
		}
		_logoskip.ButtonPressed = logoskip;
		try{
			gemsSfx = Boolean.Parse(data["main"]["gemsSfx"]);
		}
		catch{
			HoneyLog(2, "gemsSfx not found in INI. Setting to default.");
			data["main"]["gemsSfx"] = "false";
		}
		try{
			cleanup = Boolean.Parse(data["main"]["cleanup"]);
		}
		catch{
			HoneyLog(2, "cleanup not found in INI. Setting to default.");
			data["main"]["cleanup"] = "true";
		}
		_cleanup.ButtonPressed = cleanup;
		new FileIniDataParser().WriteFile(honeyConfig, data);
		UpdateGame();
		HoneyLog(3, "loaded HoneyConfig.ini");
	}
	
	private void HoneyLog(byte severity, string message, bool exception = false){
		if (severity > loglevel){
			return;
		}
		string d = "?";
		switch (severity){
			case 1: d = "E"; break; // Error
			case 2: d = "W"; break; // Warning
			case 3: d = "I"; break; // Information
			case >= 4: d = "D"; break; // Debug
			default: return;
		}
		if (exception){	
			GD.Print($"[{d}] {message}");
			using (StreamWriter sw = File.AppendText(honeyLog))
			{
				sw.WriteLine($"[{d}] {message}");
			}	
		}
		else{
			/* This fixes multithreading. */
			_progress.CallDeferred("append_text", $"[{d}] {message}\n");
		}
		GD.Print($"[{d}] {message}");
		using (StreamWriter sw = File.AppendText(honeyLog))
		{
			sw.WriteLine($"[{d}] {message}");
		}	
	}
	
	private void CleanUp(){
		if (!cleanup){
			HoneyLog(3, "Skipping cleanup.");
			return;
		}
		string[] cleanupExts = { "*.txt", "*.rom_code", "*.rom_code1", "*.rom_code2", "*.rom_tex", "*.rom_ep", "*.rom_ep1", "*.rom_ep2", "*.rom_cop", "*.rom_pol", "*.rom_data", "*.ic12_13", "*.ic12_15", "*.string_array_en", "*.string_array2_en", "*.string_array_jp", "*.string_array2_jp", "*.loc" };
		foreach (string ext in cleanupExts){
			string[] files = Directory.EnumerateFiles(usrdir, ext, SearchOption.AllDirectories).ToArray();
			foreach (string file in files){
				try{
					File.Delete(file);
					HoneyLog(4, $"Deleted {file}.");
				}
				catch (Exception e){
					HoneyLog(1, $"There was an issue cleaning up {file}. Check HoneyLog.txt for more details.");
					HoneyLog(1, e.ToString(), true);
				}
			}
		}
		HoneyLog(3, "Finished cleaning up.");
	}
	
	private void ToggleLogoskip(bool toggle){
		IniData data = new FileIniDataParser().ReadFile(honeyConfig);
		data["main"]["logoskip"] = "false";
		logoskip = false;
		if (toggle){
			data["main"]["logoskip"] =  "true";
			logoskip = true;
		}
		new FileIniDataParser().WriteFile(honeyConfig, data);
		HoneyLog(3, "Toggled logoskip.");
	}
	
	private void ToggleCleanup(bool toggle){
		IniData data = new FileIniDataParser().ReadFile(honeyConfig);
		data["main"]["cleanup"] = "false";
		cleanup = false;
		if (toggle){
			data["main"]["cleanup"] =  "true";
			cleanup = true;
		}
		new FileIniDataParser().WriteFile(honeyConfig, data);
		HoneyLog(3, "Toggled cleanup.");
	}
	
	private void LogoSkip(){
		string bin = Path.Combine(usrdir, "EBOOT.BIN"); // retail bin
		/* Sanity Checks. */
		if (game != "stf"){
			HoneyLog(4, "Game is not Sonic the Fighters. Skipping.");
			return;
		}
		if (!logoskip){
			HoneyLog(4, "LogoSkip disabled. Skipping.");
			return;
		}
		/* Attempt a local copy first, as a manual override. */
		try{
			File.Copy(elf, bin, true);
			HoneyLog(3, $"Copied {elf} to {bin}.");
			return;
		}
		catch{
			HoneyLog(2, $"Failed to copy {elf} to your usrdir. It may not exist. Attempting download from GitHub...");
		}
		/* Download the latest EBOOT from the GitHub. */
		try{
			using (var client = new System.Net.Http.HttpClient()){
				using (var s = client.GetStreamAsync("https://github.com/coatlessali/HoneyPatcher/raw/refs/heads/main/EBOOT.bin")){
					using (var fs = new FileStream(bin, FileMode.OpenOrCreate)){
						s.Result.CopyTo(fs);
						HoneyLog(3, $"Downloaded patched EBOOT to {bin}.");
					}
				}
			}
		}
		catch (Exception e){
			HoneyLog(1, "A problem occurred trying to download EBOOT.bin. Check HoneyLog.txt for more details.");
			HoneyLog(1, e.ToString(), true);
		}
	}
}
