default:
	@dotnet watch --project src/FishGame.csproj

oneshot:
	@dotnet run --project src/FishGame.csproj

clean:
	@dotnet clean src/FishGame.csproj