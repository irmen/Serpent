echo "Compiling .net source (with microsoft windows sdk msbuild)"
msbuild /verbosity:minimal /p:Platform="Any CPU" /p:Configuration="Debug" Serpent.sln /t:Rebuild
copy Serpent\bin\Debug\*.dll build\
copy Serpent\bin\Debug\*.pdb build\
