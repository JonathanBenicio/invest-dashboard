# Generate version code based on timestamp
VERSION_CODE=$(date +%s)
VERSION_NAME="1.0.$VERSION_CODE"

echo "Building APK version $VERSION_NAME ($VERSION_CODE)..."

# Build the Docker image
docker build -t android-builder -f Dockerfile.android \
    --build-arg VERSION_CODE=$VERSION_CODE \
    --build-arg VERSION_NAME=$VERSION_NAME .

# Create a container creating the APK
id=$(docker create android-builder)

# Copy the APK from the container to the host
mkdir -p release
docker cp $id:/app/android/app/build/outputs/apk/debug/app-debug.apk ./release/app-debug.apk

# Cleanup
docker rm -v $id

echo "APK saved to release/app-debug.apk"
