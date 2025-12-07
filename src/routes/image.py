from typing import Any

from fastapi import APIRouter
from starlette.websockets import WebSocket, WebSocketDisconnect

from src.core.connections import ConnectionManager
from src.core.exceptions import TypeNotIncluded
from src.core.logic.image import Image
from src.dtos.requests.image import CropImageRequest, ImageRequestType
from src.dtos.requests.request import ImageRequest
from src.dtos.responses.image import CropImageResponse
from src.dtos.responses.response import ErrorResponse

router = APIRouter(prefix="/image", tags=["Image"])
manager = ConnectionManager()


#! NOT TESTED
@router.websocket("/")
async def websocket_endpoint(ws: WebSocket):
    await manager.connect(ws)

    while True:
        request: ImageRequest = None

        try:
            message: dict[str, Any] = await ws.receive_json()
            raw_type = message.get("type")
            if raw_type is None:
                raise TypeNotIncluded(ImageRequestType)
            raw_type = str(raw_type).lower()

            if raw_type == ImageRequestType.Crop.value.lower():
                request = CropImageRequest(
                    file_path=message["file_path"],
                    width=message["width"],
                    height=message["height"],
                    mode=message.get("mode")
                )
                execute_crop(request)
                response = CropImageResponse(request)

            else:
                raise TypeNotIncluded(ImageRequestType)

        except WebSocketDisconnect:
            await manager.disconnect(ws)
            break

        except Exception as e:
            response = ErrorResponse(request, e)

        await ws.send_json(response.model_dump())


def execute_crop(request: CropImageRequest):
    image = Image(request.file_path)
    mode = str(request.mode).lower()

    if mode == CropImageRequest.CropMode.Prominent.value:
        x, y = image.get_prominent_crop(request.width, request.height)
    elif mode == CropImageRequest.CropMode.Center.value:
        x, y = image.get_center_crop(request.width, request.height)
    else:
        raise TypeNotIncluded(CropImageRequest.CropMode)

    image.crop(x, y, request.width, request.height)
    image.save()
