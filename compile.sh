#!/bin/bash
cd .
dotnet restore
dotnet publish --no-restore -r $2-$3 -p:Version="$1"
