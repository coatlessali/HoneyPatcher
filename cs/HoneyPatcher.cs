using Godot;
using Gibbed.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Linq;
using MikuMikuLibrary.Archives;
using MikuMikuLibrary.IO;
using IniParser;
using IniParser.Model;
using UnPSARC;

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
	public Button _modsfolder; // Opens mods folder, doesn't currently work on my setup for some reason
	[Export]
	public Button _genpatches; // Generate Patches button
	[Export]
	public RichTextLabel _progress; // Progress label
	[Export]
	public Label _game; // game label
	[Export]
	public LineEdit _patchname; // Name of patch
	[Export]
	public Button _patchesfolder; // Opens patches folder
	[Export]
	public PopupMenu _gameselector; // Selects a game
	
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
		if(!File.Exists(honeyConfig)){
			string defaultConfig = "[main]\nlogoskip = false\nstfusrdir = .\nvf2usrdir = .\n fvusrdir = .\n omgusrdir = .\ngame = stf";
			File.WriteAllText(honeyConfig, defaultConfig);
			_progress.Text += "[I] Created default configuration file.\n";
		}
		
		string[] essentialDirs = {modsDir, workbenchDir, Path.Combine(workbenchDir, "original"), Path.Combine(workbenchDir, "modified"), Path.Combine(workbenchDir, "patches")};
		string[] gamesList = {"stf", "vf2", "fv", "omg"};
		
		// This is so fucking dumb can someone please tell me a better way to do this
		foreach (string h in essentialDirs){
			if (!Directory.Exists(h)){
				Directory.CreateDirectory(h);
				_progress.Text += $"[I] Created directory {h}.\n";
			}
		}
		foreach (string h in gamesList){
			if (!Directory.Exists(Path.Combine(modsDir, h))){
				Directory.CreateDirectory(h);
				_progress.Text += $"[I] Created directory {h}.\n";
			}
			if (!Directory.Exists(Path.Combine(workbenchDir, "original", h))){
				Directory.CreateDirectory(h);
				_progress.Text += $"[I] Created directory {h}.\n";
			}
			if (!Directory.Exists(Path.Combine(workbenchDir, "modified", h))){
				Directory.CreateDirectory(h);
				_progress.Text += $"[I] Created directory {h}.\n";
			}
			if (!Directory.Exists(Path.Combine(workbenchDir, "patches", h))){
				Directory.CreateDirectory(h);
				_progress.Text += $"[I] Created directory {h}.\n";
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
			catch{GD.Print("error deleting stf/stf");}
		}
		// Migrate workbench
		if (Directory.GetFiles(Path.Combine(workbenchDir, "original")).Length != 0){
			Directory.CreateDirectory(Path.Combine(workbenchDir, "original", "stf"));
			CopyFilesRecursively(Path.Combine(workbenchDir, "original"), Path.Combine(workbenchDir, "original", "stf"));
			Directory.Delete(Path.Combine(workbenchDir, "original", "stf", "stf"), true);
			foreach (string file in Directory.GetFiles(Path.Combine(workbenchDir, "original"))){
				File.Delete(file);
			}
		}
		if (Directory.GetFiles(Path.Combine(workbenchDir, "modified")).Length != 0){
			Directory.CreateDirectory(Path.Combine(workbenchDir, "modified", "stf"));
			CopyFilesRecursively(Path.Combine(workbenchDir, "modified"), Path.Combine(workbenchDir, "modified", "stf"));
			Directory.Delete(Path.Combine(workbenchDir, "modified", "stf", "stf"), true);
			foreach (string file in Directory.GetFiles(Path.Combine(workbenchDir, "modified"))){
				File.Delete(file);
			}
		}
		if (Directory.GetFiles(Path.Combine(workbenchDir, "patches")).Length != 0){
			Directory.CreateDirectory(Path.Combine(workbenchDir, "patches", "stf"));
			CopyFilesRecursively(Path.Combine(workbenchDir, "patches"), Path.Combine(workbenchDir, "patches", "stf"));
			Directory.Delete(Path.Combine(workbenchDir, "patches", "stf", "stf"), true);
			foreach (string file in Directory.GetFiles(Path.Combine(workbenchDir, "patches"))){
				File.Delete(file);
			}
		}
		
		foreach (string gayme in gamesList){
			if (!Directory.Exists(Path.Combine(modsDir, gayme))){
				Directory.CreateDirectory(Path.Combine(modsDir, gayme));
				_progress.Text += $"[I] Created directory {Path.Combine(modsDir, gayme)}.\n";
			}
			if (!Directory.Exists(Path.Combine(workbenchDir, "original", gayme))){
				Directory.CreateDirectory(Path.Combine(workbenchDir, "original", gayme));
				_progress.Text += $"[I] Created directory {Path.Combine(workbenchDir, "original", gayme)}.\n";
			}
			if (!Directory.Exists(Path.Combine(workbenchDir, "modified", gayme))){
				Directory.CreateDirectory(Path.Combine(workbenchDir, "modified", gayme));
				_progress.Text += $"[I] Created directory {Path.Combine(workbenchDir, "modified", gayme)}.\n";
			}
			if (!Directory.Exists(Path.Combine(workbenchDir, "patches", gayme))){
				Directory.CreateDirectory(Path.Combine(workbenchDir, "patches", gayme));
				_progress.Text += $"[I] Created directory {Path.Combine(workbenchDir, "patches", gayme)}.\n";
			}
		}
		
		// Migrate backup folder.
		if (File.Exists(Path.Combine(backupDir, "EBOOT.BIN"))){
			_progress.Text += "[I] Migrating STF backup directory.\n";
			Directory.CreateDirectory(Path.Combine(backupDir, "stf"));
			// _progress.Text += "[D] Created stf backup dir.\n";
			CopyFilesRecursively(backupDir, Path.Combine(backupDir, "stf"));
			// _progress.Text += "[D] Copied files.\n";
			string[] fdelete = {Path.Combine(backupDir, "EBOOT.BIN"), Path.Combine(backupDir, "chkboot.edat"), Path.Combine(backupDir, "rom.psarc")};
			string[] ddelete = {Path.Combine(backupDir, "ps3"), Path.Combine(backupDir, "rom")};
			foreach (string file in fdelete){
				try{File.Delete(file);}
				catch{_progress.Text += $"[E] Failed to delete {file}.\n";}
			}
			foreach (string dir in ddelete){
				try{Directory.Delete(dir, true);}
				catch{_progress.Text += $"[E] Failed to delete {dir}.\n";}
			}
			try{Directory.Delete(Path.Combine(backupDir, "stf", "stf"), true);}
			catch(Exception e){GD.Print(e.ToString());}
		}
		
		
		// https://github.com/rickyah/ini-parser
		// MIT License
		// Read INI file
		IniData data = new FileIniDataParser().ReadFile(honeyConfig);
		// Migrate config from V5 to V6
		try{
			/* TO BE DELETED BY VERSION 8 */
			// usrdir = data["main"]["usrdir"];
			if (data["main"]["usrdir"] != "migrated"){
				data["main"]["stfusrdir"] = data["main"]["usrdir"];
				data["main"]["vf2usrdir"] = ".";
				data["main"]["fvusrdir"] = ".";
				data["main"]["omgusrdir"] = ".";
				data["main"]["usrdir"] = "migrated";
				data["main"]["game"] = "stf";
				_progress.Text += "[I] Migrated old config.\n";
				new FileIniDataParser().WriteFile(honeyConfig, data);
			}
			else{
				// _progress.Text += "[D] Usrdir already migrated.\n";
			}
		}
		catch{
			// _progress.Text += "[D] Skipping migration.\n";
		}
		// try to set userdir
		try {
			game = data["main"]["game"];
			_progress.Text += "[I] set game.\n";
			usrdir = data["main"][$"{game}usrdir"];
			// _progress.Text += "[D] set usrdir for current game.\n";
		}
		catch (Exception e){
			_progress.Text += $"[W] {game}usrdir not found in INI.\n";
			usrdir = ".";
		}
		UpdateGame();
		_progress.Text += "[I] loaded HoneyConfig.ini.\n";
		
	}

	// Signals
	private void OnUsrdirDialog(string dir){
		usrdir = Path.GetFullPath(dir); // Set usrdir for current session
		IniData data = new FileIniDataParser().ReadFile(honeyConfig); // Open config file
		data["main"][$"{game}usrdir"] = dir; // Set usrdir
		new FileIniDataParser().WriteFile(honeyConfig, data); // Write config file
		_progress.Text += "[I] Saved changes.\n";
	}
	
	private void UpdateGame(){
		switch (game){
			case "stf":
				pretty_game = "Sonic the Fighters";
				break;
			case "vf2":
				pretty_game = "Virtua Fighter 2";
				break;
			case "fv":
				pretty_game = "Fighting Vipers";
				break;
			case "omg":
				pretty_game = "Cyber Troopers Virtual-On: Operation Moongate";
				break;
		}
		// _game.Text = $"Current Game: {pretty_game}";
	}
	
	private void GameSelector(long id){
		IniData data = new FileIniDataParser().ReadFile(honeyConfig); // Open config file
		switch (id)
		{
			case 0:
				game = "stf";
				break;
			case 1:
				game = "vf2";
				break;
			case 2:
				game = "fv";
				break;
			case 3:
				game = "omg";
				break;
			default:
				break; 
		}
		UpdateGame();
		data["main"]["game"] = game;
		new FileIniDataParser().WriteFile(honeyConfig, data); // Write config file
		usrdir = data["main"][$"{game}usrdir"];
		// _progress.Text = $"[D] Changed usrdir to {usrdir}.\n";
		// _progress.Text += $"[D] Game ID: {id.ToString()}.\n";
		// _progress.Text += $"[D] Game Name: {game}.\n";
		_progress.Text += $"[I] Changed Game: {pretty_game}.\n";
	}
	
	// Install mods
	private void OnInstallPressed(){
		// Check if usrdir is set.
		if (usrdir == "."){
			ShowError("Error", "USRDIR is unset. Please select a USRDIR.");
			_progress.Text += "[E] usrdir unset.\n";
			return;
		}
		// Check for clean copy of game w/ rom.psarc still intact
		string psarc_path = Path.Combine(usrdir, "rom.psarc");
		if (!File.Exists(psarc_path)){
			ShowError("Error", $"rom.psarc could not be found. Please ensure you have a clean copy of {pretty_game} if this\nis your first time, or Uninstall mods before proceeding.");
			_progress.Text += "[E] rom.psarc not found.\n";
			return;
		}
		
		// Make backup if valid stf found and no backup exists
		if(!Directory.Exists(Path.Combine(backupDir, game))){
			Directory.CreateDirectory(Path.Combine(backupDir, game));
			_progress.Text += "[I] Created backup directory.\n";
		}
		CopyFilesRecursively(usrdir, Path.Combine(backupDir, game));
		_progress.Text += "[I] Created backup.\n";
		
		// Extract rom.psarc - used UnPSARC by NoobInCoding as a base, stripped it down,
		// and turned it into a DLL. It's honestly still really bloated and could do with
		// a bit more cleanup.
		
		PsarcThing.UnpackArchiveFile(psarc_path, Path.Combine(usrdir, "rom"));
		
		_progress.Text += "[I] Extracted rom.psarc.\n";
		File.Delete(psarc_path);
		_progress.Text += "[I] Extracted and removed rom.psarc.\n";
		FarcUnpack();
		_progress.Text += "[I] Unpacked farc files.\n";
		ExtractMods();
		_progress.Text += "[I] Extracted mods.\n";
		ApplyPatches();
		_progress.Text += "[I] Applied patches.\n";
		DDSFixHeader();
		_progress.Text += "[I] Sanitized DDS headers.\n";
		FarcPack();
		_progress.Text += "[I] Repacked farc files.\n";
		if (nomods)
			ShowError("Success?", "No mods were found, but I extracted rom.psarc for you anyways.");
		else
			ShowError("Success!", "Mods have been installed!");
	}
	
	// Uninstall Mods
	private void OnRestoreUsrdirPressed(){
		// Check if backup exists
		if (!Directory.Exists(Path.Combine(backupDir, game))){
			ShowError("Error", "No backup found.");
			_progress.Text += "[E] No backup found.\n";
			return;
		}
		
		// Clear contents of USRDIR
		try{
			// https://stackoverflow.com/questions/1288718/how-to-delete-all-files-and-folders-in-a-directory
			System.IO.DirectoryInfo di = new DirectoryInfo(usrdir);
			foreach (FileInfo file in di.GetFiles())
				file.Delete(); 
			foreach (DirectoryInfo dir in di.GetDirectories())
				dir.Delete(true); 
		}
		catch (Exception e){
			ShowError("Exception", e.ToString());
			_progress.Text += "[E] Error restoring files. (Couldn't wipe game files.)\n";
			return;
		}
		
		// Restore backup
		try{
			CopyFilesRecursively(Path.Combine(backupDir, game), usrdir);
		}
		catch (Exception e){
			ShowError("Exception", e.ToString());
			_progress.Text += "[E] Error restoring files. (Couldn't copy backup.)\n";
			return;
		}
		ShowError("Success", "Files restored.");
		_progress.Text += "[I] Restored game files.\n";
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
		GD.Print("creating patches");
		// string[] roms;
		List<string> roms = new List<string>();
		switch (game){
			case "stf":
				roms = stf_roms.ToList();
				break;
			case "vf2":
				roms = vf2_roms.ToList();
				break;
			case "fv":
				roms = fv_roms.ToList();
				break;
			case "omg":
				roms = omg_roms.ToList();
				break;
			default:
				break;
		}
		// string[] roms = {"rom_code1.bin", "rom_data.bin", "rom_ep.bin", "rom_pol.bin", "rom_tex.bin", "string_array_en.bin"};
		foreach (string rawhm in roms){
			if (!File.Exists(Path.Combine(workbenchDir, "original", game, rawhm))){
				GD.Print(Path.Combine(workbenchDir, "original", game, rawhm));
				GD.Print("original " + rawhm + " not found");
				ShowError("Error", "Original " + rawhm + "not found.");
				return;
			}
			if (File.Exists(Path.Combine(workbenchDir, "modified", game, rawhm))){
				GD.Print("modified " + rawhm + " found");
				files.Add(rawhm);
			}
		}
		foreach (string filename in files){
			uint count = 0;
			//uint changecount = 0;
			List<string> locations = new List<string>();
			List<byte> changes = new List<byte>();
			GD.Print("Patch Name: " + patchname);
			string patchextension = Path.GetFileNameWithoutExtension(Path.Combine(workbenchDir, "original", game, filename));
			GD.Print("Patch Extension: " + patchextension);
			byte[] original = File.ReadAllBytes(Path.Combine(workbenchDir, "original", game, filename));
			byte[] modified = File.ReadAllBytes(Path.Combine(workbenchDir, "modified", game, filename));
			foreach (byte b in original)
			{
				if (b != modified[count]){
					locations.Add(count.ToString());
					changes.Add(modified[count]);
					//changecount++;
				}
				count++;
			}
			GD.Print("Change Locations: " + locations.Count.ToString());
			GD.Print("Changes: " + changes.Count.ToString());
			string patch = Path.Combine(workbenchDir, "patches", game, patchname + "." + patchextension);
			string patchloc = patch + ".loc";
			GD.Print("Patch: " + patch);
			GD.Print("Patch Locations: " + patchloc);
			File.WriteAllBytes(patch, changes.ToArray());
			_progress.Text += $"[I] Created {patch}.\n";
			File.WriteAllLines(patchloc, locations.ToArray());
			_progress.Text += $"[I] Created {patchloc}.\n";
		}
	}
	
	public override void _Process(double delta){}

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
		string[] farcs =   {"sprite/n_advstf.farc", 
							"sprite/n_advfv.farc",
							"sprite/n_advvf2.farc",
							"sprite/n_adv.farc",
							"sprite/n_cmn.farc", 
							"sprite/n_fnt.farc", 
							"sprite/n_info.farc", 
							"sprite/n_stf.farc",
							"sprite/n_fv.farc",
							"sprite/n_advfv2.farc",
							"sprite/n_omg.farc",
							"string_array.farc", 
							"sprite/n_advstf/texture.farc",
							"sprite/n_advfv/texture.farc",
							"sprite/n_advvf2/texture.farc",
							"sprite/n_adv/texture.farc",
							"sprite/n_omg/texture.farc", 
							"sprite/n_cmn/texture.farc", 
							"sprite/n_fnt/texture.farc", 
							"sprite/n_info/texture.farc", 
							"sprite/n_stf/texture.farc",};
	
		foreach (string farc in farcs )
		{
			string sourceFileName = Path.Combine(usrdir, "rom", farc);
			try{
				// Set source and destination filename
				string destinationFileName = Path.ChangeExtension(sourceFileName, null);
				
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
			catch (Exception e){
				// GD.Print(e.ToString());
				if (!File.Exists(sourceFileName)){
					// _progress.Text += $"[D] {sourceFileName} not found. Skipping.\n";
				}
				else{
					_progress.Text += $"[E] {sourceFileName} could not be unpacked.\n";
				}
			}
		}
	}
	
	private void FarcPack(){
		string[] dirlist = {"sprite/n_advstf/texture",
							"sprite/n_advfv/texture",
							"sprite/n_advvf2/texture",
							"sprite/n_adv/texture",
							"sprite/n_omg/texture", 
							"sprite/n_cmn/texture", 
							"sprite/n_fnt/texture", 
							"sprite/n_info/texture", 
							"sprite/n_stf/texture", 
							"string_array", 
							"sprite/n_advstf",
							"sprite/n_advfv",
							"sprite/n_advvf2",
							"sprite/n_adv",
							"sprite/n_omg", 
							"sprite/n_cmn", 
							"sprite/n_fnt", 
							"sprite/n_info", 
							"sprite/n_stf"};
	
		foreach (string dir in dirlist)
		{
			string sourceFileName = Path.GetFullPath(Path.Combine(usrdir, "rom", dir));
			try{
				// Set source and destination file name
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
			catch (Exception e){
				// GD.Print(e.ToString());
				if (!Directory.Exists(sourceFileName)){
					// _progress.Text += $"[D] {sourceFileName} does not exist. Skipping.\n";
				}
				else{
					_progress.Text += $"[E] {sourceFileName} could not be repacked.\n";
				}
			}
		}
	}
	
	private void ExtractMods(){
		string[] files = Directory.GetFiles(Path.Combine(modsDir, game));
		Array.Sort(files);
		if (files.Length == 0){
			nomods = true;
			return;
		}
		else{
			nomods = false;
		}
		
		foreach (string mod in files)
		{
			string modpath = mod;
			string romdir = Path.Combine(usrdir, "rom");
			string stf_rom = Path.Combine(romdir, $"{game}_rom");
			if (Path.GetExtension(modpath) == ".zip")
				ZipFile.ExtractToDirectory(modpath, romdir, true);
		}
	}
	
	private void ApplyPatches(){
		// Get list of files in rom folder
		string[] files = Directory.GetFiles(Path.Combine(usrdir, "rom"));
		// Apply in alphabetical order
		Array.Sort(files);
		foreach (string mod in files)
		{
			string modpath = mod; // patch
			string romdir = Path.Combine(usrdir, "rom");
			string stf_rom = Path.Combine(romdir, $"{game}_rom");
			string patchdest; // file to be patched
			switch(Path.GetExtension(modpath))
			{
				// Check the file extension, which should be the name of the file you want to patch
				case ".rom_code": // OMG
					patchdest = Path.Combine(stf_rom, "rom_code.bin");
					break;
				case ".rom_code1": // STF, FV
					patchdest = Path.Combine(stf_rom, "rom_code1.bin");
					break;
				case ".rom_code2": // FV
					patchdest = Path.Combine(stf_rom, "rom_code2.bin");
					break;
				case ".rom_cop": // OMG
					patchdest = Path.Combine(stf_rom, "rom_cop.bin");
					break;
				case ".rom_data":
					patchdest = Path.Combine(stf_rom, "rom_data.bin");
					break;
				case ".rom_ep":
					patchdest = Path.Combine(stf_rom, "rom_ep.bin");
					break;
				case ".rom_ep1": // FV
					patchdest = Path.Combine(stf_rom, "rom_ep1.bin");
					break;
				case ".rom_ep2": // FV
					patchdest = Path.Combine(stf_rom, "rom_ep1.bin");
					break;
				case ".rom_pol":
					patchdest = Path.Combine(stf_rom, "rom_pol.bin");
					break;
				case ".rom_tex":
					patchdest = Path.Combine(stf_rom, "rom_tex.bin");
					break;
				case ".ic12_13": // VF2
					patchdest = Path.Combine(stf_rom, "ic12_13.bin");
					break;
				case ".ic12_15": // VF2
					patchdest = Path.Combine(stf_rom, "ic12_15.bin");
					break;
				
				// At some point we'll handle these with actual XML extraction/injection!
				// For now, this will do.
				case ".string_array_en":
					patchdest = Path.Combine(romdir, "string_array", "string_array_en.bin");
					break;
				case ".string_array2_en":
					patchdest = Path.Combine(romdir, "string_array", "string_array2_en.bin");
					break;
				case ".string_array_jp":
					patchdest = Path.Combine(romdir, "string_array", "string_array_jp.bin");
					break;
				case ".string_array2_jp":
					patchdest = Path.Combine(romdir, "string_array", "string_array2_jp.bin");
					break;
				default:
					patchdest = null;
					break;
			}
			if (patchdest != null){
				// modpath = patch
				// patchdest = file to be patched
				try{
					byte[] changes = File.ReadAllBytes(modpath);
					string[] locations = File.ReadAllLines(modpath+".loc");
					uint inc = 0;
					using (FileStream fs = File.Open(patchdest, FileMode.Open, System.IO.FileAccess.ReadWrite, FileShare.ReadWrite)){
						foreach (string i in locations){
							long loc = Int64.Parse(i);
							fs.Seek(loc, SeekOrigin.Begin);
							// GD.Print(loc.ToString());
							fs.WriteByte(changes[inc]);
							inc++;
						}
					}
				}
				catch(Exception e){
					// Need to write error handling here later, this will do for now
					GD.Print(e.ToString());
				}
			}
		}
	}
	
	private void DDSFixHeader(){
		string[] ddsList = Directory.EnumerateFiles(usrdir, "*.dds", SearchOption.AllDirectories).ToArray();
		foreach (string dds in ddsList){
			using (FileStream fs = File.Open(dds, FileMode.Open, System.IO.FileAccess.ReadWrite, FileShare.ReadWrite)){
				byte[] file = File.ReadAllBytes(dds);
				byte[] headerbytes = {file[0], file[1], file[2]};
				string header = System.Text.Encoding.UTF8.GetString(headerbytes, 0, 3);
				const string valid = "DDS";
				if (header != valid){
					_progress.Text += $"[W] DDS file {dds} has invalid header magic. Skipping.\n";
					return;
				}
				GD.Print(Path.GetFileName(dds));
				if (Path.GetFileName(dds).Contains("d5comp")){
					GD.Print($"d5comp {dds}");
					fs.Seek(8, SeekOrigin.Begin);
					fs.Write(ddscomp);
					fs.Seek(20, SeekOrigin.Begin);
					fs.Write(d5comp);
				}
				else if (Path.GetFileName(dds).Contains("nocomp")){
					GD.Print($"nocomp {dds}");
					fs.Seek(8, SeekOrigin.Begin);
					fs.Write(ddscomp);
					fs.Seek(20, SeekOrigin.Begin);
					fs.Write(nocomp);
				}
				else{
					_progress.Text += $"[W] Could not determine compression type of file {dds}. Skipping.\n";
				}
			}
		}
	}
	
	/* Original Code by Bekzii, ported with permission */
	
	private void InjectModels(){
		_progress.Text += $"[W] InjectModels(): TODO.\n";
		// Get list of files in rom folder
		string[] files = Directory.GetFiles(Path.Combine(usrdir, "rom"));
		// Apply in alphabetical order
		Array.Sort(files);
		foreach (string mod in files)
		{
			string modpath = mod; // patch
			string romdir = Path.Combine(usrdir, "rom");
			string stf_rom = Path.Combine(romdir, $"{game}_rom");
			
			if (Path.GetExtension(modpath) != ".stfmdl")
			  return;
		}
	}
	
	const string EXT_MODEL = ".stfmdl";
	const string EXT_TH = ".stfmat";
	const string EXT_TP = ".stfuvs";
	
	const uint MODEL_TABLE_ADDR = 0xE0004;
	const uint POL_BASE_ADDR = 0xEC25E0;
	const uint TEX_BASE_ADDR = 0x790000;
	
	private static uint Swap32(uint x) {
		return ((((x) & 0xff000000) >> 24) | (((x) & 0x00ff0000) >>  8) | (((x) & 0x0000ff00) <<  8) | (((x) & 0x000000ff) << 24));
	}
	
	private static uint GetModelTableAddr(uint i){
		return MODEL_TABLE_ADDR + (i*0x10);
	}
	
	private uint GetTexAddr(uint addr){
		addr /= 2;
		addr = Swap32(addr);
		return addr;
	}
	
	private void InjectData(string file, uint addr, byte[] data){
		using (FileStream fs = File.Open(file, FileMode.Open, System.IO.FileAccess.ReadWrite, FileShare.ReadWrite)){
			fs.Seek(addr, SeekOrigin.Begin);
			fs.Write(data);
		}
	}
}
