package main

import (
	"bufio"
	"encoding/base64"
	"fmt"
	"net"
	"strings"
	"time"
)

const ntripServer = "address"
const mountPoint = "mnt"
const username = "u"
const password = "p"

func main() {
	// Connect to the NTRIP server
	conn, err := net.Dial("tcp", ntripServer)
	if err != nil {
		fmt.Println("Error connecting to NTRIP server:", err)
		return
	}
	defer conn.Close()

	// Send NTRIP request
	request := fmt.Sprintf("GET /%s HTTP/1.1\r\n"+
		"User-Agent: NTRIP A2A Client/2.0\r\n"+
		"Connection: close\r\n"+
		"Authorization: Basic %s\r\n"+
		"Ntrip-Version: Ntrip/2.0\r\n"+
		"\r\n", mountPoint, base64Credentials(username, password))

	fmt.Println("Sending NTRIP request:")
	fmt.Println(request)

	_, err = conn.Write([]byte(request))
	if err != nil {
		fmt.Println("Error sending NTRIP request:", err)
		return
	}

	// Read and process NTRIP response
	reader := bufio.NewReader(conn)
	headers, err := readHeaders(reader)
	if err != nil {
		fmt.Println("Error reading NTRIP response headers:", err)
		return
	}

	fmt.Println("NTRIP Response Headers:")
	fmt.Println(headers)

	// Check if the response indicates success
	if !strings.Contains(headers, "HTTP/1.1 200 OK") {
		fmt.Println("NTRIP server did not respond with 'ICY 200 OK'.")
		return
	}

	// Read and process NTRIP data (persistent connection)
	for {
		data := make([]byte, 1024)
		_, err := conn.Read(data)
		if err != nil {
			fmt.Println("Error reading NTRIP data:", err)
			return
		}

		// Process the received NTRIP data here
		// ...
		fmt.Println(string(data))

		time.Sleep(time.Second) // Simulate processing time
	}
}

func base64Credentials(username, password string) string {
	auth := username + ":" + password
	return base64.StdEncoding.EncodeToString([]byte(auth))
}

func readHeaders(reader *bufio.Reader) (string, error) {
	var headers strings.Builder

	for {
		line, err := reader.ReadString('\n')
		if err != nil {
			return "", err
		}

		headers.WriteString(line)

		// Check for the end of the response headers
		if line == "\r\n" {
			break
		}
	}

	return headers.String(), nil
}
