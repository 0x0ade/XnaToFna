#!/bin/bash
set -e
find "$1" -name "*.wma" -exec bash -c 'ffmpeg -i "{}" -q:a 10 "${0/.wma}.ogg"' {} \;
find "$1" -name "*.wmv" -exec bash -c 'ffmpeg -i "{}" -q:v 10 -q:a 10 "${0/.wmv}.ogv"' {} \;
