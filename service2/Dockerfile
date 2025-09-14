# Build stage
FROM rust:1.89-bookworm AS builder
WORKDIR /app

# clang and mold for faster compilation
RUN apt-get update \
 && apt-get install -y --no-install-recommends clang mold \
 && rm -rf /var/lib/apt/lists/*

ENV RUSTFLAGS="-C linker=clang -C link-arg=-fuse-ld=mold"

COPY . .
RUN cargo build --release

# Runtime
FROM debian:bookworm-slim
COPY --from=builder app/target/release/service2 /service2
EXPOSE 8198
CMD ["/service2"]