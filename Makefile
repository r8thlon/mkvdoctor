IMAGE_NAME   := mkvdoctor:latest
CONTAINER    := mkvdoctor
PROJECT_DIR  := src/MkvDoctor
HOST_PROJECT := $(shell pwd)

.PHONY: all setup build run clean shell rebuild

all: build

setup:
	docker build -t $(IMAGE_NAME) -f distrobox/Containerfile .
	-DBX_CONTAINER_MANAGER=docker distrobox create --name $(CONTAINER) --image $(IMAGE_NAME)

build:
	distrobox enter --name $(CONTAINER) --no-workdir -- dotnet build $(HOST_PROJECT)/$(PROJECT_DIR)

run:
	distrobox enter --name $(CONTAINER) --no-workdir -- dotnet run --project $(HOST_PROJECT)/$(PROJECT_DIR)

clean:
	distrobox enter --name $(CONTAINER) --no-workdir -- dotnet clean $(HOST_PROJECT)/$(PROJECT_DIR)

shell:
	DBX_CONTAINER_MANAGER=docker distrobox enter $(CONTAINER)

rebuild: clean
	-DBX_CONTAINER_MANAGER=docker distrobox rm --force $(CONTAINER)
	-docker rmi $(IMAGE_NAME)
	$(MAKE) setup
