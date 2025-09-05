extends Label
@export var stf : Sprite2D
@export var fv : Sprite2D
@export var vf2 : Sprite2D
@export var omg : Sprite2D
@export var hp : Node2D

func _ready() -> void:
	# dirty hack to prevent it loading the default of stf
	await get_tree().create_timer(0.1).timeout
	match hp.game:
		"stf":
			stf.show()
			fv.hide()
			vf2.hide()
			omg.hide()
		"fv":
			stf.hide()
			fv.show()
			vf2.hide()
			omg.hide()
		"vf2":
			stf.hide()
			fv.hide()
			vf2.show()
			omg.hide()
		"omg":
			stf.hide()
			fv.hide()
			vf2.hide()
			omg.show()

func _on_popup_menu_id_pressed(id: int) -> void:
	match id:
		0:
			stf.show()
			fv.hide()
			vf2.hide()
			omg.hide()
		2:
			stf.hide()
			fv.show()
			vf2.hide()
			omg.hide()
		1:
			stf.hide()
			fv.hide()
			vf2.show()
			omg.hide()
		3:
			stf.hide()
			fv.hide()
			vf2.hide()
			omg.show()
