80038 CONSTANT CharSelect
80000 CONSTANT RoundNames

0 VALUE select-max

S" raylib.dll" DLOPEN VALUE raylib
S" tinyfiledialogs64.dll" DLOPEN VALUE tiny
S" user32.dll" DLOPEN VALUE user32

S" InitWindow" raylib DLSYM ACALL: init-window { $.title i.height i.width -- }
S" BeginDrawing" raylib DLSYM ACALL: begin-drawing { -- }
S" EndDrawing" raylib DLSYM ACALL: end-drawing { -- }
S" ClearBackground" raylib DLSYM ACALL: clear-background { i.color -- }
S" DrawPixel" raylib DLSYM ACALL: draw-pixel { i.color i.y i.x -- }
S" LoadTexture" raylib DLSYM ACALL: load-texture { $.other a.buffer -- }
S" LoadImage" raylib DLSYM ACALL: load-image { $.other a.buffer -- }
S" LoadTextureFromImage" raylib DLSYM ACALL: load-texture-from-image { a.image a.buffer -- }
S" DrawTexture" raylib DLSYM ACALL: draw-texture { a.color i.y i.x a.texture -- }
S" DrawTextureEx" raylib DLSYM ACALL: draw-texture-ex { i.color i.scale i.rot i.vec a.texture -- }
S" WindowShouldClose" raylib DLSYM ACALL: window-should-close { -- i.bool }
S" CloseWindow" raylib DLSYM ACALL: close-window { -- }
S" LoadDirectoryFiles" raylib DLSYM ACALL: DIR { $.dir a.buf -- }
S" GetDirectoryFileCount" raylib DLSYM ACALL: FILE-COUNT { $.dir -- i.count }
S" GenImageColor" raylib DLSYM ACALL: generate-image { i.color i.h i.w a.buffer -- }
S" GetMousePosition" raylib DLSYM ACALL: mouse { -- a.vec }
S" CheckCollisionPointRec" raylib DLSYM ACALL: check-collision { a.rec a.point -- i.res }
S" DrawRectangleRec" raylib DLSYM ACALL: draw-rec { i.color a.rec -- }
S" IsMouseButtonPressed" raylib DLSYM ACALL: check-click { i.mouse -- i.res }
S" IsMouseButtonDown" raylib DLSYM ACALL: check-down { i.mouse -- i.res }
S" UnloadTexture" raylib DLSYM ACALL: unload-texture { a.texture -- }
S" DrawText" raylib DLSYM ACALL: draw-text { i.color i.fontsize i.y i.x $.text -- }
S" tinyfd_openFileDialog" tiny DLSYM ACALL: browse-file { i.multiple $.desc a.patterns i.numpatterns $.defpath $.title -- a.path } 
S" MessageBoxA" user32 DLSYM ACALL: message-box { i.type $.title $.body a.owner -- }
S" ExportImage" raylib DLSYM ACALL: export-image { $.name a.image -- }

: 4DROP 2DROP 2DROP ;
: RDUP R> DUP >R ;

0 VALUE 'string1
0 VALUE #string1
0 VALUE 'string2
0 VALUE #string2
: S+ 
	TO #string2 TO 'string2
	TO #string1 TO 'string1
	'string1 PAD #string1 CMOVE
	'string2 PAD #string1 + #string2 CMOVE
	PAD #string1 #string2 +
;

DECIMAL

CREATE 'filter 50 ALLOT
S" *.png" 'filter SWAP CMOVE

CREATE 'filters 'filter , 0 ,

S" StF Texture Tool by seek+ and Jeffrey/Bekzii the Fighter" 480 640 init-window

: alpha 255 24 LSHIFT ;
: blue 255 16 LSHIFT ;
: green 255 8 LSHIFT ;
: red 255 ;


: a: BL PARSE EVALUATE 24 LSHIFT + ;
: b: BL PARSE EVALUATE 16 LSHIFT + ;
: g: BL PARSE EVALUATE  8 LSHIFT + ;
: r: BL PARSE EVALUATE ;
: white [ r: 255 g: 255 b: 255 a: 255 ] LITERAL ;
: black [ r: 0 g: 0 b: 0 a: 255 ] LITERAL ;
: dred [ r: 255 g: 0 b: 0 a: 255 ] LITERAL ;
: dgreen [ r: 0 g: 255 b: 0 a: 255 ] LITERAL ;


CREATE pixel-buffer 8192 ALLOT
CREATE palette-buffer 256 ALLOT
CREATE order-buffer 512 ALLOT

