#!/bin/bash

# Be smart. Be safe.
set -ex

# Be sure we're in the right spot
cd "`dirname "$0"`"

# Clean up previous builds
rm -rf fna
rm -rf fnalibs
rm -f fnalibs.tar.bz2

# For all the managed files...
mkdir fna

# Why does NuGet not have a --directory...
cd ..
nuget restore
cd rhys

# XnaToFna
msbuild ../XnaToFna.sln /p:Configuration=Release
cp ../bin/Release/* fna/

# FNA, FNA.NetStub, ABI files
msbuild ../lib-projs/FNA/abi/Microsoft.Xna.Framework.sln /p:Configuration=Release
cp ../lib-projs/FNA/abi/bin/Release/* fna/

# fnalibs
curl -O http://fna.flibitijibibo.com/archive/fnalibs.tar.bz2
tar xvfj fnalibs.tar.bz2 lib64
mv lib64 fnalibs
