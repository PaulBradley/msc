.DEFAULT_GOAL = binary

tux:
	dotnet publish -c Release -p:PublishTrimmed=true -p:PublishSingleFile=true --self-contained -o ./tux

binary:
	dotnet publish -c Release --runtime linux-arm64 -p:PublishTrimmed=true -p:PublishSingleFile=true --self-contained

test:
# 	dotnet run ../testing/testing01.txt
	cat ../testing/testing01.txt | dotnet run
windows:
	dotnet publish -c Release --runtime win-x64 -p:PublishTrimmed=true -p:PublishSingleFile=true --self-contained -o ./msc.exe