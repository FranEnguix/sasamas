import time
import asyncio
import socket
import sys

from spade.agent import Agent
from spade.behaviour import FSMBehaviour, State
from spade.message import Message

# --------------------------------------------- #
# BEHAVIOUR                                     #
# --------------------------------------------- #
STATE_MOVING = "STATE_MOVING"
STATE_IDLE = "STATE_IDLE"
    
class BirdBehaviour(FSMBehaviour):
    def send_msg_to_server(self, msg:str) -> str:
        """
        Send a message and waits for a response
        """
        self.sock.sendall(msg)
        rcv = 0
        return self.sock.recv(16)


    async def on_start(self):
        print(f"Agente iniciado en: {self.current_state}")
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.sock.connect(self.agent.socket_info)
        print(f"Agente se ha conectado al servidor {self.agent.socket_info}.")

    async def run(self):
        msg = b"Hola, esto es una prueba\n"
        data = self.send_msg_to_server(msg)
        print(f"{data}")

    async def on_end(self):
        print(f"Agente se ha ido a dormir en el estado: {self.current_state}")
        await self.agent.stop()

class StateIdle(State):
    async def run(self):
        print(f"  Estoy idle")
        msg = Message(to=str(self.agent.jid))
        msg.body = "Estaba idle..."
        await self.send(msg)
        self.set_next_state(STATE_MOVING)

class StateMoving(State):
    async def run(self):
        msg = await self.receive(timeout=5)
        print(f"  Mensaje: {msg.body}")
        time.sleep(1)
        self.set_next_state(STATE_IDLE)
# --------------------------------------------- #
# AGENT                                         #
# --------------------------------------------- #
class DummyAgent(Agent):
    async def setup(self):
        self.socket_info = ('192.168.67.10', 60444)
        my_behav = BirdBehaviour()
        my_behav.add_state(name=STATE_IDLE, state=StateIdle(), initial=True)
        my_behav.add_state(name=STATE_MOVING, state=StateMoving())
        my_behav.add_transition(source=STATE_IDLE, dest=STATE_MOVING)
        my_behav.add_transition(source=STATE_MOVING, dest=STATE_IDLE)
        self.add_behaviour(my_behav)
        print(f"Agente se acaba de desperpertar.")

# --------------------------------------------- #
# MAIN                                          #
# --------------------------------------------- #
if __name__ == "__main__":
    dummy = DummyAgent("fran@localhost", "xmppserver")
    future = dummy.start()
    future.result()
    while dummy.is_alive():
        try:
            time.sleep(1)
        except KeyboardInterrupt:
            break
    dummy.stop()
