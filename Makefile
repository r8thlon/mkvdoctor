IMAGE_NAME   := mkvdoctor:latest
CONTAINER    := mkvdoctor
PROJECT_DIR  := src/MkvDoctor
HOST_PROJECT := $(shell pwd)

.PHONY: all setup build run clean shell rebuild

all: build

setup:
	podman build -t $(IMAGE_NAME) -f distrobox/Containerfile
	-distrobox create --name $(CONTAINER) --image $(IMAGE_NAME)

ensure-running:
	@podman start $(CONTAINER) 2>/dev/null || true
	@sleep 1

build: ensure-running
	podman exec -w $(HOST_PROJECT) $(CONTAINER) dotnet build $(PROJECT_DIR)

run: ensure-running
	podman exec -e DISPLAY=$(DISPLAY) -e WAYLAND_DISPLAY=$(WAYLAND_DISPLAY) -e XAUTHORITY=$(XAUTHORITY) -w $(HOST_PROJECT) $(CONTAINER) dotnet run --project $(PROJECT_DIR)

clean: ensure-running
	podman exec -w $(HOST_PROJECT) $(CONTAINER) dotnet clean $(PROJECT_DIR)

shell:
	distrobox enter $(CONTAINER)

rebuild: clean
	-distrobox rm --force $(CONTAINER)
	-podman rmi $(IMAGE_NAME)
	$(MAKE) setup
