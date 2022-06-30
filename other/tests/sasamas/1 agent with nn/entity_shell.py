import time
import os

from commander import Commander, Axis, ImageMode
from spade.agent import Agent

from image_data import ImageData
from orange_network import OrangeNetwork

class EntityShell:

    async def init(agent: Agent, commander: Commander):
        agent.total_moves = 16 * 5 + 4
        agent.distancex = 6
        agent.distancey = 3
        agent.moves = 0
        agent.roads = 0
        agent.orange_net = OrangeNetwork(\
            r"C:\Users\Fran\Documents\Py\Citrus\Citrus_ft_optimized.tflite", 
            agent.image_folder_name, 
            r"C:/Users/Fran/Documents/Py/Agents/Agents/out/" + agent.name
        )
        if agent.name == "agente1":
            await commander.change_color(0.5, 0.5, 0, 0.8)
        else:
            await commander.change_color(0.5, 0, 0, 0.8)
        await commander.move_camera(0, Axis.Y, -1)
        await commander.fov_camera(0, 7)
        time.sleep(4)

    async def perception(agent: Agent, commander: Commander, data: ImageData):
        agent.image_data = data

    async def cognition(agent: Agent, commander: Commander):
        if not agent.orange_net.is_alive():
            if len(os.listdir(agent.orange_net.output_path)) <= agent.total_moves:
                agent.orange_net.start()

    async def action(agent: Agent, commander: Commander):
        if agent.moves < agent.total_moves:
            agent.moves += 1
            if agent.moves % 17 != 0:
                await EntityShell.move_forward(agent, commander, agent.roads)
                # time.sleep(3)
                await EntityShell.take_picture(commander)
            else:
                agent.roads += 1
                camera_sign = -1 if agent.roads % 2 == 0 else 1
                await commander.move_camera(0, Axis.Z, 0.56 * camera_sign)
                await EntityShell.move_next_road(agent, commander, agent.roads)

    async def move_forward(agent: Agent, commander: Commander, road_number: int = 0):
        position = agent.position.copy()
        amount = agent.distancey
        if road_number % 2 == 1:
            amount = -amount
        position[2] += amount
        new_position = f"({position[0]} {position[1]} {position[2]})"
        agent.position = await commander.move_agent(agent, new_position)

    async def move_next_road(agent: Agent, commander: Commander, road_number: int = 0):
        space = agent.distancey * 3
        position = agent.position.copy()
        if road_number % 2 == 1:
            position[2] += space
        else:
            position[2] -= space
        position[0] += agent.distancex
        position_str = f"({position[0]} {position[1]} {position[2]})"
        agent.position = await commander.move_agent(agent, position_str)
        position = agent.position.copy()
        position[0] += agent.distancex
        if road_number % 2 == 1:
            position[2] -= space - agent.distancey
        else:
            position[2] += space - agent.distancey
        position_str = f"({position[0]} {position[1]} {position[2]})"
        agent.position = await commander.move_agent(agent, position_str)

    async def take_picture(commander: Commander):
        await commander.rotate_camera(0, Axis.Y, 90)
        time.sleep(1)
        await commander.take_image(0, ImageMode.INSTANT)

