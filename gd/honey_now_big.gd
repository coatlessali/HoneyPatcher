extends Sprite2D


# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	hide()
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _physics_process(_delta: float) -> void:
	pass
	if modulate.a > 0.0:
		modulate.a -= 0.025
