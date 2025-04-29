use std::net::SocketAddr;
use tokio::net::TcpStream;
use async_socks5::Auth;
use rand::Rng;
use anyhow::Context;
use tracing::{info};

#[derive(Debug, Copy, Clone)]
pub struct Proxy {
    address: SocketAddr,
    password: &'static str,
    suffix_len: usize,
}

impl Proxy {
    pub fn new(address: SocketAddr, password: &'static str, suffix_len: usize) -> Self {
        Proxy {
            address,
            password,
            suffix_len,
        }
    }

    fn generate_random_suffix(&self) -> u128 {
        let mask = (1u128 << self.suffix_len) - 1;
        (rand::rng().random::<u128>()) & mask
    }

    #[tracing::instrument]
    pub async fn connect(&self, target: SocketAddr) -> anyhow::Result<TcpStream> {
        let mut stream = TcpStream::connect(self.address)
            .await
            .context("connecting to proxy")?;

        let suffix = self.generate_random_suffix();

        let auth = Auth {
            username: "jioyasd2rw-corp-country-US-hold-query".to_string(), // format!("{:x}", suffix),
            password: self.password.to_string(),
        };

        info!("generated socks suffix {}", &auth.username);

        async_socks5::connect(&mut stream, target, Some(auth))
            .await
            .context("setting up socks5")?;

        Ok(stream)
    }
}