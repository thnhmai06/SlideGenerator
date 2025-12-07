from fastapi import APIRouter
from starlette.websockets import WebSocket

router = APIRouter(prefix="/image", tags=["Image"])


@router.get("/")
def route(ws: WebSocket):
    pass