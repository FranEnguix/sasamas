import socket
import threading

from image_data import ImageData
from queue import LifoQueue 

class ImageManager(threading.Thread):
    def __init__(self, name: str, image_socket: socket.socket, socket_recv_size:int, queue: LifoQueue):
        threading.Thread.__init__(self)
        self.__name = name
        self.__image_socket = image_socket
        self.__socket_recv_size = socket_recv_size
        self.__queue = queue

    def recv_image_from_server(self, image_socket: socket.socket) -> ImageData:
        raw_data = image_socket.recv(self.__socket_recv_size)
        return ImageData(raw_data.decode('UTF-8'))

    def send_msg_to_server(self, msg:str):
        """
        Send a message
        """
        encoded_msg = (msg).encode()
        self.__image_socket.sendall(bytearray(encoded_msg))

    def run(self):
        self.send_msg_to_server(self.__name)
        print(f"{self.__name}: ImageManager name sent.")
        while True:
            data = self.recv_image_from_server(self.__image_socket)
            self.__queue.put(data)
