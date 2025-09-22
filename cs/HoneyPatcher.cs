using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Linq;
using SonicAudioLib;
using SonicAudioLib.CriMw;
using SonicAudioLib.IO;
using SonicAudioLib.Archives;
using MikuMikuLibrary.Databases;
using MikuMikuLibrary.Archives;
using MikuMikuLibrary.IO;
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
	
	// model 2 game file lists for patch creation
	string[] stf_roms = {"rom_code1.bin", "rom_data.bin", "rom_ep.bin", "rom_pol.bin", "rom_tex.bin", "string_array_en.bin", "string_array_jp.bin"};
	string[] fv_roms = {"rom_code1.bin", "rom_code2.bin", "rom_data.bin", "rom_ep1.bin", "rom_ep2.bin", "rom_pol.bin", "rom_tex.bin", "string_array_en.bin", "string_array_jp.bin"};
	string[] vf2_roms = {"ic12_13.bin", "ic12_15.bin", "rom_data.bin", "rom_pol.bin", "rom_tex.bin", "string_array_en.bin", "string_array_jp.bin"};
	string[] omg_roms = {"farc_tex.bin", "rom_code.bin", "rom_data.bin", "rom_pol.bin", "rom_tex.bin", "string_array_en.bin", "string_array_jp.bin", "string_array2_en.bin", "string_array2_jp.bin"};
	
	string usrdir = ".";
	string patchname = "Default";
	bool nomods = false;
	string game = "stf";
	string pretty_game = "Sonic the Fighters";
	string log;
	byte loglevel = 3;
	string modsStr;
	
	public override void _Ready(){	
		
		// Signal Connection
		_usrdirdialog.DirSelected += OnUsrdirDialog;
		_install.Pressed += OnInstallPressed;
		_restoreusrdir.Pressed += OnRestoreUsrdirPressed;
		_modsfolder.Pressed += OpenModsFolder;
		_genpatches.Pressed += CreatePatches;
		_patchesfolder.Pressed += OpenPatchesFolder;
		_gameselector.IdPressed += GameSelector;
		
		// Generate default config
		LoadConfig();
		// Empty log
		File.Create(honeyLog).Close();

		
		string[] essentialDirs = {modsDir, workbenchDir, Path.Combine(workbenchDir, "original"), Path.Combine(workbenchDir, "modified"), Path.Combine(workbenchDir, "patches")};
		string[] gamesList = {"stf", "vf2", "fv", "omg"};
		
		// Okay it's a little better now
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
		
		/* TO BE DELETED BY V8 */
		
		// Migrate mods folder
		if (Directory.GetFiles(modsDir).Length != 0){
			Directory.CreateDirectory(Path.Combine(modsDir, "stf"));
			CopyFilesRecursively(modsDir, Path.Combine(modsDir, "stf"));
			foreach (string file in Directory.GetFiles(modsDir)){
				File.Delete(file);
			}
			try{Directory.Delete(Path.Combine(modsDir, "stf", "stf"), true);}
			catch{}
		}
		// Migrate workbench
		string[] migration = {"original", "modified", "patches"};
		foreach (string migrate in migration){
			if (Directory.GetFiles(Path.Combine(workbenchDir, migrate)).Length != 0){
				Directory.CreateDirectory(Path.Combine(workbenchDir, migrate, "stf"));
				CopyFilesRecursively(Path.Combine(workbenchDir, migrate), Path.Combine(workbenchDir, migrate, "stf"));
				Directory.Delete(Path.Combine(workbenchDir, migrate, "stf", "stf"), true);
				foreach (string file in Directory.GetFiles(Path.Combine(workbenchDir, migrate))){
					File.Delete(file);
				}
			}
		}
		
		// Migrate backup folder.
		string eboot = Path.Combine(backupDir, "EBOOT.BIN");
		if (File.Exists(eboot)){
			HoneyLog(3, "Migrating STF backup directory.");
			Directory.CreateDirectory(Path.Combine(backupDir, "stf"));
			// _progress.Text += "[D] Created stf backup dir.\n";
			CopyFilesRecursively(backupDir, Path.Combine(backupDir, "stf"));
			// _progress.Text += "[D] Copied files.\n";
			string[] fdelete = {Path.Combine(backupDir, "EBOOT.BIN"), Path.Combine(backupDir, "chkboot.edat"), Path.Combine(backupDir, "rom.psarc")};
			string[] ddelete = {Path.Combine(backupDir, "ps3"), Path.Combine(backupDir, "rom")};
			foreach (string file in fdelete){
				try{File.Delete(file);}
				catch (Exception e){
					HoneyLog(1, $"Failed to delete {file}.");
					HoneyLog(1, e.ToString(), true);
				}
			}
			foreach (string dir in ddelete){
				try{Directory.Delete(dir, true);}
				catch (Exception e){
					HoneyLog(1, $"Failed to delete {dir}.");
					HoneyLog(1, e.ToString(), true);
				}
			}
			try{Directory.Delete(Path.Combine(backupDir, "stf", "stf"), true);}
			catch{}
		}
	}

	// Signals
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
	
	// Install mods
	private void OnInstallPressed(){
		// Check if usrdir is set.
		if (usrdir == "."){
			_back.Play();
			ShowError("Error", "USRDIR is unset. Please select a USRDIR.");
			HoneyLog(1, "usrdir is unset.");
			return;
		}
		// Check for clean copy of game w/ rom.psarc still intact
		string psarc_path = Path.Combine(usrdir, "rom.psarc");
		if (!File.Exists(psarc_path)){
			_back.Play();
			ShowError("Error", $"rom.psarc could not be found. Please ensure you have a clean copy of {pretty_game} if this\nis your first time, or Uninstall mods before proceeding.");
			HoneyLog(1, $"rom.psarc not found at {psarc_path}.");
			return;
		}
		
		// Make backup if valid stf found and no backup exists
		string gameBackupDir = Path.Combine(backupDir, game);
		Directory.CreateDirectory(gameBackupDir);
		CopyFilesRecursively(usrdir, gameBackupDir);
		HoneyLog(3, "Created backup.");
		
		// This gets AcbEditor working.
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		
		// Extract rom.psarc - used UnPSARC by NoobInCoding as a base, stripped it down,
		// and turned it into a DLL. It's honestly still really bloated and could do with
		// a bit more cleanup.
		PsarcThing.UnpackArchiveFile(psarc_path, Path.Combine(usrdir, "rom"));
		HoneyLog(3, "Extracted rom.psarc");
		File.Delete(psarc_path);
		HoneyLog(3, "Removed rom.psarc");
		FarcUnpack();
		HoneyLog(3, "Unpacked farc files.");
		UnpackAcb();
		HoneyLog(3, "Unpacked ACB file.");
		DbToXml();
		HoneyLog(3, "Converted string DBs to XML.");
		ExtractMods();
		HoneyLog(3, "Extracted mods.");
		ApplyPatches();
		HoneyLog(3, "Applied patches.");
		// LibSTF by Bekzii
		InjectModels();
		if (game == "stf")
			HoneyLog(3, "Injected models.");
		DDSFixHeader();
		HoneyLog(3, "Sanitized DDS headers.");
		InjectModsStr();
		HoneyLog(3, "Injected mod list into string_array_en.xml.");
		XmlToDb();
		HoneyLog(3, "Converted string XMLs to DBs.");
		FarcPack();
		HoneyLog(3, "Repacked farc files.");
		PackAcb();
		HoneyLog(3, "Packed ACB file.");
		if (nomods){
			GameSound();
			ShowError("Success?", "No mods were found, but I extracted rom.psarc and unpacked your game files for you anyways.");
			return;
		}
		GameSound();
		ShowError("Success!", "Mods have been installed!");
	}
	
	// Uninstall Mods
	private void OnRestoreUsrdirPressed(){
		// Check if backup exists
		if (!Directory.Exists(Path.Combine(backupDir, game))){
			_back.Play();
			ShowError("Error", "No backup found.");
			HoneyLog(1, "No backup found.");
			return;
		}
		
		// Clear contents of USRDIR
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
		
		// Restore backup
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
		GameSound();
		ShowError("Success", "Files restored.");
		HoneyLog(3, "Restored game files.");
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
			HoneyLog(3, $"Created {patch}");
			File.WriteAllLines(patchloc, locations.ToArray());
			HoneyLog(3, $"Created {patchloc}");
		}
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
		// Experimental - use Directory.GetFiles(), which requires two passes
		/*string[] farcs =   {"sprite/n_advstf.farc", "sprite/n_advfv.farc", "sprite/n_advvf2.farc",
							"sprite/n_adv.farc", "sprite/n_cmn.farc", "sprite/n_fnt.farc", 
							"sprite/n_info.farc", "sprite/n_stf.farc", "sprite/n_fv.farc",
							"sprite/n_advfv2.farc", "sprite/n_omg.farc", "string_array.farc", 
							"sprite/n_advstf/texture.farc", "sprite/n_advfv/texture.farc", "sprite/n_advvf2/texture.farc",
							"sprite/n_adv/texture.farc", "sprite/n_omg/texture.farc", "sprite/n_cmn/texture.farc", 
							"sprite/n_fnt/texture.farc", "sprite/n_info/texture.farc", "sprite/n_stf/texture.farc",};
		*/
		
		string romdir = Path.Combine(usrdir, "rom");
		List<string> unpacked = new List<string>();
		
		for (int i = 0; i < 2; i++){
			// get all farc files, we need to do this twice hence the for loop
			string[] farcs = Directory.GetFiles(romdir, "*.farc", SearchOption.AllDirectories);
			HoneyLog(4, farcs.Length.ToString());
			foreach (string farc in farcs ){
				if (unpacked.Contains(farc)){
					HoneyLog(4, $"{farc} already unpacked.");
					continue;
				}
				unpacked.Add(farc);
				// string sourceFileName = Path.Combine(usrdir, "rom", farc);
				string sourceFileName = farc;
				try{
					// Set source and destination filename
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
				}
				catch (Exception e){
					HoneyLog(1, $"{sourceFileName} could not be unpacked.");
					HoneyLog(1, e.ToString(), true);
				}
			}
		}
	}
	
	private void FarcPack(){
		// Experimental - use Directory.GetFiles()
		/*string[] dirlist = {"sprite/n_advstf/texture", "sprite/n_advfv/texture", "sprite/n_advvf2/texture",
							"sprite/n_adv/texture", "sprite/n_omg/texture", "sprite/n_cmn/texture", 
							"sprite/n_fnt/texture", "sprite/n_info/texture", "sprite/n_stf/texture", 
							"string_array", "sprite/n_advstf", "sprite/n_advfv",
							"sprite/n_advvf2", "sprite/n_adv", "sprite/n_omg", 
							"sprite/n_cmn", "sprite/n_fnt", "sprite/n_info", "sprite/n_stf"};
		*/
		string romdir = Path.Combine(usrdir, "rom");
		string[] farcs = Directory.GetFiles(romdir, "*.farc", SearchOption.AllDirectories);
		Array.Reverse(farcs);
		// HoneyLog(4, String.Join("\n", farcs));
	
		foreach (string farc in farcs)
		{
			// string sourceFileName = Path.GetFullPath(Path.Combine(usrdir, "rom", dir));
			string sourceFileName = farc.Replace(".farc", String.Empty);
			try{
				// Set source and destination file name
				string destinationFileName = Path.ChangeExtension(sourceFileName, "farc");;
				
				// Modified by me, otherwise it throws access errors if you don't use "using"
				using (var farcArchive = new FarcArchive { IsCompressed = false, Alignment = 16 }){
				
					if (File.GetAttributes(sourceFileName).HasFlag(FileAttributes.Directory)){
						foreach (string filePath in Directory.EnumerateFiles(sourceFileName))
						farcArchive.Add(Path.GetFileName(filePath), filePath);
					}
					else
						farcArchive.Add(Path.GetFileName(sourceFileName), sourceFileName);
					farcArchive.Save(destinationFileName);
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
			}
		}
	}
	
	private void ApplyPatches(){
		// Get list of files in rom folder
		string[] files = Directory.GetFiles(Path.Combine(usrdir, "rom"));
		// Apply in alphabetical order
		Array.Sort(files);
		foreach (string mod in files){
			string modpath = mod; // patch
			string romdir = Path.Combine(usrdir, "rom");
			string stf_rom = Path.Combine(romdir, $"{game}_rom");
			string patchdest; // file to be patched
			switch(Path.GetExtension(modpath)){
				// Check the file extension, which should be the name of the file you want to patch
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
				
				// At some point we'll handle these with actual XML extraction/injection! For now, this will do.
				// case ".string_array_en": patchdest = Path.Combine(romdir, "string_array", "string_array_en.bin"); break;
				// case ".string_array2_en": patchdest = Path.Combine(romdir, "string_array", "string_array2_en.bin"); break;
				// case ".string_array_jp": patchdest = Path.Combine(romdir, "string_array", "string_array_jp.bin"); break;
				// case ".string_array2_jp": patchdest = Path.Combine(romdir, "string_array", "string_array2_jp.bin"); break;
				default: continue;
			}
			byte[] original = File.ReadAllBytes(patchdest);
			byte[] changes = File.ReadAllBytes(modpath);
			string[] locations = File.ReadAllLines(modpath+".loc");
			uint inc = 0;
			using (FileStream fs = File.Open(patchdest, FileMode.Open, System.IO.FileAccess.ReadWrite, FileShare.ReadWrite)){
				foreach (string i in locations){
					long loc = Int64.Parse(i);
					fs.Seek(loc, SeekOrigin.Begin);
					fs.WriteByte(changes[inc]);
					inc++;
				}
			}
		}
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
			// get the first 4 digits of the filename to be the id
			fileName = fileName.Substring(0, 4);
			int modelId;
			// attempt parsing
			if (!Int32.TryParse(fileName, out modelId)){
				HoneyLog(2, $"Filename of {model} is invalid - skipping.");
				continue;
			}
			// remove extension
			string modelName = Path.GetFileNameWithoutExtension(model);
			// read filepath
			modelName = Path.Combine(usrdir, "rom", modelName);
			ModelInject.AddModel(modelId, modelName);
		}
		try{
			ModelInject.Verbose = true;
			ModelInject.InjectModels(Path.Combine(usrdir, "rom", "stf_rom"));
		}
		catch (Exception e){
			HoneyLog(1, "There was an error injecting models. See HoneyLog.txt for more details.");
			HoneyLog(1, e.ToString(), true);
		}
	}
	
	private void UnpackAcb(){
		// AcbEditor by Skyth - did you know the upstream build literally can't run without a console?
		string[] AcbFile = {Path.Combine(usrdir, "rom", "sound", $"{game}_all.acb")};
		AcbEditorThing.AcbEdit(AcbFile);
	}

	private void PackAcb(){
		string[] AcbFolder = {Path.Combine(usrdir, "rom", "sound", $"{game}_all")};
		AcbEditorThing.AcbEdit(AcbFolder);
	}
	
	private void DbToXml(){
		// DatabaseConverter by Skyth - did you know the upstream build will fail due to invalid xml characters?
		string stringArrayDir = Path.Combine(usrdir, "rom", "string_array");
		string[] stringArrays = {Path.Combine(stringArrayDir, "string_array_en.bin"), Path.Combine(stringArrayDir, "string_array2_en.bin"), Path.Combine(stringArrayDir, "string_array_jp.bin"), Path.Combine(stringArrayDir, "string_array2_jp.bin")};
		string[] dbFile = new string[1];
		foreach (string stringArray in stringArrays){
			if (!File.Exists(stringArray)){
				HoneyLog(3, $"{stringArray} not found. Skipping.");
				continue;
			}
			dbFile[0] = stringArray;
			DBConverter.Convert(dbFile);
			HoneyLog(3, $"Converted {stringArray} to XML.");
		}
	}
	
	private void XmlToDb(){
		// DatabaseConverter by Skyth - did you know the upstream build will fail due to invalid xml characters?
		string stringArrayDir = Path.Combine(usrdir, "rom", "string_array");
		string[] stringArrays = {Path.Combine(stringArrayDir, "string_array_en.xml"), Path.Combine(stringArrayDir, "string_array2_en.xml"), Path.Combine(stringArrayDir, "string_array_jp.xml"), Path.Combine(stringArrayDir, "string_array2_jp.xml")};
		string[] dbFile = new string[1];
		foreach (string stringArray in stringArrays){
			if (!File.Exists(stringArray)){
				HoneyLog(3, $"{stringArray} not found. Skipping.");
				continue;
			}
			dbFile[0] = stringArray;
			DBConverter.Convert(dbFile);
			HoneyLog(3, $"Converted {stringArray} to DB.");
		}
	}
	
	private void InjectModsStr(){
		string stringArrayEnPath = Path.Combine(usrdir, "rom", "string_array", "string_array_en.xml");
		string stringArrayEn = File.ReadAllText(stringArrayEnPath);
		stringArrayEn = stringArrayEn.Replace("Font Design by FONTWORKS Inc.\n", String.Empty);
		stringArrayEn = stringArrayEn.Replace("The typefaces included herein are solely developed\nby DynaComware.\n", String.Empty);
		stringArrayEn = stringArrayEn.Replace("”PlayStation” is a registered trademark\nof Sony Computer Entertainment Inc.\n", modsStr);
		File.WriteAllText(stringArrayEnPath, stringArrayEn);
	}
	
	private void DDSFixHeader(){
		string[] ddsList = Directory.EnumerateFiles(usrdir, "*.dds", SearchOption.AllDirectories).ToArray();
		foreach (string dds in ddsList){
			using (FileStream fs = File.Open(dds, FileMode.Open, System.IO.FileAccess.ReadWrite, FileShare.ReadWrite)){
				// byte[] file = File.ReadAllBytes(dds);
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
		// https://github.com/rickyah/ini-parser
		// MIT License
		// Read INI file
		// Migrate config from V5 to V6
		if(!File.Exists(honeyConfig)){
			string defaultConfig = "[main]\nlogoskip = false\nstfusrdir = .\nvf2usrdir = .\n fvusrdir = .\n omgusrdir = .\ngame = stf\nloglevel = 2";
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
		
		// try to set userdir
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
		new FileIniDataParser().WriteFile(honeyConfig, data);
		UpdateGame();
		HoneyLog(3, "loaded HoneyConfig.ini");
	}
	
	private void HoneyLog(byte severity, string message, bool exception = false){
		if (severity > loglevel){
			return;
		}
		string d;
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
			_progress.Text += $"[{d}] {message}\n";}
			GD.Print($"[{d}] {message}");
			using (StreamWriter sw = File.AppendText(honeyLog))
			{
				sw.WriteLine($"[{d}] {message}");
			}	
		}
	}
