.DEFAULT_GOAL = binary

binary:
	dotnet publish --runtime linux-arm64 -p:PublishTrimmed=true -p:PublishSingleFile=true --self-contained

test:
	dotnet run ../testing/testing01.txt