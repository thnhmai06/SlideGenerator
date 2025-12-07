from fastapi import FastAPI

from src.config import Config, APP_NAME, APP_TYPE, APP_DESCRIPTION
from src.routes.image import router as image_router

app = FastAPI(debug=Config().server_debug, title=APP_NAME + "|" + APP_TYPE, description=APP_DESCRIPTION)

app.include_router(image_router)

if __name__ == "__main__":
    import uvicorn

    uvicorn.run(
        "main:app",
        host=Config().server_host,
        port=Config().server_port,
        reload=True
    )
