# BondPollingService

A .NET 9 BackgroundService that polls the Tiwaz API for bond information every 5 minutes and writes a health status file. Designed to run in Docker and Kubernetes environments.

---

## Table of Contents

- [Prerequisites](#prerequisites)
- [Building the Docker Container](#building-the-docker-container)
- [Running the Container](#running-the-container)
- [Checking Health Status](#checking-health-status)
- [Kubernetes Health Monitoring](#kubernetes-health-monitoring)
- [File Locations](#file-locations)

---

## Prerequisites

- Docker Desktop or a Linux Docker environment
- .NET 9 SDK (for local builds, optional if using Docker)
- Root CA certificate for Tiwaz API (`certs/rootca.cer`)

---

## Building the Docker Container

Build the container using `docker-compose`:

```bash
docker-compose build
```

Or using the provided Makefile:

```bash
make build
```

This will:

- Restore and build the .NET project
- Publish the output
- Copy the root CA certificate into the container trust store
- Prepare the container for runtime execution

---

## Running the Container

Start the container in detached mode:

```bash
docker-compose up -d
```

Or using Makefile:

```bash
make up
```

Verify the container is running:

```bash
docker ps
```

---

## Checking Health Status

The service writes its health information to a JSON file (`healthstatus.json`) every 5 minutes.

### Inside the container:

```bash
docker exec -it bondservice sh
cat /tmp/healthstatus.json
```

### From the host (if `/tmp` is mapped to `./temp`):

```bash
cat ./temp/healthstatus.json
```

**Example output:**

```json
{
  "LastRun": "2025-10-25T12:30:00Z",
  "LastSuccess": "2025-10-25T12:30:05Z",
  "LastError": "",
  "IsHealthy": true
}
```

- `LastRun` – last time the service executed a poll  
- `LastSuccess` – last successful API call  
- `LastError` – any error message from the last run  
- `IsHealthy` – computed health status

---

## Kubernetes Health Monitoring

Since there is no HTTP endpoint, Kubernetes can monitor the container by checking the health file:

1. Mount the same `/tmp` path as a volume in the pod.
2. Configure a `readinessProbe` and/or `livenessProbe` to check the `healthstatus.json` file or a simple `healthy` file:

```yaml
livenessProbe:
  exec:
    command:
      - cat
      - /tmp/healthy
  initialDelaySeconds: 30
  periodSeconds: 60

readinessProbe:
  exec:
    command:
      - cat
      - /tmp/healthy
  initialDelaySeconds: 10
  periodSeconds: 30
```

- The container writes a plain-text `/tmp/healthy` file with content `healthy` on each successful poll.  
- Kubernetes uses this to restart the pod if the service becomes unhealthy or stops updating.

---

## File Locations

| File                     | Location in container | Notes                                       |
|---------------------------|--------------------|--------------------------------------------|
| Health JSON               | `/tmp/healthstatus.json` | Contains detailed last run, last success, last error, and IsHealthy |
| Root CA certificate       | `/usr/local/share/ca-certificates/rootca.crt` | Mounted from host `certs/rootca.cer` and trusted inside container |
| Health plain-text (`healthy`) | `/tmp/healthy`      | Optional for Docker/K8s health probes     |

---

## Makefile Commands

```bash
make build     # Build Docker image
make up        # Start container
make down      # Stop container
make clean     # Stop container and remove images/volumes
make help      # Show help message
```

---

## Notes

- The container resolves `tiwaz.hayatnet.local` via `extra_hosts` in Docker Compose.
- The root CA must be mounted to validate the HTTPS connection.
- Health file is overwritten every 5 minutes; it can safely be read from the host without locking issues.