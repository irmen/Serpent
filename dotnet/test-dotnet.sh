#!/bin/sh
echo "Running tests"
nunit-console -noshadow -nothread Serpent.Test/bin/Debug/Razorvine.Serpent.Test.exe
