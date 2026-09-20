CREATE 'buf 1024 ALLOT
80038 CONSTANT CharSelect
F4000 CONSTANT CharSelectPalettes
F5000 CONSTANT CharSelectIndices
348 VALUE #CharSelectPalettes
78 VALUE #CharSelectIndices
CREATE CharSelectOrderIndex 21 L,

80008 CONSTANT RoundNames
F6000 CONSTANT RoundNamesPalettes
F7000 CONSTANT RoundNamesIndices
468 VALUE #RoundNamesPalettes \ 24C works 802ECC
B0 VALUE #RoundNamesIndices
CREATE RoundNamesOrderIndex 26 L, \ 12 is A40 13 is A60

F8000 VALUE 'order-next
0 VALUE order-index
10ED00 VALUE 'pixel-next

0 VALUE rom-data
803888 VALUE 'from
#CharSelectPalettes VALUE #from
CharSelectPalettes VALUE 'to
DECIMAL

( Reset rom_data from the backup )
\ : reset S" /home/arch/reset-rom-data.sh" SYSTEM ;
S" paths.ini" INCLUDED

( Move character icon palettes )
: open data-path R/W OPEN-FILE THROW TO rom-data ;
: open-ep ep-path R/W OPEN-FILE THROW TO rom-data ;
: repos-read 'from S>D rom-data REPOSITION-FILE THROW ;
: repos-write 'to S>D rom-data REPOSITION-FILE THROW ;
: read 'buf #from rom-data READ-FILE THROW DROP ;
: write repos-write 'buf #from rom-data WRITE-FILE THROW ;
: close rom-data CLOSE-FILE THROW ;
: copy repos-read read repos-write write ;
\ reset
open copy close
( Move character icon pixel indices )
HEX
801594 TO 'from
#CharSelectIndices TO #from
CharSelectIndices TO 'to
open copy close
DECIMAL

( Move nameplate palettes )
HEX
802ECC TO 'from
#RoundNamesPalettes TO #from
RoundNamesPalettes TO 'to
open copy close
DECIMAL

( Move nameplate pixel indices )
HEX
801420 TO 'from
#RoundNamesIndices TO #from
RoundNamesIndices TO 'to
open copy close
DECIMAL

( Patch the data nameplate addresses )
HEX
CREATE 'patch 8 ALLOT
0 VALUE patch-here
: patch, 'patch patch-here + C! patch-here 1 + TO patch-here ;
: set-patch-nameplates
  0 TO patch-here
  \ Switch this to bit shifting later
  02 0F 60 00 02 0F 70 00 8 0 DO patch, LOOP
;
: write-patch-data open repos-write 'patch 8 rom-data WRITE-FILE THROW close ;
set-patch-nameplates
\ 80000C TO 'to write-patch-data
\ 80001C TO 'to write-patch-data

( Patch the ep nameplate addresses )
: write-patch-ep open-ep repos-write 'patch 8 rom-data WRITE-FILE THROW close ;
RoundNames TO 'to write-patch-ep

( Patch the ep character icon addresses )
: set-patch-character-icons
  0 TO patch-here
  02 0F 40 00 02 0F 50 00 8 0 DO patch, LOOP
;
set-patch-character-icons
CharSelect TO 'to write-patch-ep

( Patch in the .stfuis )
0 VALUE process-list
CREATE 'list-current 64 ALLOT
CREATE 'check 1 ALLOT
0 VALUE list-here
0 VALUE base
0 VALUE index
0 VALUE ep-index
0 VALUE size-x
0 VALUE size-y
: reset-current 64 0 DO 0 I 'list-current + C! LOOP 0 TO list-here ;
: list-here? list-here DUP 1 + TO list-here ;
: $process-list S" ./process-list" ;
: open-list $process-list R/W OPEN-FILE THROW TO process-list ;
: check 'check 1 process-list READ-FILE THROW ;
: read-list 'check C@ 'list-current list-here? + C! ;
: close-list process-list CLOSE-FILE THROW ;

: get-underscore-loop
  BEGIN
  check DROP
  [CHAR] _ 'check C@ = IF EXIT THEN
  read-list
  AGAIN
