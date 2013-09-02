echo "Compiling .net source (with microsoft windows sdk msbuild)"
msbuild /verbosity:minimal /p:Platform="Any CPU" /p:Configuration="Release" Serpent.sln /t:Rebuild

if not exist build mkdir build
copy Serpent\bin\Release\*.dll build\
