RMDIR /Q/S "V:/Projects/Nebula/Nebula_ModPack/Data/Scripts/Scripts/.idea/"
RMDIR /Q/S "V:/Projects/Nebula/Nebula_ModPack/Data/Scripts/Scripts/.vs/"
RMDIR /Q/S "V:/Projects/Nebula/Nebula_ModPack/Data/Scripts/Scripts/.obj/"
RMDIR /Q/S "V:/Projects/Nebula/Nebula_ModPack/Data/Scripts/Scripts/.bin/"
RMDIR /Q/S "V:/Projects/Nebula/Nebula_ModPack/Data/Scripts/Scripts/bin/"
RMDIR /Q/S "V:/Projects/Nebula/Nebula_ModPack/Data/Scripts/Scripts/obj/"
RMDIR /Q/S "V:/Projects/Nebula/Nebula_ModPack/Data/Scripts/Scripts/Specials/Garage/obj/"
RMDIR /Q/S "V:/Projects/Nebula/Nebula_ModPack/Data/Scripts/Scripts/Specials/Garage/bin/"
RMDIR /Q/S "V:/Projects/Nebula/Nebula_ModPack/Data/Scripts/Scripts/.idea/"

DEL /Q/S "V:\Steam\steamapps\workshop\appworkshop_244850.acf"
RMDIR /Q/S "V:/Steam/steamapps/workshop/content/244850/2224913937"
MD "V:/Steam/steamapps/workshop/content/244850/2224913937/"


xcopy /i /s /y "V:\Projects\Nebula\Nebula_ModPack" "V:\Steam\steamapps\workshop\content\244850\2224913937"
pause