;
: get-period-loop
  BEGIN
  check DROP
  [CHAR] . 'check C@ = IF EXIT THEN
  read-list
  AGAIN
;
: get-address
  reset-current
  get-underscore-loop
  'list-current list-here EVALUATE TO base
  base .
;
: get-index
  reset-current
  get-underscore-loop
  'list-current list-here EVALUATE TO index
  index .
;
: get-ep-index
  reset-current
  get-underscore-loop
  'list-current list-here EVALUATE TO ep-index
  ep-index .
;
: get-x
  reset-current
  get-underscore-loop
  'list-current list-here EVALUATE TO size-x
  size-x .
;
: get-y
  reset-current
  get-period-loop
  'list-current list-here EVALUATE TO size-y
  size-y .
;
: get-space?
  BEGIN
  check 0= IF -1 EXIT THEN
  A 'check C@ = IF 
    check 0= IF -1 EXIT THEN
    process-list FILE-POSITION THROW DROP 1 - S>D process-list REPOSITION-FILE THROW
    0 EXIT
  THEN
  AGAIN
;
: grab-next
  get-address
  get-index
  get-ep-index
  get-x
  get-y
;
CREATE $ui-file 40 ALLOT
0 VALUE #ui-file
0 VALUE #temp
0 VALUE ui-file
CREATE 'ui-palette 20 ALLOT
20 VALUE #ui-palette
CREATE 'ui-pixels 1000 ALLOT
0 VALUE #ui-pixels
: $file-name $ui-file #ui-file ;
: make-file-name
  base S>D <# #S #> TO #ui-file
  $ui-file #ui-file CMOVE
  [CHAR] _ $ui-file #ui-file + C!
  #ui-file 1 + TO #ui-file

  index S>D <# #S #> TO #temp
  $ui-file #ui-file + #temp CMOVE
  #ui-file #temp + TO #ui-file
  [CHAR] _ $ui-file #ui-file + C!
  #ui-file 1 + TO #ui-file

  ep-index S>D <# #S #> TO #temp
  $ui-file #ui-file + #temp CMOVE
  #ui-file #temp + TO #ui-file
  [CHAR] _ $ui-file #ui-file + C!
  #ui-file 1 + TO #ui-file

  size-x S>D <# #S #> TO #temp
  $ui-file #ui-file + #temp CMOVE
  #ui-file #temp + TO #ui-file
  [CHAR] _ $ui-file #ui-file + C!
  #ui-file 1 + TO #ui-file

  size-y S>D <# #S #> TO #temp
  $ui-file #ui-file + #temp CMOVE
  #ui-file #temp + TO #ui-file

  S" .stfui" TO #temp
  $ui-file #ui-file + #temp CMOVE
  #ui-file #temp + TO #ui-file
;
: open-ui-file
  $file-name R/W OPEN-FILE THROW TO ui-file
;
: read-palette
  'ui-palette 20 ui-file READ-FILE THROW DROP
;
: read-pixels
  BEGIN
  'ui-pixels #ui-pixels + 8 ui-file READ-FILE THROW
  DUP #ui-pixels + TO #ui-pixels
  0= UNTIL
;
: close-ui-file ui-file CLOSE-FILE THROW ;
: process-ui-file
  make-file-name
  open-ui-file
  read-palette
  0 TO #ui-pixels
  read-pixels
  close-ui-file
;
0 VALUE 'match-palette
0 VALUE #match-palette
0 VALUE 'match-indices
0 VALUE #match-indices
CREATE match-palette-count 4 ALLOT
: match
  base CharSelect = IF 
    CharSelectIndices TO 'match-indices
    #CharSelectIndices TO #match-indices
    #CharSelectPalettes TO #match-palette
    CharSelectPalettes TO 'match-palette
    #CharSelectIndices 8 + TO #CharSelectIndices
    #CharSelectPalettes 40 + TO #CharSelectPalettes
    CharSelectOrderIndex TO order-index
    EXIT THEN
  base RoundNames = base 80000 = OR IF
    RoundNamesIndices TO 'match-indices
    #RoundNamesIndices TO #match-indices
    #RoundNamesPalettes TO #match-palette
    RoundNamesPalettes TO 'match-palette
    #RoundNamesIndices 8 + TO #RoundNamesIndices
    #RoundNamesPalettes 40 + TO #RoundNamesPalettes
    RoundNamesOrderIndex TO order-index
    EXIT THEN
  CR S" Couldn't match palette to base!" TYPE CR

  CharSelectIndices TO 'match-indices
  #CharSelectIndices TO #match-indices
  #CharSelectPalettes TO #match-palette
  CharSelectPalettes TO 'match-palette
  #CharSelectIndices 8 + TO #CharSelectIndices
  #CharSelectPalettes 40 + TO #CharSelectPalettes
  CharSelectOrderIndex TO order-index
