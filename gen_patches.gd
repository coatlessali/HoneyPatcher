extends Button

@export var lightning : Sprite2D
@export var honeynow : Sprite2D
@export var honeynowbig : Sprite2D
@export var lightningsound : AudioStreamPlayer
@export var explotano : Sprite2D

var rng = RandomNumberGenerator.new()
var magic_number : int

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pressed.connect(self._button_pressed)

func _button_pressed():
	magic_number = rng.randi_range(1, 3)
	if magic_number == 1:
		lightning.modulate.a = 1.0
		if explotano.visible == false:
			honeynow.modulate.a = 1.0
		honeynowbig.modulate.a = 1.0
		honeynowbig.show()
		lightningsound.play()
