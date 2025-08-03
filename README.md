# WebAppsScraper

**WebAppsScraper** is a network reconnaissance tool designed for internal infrastructure penetration testing. Its primary objective is to identify accessible web applications across a network, thereby helping expand the attack surface available for further analysis.

The tool performs HTTP and HTTPS requests against a set of IP addresses (either specified directly or resolved via LDAP) and saves the response content for offline review. It is particularly useful during the reconnaissance phase of a security assessment.

---

## Features

- Supports single IPs, IP ranges, and CIDR subnets.
- LDAP enumeration of domain-joined machines using domain controller and domain name.
- Concurrent scanning with configurable thread count.
- Adjustable HTTP timeout values.
- Ability to specify custom ports for scanning.
- Saves discovered HTML content locally for each responsive host.

---

## Requirements

- **.NET Framework 4.7.2** or later
- Windows OS (tested on Windows 10/11 and Windows Server)

---

## Usage

```bash
WebAppsScraper.exe [Target or Options]
```

The tool supports two modes:
1. **IP-based Scanning**: Directly scan a provided IP address, range, or subnet.
2. **LDAP-based Discovery**: Resolve domain-joined machines using LDAP and scan them.

---

## Examples

### 1. LDAP-Based Scan

```bash
WebAppsScraper.exe --domain=lab.domain.net --dc=10.10.15.1 --threads=20 --timeout=500
```

**Parameters:**
- `--domain`: FQDN of the Active Directory domain (e.g., `lab.domain.net`)
- `--dc`: IP address of the Domain Controller used for LDAP resolution
- `--threads`: Number of parallel worker threads
- `--timeout`: Timeout per HTTP request in milliseconds

This example performs an LDAP query to enumerate domain computers and scans each host for web applications.

---

### 2. CIDR Scan with Custom Ports

```bash
WebAppsScraper.exe 10.10.1.0/24 --ports=8082,9001 --threads=20 --timeout=500
```

**Parameters:**
- `10.10.1.0/24`: CIDR subnet to be scanned
- `--ports`: Comma-separated list of ports to scan (e.g., 8082,9001)
- `--threads`: Number of concurrent scanning threads
- `--timeout`: HTTP request timeout in milliseconds

This command scans all IPs in the `10.10.1.0/24` subnet for running web servers on ports 8082 and 9001.

---

## Output

- Discovered web pages are saved under a directory named after the IP address of the host.
- The file name format is: `index-[port].html` (e.g., `index-8080.html`), based on the port that responded.

Example output structure:

```
10.10.1.5/
├── index-8082.html
└── index-9001.html
```

---

## Notes

- Default scanned ports are: **80, 443, 8000, 8080, 8081**
- Both **HTTP** and **HTTPS** protocols are attempted for each target and port combination.
- The tool ignores certificate validation for HTTPS.

---

## Legal Disclaimer

This tool is intended for **authorized penetration testing** and **security research** only. Ensure you have proper authorization before scanning any systems. Unauthorized use of this tool may be illegal and unethical.