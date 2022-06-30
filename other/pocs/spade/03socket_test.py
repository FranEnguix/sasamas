import time
import asyncio
import socket
import sys

from spade.agent import Agent
from spade.behaviour import CyclicBehaviour

class DummyAgent(Agent):

    async def setup(self):
        self.my_behav = self.MyBehav()
        self.add_behaviour(self.my_behav)
        print(f"Agente se acaba de desperpertar.")


    class MyBehav(CyclicBehaviour):
        def send_msg_to_server(self, msg:str) -> str:
            """
            Send a message and waits for a response
            """
            self.sock.sendall(msg)
            rcv = 0
            return self.sock.recv(16)


        async def on_start(self):
            server_address = ('192.168.67.10', 60444)
            self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self.sock.connect(server_address)
            print(f"Agente se ha conectado al servidor.")

        async def run(self):
            msg = b"Hola, esto es una prueba\n"
            data = self.send_msg_to_server(msg)
            print(f"{data}")

        async def on_end(self):
            print(f"Agente se ha ido a dormir.")

if __name__ == "__main__":
    dummy = DummyAgent("fran@localhost", "xmppserver")
    future = dummy.start()
    future.result()
    while not dummy.my_behav.is_killed():
        try:
            time.sleep(1)
        except KeyboardInterrupt:
            break
    dummy.stop()
