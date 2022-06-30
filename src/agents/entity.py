import asyncio
import json
import socket
import sys
import time

from entity_behaviour import AgentBehaviour
from entity_state import STATE_INIT, STATE_PERCEPTION, STATE_COGNITION, STATE_ACTION
from entity_state import StateInit, StatePerception, StateCognition, StateAction

from spade.agent import Agent

class EntityAgent(Agent):
    def __init__(self, name_at: str, password: str, command_socket_info: tuple, image_socket_info: tuple, image_buffer_size: int, image_folder_name: str, enable_agent_collision: bool, prefab_name: str, starter_position: dict):
        Agent.__init__(self, name_at, password)
        self.command_socket_info = command_socket_info
        self.image_socket_info = image_socket_info
        self.image_buffer_size = image_buffer_size
        self.image_folder_name = image_folder_name
        self.agent_collision = enable_agent_collision
        self.prefab_name = prefab_name
        self.starter_position = starter_position

    async def setup(self):
        behaviour = AgentBehaviour()

        # STATES
        behaviour.add_state(name=STATE_INIT, state=StateInit(), initial=True)
        behaviour.add_state(name=STATE_PERCEPTION, state=StatePerception())
        behaviour.add_state(name=STATE_COGNITION, state=StateCognition())
        behaviour.add_state(name=STATE_ACTION, state=StateAction())

        # TRANSITIONS
        behaviour.add_transition(source=STATE_INIT, dest=STATE_PERCEPTION)
        behaviour.add_transition(source=STATE_PERCEPTION, dest=STATE_COGNITION)
        behaviour.add_transition(source=STATE_COGNITION, dest=STATE_ACTION)
        behaviour.add_transition(source=STATE_ACTION, dest=STATE_PERCEPTION)

        self.add_behaviour(behaviour)
        print(f"{self.name}: behaviour is ready.")
