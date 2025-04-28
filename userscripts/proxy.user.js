// ==UserScript==
// @name        onemillionchessboards proxy
// @namespace   Violentmonkey Scripts
// @match       https://onemillionchessboards.com/*
// @grant       none
// @version     1.0
// @author      NotNite
// @description 4/28/2025, 5:03:53 PM
// ==/UserScript==

class CustomWebSocket extends WebSocket {
  constructor(oldUrl) {
    const url = new URL(oldUrl);
    url.protocol = "http:";
    url.host = "localhost:8000";
    url.pathname = "/";

    console.log("proxying", oldUrl, url);

    super(url.toString());
  }
}

CustomWebSocket.prototype = WebSocket.prototype;
CustomWebSocket.prototype.constructor = CustomWebSocket;

// deno-lint-ignore no-global-assign
WebSocket = CustomWebSocket;
