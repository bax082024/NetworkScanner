# Network Scanner

A simple console-based network scanner built in C# that allows users to scan a range of IP addresses for active hosts, 
perform port scans, and save the results to a file.

---

## Features

- IP Range Scanning: Scan any given IP range for active hosts.

- Port Scanning: Scan open ports within a specified range on detected hosts.

- Dynamic Timeout Calculation: Estimates optimal timeout based on network response times.

- Parallel Processing: Supports parallel scanning for faster results.

- Session Data Management: Stores scan results for the current session.

- Report Generation: Saves scan results to a text file with a timestamp.

---

## Installation

1. Clone Repository :
	- https://github.com/bax082024/NetworkScanner.git
2. cd NetworkScanner
3. dotnet build
4. dotnet run
5. follow instructions.

---

## How To Use

- Step 1: Enter the number of parallel tasks (recommended: double the number of CPU cores).

- Step 2: Provide an IP range to scan (e.g., 192.168.1.1-192.168.1.255).

- Step 3: Enter the number of samples for estimating network speed.

- Step 4: After scanning, choose from the menu to perform port scans, save results, or exit.

---

## Output

- Console Output: Displays active hosts, open ports, and progress.

- Saved Report: Generates a .txt file with scan results in the project directory.

---

