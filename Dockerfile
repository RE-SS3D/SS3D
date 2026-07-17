# Runs a pre-built SS3D dedicated server (StandaloneLinux64, Server subtarget).
# The binary is not compiled inside this image - build it first (Unity Editor or
# the "Build Dedicated Server" CI job, see .github/workflows/main.yml) and pass
# its output directory as the build context, e.g.:
#   docker build -t ss3d-server -f Dockerfile build/StandaloneLinux64
FROM debian:bookworm-slim

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates libc6 libstdc++6 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /server
COPY . /server/
RUN chmod +x /server/*.x86_64

EXPOSE 2222/udp

ENTRYPOINT ["/bin/sh", "-c", "exec /server/*.x86_64 -batchmode -nographics -serveronly -port=2222 -logFile /dev/stdout"]
