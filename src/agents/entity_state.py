import json
import os
from queue import LifoQueue 
from datetime import datetime

import image_manager
from image_data import ImageData
from entity_shell import EntityShell
from spade.behaviour import State

# --------------------------------------------- #
# BEHAVIOUR                                     #
# --------------------------------------------- #
STATE_INIT = "STATE_INITIALIZATION"
STATE_PERCEPTION = "STATE_PERCEPTION"
STATE_COGNITION = "STATE_COGNITION"
STATE_ACTION = "STATE_ACTION"
    
class StateInit(State):
    async def on_start(self):
        behaviour = self.agent.behaviours[0]
        self.__image_socket = behaviour.image_socket
        self.__commander = behaviour.commander

        print(f"{self.agent.name}: Create command sent.")
        self.agent.position = await self.__commander.create_agent(self.agent)
        
        self.agent.image_counter = 0
        self.agent.images = LifoQueue()
        self.agent.image_thread = image_manager.ImageManager(self.agent.name, self.__image_socket, 32768, self.agent.images)
        self.agent.image_thread.daemon = True
        self.agent.image_thread.start()
        print(f"{self.agent.name}: ImageManager started.")

    async def run(self):
        print(f"{self.agent.name}: state {STATE_INIT}.")
        await EntityShell.init(self.agent, self.__commander)
        self.set_next_state(STATE_PERCEPTION)

class StatePerception(State):
    async def on_start(self):
        behaviour = self.agent.behaviours[0]
        self.__commander = behaviour.commander
        
    async def run(self):
        print(f"{self.agent.name}: state {STATE_PERCEPTION}.")
        data = self.save_images()
        await EntityShell.perception(self.agent, self.__commander, data)
        self.set_next_state(STATE_COGNITION)

    def save_images(self):
        data = None
        if (not self.agent.images.empty()):
            data = self.agent.images.get()
            self.agent.image_counter += 1
            if (self.agent.image_buffer_size > 0):
                if (not os.path.isdir(self.agent.image_folder_name)):
                    os.makedirs(self.agent.image_folder_name)
                self.agent.image_counter = self.agent.image_counter % (self.agent.image_buffer_size + 1)
                if self.agent.image_counter == 0:
                    self.agent.image_counter = 1
                image_filename = f"{self.agent.name}_{data.camera_index}_{self.agent.image_counter}.jpg"
                with open(f"{self.agent.image_folder_name}/{image_filename}", "wb") as image_file:
                    image_file.write(data.image)
        return data


class StateCognition(State):
    async def on_start(self):
        behaviour = self.agent.behaviours[0]
        self.__commander = behaviour.commander

    async def run(self):
        print(f"{self.agent.name}: state {STATE_COGNITION}.")
        await EntityShell.cognition(self.agent, self.__commander)
        self.set_next_state(STATE_ACTION)

class StateAction(State):
    async def on_start(self):
        behaviour = self.agent.behaviours[0]
        self.__commander = behaviour.commander

    async def run(self):
        print(f"{self.agent.name}: state {STATE_ACTION}.")
        await EntityShell.action(self.agent, self.__commander)
        self.set_next_state(STATE_PERCEPTION)
