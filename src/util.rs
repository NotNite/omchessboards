use std::net::SocketAddr;
use anyhow::{anyhow, Context};

/// Resolves a host to an IPv6 address.
pub async fn resolve_v6(target: &str) -> anyhow::Result<SocketAddr> {
    tokio::net::lookup_host(target)
        .await
        .context("resolving host")?
        .find(|addr| addr.is_ipv6())
        .ok_or_else(|| anyhow!("host has no V6 address"))
}

/// Freestanding version of [`Default::default`].
pub fn default<T: Default>() -> T {
    Default::default()
}