# VRCPlusPet
VRChat Mod that uses [MelonLoader](https://github.com/HerpDerpinstine/MelonLoader). <br>
Removes VRC+ advertising (w/o removing "Early Supporter Badge" and "Supporter Badge"), can replace default pet, his phrases and poke sounds (if needed).

![Natsuki](https://i.ibb.co/sbDK5kG/image.png)

<h3>How to install:</h3>

> VRChat game folder: Steam Library -> RMB on VRChat -> Properties -> Local Files -> Browse Local Files.

Just drop "VRCPlusPet.dll" to "VRChat/Mods" folder.
 
<h3>Mod uses launch options:</h3>

> Editing launch options: Steam Library -> RMB on VRChat -> Properties -> Set Launch Options

Options must be added only after: "-pp". <br>
So all options will looks like: "-pp.ads.pet.phs.aud". <br>
If you want to remove some options - then just do it: "-pp.pet.phs". <br>

| Option | Description |
| --- | --- |
| .ads | The mod will not touch any VRC+ advertising |
| .pet | Pet will be replaced |
| .phs | Pet phrases will be replaced |
| .aud | Pet poke sounds will be replaced |


<h3>How to replace Phrases for Pet?</h3>

  1. Use "-mp.phs" option, run VRChat once;
  2. Open VRChat game folder  (Steam Library -> RMB on VRChat -> Properties -> Local Files -> Browse Local Files);
  3. Change files in "UserData/VRCPlusPet_Config/" folder.

  > "normalPhrases.txt" - Random phrases. <br>
  > "pokePhrases.txt" - Random Phrases when poking.


<h3>How to replace Pet?</h3>

  > Mod currently supports only ".png" image files. <br>
  > Image file name must be "pet.png". <br>
  
  1. Use ".pet" option;
  2. Open VRChat game folder  (Steam Library -> RMB on VRChat -> Properties -> Local Files -> Browse Local Files);
  3. Drop image file to "UserData/VRCPlusPet_Config/" folder (make sure that file has name "pet.png").
  
<h3>How to replace Poke Sounds?</h3>

  > Mod currently supports only ".ogg/.wav" sound files. <br>
  > You can use any names of sound files. <br>
  
  1. Use ".aud" option, run VRChat once;
  2. Open VRChat game folder  (Steam Library -> RMB on VRChat -> Properties -> Local Files -> Browse Local Files);
  3. Drop audio files to "UserData/VRCPlusPet_Config/audio/" folder.

<h3>TODO</h3>
Nothing