HEX

: mouse-x mouse FFFF0000 AND 16 RSHIFT ;
: mouse-y mouse FFFF AND ;

0 VALUE target-pixels
0 VALUE target-palette
0 VALUE target-order
0 VALUE target-palette-index
0 VALUE diff-palette-index

S" Data path" TYPE CR

S" paths.ini" INCLUDED

\ File reading
0 VALUE fh

80000 CONSTANT ep-base-table

CREATE ep-buffer C00 ALLOT

: ep-open ep-path R/W OPEN-FILE THROW TO fh ;
: ep-close fh CLOSE-FILE DROP ;
: ep-reposition 80000 0 fh REPOSITION-FILE THROW ;
: ep-read ep-open ep-reposition ep-buffer B5C fh READ-FILE THROW DROP ep-close ;
: rom-open data-path R/W OPEN-FILE THROW TO fh ;
: pixel-reposition target-pixels 0 fh REPOSITION-FILE THROW ;
: palette-reposition target-palette 0 fh REPOSITION-FILE THROW ;
: order-reposition target-order 0 fh REPOSITION-FILE THROW ;
: rom-close fh CLOSE-FILE DROP ;
: pixel-read pixel-reposition pixel-buffer 2000 fh READ-FILE THROW DROP ;
: palette-read palette-reposition palette-buffer 100 fh READ-FILE THROW DROP ;
: order-read order-reposition order-buffer 200 fh READ-FILE THROW DROP ;
: rom-read rom-open pixel-read palette-read order-read rom-close ;
: data-read rom-open 0 fh REPOSITION-FILE THROW PAD 4 fh READ-FILE THROW DROP PAD L@ rom-close ;
: rom-write fh WRITE-FILE THROW ;
ep-read rom-read

: ep@ 80000 - ep-buffer + L@ FFFFFF AND ;
: field-ep@ 4 * 80300 + ep@ ;

7 CONSTANT css-base
10C CONSTANT tails-field


: Base: HEX BL PARSE EVALUATE , select-max 1 + TO select-max ;
: Base-Index: DECIMAL BL PARSE EVALUATE , ;
: Ep-Index: DECIMAL BL PARSE EVALUATE , ;

0 VALUE current-selection


\ 0 VALUE 'pixels
CREATE lods 2048 ALLOT
S" lods.png" lods load-image
lods @ VALUE 'pixels
lods VALUE clods


CREATE gen 4096 ALLOT
: generate white ROT ROT gen generate-image ;

16 88 generate
gen TO clods
gen @ TO 'pixels

lods VALUE CharacterIcon
gen VALUE RoundName

CREATE 'selections
S" textures.ini" INCLUDED
HEX


\ Read cached pixel data
0 VALUE x
0 VALUE y
0 VALUE ip
0 VALUE tile-ip
0 VALUE tile-subtract
0 VALUE pixel-low
0 VALUE pixel-high
0 VALUE place-x
0 VALUE offset-y
0 VALUE surface-ip
CREATE 'resolution 1 ALLOT
: set-resolution 'resolution C! 'resolution 1 + C! ;
: resolution-x 'resolution C@ ;
: resolution-y 'resolution 1 + C@ ;
: resolution 'resolution C@ 'resolution 1 + C@ * ;


: select 3 CELLS * 'selections + ;
: base 0 CELLS + @ ;
: base-index 1 CELLS + @ ;
: ep-index 2 CELLS + @ ;
: print-top DUP . ;
: to-target
	\ Get the pixel address base and duplicate it
	current-selection select base ep@ DUP
	\ Read the very first palette index and store it for diffing
	4 + data-read FFFFFF AND C RSHIFT TO diff-palette-index
	\ Reposition to the base-index we want and duplicate it
	current-selection select base-index 8 * + DUP
	\ Read the pixel address
	data-read FFFFFF AND 4 + TO target-pixels
	\ Read the palette index
	4 + data-read FFFFFF AND C RSHIFT TO target-palette-index
	\ Read the base palette address
	current-selection select base 4 + ep@
	\ Subtract the indices and move down by 64 bytes to the calculated target palette
	target-palette-index diff-palette-index - 20 * + 8 + TO target-palette
	\ Get the order address
	current-selection select ep-index field-ep@ TO target-order
	rom-read
	order-buffer 8 + L@ 8 * order-buffer 4 + L@ 8 * set-resolution
	resolution-x resolution-y generate
	gen @ TO 'pixels
	gen TO clods
;

