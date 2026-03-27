#!/bin/bash
cd .
dotnet restore
dotnet build --no-restore -p:Version="$1" -c $2
