#!/bin/sh
echo "Building..."
. ./build-dotnet-mono.sh

echo "Running tests"
nunit-console -noshadow Serpent.Test/bin/Release/Razorvine.Serpent.Test.dll