: select! TO current-selection to-target ;


DECIMAL


: ip+ ip 1 + TO ip ;
: pixel@ pixel-buffer ip + C@ ;
: >surface-ip SWAP resolution-x * + 4 * TO surface-ip ;
: surface-color! 
	'pixels surface-ip + 0 + C!
	'pixels surface-ip + 1 + C!
	'pixels surface-ip + 2 + C!
	'pixels surface-ip + 3 + C!
;
HEX
0 VALUE color-16
: nibble-high@ F0 AND 4 RSHIFT ;
: nibble-low@ 0F AND ;

7800 VALUE blue-mask
1F VALUE low-mask
DECIMAL
: scale-color S>D D>F F# 31.0 F/ F# 255.0 F* F>D DROP ;
: palette-rgba@ 
	DUP palette-buffer SWAP 2 * + C@
	SWAP palette-buffer SWAP 2 * + 1 + C@ 8 LSHIFT
	+ TO color-16
	color-16 [ HEX ] 8000 AND 8000 / FF * [ DECIMAL ]
	color-16 blue-mask AND 10 RSHIFT scale-color
	color-16 5 RSHIFT low-mask AND scale-color
	color-16 low-mask AND scale-color
;

: tile-ip@ 
	12 tile-ip + order-buffer + W@
;

: order-x@ order-buffer 4 + L@ 8 * ;
: order-y@ order-buffer 8 + L@ 8 * ;

: tile-ip+ tile-ip 2 + TO tile-ip ;

: two 2 0 DO 
	pixel@ TO pixel-low ip+
	pixel@ TO pixel-high ip+
	
	x resolution-x MOD TO place-x
	x resolution-x / 8 * TO offset-y
	y offset-y + resolution-y < IF
		y offset-y + place-x >surface-ip
		pixel-high nibble-high@ palette-rgba@
		surface-color!
	THEN

	x 1 + resolution-x MOD TO place-x
	x 1 + resolution-x / 8 * TO offset-y
	y offset-y + resolution-y < IF
		y offset-y + place-x >surface-ip
		pixel-high nibble-low@ palette-rgba@
		surface-color!
	THEN

	x 2 + resolution-x MOD TO place-x
	x 2 + resolution-x / 8 * TO offset-y
	y offset-y + resolution-y < IF
		y offset-y + place-x >surface-ip
		pixel-low nibble-high@ palette-rgba@
		surface-color!
	THEN

	x 3 + resolution-x MOD TO place-x
	x 3 + resolution-x / 8 * TO offset-y
	y offset-y + resolution-y < IF
		y offset-y + place-x >surface-ip
		pixel-low nibble-low@ palette-rgba@
		surface-color!
	THEN
	
	x 4 + TO x

	LOOP
;
: eight 
	tile-ip@ tile-subtract -
	32 * TO ip
	tile-ip+
	8 0 DO 
	two
	x 8 - TO x
	y 1 + TO y
	LOOP 
;
: get-ep-index current-selection select ep-index ;
: rom>surface 
	0 TO tile-ip
	order-x@ order-y@ set-resolution
	tile-ip@ [ HEX ] FF AND 80 [ DECIMAL ] >= 0= IF
	tile-ip@ [ HEX ] FF00 [ DECIMAL ] AND TO tile-subtract ELSE
	tile-ip@ [ HEX ] FF00 AND 80 + [ DECIMAL ] TO tile-subtract THEN
	\ 128 TO tile-subtract
	resolution 64 / 0 DO 
	eight
	x 8 + TO x
	y 8 - TO y
	LOOP
;

CREATE clods-tex 128 ALLOT
clods clods-tex load-texture-from-image

\ Injection
0 VALUE color-count
0 VALUE inj-buffer-ip
0 VALUE color-present
0 VALUE color-byte
CREATE 'inject-colors 256 256 * ALLOT
CREATE 'inject-pixels 512 512 * ALLOT
CREATE 'inject-palette 32 ALLOT
: inj-buffer-ip+ inj-buffer-ip 1 + TO inj-buffer-ip ;
: reset-inject-colors 0 TO color-count 64 64 * 0 DO 0 'inject-colors I 4 * + L! LOOP ;
: reset-inject-palette 32 0 DO 0 'inject-palette I + C! LOOP ;
: reset-inject-pixels 512 512 * 0 DO 0 'inject-pixels I + C! LOOP ;
: color-present?
	0 TO color-present
	64 64 * 0 DO DUP 'inject-colors I 4 * + L@
	= IF -1 TO color-present LEAVE THEN LOOP DROP color-present
