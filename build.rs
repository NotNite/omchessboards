pub fn main() {
    protobuf_codegen::Codegen::new()
        .protoc()
        .includes(&["src/protos"])
        .input("src/protos/network.proto")
        .cargo_out_dir("protos")
        .run_from_script();
}