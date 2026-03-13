# ── Moogle Makefile ───────────────────────────────────────────────────────────
# Usage:
#   make dev      → Run the app in development mode with hot reload
#   make build    → Build the solution in Release mode
#   make clean    → Remove all build artifacts
#   make restore  → Restore NuGet packages
#   make publish  → Publish a self-contained binary to ./publish/

dev:
	dotnet watch run --project MoogleServer

build:
	dotnet build Moogle.sln --configuration Release

clean:
	dotnet clean Moogle.sln
	rm -rf MoogleServer/bin MoogleServer/obj
	rm -rf MoogleEngine/bin MoogleEngine/obj

restore:
	dotnet restore Moogle.sln

publish:
	dotnet publish MoogleServer --configuration Release --output ./publish --self-contained false

.PHONY: dev build clean restore publish
