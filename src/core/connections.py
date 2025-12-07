import asyncio
from typing import Any

from starlette.websockets import WebSocket


class ConnectionManager:
    """Thread-safe & async-safe Singleton WebSocket manager."""

    _instance = None
    _initialized = False

    def __new__(cls):
        if cls._instance is None:
            cls._instance = super().__new__(cls)
        return cls._instance

    def __init__(self):
        if ConnectionManager._initialized:
            return

        self.connections = set()
        self.lock = asyncio.Lock()

        ConnectionManager._initialized = True

    @classmethod
    def instance(cls):
        """Return the singleton instance."""
        return cls()

    async def connect(self, ws: WebSocket):
        """Register a WebSocket connection."""
        async with self.lock:
            await ws.accept()
            self.connections.add(ws)

    async def disconnect(self, ws: WebSocket):
        """Remove a WebSocket connection."""
        async with self.lock:
            self.connections.discard(ws)

    async def broadcast(self, data: Any):
        """Send a JSON message to all active connections."""
        async with self.lock:
            for ws in self.connections:
                try:
                    await ws.send_json(data)
                except Exception:
                    self.connections.discard(ws)