;
: color-count+ color-count 1 + TO color-count ;
: color-count-blown? color-count 16 > ;
: append-color 'inject-colors color-count 4 * + L! color-count+ ;
: palette-success 
	S" The provided texture is injectable." TYPE CR
	S" Color count: " TYPE color-count S>D <# #S #> TYPE CR
;
: palette-failure 
	S" The provided texture is not injectable, it has more than 16 colors. " TYPE CR
	S" Color count: " TYPE color-count S>D <# #S #> TYPE CR
;
: pixels 4 * ;
: extract-image-colors
	reset-inject-colors
	resolution-x resolution-y * 0 DO
	'pixels I pixels + L@ color-present? 0= IF
	'pixels I pixels + L@ append-color THEN
	LOOP
	color-count-blown? IF palette-failure ELSE palette-success THEN
;

: extract
	0 TO x
	0 TO y
	0 TO ip
	0 TO pixel-low
	0 TO pixel-high
	0 TO place-x
	0 TO offset-y
	0 TO surface-ip
	rom-read
	rom>surface
	clods clods-tex load-texture-from-image
	extract-image-colors
;

: >16-red [ HEX ] S>D D>F F# 255.0 F/ F# 31.0 F* F>D DROP 1F AND [ DECIMAL ] ;
: >16-green [ HEX ] S>D D>F F# 255.0 F/ F# 31.0 F* F>D DROP 1F AND 5 LSHIFT [ DECIMAL ] ;
: >16-blue [ HEX ] S>D D>F F# 255.0 F/ F# 31.0 F* F>D DROP 1F AND A LSHIFT [ DECIMAL ] ;
: >16-alpha [ HEX ] FF / 8000 * [ DECIMAL ] ;
: palette-word 2 * ;
: >color-index
	16 0 DO DUP 'inject-colors I pixels + L@
	= IF DROP I LEAVE THEN LOOP
;
: convert-image-colors
	reset-inject-palette
	16 0 DO
	'inject-colors I pixels + C@ >16-red
	'inject-colors I pixels + 1 + C@ >16-green +
	'inject-colors I pixels + 2 + C@ >16-blue +
	'inject-colors I pixels + 3 + C@ >16-alpha +
	'inject-palette I palette-word + W!
	LOOP
;
: color@ resolution-x * + 4 * + L@ ;
: in-two
	2 0 DO
	'pixels x 2 + y color@ >color-index
	4 LSHIFT
	'pixels x 3 + y color@ >color-index
	+ 'inject-pixels inj-buffer-ip + C! inj-buffer-ip+

	'pixels x y color@ >color-index
	4 LSHIFT
	'pixels x 1 + y color@ >color-index
	+ 'inject-pixels inj-buffer-ip + C! inj-buffer-ip+
	x 4 + TO x
	LOOP
;
: in-eight
	8 0 DO
	in-two
	x 8 - TO x
	y 1 + TO y
	LOOP
;

: emit-pixels
	0 TO x 0 TO y 0 TO inj-buffer-ip
	reset-inject-pixels
	resolution 64 / 0 DO
	in-eight
	x 8 + TO x
	y 8 - TO y
	x resolution-x >= IF 0 TO x y 8 + TO y THEN
	LOOP
;

: >string
	S>D <# #S #>
;

: export-string 
	HEX
	current-selection select base >string
	S" _" S+ 
	current-selection select base-index >string S+
	S" _" S+ 
	current-selection select ep-index >string S+
	S" _" S+
        resolution-x 8 / >string S+
	S" _" S+
	resolution-y 8 / >string S+
	S" .png" S+
	DECIMAL
;
: id-string 
	HEX
	current-selection select base >string
	S" _" S+ 
	current-selection select base-index >string S+
	S" _" S+ 
	current-selection select ep-index >string S+
	S" _" S+
        resolution-x 8 / >string S+
	S" _" S+
	resolution-y 8 / >string S+
	S" .stfui" S+
	DECIMAL
;
0 VALUE id-fh
: id-open
	id-string W/O CREATE-FILE THROW TO id-fh
;
: id-reposition 0 id-fh REPOSITION-FILE THROW ;
: id-write id-fh WRITE-FILE THROW ;
: id-close
	id-fh CLOSE-FILE THROW
