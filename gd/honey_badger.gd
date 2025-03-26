extends Sprite2D

@export var explode : Sprite2D
@export var explotano : AnimatedSprite2D
@export var explodesound : AudioStreamPlayer

var rng = RandomNumberGenerator.new()

func _ready() -> void:
	var magic_number = rng.randi_range(1, 10)
	# magic_number = 1
	if magic_number == 1:
		cat_explotano()

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	pass

func cat_explotano():
	explode.visible = true
	explotano.visible = true
	visible = false
	explotano.play()
	explodesound.play()

func _on_explotano_animation_finished() -> void:
	explotano.visible = false
