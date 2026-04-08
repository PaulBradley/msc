package main

import (
	"context"
	"errors"
	"io"
	"log"
	"os/exec"
	"strings"
	"time"

	"github.com/aws/aws-lambda-go/events"
	"github.com/aws/aws-lambda-go/lambda"
	"github.com/oklog/ulid/v2"
)

func main() {
	lambda.Start(handleRequest)
}

func handleRequest(ctx context.Context, request events.LambdaFunctionURLRequest) (events.LambdaFunctionURLResponse, error) {
	var result []byte
	var body string
	var err error
	var errCode int

	if request.Headers["authorization"] != "BRADLEY.SOFTWARE" {
		return Response(401, "Unauthorized"), nil
	}

	body = strings.TrimSpace(request.Body)

	if len(body) == 0 {
		return Response(400, "Bad Request - Missing body text"), nil
	}

	fileName := ulid.Make().String()
	args := []string{fileName}

	result, errCode, err = executeExternalCommand(ctx, "./msc", args, body, 10*time.Second)
	if err != nil {
		log.Println("ERROR:", err.Error())

		if errCode != 0 {
			return Response(503, "503 Service Unavailable"), nil
		}
		return Response(500, "Internal Server Error"), nil
	}

	return Response(200, string(result)), nil
}

func Response(StatusCode int, Body string) events.LambdaFunctionURLResponse {
	return events.LambdaFunctionURLResponse{
		IsBase64Encoded: false,
		Body:            Body,
		Headers: map[string]string{
			"Content-Type": "text/plain",
		},
		StatusCode: StatusCode,
	}
}

func executeExternalCommand(
	parent context.Context,
	executable string,
	args []string,
	input string,
	timeout time.Duration) ([]byte, int, error) {

	ctx := parent
	if timeout > 0 {
		var cancel context.CancelFunc
		ctx, cancel = context.WithTimeout(parent, timeout)
		defer cancel()
	}

	cmd := exec.CommandContext(ctx, executable, args...)
	stdin, err := cmd.StdinPipe()
	if err != nil {
		return nil, -1, err
	}
	stdoutPipe, err := cmd.StdoutPipe()
	if err != nil {
		return nil, -1, err
	}
	cmd.Stderr = cmd.Stdout

	if err := cmd.Start(); err != nil {
		return nil, -1, err
	}

	writeErrCh := make(chan error, 1)
	go func() {
		_, werr := io.WriteString(stdin, input)
		_ = stdin.Close()
		writeErrCh <- werr
	}()

	out, readErr := io.ReadAll(stdoutPipe)
	if werr := <-writeErrCh; werr != nil {
		_ = cmd.Wait()
		return nil, -1, werr
	}
	if readErr != nil {
		_ = cmd.Wait()
		return nil, -1, readErr
	}
	if err := cmd.Wait(); err != nil {
		var exitErr *exec.ExitError
		if errors.As(err, &exitErr) {
			return nil, exitErr.ExitCode(), nil
		}
		return nil, -1, err
	}

	return out, 0, nil
}