;
CREATE 'msg-buffer 1024 ALLOT
0 VALUE #msg-buffer
: inject
	extract-image-colors
	color-count-blown? 0= IF
	convert-image-colors
	emit-pixels
	id-open
	'inject-palette 32 id-write
	'inject-pixels resolution-x resolution-y * 2 / id-write
	id-close
	id-string DUP TO #msg-buffer 'msg-buffer SWAP CMOVE
	0 S" Generation Status" 
	S" Generation was successful. File generated as: "
	'msg-buffer #msg-buffer S+
	0 message-box

	\ rom-open palette-reposition 'inject-palette 32 rom-write rom-close \ Direct palette
	\ order-buffer 12 + L@ 255 AND 32 * target-pixels + TO target-pixels
	\ rom-open pixel-reposition 'inject-pixels resolution-x resolution-y * 2 / rom-write rom-close \ Direct pixels
	ELSE
	16 S" Generation Status" S" Generation failed. The provided image has too many colors: "
	color-count <# #S #> S+
	0 message-box
	THEN
;

\ Draw loop

DECIMAL
48 VALUE tex-x
48 VALUE tex-y
100 VALUE scale-x
100 VALUE scale-y


CREATE clods-vec F# 1.0 SF, F# 1.0 SF,
CREATE clods-vec3 F# 1.0 F, F# 1.0 F,
CREATE clods-vec2 F# 0.5 SF, F# 0.5 SF,

: scale >R >R DUP 4 + R> SWAP L! 8 + R> SWAP L! ;
: pos TO tex-x TO tex-y ;

0 VALUE should-pause
0 VALUE 'button-name
0 VALUE #button-name
: button-name 'button-name #button-name ;
: button-name+ button-name S+ ;
: make-button 
	BL PARSE TO #button-name TO 'button-name
	S" CREATE " button-name+ S" -rec" S+ EVALUATE SF, SF, SF, SF,
	S" VALUE #" button-name+ S" -text" S+ EVALUATE
	S" VALUE '" button-name+ S" -text" S+ EVALUATE
	S" : " button-name+ S" -hover " S+ button-name+ S" -rec mouse check-collision ; " S+ EVALUATE
	S" : " button-name+ S" -down " S+ button-name+ 
	S" -rec mouse check-collision 0 check-down AND ; " S+ EVALUATE
	S" : " button-name+ S" -click " S+ button-name+ S" -rec mouse check-collision 0 check-click AND ; " S+ EVALUATE
	S" 0 VALUE " button-name+ S" -color" S+ EVALUATE 
	S" : " button-name+ S" -draw " S+
	button-name+ S" -color " S+ 
	button-name+ S" -rec draw-rec ; " S+ EVALUATE
;

F# 50.0 F# 150.0 F# 400.0 F# 50.0 make-button button
F# 50.0 F# 150.0 F# 400.0 F# 450.0 make-button button2

F# 50.0 F# 150.0 F# 400.0 F# 250.0 make-button test-button

3 VALUE preview-scale
: rescale
	clods-tex resolution-x preview-scale * resolution-y preview-scale * scale
	480 2 / resolution-y 2 / preview-scale * -
	640 2 / resolution-x 2 / preview-scale * -
	pos
;

: button
	button-hover IF 
	[ r: 200 g: 200 b: 200 a: 255 ] LITERAL TO button-color ELSE
	[ r: 255 g: 255 b: 255 a: 255 ] LITERAL TO button-color THEN

	button-down IF 
	[ r: 255 g: 255 b: 0 a: 255 ] LITERAL TO button-color THEN

	button-click IF 
	clods-tex unload-texture
	current-selection 1 - select-max MOD select! extract
	rescale
	THEN
	button-draw
;

: button2
	button2-hover IF 
	[ r: 200 g: 200 b: 200 a: 255 ] LITERAL TO button2-color ELSE
	[ r: 255 g: 255 b: 255 a: 255 ] LITERAL TO button2-color THEN

	button2-down IF 
	[ r: 255 g: 255 b: 0 a: 255 ] LITERAL TO button2-color THEN

	button2-click IF 
	clods-tex unload-texture
	current-selection 1 + select-max MOD select! extract
	rescale
	THEN
	button2-draw
;

: test-button
	test-button-hover IF 
	[ r: 200 g: 200 b: 200 a: 255 ] LITERAL TO test-button-color ELSE
	[ r: 255 g: 255 b: 255 a: 255 ] LITERAL TO test-button-color THEN

	test-button-down IF 
	[ r: 255 g: 255 b: 0 a: 255 ] LITERAL TO test-button-color THEN

	test-button-click IF 
	clods-tex unload-texture
	current-selection select base-index 1 +
	DUP .
	current-selection select 1 CELLS + !
	to-target
        extract
	rescale
	THEN
	test-button-draw
