import json

from base64 import b64decode
from datetime import datetime
from dateutil import parser

class ImageData:

    def __init__(self, json_string: str):
        raw_data = json.loads(json_string)
        self.image: bytes = b64decode(raw_data['imageBase64'])
        self.camera_index: int = raw_data['cameraIndex']
        self.datetime_utc: datetime = parser.isoparse(raw_data['dateTimeUTC'])



