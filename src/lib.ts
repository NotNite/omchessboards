import { decompress, init } from "@bokuweb/zstd-wasm";
import { MessageFns } from "./proto/network.ts";

await init();

const ZSTD_MAGIC = [
  40,
  181,
  47,
  253
];

// deno-lint-ignore no-explicit-any
export async function decode<T>(event: MessageEvent<any>, fns: MessageFns<T>) {
  // these types suck ass lol
  let data = event.data instanceof Blob
    ? (await event.data.bytes())
    : new Uint8Array(event.data);

  const isZstd = ZSTD_MAGIC.every((value, i) => data.at(i) === value);
  if (isZstd) data = decompress(data);

  return fns.decode(data);
}

export function encode<T>(data: T, fns: MessageFns<T>) {
  // TODO: compress maybe
  return fns.encode(data).finish();
}
