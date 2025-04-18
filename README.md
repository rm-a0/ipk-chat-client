# IPK Chat Client
## Overview
Client application for remote server communication using IPK25-CHAT protocol written in C#.

## Table of Contents
- [Summary](#summary)
- [How to Use](#how-to-use)
  - [Argument Options](#argument-options)
  - [Command Options](#command-options)
- [Theory](#theory)
  - [General Theory](#general-theory)
    - [L4 (Transport Layer)](#l4-transport-layer)
    - [TCP](#tcp)
    - [UDP](#udp)
  - [Implementation Theory](#implementation-theory)
    - [TCP Messaging](#tcp-messaging)
    - [UDP Messaging](#udp-messaging)
    - [State Machine](#state-machine)
    - [IPK25-CHAT Protocol](#ipk25-chat-protocol)
- [Implementation Details](#implementation-details)
  - [Code Structure](#code-structure)
  - [Key Classes and Namespaces](#key-classes-and-namespaces)
  - [Simplified Class Diagram](#simplified-class-diagram)
  - [Activity Diagram](#activity-diagram)

## Summary
This application communicates with a remote server using the `IPK25-CHAT` protocol built on top of TCP/UDP transport protocols.
Key features:
- **TCP Support**: Reliable, text-based messaging.
- **UDP Support**: Binary messaging with confirmation and retransmission.
- **IPv4 Only**: Per protocol spec, supports IPv4 addressing.
- **Stateful Operation**: Uses a finite state machine (FSM) for protocol compliance.
- **Flexible CLI**: Configurable via command-line arguments.

## Requirements
- .NET SDK 6.0+ (tested with 8.0)
- IPv4 network access
- Compatible with Linux

## Installation
1. Clone the repository:
   ```shell
   git clone https://github.com/rm-a0/ipk-chat-client
   cd ipk-chat-client
   ```
2. Compile the source code using make or dotnet:
  ```shell
  make
  ```
  ```shell
  cd src
  dotnet build ipk25chat-client.csproj
  ```
3. Run the application directly or using dotnet:
  ```shell
  ./ipk25chat-client [OPTIONS] 
  ```
  ```shell
  dotnet run --project ipk25chat-client.csproj [OPTIONS]
  ```
## How to Use
When exectuing application user is required/allowed to specify different options
### Argument Options
| Argument         | Values                                          | Possible values        | Meaning or expected program behaviour
| ---------------- | ----------------------------------------------- | ---------------------- | -------------------------------------
| `-t` `--protocol`|  **User provided**                              | `tcp` or `udp`         | Transport protocol used for connection
| `-s` `---server` |  **User provided**                              | IP address or hostname | Server IP or hostname
| `-p` `--port`    | `4567`                                          | `uint16`               | Server port
| `-d` `--timeout` | `250`                                           | `uint16`               | UDP confirmation timeout (in milliseconds)
| `-r` `--retries` | `3`                                             | `uint8`                | Maximum number of UDP retransmissions
| `-h` `--help`    |                                                 |                        | Prints program help output and exits
| `-dbg` `--debug` |                                                 |                        | Enables debugging and displays logs during execution

### Command Options
After execution user can interact with CLI and input different commands
| Command     | Parameters                              | Client behaviour
| ----------- | --------------------------------------- | ----------------
| `/auth`     | `{Username}`&nbsp;`{Secret}`&nbsp;`{DisplayName}` | Sends `AUTH` message with the data provided from the command to the server (and correctly handles the `Reply` message), locally sets the `DisplayName` value (same as the `/rename` command)
| `/join`     | `{ChannelID}`                           | Sends `JOIN` message with channel name from the command to the server (and correctly handles the `Reply` message)
| `/rename`   | `{DisplayName}`                         | Locally changes the display name of the user to be sent with new messages/selected commands
| `/help`     |                                         | Prints out supported local commands with their parameters and a description
## Theory
### General Theory
#### L4 (Transport Layer)
- Manages end-to-end communication between applications.
- Uses ports (0-65535) to identify services.
- Protocols: **TCP** (connection-oriented, reliable) and **UDP** (connectionless, fast).

#### TCP
- **Connection-Based**: Establishes a session via handshake.
- **Reliable**: Ensures delivery with retransmission and ordering.
- **Used Here**: Sends text-based commands (e.g., `AUTH`, `MSG`) terminated with `\r\n`.

#### UDP
- **Connectionless**: No session setup, just datagrams.
- **Unreliable**: No built-in delivery guarantees.
- **Used Here**: Sends binary messages with application-level confirmation.

### Implementation Theory
#### TCP Messaging
- **Mechanism**:
  - Utilizes .NET’s `TcpClient` to establish a persistent, stateful connection to the server.
  - Sends text-based messages compliant with the protocol’s ABNF grammar (e.g., `AUTH user AS User USING secret\r\n`).
- **Reliability**: Relies on TCP’s built-in guarantees for ordered, error-free delivery ([RFC 9293](https://datatracker.ietf.org/doc/html/rfc9293)).
- **Termination**: Ensures graceful closure without the `RST` flag on receiving `BYE` or user interrupt (e.g., Ctrl+C), per spec requirements.

#### UDP Messaging
- **Mechanism**:
  - Employs `UdpClient` to send binary messages with a 3-byte header (`Type` as `uint8`, `MessageID` as `uint16`) followed by zero-terminated strings.
  - Implements application-level confirmation with a 250ms timeout and up to 3 retries for unconfirmed messages.
  - Adapts to dynamic server port changes after the initial `AUTH` message, inspired by TFTP behavior ([RFC 1350](https://datatracker.ietf.org/doc/html/rfc1350)).
- **Reliability**:
  | Issue         | Solution                     | Reference                          |
  |---------------|------------------------------|------------------------------------|
  | Packet Loss   | Retransmits after timeout    | Spec: UDP Variant, Solving Transport Issues |
  | Duplication   | Tracks `MessageID` to ignore duplicates | Spec: Message Header              |
  | Reordering    | Processes all messages, confirms once | Spec: Solving Transport Issues    |

#### State Machine
- **Mealy FSM**: Drives client behavior with transitions based on server inputs (e.g., `REPLY`) and client outputs (e.g., `AUTH`).
- **States**: `start`, `auth`, `join`, `open`, `end`.
- **Key Behaviors**:
  - `AUTH` initiates authentication, expecting a `REPLY` to proceed.
  - `JOIN` is optional after automatic default channel assignment post-authentication.
  - `ERR` or `BYE` triggers transition to `end`, followed by graceful connection termination.

#### IPK25-CHAT Protocol
This protocol builds on the TCP and UDP variants described above, defining a chat application framework with message types like `AUTH`, `MSG`, and `BYE`. It supports IPv4-only communication on a default port of 4567. For comprehensive details, refer to the original [project specification document](https://git.fit.vutbr.cz/NESFIT/IPK-Projects/src/branch/master/Project_2/README.md) provided by the IPK course at FIT VUT.

## Implementation Details
### Code Structure
```
src/
├── Core/
│   ├── ArgumentParser.cs     # CLI argument parsing
│   ├── ChatApplication.cs    # Main logic
│   ├── ChatStateMachine.cs   # FSM implementation
├── Debug/
│   ├── Debugger.cs           # Logging utilities
├── IO/
│   ├── Command.cs            # Command structure
│   ├── InputParser.cs        # User input parsing into command objects
│   ├── OutputParser.cs       # Server output parsing into repsponse objects
│   ├── Response.cs           # Response structure
├── Network/
│   ├── ChatClient.cs         # Client intraface
│   ├── ClientFactory.cs      # Client instantiation
│   ├── TcpChatClient.cs      # TCP client
│   ├── UdpChatClient.cs      # UDP client
├── GlobalUsing.cs            # Global usings (Common namespaces used globally)
├── Program.cs                # Entry point
├── ipk25chat-client.csproj   # Project file
```
### Key Classes and Namespaces

### Simplified Class Diagram
The class diagram shows the main system components and how they interact between each other.
![class-diagram](doc/class-diagram.jpg)

### Activity Diagram
The activity diagram illustrates parallel processes and concurrent tasks within the system.
![activity-diagram](doc/activity-diagram.jpg)