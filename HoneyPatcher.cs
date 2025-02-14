using Godot;
using Gibbed.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using MikuMikuLibrary.Archives;
// using MikuMikuLibrary.Archives.CriMw;
using MikuMikuLibrary.IO;
// using PsarcSharp;
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
	public Label _progress; // Progress label
	[Export]
	public LineEdit _patchname; // Name of patch
	
	string psarc;
	string usrdir;
	string patchname = "Default";
	bool nomods = false;
	
	public override void _Ready(){	
		// Dumb hack. Will be required until I can implement psarc.
		if (!Engine.IsEditorHint()){
			if (OS.GetName() == "macOS")
				Directory.SetCurrentDirectory(Path.Combine(OS.GetExecutablePath(), "../../../.."));
		}
		
		// Signal Connection
		_usrdirdialog.DirSelected += OnUsrdirDialog;
		_install.Pressed += OnInstallPressed;
		_restoreusrdir.Pressed += OnRestoreUsrdirPressed;
		_modsfolder.Pressed += OpenModsFolder;
		_genpatches.Pressed += CreatePatches;
		// _patchname.TextSubmitted += ChangeNameTheSequel;
		
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
		
		if(!Directory.Exists("workbench")){
			Directory.CreateDirectory("workbench");
			_progress.Text = "Created workbench directory.";
		}
		
		if(!Directory.Exists("workbench/original")){
			Directory.CreateDirectory("workbench/original");
			_progress.Text = "Created workbench/original directory.";
		}
		
		if(!Directory.Exists("workbench/modified")){
			Directory.CreateDirectory("workbench/modified");
			_progress.Text = "Created workbench/modified directory.";
		}
		
		if(!Directory.Exists("workbench/patches")){
			Directory.CreateDirectory("workbench/patches");
			_progress.Text = "Created workbench/patches directory.";
		}
		
		// https://github.com/rickyah/ini-parser
		// MIT License
		IniData data = new FileIniDataParser().ReadFile("HoneyConfig.ini");
		usrdir = data["main"]["usrdir"];
		_progress.Text = "loaded HoneyConfig.ini.";
		
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
		// Check for clean copy of stf w/ rom.psarc still intact
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
		
		// Extract rom.psarc - used UnPSARC by NoobInCoding as a base, stripped it down,
		// and turned it into a DLL. It's honestly still really bloated and could do with
		// a bit more cleanup.
		
		PsarcThing.UnpackArchiveFile(psarc_path, Path.Combine(usrdir, "rom"));
		
		_progress.Text = "Extracted rom.psarc.";
		File.Delete(psarc_path);
		_progress.Text = "Extracted and removed rom.psarc.";
		FarcUnpack();
		_progress.Text = "Unpacked farc files.";
		ExtractMods();
		_progress.Text = "Extracted mods.";
		ApplyPatches();
		_progress.Text = "Applied patches.";
		FarcPack();
		_progress.Text = "Repacked farc files.";
		if (nomods)
			ShowError("Success?", "No mods were found, but I extracted rom.psarc for you anyways.");
		else
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
			_progress.Text = "Error restoring files. (Couldn't wipe game files.)";
			return;
		}
		
		// Restore backup
		try{
			CopyFilesRecursively("BACKUP", usrdir);
		}
		catch (Exception e){
			ShowError("Exception", e.ToString());
			_progress.Text = "Error restoring files. (Couldn't copy backup.)";
			return;
		}
		ShowError("Success", "Files restored.");
		_progress.Text = "Restored game files.";
	}

	private void OpenModsFolder(){
		//OS.ShellShowInFileManager("mods", true);
		// This will need some work once we move to using user://
		OS.ShellOpen("mods");
	}
	
	private void CreatePatches(){
		if (_patchname.Text != "")
		  patchname = _patchname.Text;
		List<string> files = new List<string>();
		GD.Print("creating patches");
		string[] roms = {"rom_code1.bin", "rom_data.bin", "rom_ep.bin", "rom_pol.bin", "rom_tex.bin", "string_array_en.bin"};
		foreach (string rawhm in roms){
			if (!File.Exists("workbench/original/" + rawhm)){
				GD.Print("original " + rawhm + " not found");
				ShowError("Error", "Original " + rawhm + "not found.");
				return;
			}
			if (File.Exists("workbench/modified/" + rawhm)){
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
			string patchextension = Path.GetFileNameWithoutExtension(Path.Combine("workbench/original", filename));
			GD.Print("Patch Extension: " + patchextension);
			byte[] original = File.ReadAllBytes(Path.Combine("workbench/original", filename));
			byte[] modified = File.ReadAllBytes(Path.Combine("workbench/modified", filename));
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
			string patch = Path.Combine("workbench/patches", patchname + "." + patchextension);
			string patchloc = patch + ".loc";
			GD.Print("Patch: " + patch);
			GD.Print("Patch Locations: " + patchloc);
			File.WriteAllBytes(patch, changes.ToArray());
			_progress.Text = "Created " + patch + ".";
			File.WriteAllLines(patchloc, locations.ToArray());
			_progress.Text = "Created " + patchloc + ".";
		}
	}
	
	// private void ChangeNameTheSequel(){
		// patchname = _patchname.Text;
		// if (patchname == "")
		  // patchname = "Default";
	// }
	
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
		Array.Sort(files);
		if (files.Length == 0){
			nomods = true;
			return;
		}
		foreach (string mod in files)
		{
			string modpath = mod;
			string romdir = Path.Combine(usrdir, "rom");
			string stf_rom = Path.Combine(romdir, "stf_rom");
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
			string stf_rom = Path.Combine(romdir, "stf_rom");
			string patchdest; // file to be patched
			switch(Path.GetExtension(modpath))
			{
				// Check the file extension, which should be the name of the file you want to patch
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
				// At some point we'll handle these with actual XML extraction/injection!
				// For now, this will do.
				case ".string_array_en":
					patchdest = Path.Combine(romdir, "string_array", "string_array_en.bin");
					break;
				case ".string_array_jp":
					patchdest = Path.Combine(romdir, "string_array", "string_array_jp.bin");
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
}
