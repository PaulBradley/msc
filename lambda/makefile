.DEFAULT_GOAL = binary

binary:
	rm bootstrap
	GOARCH=arm64 GOOS=linux go build -tags="lambda.norpc" -ldflags="-w -s" -o bootstrap main.go
