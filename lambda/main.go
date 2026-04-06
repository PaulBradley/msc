package main

import (
	"context"
	"log"
	"os"
	"os/exec"
	"strings"

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
	var errCode int
	var errMessage string
	var cmdOutput string

	if request.Headers["authorization"] != "BRADLEY.SOFTWARE" {
		return Response(401, "Unauthorized"), nil
	}

	body = strings.TrimSpace(request.Body)

	if len(body) == 0 {
		return Response(400, "Bad Request - Missing body text"), nil
	}

	// create a random file name based on a ULID
	// & write the contents of the body to the file
	fileName := ulid.Make().String()
	err := os.WriteFile("/tmp/"+fileName+".txt", []byte(body), 0644)
	if err != nil {
		log.Fatal(err)
	}

	args := []string{fileName}
	errCode, errMessage, cmdOutput = executeExternalCommand("./msc", args)
	if errCode != 0 {
		log.Println("> ERR " + errMessage)
		log.Println("> ERR " + cmdOutput)
	}

	result, err = os.ReadFile("/tmp/" + fileName + ".json")
	if err != nil {
		log.Println("> ERR " + err.Error())
	}

	os.Remove("/tmp/" + fileName + ".txt")
	os.Remove("/tmp/" + fileName + ".json")

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

func executeExternalCommand(executable string, arguments []string) (errorCode int, errorMessage string, output string) {
	var err error
	var errorMsg string
	var commandContext string
	var cmd *exec.Cmd
	var out []byte

	errorMsg = ""
	commandContext = ":" + executable + " [" + strings.Join(arguments, " ") + "]"

	log.Println("INFO: Calling" + commandContext)

	cmd = exec.Command(executable, arguments...)
	out, err = cmd.CombinedOutput()
	if err != nil {
		if exitError, ok := err.(*exec.ExitError); ok {
			errorMsg = "FATAL" + commandContext
			log.Println(errorMsg)
			log.Println(string(out))
			return exitError.ExitCode(), errorMsg, ""
		}
		errorMsg = "FATAL: could not run [" + executable + "] " + err.Error()
		return -1, errorMsg, ""
	}

	return 0, "", string(out)
}
