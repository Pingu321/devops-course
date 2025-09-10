# Build stage
FROM rust:1.89-bookworm AS builder
COPY . .
RUN cargo build --release

# Runtime
FROM debian:bookworm-slim
COPY --from=builder /target/release/service2 /service2
EXPOSE 8198
CMD ["/service2"]