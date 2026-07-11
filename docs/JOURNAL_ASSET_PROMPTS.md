# Journal UI image-generation prompts

All four sheets used the supplied Heritage Pages, Coding Reference, and Oracle screenshots as
style/composition references and a flat `#00ff00` chroma background. The shared constraints were:
crisp text-free 2D pixel art; sharp square pixels; warm aged parchment; dark brown outlines; deep
plum; amber-gold double borders; rust-red leather; no antialiasing, blur, modern flat UI, smooth
gradients, generic fantasy filigree, baked labels, watermark, shadows, or cropped assets.

1. **Cards and ribbons:** Generate isolated normal/selected/locked heritage cards, bookmark,
   normal/selected coding rows with plum icon sockets, Oracle topic row, wide title ribbon, assistant
   ribbon, and separator ornament. Keep generous green gutters and empty TMP-safe centers.
2. **Controls and Oracle frames:** Generate isolated previous/next/page-selector controls in normal,
   highlighted, pressed, and disabled states; close button; player and Oracle parchment message
   frames; input frame; text-free wax seal; feather; and vertical scrollbar track/thumb/arrows.
3. **Coding icons:** Generate a 6×3 grid of equally sized plum-and-gold tiles for commands, sensing,
   sequencing, variables, operators, conditionals, loops, lists, functions, nested conditions,
   comments, indentation, booleans, stopping loops, endless driving, command catalog, errors, and
   autopilot. Use symbols only, with no words, letters, or numerals.
4. **Landmarks and Oracle banner:** Generate six isolated historically recognizable architectural
   vignettes in order—Jaro Cathedral, Molo Church, Oton coastal church/river heritage, Tigbauan
   Church, Miag-ao Church, and San Joaquin Church—using a Philippine coastal sunset palette. Add a
   separate wide Oracle night-sky banner with a golden celestial eye and purple crystals.

The retained generated sheets are under `Assets/UI/Sprites/LugarithmUi/Journal/Sources/`; individual
Unity-ready alpha sprites are under the sibling `Parts/` directory.