;

F# 40.0 F# 100.0 F# 350.0 F# 275.0 make-button browse-button

0 VALUE 'base-string
0 VALUE #base-string
: find-string-term
	TO 'base-string
	8192 0 DO
		'base-string I + C@ 0= IF
			I TO #base-string
			LEAVE
		THEN
	LOOP
;

0 VALUE 'inject-png
0 VALUE #inject-png
: import
	clods-tex unload-texture
	'inject-png #inject-png lods load-image
	lods @ TO 'pixels
	lods TO clods
	clods clods-tex load-texture-from-image
	clods-tex 8 + L@ clods-tex 4 + L@ set-resolution
	extract-image-colors
	rescale
;

: browse-button
	browse-button-hover IF 
	[ r: 200 g: 200 b: 200 a: 255 ] LITERAL TO browse-button-color ELSE
	[ r: 255 g: 255 b: 255 a: 255 ] LITERAL TO browse-button-color THEN

	browse-button-down IF 
	[ r: 255 g: 255 b: 0 a: 255 ] LITERAL TO browse-button-color THEN

	browse-button-click IF 
	0 S" Replacement PNG" 'filters 1 S" " S" Replacement Browser" browse-file
	DUP 0= 0= IF
	  find-string-term
	  'base-string TO 'inject-png
	  #base-string TO #inject-png
	  import
	  THEN
	  DROP
	THEN
	browse-button-draw
;

F# 40.0 F# 155.0 F# 400.0 F# 250.0 make-button inject-button

: inject-button
	color-count 16 > IF dred TO inject-button-color ELSE
	dgreen TO inject-button-color THEN

	inject-button-hover IF 
	[ r: 200 g: 200 b: 200 a: 255 ] LITERAL TO inject-button-color THEN

	inject-button-down IF 
	[ r: 255 g: 255 b: 0 a: 255 ] LITERAL TO inject-button-color THEN

	inject-button-click IF 
	inject THEN
	inject-button-draw
;

F# 50.0 F# 130.0 F# 200.0 F# 40.0 make-button export-button
CREATE $temp 1024 ALLOT
0 VALUE #$temp
: $temp! TO #$temp $temp #$temp CMOVE ;
: $temp+
  $temp #$temp + SWAP DUP #$temp + TO #$temp CMOVE
;
: $exported S" Export was successful. File exported as: " ;
: export-button
	color-count 16 > IF dred TO export-button-color ELSE
	white TO export-button-color THEN

	export-button-hover IF 
	[ r: 200 g: 200 b: 200 a: 255 ] LITERAL TO export-button-color THEN

	export-button-down IF 
	[ r: 255 g: 255 b: 0 a: 255 ] LITERAL TO export-button-color THEN

	export-button-click IF 
	export-string clods export-image 

	$exported $temp!
	export-string $temp+
	0 S" Export Status" 
	$temp #$temp 0 message-box
	THEN
	export-button-draw
;

: address-text
	white 24 30 50 S" ID: "
	HEX
	current-selection select base >string S+
	S" _" S+
	current-selection select base-index >string S+
	S" _" S+
	current-selection select ep-index >string S+
	S" _" S+
        resolution-x 8 / >string S+
	S" _" S+
	resolution-y 8 / >string S+
	draw-text
	white 24 60 50 S" Resolution: "
	DECIMAL resolution-x >string S+
	S" x" S+
	resolution-y >string S+
	draw-text
	color-count 16 > IF dred ELSE white THEN
	24 90 50 
	color-count 16 > IF S" ERROR: " ELSE S" " THEN
	S" Color Count (Limit of 16): " S+
	color-count >string S+
	draw-text
;
: button-text
  black 24 355 280 S" Replace" draw-text
  black 24 410 255 S" Generate UI" draw-text
  black 24 415 90 S" Back" draw-text
  black 24 415 495 S" Next" draw-text
  black 20 213 45 S" Export PNG" draw-text
;
: draw-loop
       white tex-y tex-x clods-tex draw-texture
       button button2 browse-button inject-button export-button
       \ test-button
       address-text button-text
;

: draw 
	begin-drawing blue clear-background draw-loop end-drawing 
	should-pause 0= 0= IF should-pause PAUSE 0 TO should-pause THEN
;

: main BEGIN draw window-should-close 0= WHILE REPEAT ;

0 select! extract
rescale
main

close-window BYE
