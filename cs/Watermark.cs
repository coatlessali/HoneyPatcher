using Godot;

public partial class Watermark : Sprite2D{
	[Export]
	public RichTextLabel Progress;
	private RandomNumberGenerator rng = new RandomNumberGenerator();

	public override void _Ready(){
		rng.Randomize();
		int magicNumber = rng.RandiRange(1, 69420);
		if (magicNumber == 67 && OS.GetName() == "Windows"){
			Progress.AppendText("[W] Could not validate license. Please go to settings and activate HoneyPatcher.");
			Visible = true;
		}
	}
}
