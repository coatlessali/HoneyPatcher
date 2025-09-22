extends Sprite2D


# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	modulate.a = 0.0


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _physics_process(_delta: float) -> void:
	if modulate.a > 0:
		modulate.a -= 0.0025
