.DEFAULT_GOAL = z

z:
	rm _dist/main.zip
	cp ./lambda/bootstrap ./_dist/
	cp ./msc/bin/Release/net10.0/linux-arm64/publish/* ./_dist/
	cp ./msc/msc.dic ./_dist/
	cp ./msc/msc.aff ./_dist/
	zip -9 --junk-paths _dist/main.zip _dist/*
	ls -lah ./_dist/