;
: inc-palettes
  open
  'match-palette 4 + S>D rom-data REPOSITION-FILE THROW
  match-palette-count 4 rom-data READ-FILE THROW DROP
  match-palette-count L@ 20 + match-palette-count L!

  'match-palette 4 + S>D rom-data REPOSITION-FILE THROW
  match-palette-count 4 rom-data WRITE-FILE THROW
  close
;
: append-palette
  open
  'match-palette #match-palette + S>D rom-data REPOSITION-FILE THROW
  \ Append the palette twice to account for larger icons
  rom-data FILE-POSITION THROW rom-data REPOSITION-FILE THROW
  2 0 DO 'ui-palette #ui-palette rom-data WRITE-FILE THROW LOOP
  close
;
: handle-palette
  inc-palettes
  append-palette
;
CREATE last-index 4 ALLOT
CREATE target-pixels 4 ALLOT

: handle-index
  open
  'match-indices #match-indices + 4 - S>D rom-data REPOSITION-FILE THROW
  last-index 4 rom-data READ-FILE THROW DROP
  last-index L@ 
  DUP 10BF000 = IF 1000 + THEN 2000 + 
  last-index L!
  2 18 LSHIFT 'pixel-next + target-pixels L!
  \ reposition to itself because windows I guess?
  rom-data FILE-POSITION THROW rom-data REPOSITION-FILE THROW
  target-pixels 4 rom-data WRITE-FILE THROW
  last-index L@ .
  last-index 4 rom-data WRITE-FILE THROW
  close
;
CREATE pixel-index 901 L,
: handle-pixels
  handle-index
  open
  'pixel-next S>D rom-data REPOSITION-FILE THROW
  pixel-index 4 rom-data WRITE-FILE THROW
  pixel-index L@ 1 + pixel-index L!
  'ui-pixels #ui-pixels rom-data WRITE-FILE THROW
  rom-data FILE-POSITION THROW DROP TO 'pixel-next
  close
;
CREATE order-header 18000 L,
CREATE temp 4 ALLOT
CREATE order-final 4 ALLOT

: handle-order
  open
  'order-next S>D rom-data REPOSITION-FILE THROW
  order-header 4 rom-data WRITE-FILE THROW
  size-y temp L! temp 4 rom-data WRITE-FILE THROW
  size-x temp L! temp 4 rom-data WRITE-FILE THROW
  size-x size-y * 0 DO
    I order-index L@ 8 LSHIFT + temp L!
    temp 2 rom-data WRITE-FILE THROW
  LOOP
  order-index L@ 1 + order-index L!
  'order-next 2 18 LSHIFT + order-final L!
  rom-data FILE-POSITION THROW DROP TO 'order-next
  close
  open-ep
  ep-index 4 * 80300 + S>D rom-data REPOSITION-FILE THROW
  order-final 4 rom-data WRITE-FILE THROW
  close
;

: skip
  handle-palette
  handle-pixels
  handle-order
  match
;

: char-eleven-fix
  order-index CharSelectOrderIndex =
  order-index L@ 2A =
  AND IF
    skip
  THEN
;

: check-hotfixes
  char-eleven-fix
;

: patch-main
  open-list
  BEGIN
  grab-next
  process-ui-file
  match
  check-hotfixes
  handle-palette
  handle-pixels
  handle-order

  \ Finished processing, check if we're done
  get-space?
  UNTIL
  close-list
;
patch-main
