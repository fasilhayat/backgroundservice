# Name of the docker image
IMAGE_NAME=backgroundservice

.PHONY: build up down clean help

# Build the Docker image
build:
	docker-compose build

# Start the container in detached mode
up:
	docker-compose up -d

# Stop the container
down:
	docker-compose down

# Clean everything (images, volumes, containers)
clean:
	docker-compose down --rmi all --volumes --remove-orphans

# Show help
help:
	@echo ""
	@echo "Makefile commands:"
	@echo "  build     Build the Docker image"
	@echo "  up        Start the container in detached mode"
	@echo "  down      Stop the container"
	@echo "  clean     Stop container and remove images, volumes, orphans"
	@echo "  help      Show this help message"
	@echo ""
