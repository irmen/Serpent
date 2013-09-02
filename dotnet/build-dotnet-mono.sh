#!/bin/sh
echo "Compiling .net source"
if [ -d Serpent/bin ]; then
  rm -r Serpent/bin
fi
if [ -d Serpent.Test/bin ]; then
  rm -r Serpent.Test/bin
fi
xbuild /verbosity:minimal /property:Configuration=Release /property:Platform="Any CPU" Serpent.sln

mkdir -p build
cp Serpent/bin/Release/*.dll build
