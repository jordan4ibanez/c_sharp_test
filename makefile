default:
	@dotnet watch --project src/FishGame.csproj

oneshot:
	@dotnet run --project src/FishGame.csproj

gdb:
	@dotnet build src/FishGame.csproj
	@gdb -ex r --args dotnet src/bin/Debug/net10.0/FishGame.dll

clean:
	@dotnet clean src/FishGame.csproj