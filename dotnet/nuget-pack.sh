#!/bin/sh
echo "Building and testing..."
. ./test-dotnet.sh

echo "Nuget packaging..."
nuget pack Serpent/Serpent.nuspec
