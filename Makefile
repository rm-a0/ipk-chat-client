# Configuration
PROJECT_NAME = ipk25chat-client
CS_PROJ = ipk25chat-client
SRC_DIR = src
OUTPUT_DIR = .
CONFIGURATION = Release
TARGET_RUNTIME = linux-x64

.PHONY: all clean publish run

all: publish

publish:
	@echo "Publishing single-file executable..."
	dotnet publish $(SRC_DIR)/$(CS_PROJ).csproj \
		-c $(CONFIGURATION) \
		-r $(TARGET_RUNTIME) \
		--self-contained true \
		--output $(OUTPUT_DIR) \
		/p:PublishSingleFile=true \
		/p:EnableCompressionInSingleFile=true \
		/p:IncludeNativeLibrariesForSelfExtract=true
	@echo "Executable created at $(OUTPUT_DIR)/$(PROJECT_NAME)"

clean:
	@echo "Cleaning build artifacts..."
	rm -rf $(PROJECT_NAME) $(SRC_DIR)/bin $(SRC_DIR)/obj

run: publish
	@echo "Running application..."
	./$(OUTPUT_DIR)/$(PROJECT_NAME)$(EXE_EXT)